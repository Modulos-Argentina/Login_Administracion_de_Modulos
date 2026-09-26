using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Login_Administracion_de_Modulos.Model;

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

        [HttpPost]
        public IActionResult Index(string correo, string clave)
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
                    
                    string query = "SELECT id_usuario, estado FROM usuarios WHERE correo = @correo AND clave_cifrada = @clave";
                    
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@correo", correo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@clave", clave ?? (object)DBNull.Value);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var usuario = new Usuario
                                {
                                    IdUsuario = reader.GetInt32(0),
                                    Estado = reader.GetString(1)
                                };

                                if (usuario.Estado == "Activo")
                                {
                                    return RedirectToAction("Bienvenido");
                                }
                                else
                                {
                                    ViewBag.Error = "La cuenta de usuario no está activa.";
                                    return View("~/Vista/Index.cshtml");
                                }
                            }
                            else
                            {
                                ViewBag.Error = "Usuario o contraseña incorrectos.";
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

        public IActionResult Bienvenido()
        {
            return Content(""); // Redirige a una página en blanco según los requerimientos
        }
    }
}
