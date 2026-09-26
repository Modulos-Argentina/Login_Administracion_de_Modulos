# 🔐 Sistema de Login y Administración de Módulos (Módulo 8)

Sistema de autenticación dual, control de acceso basado en roles (RBAC) y administración de módulos desarrollado en **ASP.NET Core (.NET 10)** y **PostgreSQL**.

---

## 📚 Documentación Rápida

- 📄 **[Documentación Técnica y Base de Datos (PostgreSQL)](Login_Administracion_de_Modulos/Documentacion.md):**
  Detalles de la arquitectura MVC, endpoints, esquemas de tablas DDL, datos por defecto y despliegue.

- 🚀 **[Guía de Comandos Git y Ramas (COMANDOS_GIT.md)](COMANDOS_GIT.md):**
  Instrucciones paso a paso para subir cambios a GitHub (`origin`), gestionar ramas (`master`, `main` u otras) y ciclo de despliegue automático.

---

## ⚡ Comandos Git Rápidos para Subir Cambios

```bash
# 1. Comprobar archivos modificados
git status

# 2. Agregar todos los cambios
git add .

# 3. Guardar commit con mensaje descriptivo
git commit -m "Actualización de documentación y comandos git"

# 4. Subir a GitHub en la rama master
git push origin master
```

---

## 🌐 Despliegue Automático (CI/CD)

Al realizar `git push origin master`, **GitHub Actions** compila el proyecto en .NET 10 y lo despliega automáticamente a **MonsterASP** vía FTP mediante [.github/workflows/deploy.yml](.github/workflows/deploy.yml).
