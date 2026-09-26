using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Login_Administracion_de_Modulos.Model;
using System;

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
                    
                    // 1. Buscamos al usuario por DNI
                    string query = "SELECT id_usuario, clave_cifrada FROM usuarios WHERE dni_login = @dni AND estado = 'Activo'";
                    
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dni", dni ?? (object)DBNull.Value);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idUsuario = reader.GetInt32(0);
                                string claveCifradaDb = reader.GetString(1);
                                
                                // 2. Comparamos contraseñas (Idealmente usando BCrypt.Net u otra librería de hashing)
                                // Ejemplo si usaras BCrypt: bool esValida = BCrypt.Net.BCrypt.Verify(clave, claveCifradaDb);
                                bool esValida = (clave == claveCifradaDb); // Comparación simple temporal

                                if (esValida)
                                {
                                    reader.Close(); // Cerramos el reader para hacer el INSERT
                                    
                                    // 3. Generar token y guardar sesión
                                    string tokenAcceso = Guid.NewGuid().ToString(); // Token simulado
                                    string insertSession = "INSERT INTO sesiones_login (id_usuario, token, metodo) VALUES (@id, @token, 'DNI_LOCAL')";
                                    
                                    using (var cmdSession = new NpgsqlCommand(insertSession, connection))
                                    {
                                        cmdSession.Parameters.AddWithValue("@id", idUsuario);
                                        cmdSession.Parameters.AddWithValue("@token", tokenAcceso);
                                        
                                        // Ejecutar si la tabla sesiones_login existe:
                                        // cmdSession.ExecuteNonQuery();
                                    }

                                    return RedirectToAction("Bienvenido");
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
        [HttpPost("login-google")]
        public IActionResult LoginGoogle(string correo)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                return Json(new { success = false, message = "Error de configuración." });
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // 1. Validar existencia del correo institucional, ignorando contraseña
                    string query = "SELECT id_usuario FROM usuarios WHERE correo = @correo AND estado = 'Activo'";
                    
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@correo", correo ?? (object)DBNull.Value);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idUsuario = reader.GetInt32(0);
                                reader.Close();
                                
                                // 2. Generar token directamente y guardar sesión
                                string tokenAcceso = Guid.NewGuid().ToString();
                                string insertSession = "INSERT INTO sesiones_login (id_usuario, token, metodo) VALUES (@id, @token, 'GOOGLE_SSO')";
                                
                                using (var cmdSession = new NpgsqlCommand(insertSession, connection))
                                {
                                    cmdSession.Parameters.AddWithValue("@id", idUsuario);
                                    cmdSession.Parameters.AddWithValue("@token", tokenAcceso);
                                    
                                    // Ejecutar si la tabla sesiones_login existe:
                                    // cmdSession.ExecuteNonQuery();
                                }

                                // Si la llamada se hizo por AJAX/Fetch desde Google, devolvemos JSON:
                                return Json(new { success = true, redirectUrl = "/Login/Bienvenido" });
                                // O si es formulario: return RedirectToAction("Bienvenido");
                            }
                            else
                            {
                                return Json(new { success = false, message = "Correo no pertenece a la institución o usuario inactivo." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error de conexión: " + ex.Message });
            }
        }

        public IActionResult Bienvenido()
        {
            return Content("¡Bienvenido al sistema!");
        }
    }
}
