#!/usr/bin/env python3
import json
import os
import sys

def main():
    target_dir = sys.argv[1] if len(sys.argv) > 1 else './publish_output'
    if not os.path.exists(target_dir):
        print(f"[ALERTA] El directorio objetivo '{target_dir}' no existe.")
        return

    appsettings_path = os.path.join(target_dir, 'appsettings.json')
    prod_settings_path = os.path.join(target_dir, 'appsettings.Production.json')
    web_config_path = os.path.join(target_dir, 'web.config')

    # 1. Leer o inicializar estructura JSON
    data = {}
    if os.path.exists(appsettings_path):
        try:
            with open(appsettings_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
        except Exception as e:
            print(f"[AVISO] Error al leer appsettings.json: {e}")

    data.setdefault('Logging', {
        'LogLevel': {
            'Default': 'Information',
            'Microsoft.AspNetCore': 'Warning'
        }
    })
    data.setdefault('AllowedHosts', '*')
    data.setdefault('ConnectionStrings', {})
    data.setdefault('Authentication', {}).setdefault('Google', {})
    data.setdefault('Security', {
        'EnableDemoLogin': True,
        'AllowLegacyPasswords': True
    })

    # 2. Obtener secretos de las variables de entorno inyectadas por GitHub Actions
    db_conn = os.environ.get('PROD_DB_CONNECTION', '').strip()
    google_client_id = os.environ.get('PROD_GOOGLE_CLIENT_ID', '').strip()
    google_client_secret = os.environ.get('PROD_GOOGLE_CLIENT_SECRET', '').strip()

    if db_conn:
        data['ConnectionStrings']['DefaultConnection'] = db_conn
        print("[OK] Cadena de conexion (DB_CONNECTION_STRING) configurada correctamente.")
    else:
        print("[ALERTA] DB_CONNECTION_STRING esta vacia o no fue configurada en GitHub Secrets.")

    if google_client_id:
        data['Authentication']['Google']['ClientId'] = google_client_id
        print("[OK] Google Client ID (GOOGLE_CLIENT_ID) configurado correctamente.")
    else:
        print("[ALERTA] GOOGLE_CLIENT_ID esta vacio o no fue configurado en GitHub Secrets.")

    if google_client_secret:
        data['Authentication']['Google']['ClientSecret'] = google_client_secret
        print("[OK] Google Client Secret (GOOGLE_CLIENT_SECRET) configurado correctamente.")
    else:
        print("[ALERTA] GOOGLE_CLIENT_SECRET esta vacio o no fue configurado en GitHub Secrets.")

    # 3. Guardar en appsettings.json y appsettings.Production.json
    with open(appsettings_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    with open(prod_settings_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print("[OK] Archivos appsettings.json y appsettings.Production.json actualizados.")

    # 4. Asegurar ASPNETCORE_ENVIRONMENT=Production en web.config para MonsterASP / IIS
    if os.path.exists(web_config_path):
        try:
            with open(web_config_path, 'r', encoding='utf-8') as f:
                content = f.read()

            if 'ASPNETCORE_ENVIRONMENT' not in content:
                env_block = (
                    "      <aspNetCore processPath=\"dotnet\" arguments=\".\\Login_Administracion_de_Modulos.dll\" stdoutLogEnabled=\"false\" stdoutLogFile=\".\\logs\\stdout\" hostingModel=\"inprocess\">\n"
                    "        <environmentVariables>\n"
                    "          <environmentVariable name=\"ASPNETCORE_ENVIRONMENT\" value=\"Production\" />\n"
                    "        </environmentVariables>\n"
                    "      </aspNetCore>"
                )
                if 'hostingModel="inprocess" />' in content:
                    content = content.replace(
                        '      <aspNetCore processPath="dotnet" arguments=".\\Login_Administracion_de_Modulos.dll" stdoutLogEnabled="false" stdoutLogFile=".\\logs\\stdout" hostingModel="inprocess" />',
                        env_block
                    )
                    with open(web_config_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print("[OK] Variable ASPNETCORE_ENVIRONMENT=Production agregada a web.config.")
        except Exception as e:
            print(f"[AVISO] No se pudo actualizar web.config: {e}")

    print("[EXITO] Proceso de preparacion de secretos completado para MonsterASP.")

if __name__ == '__main__':
    main()
