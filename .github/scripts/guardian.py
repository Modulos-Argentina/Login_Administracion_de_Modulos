#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
==============================================================================
Tutor IA Guardián de Integración Continua (.NET + Gemini)
==============================================================================
Propósito:
  1. Analiza el log de compilación de .NET ('dotnet build').
  2. Filtra estrictamente las líneas de error (ej: error CS, error MSB).
  3. Trunca el texto a menos de 2,500 caracteres (~40 líneas) para ahorrar tokens.
  4. Invoca a Gemini (2.0-flash / 1.5-flash) limitando a máx. 500 tokens con:
       - 1) ¿Qué pasó?
       - 2) Archivo y línea afectada
       - 3) Solución recomendada (código o namespace)
  5. Publica el diagnóstico en GitHub Actions Step Summary ($GITHUB_STEP_SUMMARY).
  6. Circuit Breaker: Si la API de Gemini falla (HTTP 429, timeout, red, sin clave),
     no rompe el flujo ruidosamente; muestra el log estándar de .NET y detiene
     el pipeline (Exit 1) para que el hosting (MonsterASP) siga 100% protegido.
==============================================================================
"""

import sys
import os
import re
import json
import urllib.request
import urllib.error

# Modelos en orden de preferencia (fallback automático)
GEMINI_MODELS = ["gemini-2.0-flash", "gemini-1.5-flash"]
MAX_LOG_LINES = 40
MAX_LOG_CHARS = 2400  # Menos de 2,500 caracteres
MAX_OUTPUT_TOKENS = 500


def extract_dotnet_errors(log_text: str) -> str:
    """
    Filtra únicamente las líneas que contienen errores de compilación de .NET
    (Roslyn / C# Compiler / MSBuild) y trunca para ahorrar tokens.
    """
    lines = log_text.splitlines()
    error_lines = []

    # Patrón que detecta errores típicos de compilación C#/MSBuild:
    # Ejemplos:
    #   Controllers/HomeController.cs(12,20): error CS0103: The name 'x' does not exist...
    #   Program.cs(5,1): error CS1002: ; expected
    #   Project.csproj : error MSB4019: The imported project...
    #   fatal error ...
    error_pattern = re.compile(
        r'(:\s*error\s+[A-Za-z0-9]+:|error\s+CS\d+|fatal\s+error|:\s*fatal\s+error|MSB\d+:)',
        re.IGNORECASE
    )

    for line in lines:
        cleaned = line.strip()
        if not cleaned:
            continue
        if error_pattern.search(cleaned):
            error_lines.append(cleaned)

    # Si no hubo coincidencia estricta con 'error CS', buscar líneas con 'error'
    if not error_lines:
        for line in lines:
            cleaned = line.strip()
            lower = cleaned.lower()
            if 'error' in lower and not any(p in lower for p in ['0 error', '0 errores', '0 warning', '0 advertencia']):
                error_lines.append(cleaned)

    # Fallback si no se encontró ninguna palabra clave: tomar las últimas líneas del log
    if not error_lines:
        error_lines = [l.strip() for l in lines[-35:] if l.strip()]

    # 1. Truncar a un máximo de líneas
    truncated_lines = error_lines[:MAX_LOG_LINES]
    filtered_text = "\n".join(truncated_lines)

    # 2. Truncar a menos de 2,500 caracteres
    if len(filtered_text) > MAX_LOG_CHARS:
        filtered_text = filtered_text[:MAX_LOG_CHARS] + "\n... [Log truncado por el Guardián para optimizar tokens]"

    return filtered_text


def call_gemini_tutor(api_key: str, error_log: str) -> str:
    """
    Invoca la API de Gemini mediante REST estándar (sin dependencias externas).
    Limita la respuesta a máximo 500 tokens y exige los 3 puntos clave.
    """
    prompt = f"""Actúa como un 'Tutor de Integración Continua' amigable, conciso y pedagógico para desarrolladores y estudiantes de .NET / C# en español.
Se ha detectado un fallo en la compilación ('dotnet build') y debes explicar de forma clara cómo solucionarlo para corregir el código.

Tu respuesta DEBE estructurarse estrictamente con los siguientes 3 puntos clave en formato Markdown:
1) **¿Qué pasó?**: Explica en lenguaje claro, empático y sencillo qué significa el error de compilación.
2) **Archivo y línea afectada**: Indica con precisión el archivo, la línea y columna mencionada en el error.
3) **Solución recomendada**: Proporciona el código exacto, namespace faltante ('using ...;') o corrección necesaria para solucionarlo.

Reglas:
- Sé empático, motivador y directo al grano.
- Máximo 500 tokens en tu respuesta.
- Si sugieres código, utiliza bloques con resaltado de sintaxis C#.

Log de errores de .NET:
```
{error_log}
```
"""

    payload = {
        "contents": [
            {
                "parts": [
                    {"text": prompt}
                ]
            }
        ],
        "generationConfig": {
            "maxOutputTokens": MAX_OUTPUT_TOKENS,
            "temperature": 0.2
        }
    }

    data = json.dumps(payload).encode("utf-8")
    last_exception = None

    for model in GEMINI_MODELS:
        url = f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={api_key}"
        req = urllib.request.Request(
            url,
            data=data,
            headers={"Content-Type": "application/json"}
        )

        try:
            with urllib.request.urlopen(req, timeout=25) as response:
                resp_json = json.loads(response.read().decode("utf-8"))
                candidates = resp_json.get("candidates", [])
                if candidates:
                    parts = candidates[0].get("content", {}).get("parts", [])
                    text_parts = [p.get("text", "") for p in parts if "text" in p]
                    if text_parts:
                        return "".join(text_parts).strip()
                raise ValueError("La respuesta de Gemini no contuvo texto válido.")
        except urllib.error.HTTPError as http_err:
            last_exception = http_err
            # Si el modelo específico retorna 404, 400 o errores transitorios de servidor, probamos el siguiente en la lista
            if http_err.code in (400, 404, 500, 502, 503):
                continue
            # Para 429 (Rate Limit) o 403 (Sin permisos/Clave inválida), dejamos que suba al circuit breaker
            raise http_err
        except Exception as e:
            last_exception = e
            # Para fallos de red o conexión, intentar siguiente modelo o lanzar
            continue

    if last_exception:
        raise last_exception
    raise RuntimeError("No se pudo obtener respuesta de la API de Gemini.")


def publish_step_summary(markdown_content: str):
    """
    Publica el informe en GitHub Actions Step Summary ($GITHUB_STEP_SUMMARY).
    """
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        try:
            with open(summary_path, "a", encoding="utf-8") as f:
                f.write("\n" + markdown_content + "\n")
            print("[OK] Reporte publicado exitosamente en GitHub Step Summary.")
        except Exception as e:
            print(f"[AVISO] No se pudo escribir en GITHUB_STEP_SUMMARY: {e}")
    else:
        print("[INFO] GITHUB_STEP_SUMMARY no está definido (ejecución local o fuera de GH Actions).")


def main():
    print("==================================================================")
    print("🛡️  TUTOR IA GUARDIÁN: ANÁLISIS DE COMPILACIÓN .NET")
    print("==================================================================")

    # 1. Obtener el log de compilación
    log_content = ""
    if len(sys.argv) > 1 and os.path.exists(sys.argv[1]):
        with open(sys.argv[1], "r", encoding="utf-8", errors="replace") as f:
            log_content = f.read()
    elif not sys.stdin.isatty():
        log_content = sys.stdin.read()

    if not log_content.strip():
        print("[AVISO] No se recibió contenido de log para analizar.")
        log_content = "Error de compilación desconocido (.NET Build falló sin salida registrada)."

    # 2. Filtrar y truncar los errores
    filtered_errors = extract_dotnet_errors(log_content)
    print(f"[INFO] Log filtrado ({len(filtered_errors)} caracteres, ~{len(filtered_errors.splitlines())} líneas):")
    print("------------------------------------------------------------------")
    print(filtered_errors)
    print("------------------------------------------------------------------")

    # 3. Invocar al Tutor IA con Circuit Breaker
    api_key = os.environ.get("GEMINI_API_KEY", "").strip()
    tutor_feedback = None
    circuit_breaker_active = False
    circuit_reason = ""

    if not api_key:
        circuit_breaker_active = True
        circuit_reason = "El secreto GEMINI_API_KEY no está configurado en GitHub Secrets."
        print(f"[CIRCUIT BREAKER] {circuit_reason}")
    else:
        try:
            print("[INFO] Consultando al Tutor IA (Gemini)...")
            tutor_feedback = call_gemini_tutor(api_key, filtered_errors)
            print("[OK] Respuesta recibida del Tutor IA exitosamente.")
        except urllib.error.HTTPError as http_err:
            circuit_breaker_active = True
            if http_err.code == 429:
                circuit_reason = "Límite de cuota / tasa de peticiones excedida (HTTP 429 Too Many Requests)."
            else:
                circuit_reason = f"Error HTTP del servicio Gemini ({http_err.code} {http_err.reason})."
            print(f"[CIRCUIT BREAKER] {circuit_reason}")
        except Exception as err:
            circuit_breaker_active = True
            circuit_reason = f"Fallo de conexión o red con la API de Gemini ({str(err)})."
            print(f"[CIRCUIT BREAKER] {circuit_reason}")

    # 4. Construir y publicar reporte en GitHub Step Summary
    if not circuit_breaker_active and tutor_feedback:
        summary_md = f"""# 🛡️ Tutor IA Guardián - Reporte de Compilación .NET

> [!CAUTION]
> **El despliegue a MonsterASP se ha detenido preventivamente.**
> Se detectaron errores de compilación (`dotnet build`). Ningún archivo roto fue enviado a producción.

### 💡 Diagnóstico del Tutor IA:
{tutor_feedback}

---
<details>
<summary>📋 Ver log filtrado de errores .NET (Original)</summary>

```text
{filtered_errors}
```
</details>
"""
    else:
        # Fallback del Circuit Breaker: muestra el log estándar sin romper ruidosamente
        summary_md = f"""# 🛡️ Tutor IA Guardián - Reporte de Compilación .NET

> [!CAUTION]
> **El despliegue a MonsterASP se ha detenido preventivamente.**
> Se detectaron errores de compilación (`dotnet build`). Ningún archivo roto fue enviado a producción.

> [!WARNING]
> **Aviso de Seguridad (Circuit Breaker activado):**
> No fue posible consultar al Tutor IA en este momento: *{circuit_reason}*.
> Para proteger la integridad del hosting en MonsterASP, el pipeline se detiene inmediatamente mostrando el log estándar de .NET.

### 📋 Log de errores de compilación (.NET):
```text
{filtered_errors}
```
"""

    publish_step_summary(summary_md)
    print("\n" + summary_md + "\n")

    print("==================================================================")
    print("❌ PIPELINE DETENIDO: Compilación fallida.")
    print("   El hosting MonsterASP permanece intacto y protegido.")
    print("==================================================================")

    # 5. Detener el pipeline con código de error 1
    sys.exit(1)


if __name__ == "__main__":
    main()
