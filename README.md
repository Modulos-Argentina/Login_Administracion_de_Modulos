# Sistema Central de Autenticación, Seguridad RBAC y Administración de Módulos (Intranet Institucional)

Bienvenido al repositorio central de la plataforma tecnológica institucional del **IESTP Argentina**. Este proyecto actúa como el **Núcleo Padre (Core / Módulo 8)** del ecosistema de software institucional, proveyendo la infraestructura base para la autenticación única de usuarios, el control de acceso basado en roles (RBAC), los paneles de control unificados (Dashboards) y la gobernanza para la integración desacoplada de los módulos desarrollados por los distintos equipos y carreras.

```
       ┌─────────────────────────────────────────────────────────────┐
       │             NÚCLEO CENTRAL (Módulo 8 - Core)                │
       │    Autenticación Dual (DNI + Google SSO) | Seguridad RBAC    │
       │           Filtro [ProtegerPanel] | Auditoría de Sesión       │
       └──────────────────────────────┬──────────────────────────────┘
                                      │
            ┌─────────────────────────┼─────────────────────────┐
            ▼                         ▼                         ▼
   [Areas/Modulo01]            [Areas/Modulo04]          [Areas/Modulo05]
 Matriculación y Alumnos    Inventario y Activos TI     Salud, Tópico y Psico
 (Submódulo / Repo Ext.)     (Submódulo / Rama Mod)    (Submódulo / Repo Ext.)
```

## 1. Visión General y Características Principales

* **Autenticación Dual Segura:** Permite el ingreso mediante credenciales locales (DNI + Contraseña con hash criptográfico PBKDF2-SHA256) o a través de Single Sign-On (SSO) institucional con Google OAuth 2.0 (`@instituto.edu.pe`). Las cuentas institucionales sin contraseña local son protegidas automáticamente bloqueando accesos por formulario directo.
* **Modo Demo Local Incorporado (Sin Base de Datos):** La pantalla de login incluye una sección de acceso rápido de prototipo que permite iniciar sesión instantáneamente en cualquiera de los 14 paneles con un solo clic, sin requerir credenciales ni conexión a PostgreSQL. Ideal para pruebas de interfaz, maquetado y validación de flujos.
* **Control de Acceso Basado en Roles (`[ProtegerPanel]`):** Filtro de acción de nivel de arquitectura que intercepta cada solicitud a `/Dashboard/...` y a las rutas `/ModuloXX/...`. Valida la identidad y el rol en sesión, protege peticiones AJAX respondiendo HTTP 401/403 en formato JSON, redirige accesos no autorizados a su panel correspondiente e inyecta metadatos del usuario en la vista.
* **Auditoría Centralizada de Accesos:** Todo inicio de sesión válido (DNI o Google SSO) registra automáticamente la IP de origen, marca de tiempo y método de acceso en la tabla `sesiones_login`, manteniendo trazabilidad de seguridad en PostgreSQL.
* **Arquitectura Modular Desacoplada (`Areas/ModuloXX`):** Estructura preparada para integrar los submódulos de cada equipo de desarrollo mediante repositorios independientes de GitHub (`git submodule`) o ramas aisladas gobernadas por flujos automáticos de CI/CD.

## 2. Stack Tecnológico y Arquitectura

| Capa / Componente | Tecnología | Versión | Descripción y Uso en el Proyecto |
| :--- | :--- | :---: | :--- |
| **Backend Core** | ASP.NET Core MVC | .NET 10 (C# 14) | Framework de alto rendimiento con inyección de dependencias nativa, controladores limpios y middleware modular. |
| **SDK de Compilación** | .NET SDK | 10.0.401 | Fijado a través de `global.json` con política de avance `rollForward: latestMinor`. |
| **Base de Datos** | PostgreSQL 17 | v17.x | Motor relacional robusto (Supabase / Local) con llaves foráneas, restricciones `CHECK`, UUIDs y roles maestros. |
| **Conector PostgreSQL** | **Npgsql (ADO.NET)** | **10.0.0** | Paquete NuGet oficial de conexión a PostgreSQL. Consultas SQL parametrizadas de alta velocidad sin sobrecarga de ORM. |
| **Autenticación Federada** | Google Authentication | **9.0.0** | Middleware institucional para Single Sign-On (SSO) con Google OAuth 2.0 (`@instituto.edu.pe`). |
| **Seguridad y Hasheo** | PBKDF2-SHA256 | 100k iteraciones | Implementado en `PasswordSecurity.cs` con salt criptográfico de 16 bytes y comparación segura `FixedTimeEquals`. |
| **Configuración Segura** | `.env` + `ConfiguracionSegura.cs` | Nativo | Aislamiento estricto de secretos fuera del código fuente con soporte para variables de sistema y Secrets de GitHub. |
| **Cookies y Sesiones** | Cookie Session `.IESTP.Session` | Nativo | Cookies blindadas con `HttpOnly = true`, `SameSite = Lax` y expiración controlada por inactividad (60 min). |
| **Cabeceras de Seguridad** | Middleware HTTP Personalizado | Nativo | Inyección automática de `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN` y `Referrer-Policy`. |
| **Frontend** | Razor Views (`.cshtml`) + CSS | HTML5 / CSS3 | Vistas limpias y diseño responsivo, maquetado con variables CSS, Phosphor Icons y Bootstrap 5. |
| **Gobernanza y CI/CD** | GitHub Actions + `CODEOWNERS` | CI/CD | Despliegue continuo a MonsterASP (`deploy.yml`) y bloqueo estricto de ramas por módulo (`proteger-ramas-modulos.yml`). |

### 2.1 Dependencias y Paquetes NuGet del Proyecto

El archivo de proyecto [`Login_Administracion_de_Modulos.csproj`](file:///c:/Users/Arias/Videos/Login_Administracion_de_Modulos/Login_Administracion_de_Modulos/Login_Administracion_de_Modulos.csproj) define formalmente las siguientes dependencias:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="9.0.0" />
  <PackageReference Include="Npgsql" Version="10.0.0" />
</ItemGroup>
```

| Paquete NuGet | Versión | Framework Objetivo | Finalidad en el Ecosistema |
| :--- | :---: | :---: | :--- |
| **[`Npgsql`](https://www.nuget.org/packages/Npgsql/)** | **`10.0.0`** | `net10.0` | **Conector de Acceso a Datos PostgreSQL:** Driver ADO.NET optimizado para PostgreSQL 17. Permite ejecutar comandos SQL parametrizados (`NpgsqlCommand`), lectura de flujo por cursores (`NpgsqlDataReader`), manejo transaccional ACID y conexión resiliente a clústeres cloud (Supabase) sin fricción de ORMs pesados. |
| **[`Microsoft.AspNetCore.Authentication.Google`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.Google/)** | **`9.0.0`** | `net10.0` | **Autenticación Delegada OAuth 2.0:** Manejador del flujo de autorización institucional mediante cuentas corporativas Google. Facilita la extracción de Claims, validación de tokens y federación de identidad de estudiantes y docentes. |

#### Comandos de Instalación / Restauración vía CLI:
```bash
# Restaurar todos los paquetes NuGet del proyecto
dotnet restore

# Si necesitas agregar o actualizar explícitamente el paquete de PostgreSQL:
dotnet add Login_Administracion_de_Modulos/Login_Administracion_de_Modulos.csproj package Npgsql --version 10.0.0

# Si necesitas actualizar el paquete de autenticación de Google:
dotnet add Login_Administracion_de_Modulos/Login_Administracion_de_Modulos.csproj package Microsoft.AspNetCore.Authentication.Google --version 9.0.0
```

### 2.2 Especificación del Conector de Datos PostgreSQL (`Npgsql 10.0.0`)

El acceso a PostgreSQL en el Núcleo y los Submódulos se realiza mediante **ADO.NET puro** utilizando las clases del espacio de nombres `Npgsql`:

```csharp
using Npgsql;

// Ejemplo de consulta segura y parametrizada con Npgsql 10.0.0
using (var connection = new NpgsqlConnection(connectionString))
{
    connection.Open();
    string sql = "SELECT id_usuario, nombres, apellidos, rol FROM usuarios WHERE numero_documento = @dni AND estado = 'Activo' LIMIT 1";
    using (var command = new NpgsqlCommand(sql, connection))
    {
        command.Parameters.AddWithValue("@dni", dni);
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                // Procesamiento fuertemente tipado
            }
        }
    }
}
```

#### Anatomía de la Cadena de Conexión (`DB_CONNECTION_STRING`) en `.env`
Para asegurar conectividad óptima tanto en bases de datos locales como en pools remotos de **Supabase / PgBouncer**, la cadena de conexión se compone de los siguientes parámetros clave:

```env
DB_CONNECTION_STRING=Host=aws-0-us-east-2.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.TU_PROYECTO;Password=TU_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;KeepAlive=30;
```

* **`Host`**: Nombre de host del servidor o clúster pooler de PostgreSQL / Supabase.
* **`Port`**: Puerto TCP (`6543` en Supabase Transaction Pooler, `5432` en PostgreSQL estándar).
* **`Database`**: Nombre de la base de datos relacional (`postgres`).
* **`SSL Mode=Require`**: Encripta todo el tráfico TCP punto a punto mediante TLS/SSL, requisito obligatorio para instancias cloud.
* **`Trust Server Certificate=true`**: Valida certificados intermedios y auto-firmados de proveedores cloud sin requerir instalación manual en el almacén de certificados del sistema operativo.
* **`Pooling=false`**: Desactiva el pool de conexiones en el cliente .NET (`Npgsql`), delegando la gestión de conexiones al **Transaction Pooler** de Supabase (PgBouncer) y evitando el agotamiento de sockets concurrentes.
* **`KeepAlive=30`**: Envía paquetes TCP Keep-Alive cada 30 segundos para evitar que balanceadores de carga o firewalls intermediarios cierren la conexión por inactividad.

---

## 3. Mapa de Estructura del Proyecto

A continuación se detalla la organización integral del código, destacando la frontera entre la **Infraestructura de Gobernanza**, el **Núcleo Padre Protegido (Módulo 8)** y los **Contenedores de Módulos (`Areas/`)**:

```
Login_Administracion_de_Modulos/
├── .github/                                     # 🛡️ GOBERNANZA Y AUTOMATIZACIÓN (Protegido)
│   ├── CODEOWNERS                               # Asignación obligatoria de revisores por carpeta
│   └── workflows/
│       ├── deploy.yml                           # Despliegue automático FTP a MonsterASP en push a master
│       └── proteger-ramas-modulos.yml           # Verificación de alcance estricto por rama de módulo
│
├── Login_Administracion_de_Modulos/             # 🛡️ NÚCLEO PADRE DEL SISTEMA (Módulo 8 - Core)
│   ├── Areas/                                   # 🧩 CONTENEDOR MODULAR DESACOPLADO
│   │   ├── Modulo04/                            # Módulo 04: Inventario y Activos TI
│   │   └── Modulo05/                            # Módulo 05: Salud, Tópico Médico y Psicología
│   │       ├── Controller/                      # Controladores ([Area("Modulo05")])
│   │       │   ├── AtencionClinicaController.cs # Consultas y atenciones médicas
│   │       │   ├── AveriaDocenteController.cs   # Reportes de averías por docentes
│   │       │   ├── EnfermeriaController.cs      # Panel y triaje de enfermería
│   │       │   ├── PsicologiaController.cs      # Expedientes y sesiones de psicología
│   │       │   └── SoporteController.cs         # Consola de soporte técnico y tickets
│   │       ├── Models/                          # Modelos de datos y repositorios Npgsql
│   │       │   ├── ActivoHardwareModel.cs       # Gestión de activos de hardware
│   │       │   ├── AtencionMedicaModel.cs       # Atenciones y recetas médicas
│   │       │   ├── ExpedientePsicoModel.cs      # Historias psicológicas
│   │       │   └── TicketAveriaModel.cs         # Ciclo de vida de tickets de soporte
│   │       └── Views/                           # Vistas Razor del módulo
│   │           ├── averias_docente/             # Formularios y listados de reportes
│   │           ├── enfermeria/                  # Atenciones, triaje e historial
│   │           ├── psicologia/                  # Expedientes y sesiones
│   │           └── soporte_tecnico/             # Consola y atención de tickets
│   │
│   ├── Controlador/                             # Controladores principales del Núcleo Core
│   │   ├── DashboardController.cs               # Enrutamiento de los 14 paneles (Protegido por [ProtegerPanel])
│   │   └── LoginController.cs                   # Autenticación DNI, Google SSO, Demo Local y Auditoría Npgsql
│   │
│   ├── Seguridad/                               # Componentes transversales de seguridad
│   │   ├── ConfiguracionSegura.cs               # Lector multi-ruta de .env y Secrets sin exponer contraseñas
│   │   ├── PasswordSecurity.cs                  # Hasheo PBKDF2-SHA256 (100k iter) y FixedTimeEquals
│   │   └── ProtegerPanelAttribute.cs            # Filtro [ProtegerPanel] para autorización RBAC estricta
│   │
│   ├── Vista/                                   # Vistas Razor institucionales
│   │   ├── Dashboard/                           # Las 14 plantillas de panel institucional
│   │   │   ├── Administrador.cshtml             # Panel de auditoría global y gestión RBAC
│   │   │   ├── Alumno.cshtml                    # Portal estudiantil de notas y asistencias
│   │   │   ├── Docente.cshtml                   # Gestión académica e incidencias de aula
│   │   │   ├── EncargadoInventario.cshtml       # Gestión de almacén y bienes patrimoniales
│   │   │   ├── JefeAdministracion.cshtml        # Taller de Administración
│   │   │   ├── JefeComputacion.cshtml           # Taller de Computación e Informática
│   │   │   ├── JefeContabilidad.cshtml          # Taller de Contabilidad
│   │   │   ├── JefeGeneral.cshtml               # Almacén general y despacho central
│   │   │   ├── Practicante.cshtml               # Órdenes de soporte y laboratorios
│   │   │   ├── Psicologia.cshtml                # Expedientes clínicos y citas
│   │   │   ├── Secretaria.cshtml                # Padrón de personas y carreras
│   │   │   ├── Soporte.cshtml                   # Panel de tickets e infraestructura TI
│   │   │   ├── Tesoreria.cshtml                 # Caja, recaudación y aranceles
│   │   │   └── Topico.cshtml                    # Triaje, atenciones médicas y botica
│   │   ├── Shared/                              # Plantillas maestras (_Layout, _DashboardLayout)
│   │   └── Index.cshtml                         # Pantalla de Login (Formulario DNI + Google SSO + Botones Demo)
│   │
│   ├── wwwroot/                                 # Assets estáticos (CSS, JS, logos institucionales)
│   ├── appsettings.json                         # Configuración base pública con marcadores seguros
│   ├── contexto_bd_completa.sql                 # Script DDL completo de PostgreSQL 17 (8 módulos)
│   ├── Login_Administracion_de_Modulos.csproj   # Especificación de paquetes (.NET 10, Npgsql 10.0.0, Google Auth)
│   └── Program.cs                               # Pipeline HTTP, cookies, autenticación y middleware
│
├── .env.example                                 # Plantilla de variables de entorno para desarrollo local
├── .mi_rama.txt                                 # Registro local de la rama asignada al desarrollador
├── global.json                                  # Bloqueo de versión del SDK de .NET (10.0.401)
├── manipular.bat                                # Panel interactivo de control de ramas y Git para el equipo
├── COMANDOS_GIT.md                              # Guía de comandos frecuentes de Git y despliegue CI/CD
├── CONTEXTO_PANELES_DOCENTE.md                  # Especificación de paneles y flujos del docente
├── CONTEXTO_PANELES_ROLES.md                    # Catálogo de widgets y menús laterales por rol
└── Estructura_Proyecto.md                       # Inventario exhaustivo de carpetas y archivos
```

---

## 4. Guía de Inicio Rápido

### Requisitos Previos del Entorno
* **[.NET SDK 10.0.401 o superior](https://dotnet.microsoft.com/download/dotnet/10.0):** Comprueba tu versión ejecutando `dotnet --version`.
* **Git:** Para clonar el repositorio y gestionar ramas de trabajo.
* **PostgreSQL 17 / Cuenta en Supabase** *(Opcional si usas el Modo Demo)*: Para el modo de autenticación real conectada.

---

### Opción 1: Ejecutar en Modo Prototipo / Demo Local (Sin Base de Datos)

Esta modalidad permite levantar y validar la plataforma inmediatamente en cualquier computadora sin instalar PostgreSQL ni solicitar credenciales de conexión:

```bash
# 1. Clonar el repositorio
git clone https://github.com/paulocarreons-max/Login_Administracion_de_Modulos.git

# 2. Entrar al directorio del proyecto
cd Login_Administracion_de_Modulos

# 3. Compilar y ejecutar la aplicación
dotnet run --project Login_Administracion_de_Modulos/Login_Administracion_de_Modulos.csproj
```

Una vez iniciado el servidor (por defecto en `http://localhost:5215` o `https://localhost:7184`):
1. Abre tu navegador y accede a `http://localhost:5215`.
2. En la pantalla de Login, localiza la sección lateral **"Acceso Rápido - Modo Demo Local"**.
3. Haz clic sobre cualquiera de los **14 botones de rol** (Tópico, Docente, Soporte, Administrador, Tesorería, etc.).
4. El sistema inicializará una sesión local de prueba válida en memoria y te conducirá directamente al panel correspondiente para validar componentes visuales, navegación y estilos.

---

### Opción 2: Ejecutar Conectado a Base de Datos PostgreSQL (.env)

Por estándares de seguridad, las credenciales reales de la base de datos y los secretos de Google jamás se almacenan en `appsettings.json`.

1. Copia el archivo de plantilla `.env.example` y crea un archivo `.env` en la raíz del proyecto:
   ```bash
   cp .env.example .env
   ```

2. Abre `.env` con tu editor de texto y completa las variables requeridas:
   ```env
   # Cadena de conexión hacia tu instancia de PostgreSQL 17 o Supabase
   DB_CONNECTION_STRING=Host=aws-0-us-east-2.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.tu_proyecto;Password=tu_password_segura;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;KeepAlive=30;

   # Credenciales de Google OAuth 2.0 (Opcional para SSO institucional)
   GOOGLE_CLIENT_ID=tu-google-client-id.apps.googleusercontent.com
   GOOGLE_CLIENT_SECRET=tu-google-client-secret
   ```

3. Si estás creando una base de datos local desde cero, ejecuta el script SQL estructurado en tu servidor PostgreSQL:
   * Archivo: `Login_Administracion_de_Modulos/contexto_bd_completa.sql`

4. Compila y arranca la aplicación:
   ```bash
   dotnet run --project Login_Administracion_de_Modulos/Login_Administracion_de_Modulos.csproj
   ```
   `ConfiguracionSegura.cs` detectará automáticamente el archivo `.env`, inyectará la cadena de conexión en memoria y habilitará el formulario de ingreso real por DNI (consultando la tabla `usuarios` vía `Npgsql 10.0.0`) y Google SSO.

---

## 5. Panel Interactivo de Trabajo (`manipular.bat`)

Para facilitar el flujo de trabajo colaborativo y evitar que los integrantes del equipo cometan errores con Git o sobreescriban ramas ajenas, se incluye la herramienta [`manipular.bat`](file:///c:/Users/Arias/Videos/Login_Administracion_de_Modulos/manipular.bat) en la raíz del proyecto.

### ¿Cómo usar la herramienta?
Haz doble clic sobre `manipular.bat` o ejecútalo desde tu terminal de Windows:
```cmd
manipular.bat
```

### Opciones disponibles en el menú interactivo:
1. **Actualizar proyecto (Bajar cambios recientes de master):** Descarga e integra las últimas actualizaciones aprobadas en el repositorio central (`git pull origin master`).
2. **Registrar o crear mi rama (Ejemplo: `modulo05`):** Guarda localmente en `.mi_rama.txt` el nombre de la rama del módulo asignado y crea/conmuta a dicha rama con la convención correcta.
3. **Ir a mi rama de trabajo:** Realiza un `git checkout` automático hacia la rama registrada en `.mi_rama.txt`.
4. **CONSULTAR EN QUÉ RAMA ESTOY:** Realiza un diagnóstico en vivo mostrando si estás parado en tu rama asignada, en `master` o en la rama de otro compañero.
5. **Ver los archivos que he modificado:** Ejecuta `git status` filtrado para comprobar que únicamente has tocado tu carpeta dentro de `Areas/`.
6. **Subir los cambios de mi rama al servidor:** Solicita un mensaje de commit descriptivo, añade los archivos y realiza el `git push origin <tu_rama>` de forma segura.
7. **Cambiar a otra rama:** Permite explorar y probar temporalmente las ramas de trabajo de otros compañeros de equipo.
8. **Salir:** Cierra el asistente.

---

## 6. Cómo Integrar tu Módulo en este Repositorio

Para garantizar la independencia de cada equipo sin comprometer la estabilidad del Núcleo Central, se admiten dos modalidades de integración:

### Modalidad A (Recomendada): Repositorio Propio mediante Submódulos de Git

Esta modalidad permite que tu equipo trabaje en su propio repositorio de GitHub independiente, con su propio historial de commits y ramas, sin necesidad de tener permisos de escritura sobre este repositorio padre.

#### Estructura requerida en tu repositorio independiente
El repositorio de tu módulo debe contener únicamente la carpeta funcional de tu área:
```
mi-repositorio-modulo04/
├── Controller/
│   └── InventarioController.cs
├── Models/
│   └── ActivoItem.cs
└── Views/
    └── inventario/
        └── index.cshtml
```

#### Reglas de código en tus controladores
Para que el motor de rutas de ASP.NET Core y el filtro de seguridad reconozcan tu módulo, decora tus controladores de la siguiente manera:

```csharp
using Microsoft.AspNetCore.Mvc;
using Login_Administracion_de_Modulos.Seguridad;

namespace MiOrganizacion.Modulo04.Controller
{
    [Area("Modulo04")]
    [Route("Modulo04/[controller]")]
    [ProtegerPanel("Encargado de Inventario", "Jefe General")]
    public class InventarioController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IConfiguration _configuration;

        public InventarioController(IConfiguration configuration)
        {
            _configuration = configuration;
            // Utilizar ConfiguracionSegura para obtener la conexión central Npgsql
            string conn = ConfiguracionSegura.ObtenerCadenaConexion(_configuration);
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            // Obtener el ID del usuario actualmente autenticado en la sesión
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1;
            
            return View("~/Areas/Modulo04/Views/inventario/index.cshtml");
        }
    }
}
```

#### Vinculación en el Repositorio Padre
El administrador del repositorio central vincula tu repositorio ejecutando:
```bash
# Registrar tu repositorio como submódulo dentro de Areas/
git submodule add https://github.com/tu-organizacion/repositorio-modulo04.git Login_Administracion_de_Modulos/Areas/Modulo04

# Confirmar la vinculación
git commit -m "feat(submodule): vincular repositorio de Modulo04 Inventario"
```

Para actualizar los cambios de tu submódulo en cualquier momento:
```bash
git submodule update --remote --merge
```

### Modalidad B: Trabajo por Ramas dentro del Repositorio Central

Si tu equipo colabora directamente sobre este repositorio, se aplican reglas automáticas de gobernanza y protección de ramas.

#### 1. Convención obligatoria de nombres de rama
Tu rama de trabajo debe iniciar obligatoriamente con el identificador de tu módulo en minúsculas:
* `modulo01/gestion-matriculas`
* `modulo04/registro-bienes`
* `modulo05/triaje-enfermeria`

#### 2. Guardián Automático de CI/CD (`proteger-ramas-modulos.yml`)
Cada vez que realices `git push` o abras un Pull Request hacia `master`:
1. El workflow de GitHub Actions detecta el prefijo de tu rama (por ejemplo, `modulo05/...`).
2. Ejecuta un análisis de diferencias (`git diff --name-only`) respecto a la rama base.
3. **Verificación Estricta:** Si tu rama modifica archivos dentro de `Login_Administracion_de_Modulos/Areas/Modulo05/`, la verificación pasará exitosamente.
4. **Bloqueo Automático (Exit 1):** Si tu rama intenta modificar cualquier archivo del Núcleo (`Program.cs`, `LoginController.cs`, `appsettings.json`, `Vista/`) o de otro módulo ajeno (`Areas/Modulo04/`), el workflow fallará automáticamente, bloqueando la integración e indicando la lista de archivos prohibidos.

---

## 7. Catálogo de Roles y Paneles del Sistema

El sistema cuenta con **14 roles configurados** con rutas protegidas mediante `[ProtegerPanel]` y rutas de acceso rápido demo:

| N° | Rol Institucional | Ruta del Panel (`GET`) | Ruta Demo Local (`GET`) | Filtro `[ProtegerPanel]` |
| :---: | :--- | :--- | :--- | :--- |
| **01** | Enfermería (Tópico) | `/Dashboard/Topico` | `/login-demo/topico` | `"Enfermería (Tópico)", "Tópico"` |
| **02** | Psicología | `/Dashboard/Psicologia` | `/login-demo/psicologia` | `"Psicología", "Psicologia"` |
| **03** | Docente | `/Dashboard/Docente` | `/login-demo/docente` | `"Docente", "Profesor"` |
| **04** | Jefe de Soporte TI | `/Dashboard/Soporte` | `/login-demo/soporte` | `"Jefe de Soporte", "Soporte"` |
| **05** | Alumno / Estudiante | `/Dashboard/Alumno` | `/login-demo/alumno` | `"Alumno", "Estudiante"` |
| **06** | Practicante | `/Dashboard/Practicante` | `/login-demo/practicante` | `"Practicante"` |
| **07** | Secretaría Académica | `/Dashboard/Secretaria` | `/login-demo/secretaria` | `"Secretaría Académica", "Secretaria"` |
| **08** | Administrador Global | `/Dashboard/Administrador` | `/login-demo/admin` | `"Administrador", "Admin"` *(Acceso Global)* |
| **09** | Jefe General / Almacén | `/Dashboard/JefeGeneral` | `/login-demo/jefegeneral` | `"Jefe General", "Almacen"` |
| **10** | Jefe Taller Computación | `/Dashboard/JefeComputacion` | `/login-demo/computacion` | `"Jefe de Taller Computación"` |
| **11** | Jefe Taller Administración | `/Dashboard/JefeAdministracion` | `/login-demo/taller-administracion` | `"Jefe de Taller Administración"` |
| **12** | Jefe Taller Contabilidad | `/Dashboard/JefeContabilidad` | `/login-demo/taller-contabilidad` | `"Jefe de Taller Contabilidad"` |
| **13** | Encargado de Inventario | `/Dashboard/EncargadoInventario` | `/login-demo/inventario` | `"Encargado de Inventario", "Inventario"` |
| **14** | Tesorería | `/Dashboard/Tesoreria` | `/login-demo/tesoreria` | `"Tesorería", "Tesorero"` |

*Nota: El usuario con rol `Administrador` posee privilegios globales para auditar y acceder a todos los paneles y áreas del sistema.*

---

## 8. Documentación Complementaria del Repositorio

Para profundizar en aspectos específicos del proyecto, consulta los documentos especializados incluidos en este repositorio:

* 📄 **[Documentación Técnica de Arquitectura y Base de Datos](Login_Administracion_de_Modulos/Documentacion.md):** Especificación completa de endpoints, contratos JSON, modelos de datos y esquemas DDL de las tablas centrales.
* 🗄️ **[Esquema Completo de Base de Datos PostgreSQL 17](Login_Administracion_de_Modulos/contexto_bd_completa.sql):** Script DDL integral de PostgreSQL 17 con la definición de tablas y llaves foráneas para los 8 módulos institucionales.
* 📋 **[Especificación de Paneles Laterales y Vistas por Rol](CONTEXTO_PANELES_ROLES.md):** Detalle de componentes visuales, menús de navegación lateral y widgets asignados a cada uno de los 14 roles.
* 👨‍🏫 **[Contexto de Paneles del Docente](CONTEXTO_PANELES_DOCENTE.md):** Flujo de trabajo, registro de asistencias, reportes de averías y gestión de notas del docente.
* 🗂️ **[Inventario de Estructura del Proyecto](Estructura_Proyecto.md):** Árbol exhaustivo de archivos, controladores, modelos y vistas de la solución.
* 🚀 **[Guía de Comandos Git y Despliegue CI/CD](COMANDOS_GIT.md):** Procedimientos de control de versiones, comandos frecuentes de Git y flujo de publicación automática vía FTP a MonsterASP.

