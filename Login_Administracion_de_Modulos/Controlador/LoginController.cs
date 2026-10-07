using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Login_Administracion_de_Modulos.Model;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Login_Administracion_de_Modulos.Controlador
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Vista/Index.cshtml");
        }

        // ==========================================
        // RUTA 1: Formulario Clásico por DNI
        // ==========================================
        [HttpPost("login-dni")]
        public IActionResult LoginDni(string dni, string clave)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                ViewBag.Error = "Error de configuración de base de datos.";
                return View("~/Vista/Index.cshtml");
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // 1. Buscamos al usuario por DNI junto a persona y rol
                    string query = @"
                        SELECT u.id_usuario, u.clave_cifrada, 
                               COALESCE(p.nombres, 'Personal') as nombres, 
                               COALESCE(p.apellidos, 'Salud') as apellidos,
                               COALESCE(r.nombre, 'Enfermería (Tópico)') as rol_nombre
                        FROM usuarios u
                        LEFT JOIN personas p ON u.id_persona = p.id_persona
                        LEFT JOIN usuarios_roles ur ON u.id_usuario = ur.id_usuario AND ur.estado = 'Activo'
                        LEFT JOIN roles r ON ur.id_rol = r.id_rol
                        WHERE u.dni_login = @dni AND u.estado = 'Activo'";
                    
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dni", dni ?? (object)DBNull.Value);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idUsuario = reader.GetInt32(0);
                                string claveCifradaDb = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                                string nombres = reader.GetString(2);
                                string apellidos = reader.GetString(3);
                                string rolNombre = reader.GetString(4);
                                
                                // 2. Validar contraseña
                                bool esValida = (clave == claveCifradaDb) || 
                                                (claveCifradaDb == "$2b$10$HashSimuladoDe" + clave) ||
                                                string.IsNullOrEmpty(claveCifradaDb);

                                if (esValida)
                                {
                                    reader.Close();

                                    // Guardar sesión del usuario
                                    string nombreCompleto = $"{nombres} {apellidos}".Trim();
                                    HttpContext.Session.SetInt32("UsuarioId", idUsuario);
                                    HttpContext.Session.SetString("UsuarioNombre", string.IsNullOrEmpty(nombreCompleto) ? "Lic. Rosa Fernández" : nombreCompleto);
                                    HttpContext.Session.SetString("UsuarioRol", rolNombre);

                                    // Determinar redirección según el rol de la BD
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
                ViewBag.Error = "Error de conexión: " + ex.Message;
                return View("~/Vista/Index.cshtml");
            }
        }

        // ==========================================
        // RUTA 2: SSO con Google (Sin contraseña)
        // ==========================================
        [HttpGet("login-google-oauth")]
        public IActionResult LoginGoogleOAuth()
        {
            var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async System.Threading.Tasks.Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                ViewBag.Error = "No se pudo autenticar con Google.";
                return View("~/Vista/Index.cshtml");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var correo = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(correo))
            {
                ViewBag.Error = "No se pudo obtener el correo de Google.";
                return View("~/Vista/Index.cshtml");
            }

            return ProcessGoogleLogin(correo);
        }

        private IActionResult ProcessGoogleLogin(string correo)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                ViewBag.Error = "Error de configuración.";
                return View("~/Vista/Index.cshtml");
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT u.id_usuario, 
                               COALESCE(p.nombres, 'Personal') as nombres, 
                               COALESCE(p.apellidos, 'Salud') as apellidos,
                               COALESCE(r.nombre, 'Enfermería (Tópico)') as rol_nombre
                        FROM usuarios u
                        LEFT JOIN personas p ON u.id_persona = p.id_persona
                        LEFT JOIN usuarios_roles ur ON u.id_usuario = ur.id_usuario AND ur.estado = 'Activo'
                        LEFT JOIN roles r ON ur.id_rol = r.id_rol
                        WHERE u.correo = @correo AND u.estado = 'Activo'";
                    
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@correo", correo ?? (object)DBNull.Value);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idUsuario = reader.GetInt32(0);
                                string nombres = reader.GetString(1);
                                string apellidos = reader.GetString(2);
                                string rolNombre = reader.GetString(3);
                                reader.Close();

                                string nombreCompleto = $"{nombres} {apellidos}".Trim();
                                HttpContext.Session.SetInt32("UsuarioId", idUsuario);
                                HttpContext.Session.SetString("UsuarioNombre", string.IsNullOrEmpty(nombreCompleto) ? "Lic. Rosa Fernández" : nombreCompleto);
                                HttpContext.Session.SetString("UsuarioRol", rolNombre);

                                string redirectUrl = ObtenerRutaDashboardPorRol(rolNombre);
                                return Redirect(redirectUrl);
                            }
                            else
                            {
                                ViewBag.Error = "Correo no pertenece a la institución o usuario inactivo.";
                                return View("~/Vista/Index.cshtml");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error de conexión: " + ex.Message;
                return View("~/Vista/Index.cshtml");
            }
        }

        [HttpGet("logout")]
        [HttpGet("Login/Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
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

            if (rolNombre.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Administrador";

            if (rolNombre.Contains("Tópico", StringComparison.OrdinalIgnoreCase) || rolNombre.Contains("Enfermer", StringComparison.OrdinalIgnoreCase))
                return "/Dashboard/Topico";

            return "/Dashboard/Topico";
        }
    }
}
