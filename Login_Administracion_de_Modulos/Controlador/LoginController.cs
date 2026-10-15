using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Login_Administracion_de_Modulos.Model;
using Login_Administracion_de_Modulos.Seguridad;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Login_Administracion_de_Modulos.Controlador
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;

        public LoginController(IConfiguration configuration, ILogger<LoginController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Vista/Index.cshtml");
        }

        [HttpGet("Error")]
        public IActionResult Error()
        {
            ViewBag.Error = "Ocurrió un error inesperado al procesar la solicitud.";
            return View("~/Vista/Index.cshtml");
        }

        // ==========================================
        // RUTA 1: Formulario Clásico por DNI
        // ==========================================
        [HttpPost("login-dni")]
        [ValidateAntiForgeryToken]
        public IActionResult LoginDni(string dni, string clave)
        {
            string connectionString = ConfiguracionSegura.ObtenerCadenaConexion(_configuration);

            if (string.IsNullOrEmpty(connectionString))
            {
                ViewBag.Error = "El servicio de autenticación con base de datos no está configurado.";
                return View("~/Vista/Index.cshtml");
            }

            if (string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(clave))
            {
                ViewBag.Error = "Debe ingresar su DNI y contraseña.";
                return View("~/Vista/Index.cshtml");
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // 1. Buscamos al usuario por DNI junto a persona y rol
                    string query = @"
                        SELECT u.id_usuario, u.clave_cifrada, u.estado,
                               COALESCE(p.nombres, 'Personal') as nombres, 
                               COALESCE(p.apellidos, 'Salud') as apellidos,
                               COALESCE(r.nombre, 'Enfermería (Tópico)') as rol_nombre
                        FROM usuarios u
                        LEFT JOIN personas p ON u.id_persona = p.id_persona
                        LEFT JOIN usuarios_roles ur ON u.id_usuario = ur.id_usuario AND ur.estado = 'Activo'
                        LEFT JOIN roles r ON ur.id_rol = r.id_rol
                        WHERE u.dni_login = @dni";
                    
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dni", dni.Trim());

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idUsuario = reader.GetInt32(0);
                                string? claveCifradaDb = reader.IsDBNull(1) ? null : reader.GetString(1);
                                string estadoUsuario = reader.IsDBNull(2) ? "Inactivo" : reader.GetString(2);
                                string nombres = reader.GetString(3);
                                string apellidos = reader.GetString(4);
                                string rolNombre = reader.GetString(5);

                                // 2. Validar estado del usuario
                                if (!string.Equals(estadoUsuario, "Activo", StringComparison.OrdinalIgnoreCase))
                                {
                                    ViewBag.Error = "Su cuenta de usuario no se encuentra activa en el sistema.";
                                    return View("~/Vista/Index.cshtml");
                                }

                                // 3. CRÍTICO: Bloquear login por DNI si la cuenta no tiene contraseña configurada (SSO exclusivo de Google)
                                if (string.IsNullOrEmpty(claveCifradaDb))
                                {
                                    ViewBag.Error = "Esta cuenta utiliza acceso institucional sin contraseña. Ingrese con el botón 'Continuar con Google'.";
                                    return View("~/Vista/Index.cshtml");
                                }

                                // 4. Validar contraseña con algoritmo seguro PBKDF2 (SHA-256)
                                bool allowLegacy = _configuration.GetValue<bool>("Security:AllowLegacyPasswords", true);
                                bool esValida = PasswordSecurity.VerificarClave(clave, claveCifradaDb, allowLegacy);

                                if (esValida)
                                {
                                    reader.Close();

                                    // 5. Auditoría y actualización de último acceso
                                    string? remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                                    RegistrarAuditoriaYActualizarAcceso(connection, idUsuario, "DNI_LOCAL", remoteIp);

                                    // Guardar sesión del usuario
                                    string nombreCompleto = $"{nombres} {apellidos}".Trim();
                                    HttpContext.Session.SetInt32("UsuarioId", idUsuario);
                                    HttpContext.Session.SetString("UsuarioNombre", string.IsNullOrEmpty(nombreCompleto) ? "Personal Institucional" : nombreCompleto);
                                    HttpContext.Session.SetString("UsuarioRol", rolNombre);

                                    // Redirección segura según el rol
                                    string redirectUrl = ObtenerRutaDashboardPorRol(rolNombre);
                                    return Redirect(redirectUrl);
                                }
                                else
                                {
                                    ViewBag.Error = "Contraseña incorrecta.";
                                    return View("~/Vista/Index.cshtml");
                                }
                            }
                            else
                            {
                                ViewBag.Error = "DNI no encontrado o usuario inactivo.";
                                return View("~/Vista/Index.cshtml");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el inicio de sesión por DNI");
                ViewBag.Error = "Ocurrió un error al procesar el inicio de sesión. Por favor, inténtelo nuevamente más tarde.";
                return View("~/Vista/Index.cshtml");
            }
        }

        // ==========================================
        // RUTA 2: SSO con Google (Sin contraseña)
        // ==========================================
        [HttpGet("login-google-oauth")]
        public IActionResult LoginGoogleOAuth()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                ViewBag.Error = "No se pudo autenticar con Google.";
                return View("~/Vista/Index.cshtml");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var correo = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(correo))
            {
                ViewBag.Error = "No se pudo obtener el correo electrónico de Google.";
                return View("~/Vista/Index.cshtml");
            }

            return ProcessGoogleLogin(correo);
        }

        private IActionResult ProcessGoogleLogin(string correo)
        {
            string connectionString = ConfiguracionSegura.ObtenerCadenaConexion(_configuration);

            if (string.IsNullOrEmpty(connectionString))
            {
                ViewBag.Error = "El servicio de base de datos no está disponible para Google SSO.";
                return View("~/Vista/Index.cshtml");
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT u.id_usuario, u.estado,
                               COALESCE(p.nombres, 'Personal') as nombres, 
                               COALESCE(p.apellidos, 'Salud') as apellidos,
                               COALESCE(r.nombre, 'Enfermería (Tópico)') as rol_nombre
                        FROM usuarios u
                        LEFT JOIN personas p ON u.id_persona = p.id_persona
                        LEFT JOIN usuarios_roles ur ON u.id_usuario = ur.id_usuario AND ur.estado = 'Activo'
                        LEFT JOIN roles r ON ur.id_rol = r.id_rol
                        WHERE LOWER(u.correo) = LOWER(@correo)";
                    
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@correo", correo.Trim());

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idUsuario = reader.GetInt32(0);
                                string estadoUsuario = reader.IsDBNull(1) ? "Inactivo" : reader.GetString(1);
                                string nombres = reader.GetString(2);
                                string apellidos = reader.GetString(3);
                                string rolNombre = reader.GetString(4);

                                if (!string.Equals(estadoUsuario, "Activo", StringComparison.OrdinalIgnoreCase))
                                {
                                    ViewBag.Error = "Su cuenta de usuario no se encuentra activa en el sistema.";
                                    return View("~/Vista/Index.cshtml");
                                }

                                reader.Close();

                                // Auditoría e inicio de sesión
                                string? remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                                RegistrarAuditoriaYActualizarAcceso(connection, idUsuario, "GOOGLE_SSO", remoteIp);

                                string nombreCompleto = $"{nombres} {apellidos}".Trim();
                                HttpContext.Session.SetInt32("UsuarioId", idUsuario);
                                HttpContext.Session.SetString("UsuarioNombre", string.IsNullOrEmpty(nombreCompleto) ? "Personal Institucional" : nombreCompleto);
                                HttpContext.Session.SetString("UsuarioRol", rolNombre);

                                string redirectUrl = ObtenerRutaDashboardPorRol(rolNombre);
                                return Redirect(redirectUrl);
                            }
                            else
                            {
                                ViewBag.Error = "El correo institucional no está registrado en el padrón o el usuario está inactivo.";
                                return View("~/Vista/Index.cshtml");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el inicio de sesión con Google SSO");
                ViewBag.Error = "Ocurrió un error al procesar el acceso con Google. Intente nuevamente más tarde.";
                return View("~/Vista/Index.cshtml");
            }
        }

        /// <summary>
        /// Actualiza ultimo_acceso en usuarios e inserta un registro en sesiones_login para auditoría de seguridad.
        /// </summary>
        private void RegistrarAuditoriaYActualizarAcceso(NpgsqlConnection connection, int idUsuario, string metodoAutenticacion, string? ipOrigen)
        {
            try
            {
                // 1. Actualizar ultimo_acceso en usuarios
                const string sqlUpdate = "UPDATE usuarios SET ultimo_acceso = CURRENT_TIMESTAMP WHERE id_usuario = @idUsuario;";
                using (var cmdUpdate = new NpgsqlCommand(sqlUpdate, connection))
                {
                    cmdUpdate.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmdUpdate.ExecuteNonQuery();
                }

                // 2. Insertar en sesiones_login (fecha_expiracion = 1 hora a partir de ahora)
                string safeIp = string.IsNullOrWhiteSpace(ipOrigen) ? "127.0.0.1" : (ipOrigen.Length > 45 ? ipOrigen.Substring(0, 45) : ipOrigen);
                const string sqlSesion = @"
                    INSERT INTO sesiones_login 
                        (id_usuario, metodo_autenticacion, ip_origen, fecha_inicio, fecha_expiracion, estado)
                    VALUES 
                        (@idUsuario, @metodo, @ip, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 hour', 'Activa');";

                using (var cmdSesion = new NpgsqlCommand(sqlSesion, connection))
                {
                    cmdSesion.Parameters.AddWithValue("@idUsuario", idUsuario);
                    cmdSesion.Parameters.AddWithValue("@metodo", metodoAutenticacion);
                    cmdSesion.Parameters.AddWithValue("@ip", safeIp);
                    cmdSesion.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // La auditoría no debe bloquear el acceso si falla una tabla secundaria, pero debe registrarse en logs
                _logger.LogWarning(ex, "Advertencia: No se pudo registrar la auditoría de sesión para el usuario {UsuarioId}", idUsuario);
            }
        }

        // ==========================================
        // RUTA 3: Sesión Local de Prueba (Demo sin BD)
        // REGLA DE ORO: NO ALTERAR NI ELIMINAR ESTA RUTA
        // ==========================================
        [HttpGet("login-demo/{rol}")]
        public IActionResult LoginDemo(string rol)
        {
            HttpContext.Session.Clear();

            string nombreDemo;
            string rolDemo;
            int idDemo;

            switch (rol?.Trim().ToLowerInvariant())
            {
                case "topico":
                case "enfermeria":
                    nombreDemo = "Lic. Rosa Fernández (Demo)";
                    rolDemo = "Enfermería (Tópico)";
                    idDemo = 101;
                    break;
                case "psicologia":
                    nombreDemo = "Lic. Juan Psicólogo (Demo)";
                    rolDemo = "Psicología";
                    idDemo = 102;
                    break;
                case "docente":
                    nombreDemo = "Prof. Carlos Docente (Demo)";
                    rolDemo = "Docente";
                    idDemo = 103;
                    break;
                case "soporte":
                    nombreDemo = "Ing. Mario Soporte (Demo)";
                    rolDemo = "Jefe de Soporte";
                    idDemo = 104;
                    break;
                case "alumno":
                case "estudiante":
                    nombreDemo = "Estudiante Ana Torres (Demo)";
                    rolDemo = "Alumno";
                    idDemo = 105;
                    break;
                case "practicante":
                    nombreDemo = "Téc. Luis Practicante (Demo)";
                    rolDemo = "Practicante";
                    idDemo = 106;
                    break;
                case "secretaria":
                    nombreDemo = "Lic. Carmen Secretaría (Demo)";
                    rolDemo = "Secretaría Académica";
                    idDemo = 107;
                    break;
                case "admin":
                case "administrador":
                    nombreDemo = "Administrador del Sistema (Demo)";
                    rolDemo = "Administrador";
                    idDemo = 108;
                    break;
                case "jefegeneral":
                case "jefe-general":
                case "jefe":
                case "almacen":
                    nombreDemo = "Ing. Roberto Alarcón (Demo)";
                    rolDemo = "Jefe General";
                    idDemo = 109;
                    break;
                case "computacion":
                case "taller-computacion":
                case "jefe-computacion":
                    nombreDemo = "Lic. Walter Computación (Demo)";
                    rolDemo = "Jefe de Taller Computación";
                    idDemo = 110;
                    break;
                case "taller-administracion":
                case "jefe-administracion":
                case "admin-taller":
                    nombreDemo = "Lic. Marcos Administración (Demo)";
                    rolDemo = "Jefe de Taller Administración";
                    idDemo = 111;
                    break;
                case "taller-contabilidad":
                case "jefe-contabilidad":
                case "contabilidad":
                    nombreDemo = "C.P.C. Elena Contabilidad (Demo)";
                    rolDemo = "Jefe de Taller Contabilidad";
                    idDemo = 112;
                    break;
                case "inventario":
                case "encargado-inventario":
                case "inventario-general":
                    nombreDemo = "Téc. Sergio Inventario (Demo)";
                    rolDemo = "Encargado de Inventario";
                    idDemo = 113;
                    break;
                case "tesoreria":
                case "tesorero":
                case "pagos":
                case "vouchers":
                    nombreDemo = "Marta Beatriz Torres";
                    rolDemo = "Tesorería";
                    idDemo = 114;
                    break;
                default:
                    nombreDemo = "Personal Demo";
                    rolDemo = "Enfermería (Tópico)";
                    idDemo = 100;
                    break;
            }

            // Iniciar sesión local en memoria sin consultar PostgreSQL
            HttpContext.Session.SetInt32("UsuarioId", idDemo);
            HttpContext.Session.SetString("UsuarioNombre", nombreDemo);
            HttpContext.Session.SetString("UsuarioRol", rolDemo);
            HttpContext.Session.SetString("EsDemo", "true");

            return Redirect(ObtenerRutaDashboardPorRol(rolDemo));
        }

        [HttpGet("logout")]
        [HttpGet("Login/Logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }

        public IActionResult Bienvenido()
        {
            string rol = HttpContext.Session.GetString("UsuarioRol") ?? string.Empty;
            return Redirect(ObtenerRutaDashboardPorRol(rol));
        }

        /// <summary>
        /// Mapea el rol registrado en bd.sql al dashboard correspondiente
        /// </summary>
        public static string ObtenerRutaDashboardPorRol(string rolNombre)
        {
            if (string.IsNullOrWhiteSpace(rolNombre)) return "/Dashboard/Topico";

            if (rolNombre.Contains("Encargado de Inventario", StringComparison.OrdinalIgnoreCase) || (rolNombre.Contains("Inventario", StringComparison.OrdinalIgnoreCase) && !rolNombre.Contains("Jefe", StringComparison.OrdinalIgnoreCase)))
                return "/Dashboard/EncargadoInventario";

            if ((rolNombre.Contains("Taller", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Jefe", StringComparison.OrdinalIgnoreCase)) && rolNombre.Contains("Computación", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/JefeComputacion";

            if ((rolNombre.Contains("Taller", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Jefe", StringComparison.OrdinalIgnoreCase)) && (rolNombre.Contains("Administración", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Administracion", StringComparison.OrdinalIgnoreCase)) && !rolNombre.Contains("General", StringComparison.OrdinalIgnoreCase) && !rolNombre.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/JefeAdministracion";

            if ((rolNombre.Contains("Taller", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Jefe", StringComparison.OrdinalIgnoreCase)) && (rolNombre.Contains("Contabilidad", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Conta", StringComparison.OrdinalIgnoreCase)))
                return "/Dashboard/JefeContabilidad";

            if (rolNombre.Contains("Jefe General", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Almacen", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/JefeGeneral";

            if (rolNombre.Contains("Alumno", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Estudiante", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Alumno";

            if (rolNombre.Contains("Docente", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Profesor", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Docente";

            if (rolNombre.Contains("Psicología", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Psicologo", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Psicologia";

            if (rolNombre.Contains("Jefe de Soporte", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Soporte", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Soporte";

            if (rolNombre.Contains("Practicante", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Practicante";

            if (rolNombre.Contains("Secretar", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Secretaria";

            if (rolNombre.Contains("Tesor", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Tesoreria";

            if (rolNombre.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Administrador";

            if (rolNombre.Contains("Tópico", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Enfermer", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Topico";

            return "/Dashboard/Topico";
        }
    }
}
