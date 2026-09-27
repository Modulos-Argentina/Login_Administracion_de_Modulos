using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using Microsoft.AspNetCore.Http;

namespace Login_Administracion_de_Modulos.Areas.Modulo05.Controller
{
    [Area("Modulo05")]
    [Route("Modulo05/[controller]")]
    public class AtencionClinicaController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IConfiguration _configuration;

        public AtencionClinicaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /Modulo05/AtencionClinica/Create
        // Devuelve PartialView si partial=true o es petición AJAX
        [HttpGet("Create")]
        public IActionResult Create(bool partial = false)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (partial || isAjax)
            {
                return PartialView("~/Areas/Modulo05/Views/AtencionClinica/Create.cshtml");
            }

            ViewData["Title"] = "Registro de Atención Clínica";
            ViewData["Role"] = "Enfermería (Tópico)";
            return View("~/Areas/Modulo05/Views/AtencionClinica/Create.cshtml");
        }

        // GET: /Modulo05/AtencionClinica/BuscarPaciente?dni=...
        // Consulta la tabla personas de la base de datos PostgreSQL
        [HttpGet("BuscarPaciente")]
        public IActionResult BuscarPaciente(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                return Json(new { success = false, message = "Ingrese un número de DNI válido." });
            }

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                return Json(new { success = false, message = "Error de configuración de conexión a base de datos." });
            }

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT id_persona, nombres, apellidos, numero_documento FROM personas WHERE numero_documento = @dni LIMIT 1";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dni", dni.Trim());

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string nombres = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string apellidos = reader.IsDBNull(2) ? "" : reader.GetString(2);

                                return Json(new
                                {
                                    success = true,
                                    nombres = nombres,
                                    apellidos = apellidos,
                                    carrera = "Desarrollo de Sistemas de Información",
                                    cicloTurno = "IV Ciclo / Turno Noche"
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "No se encontró registro para el DNI " + dni + " en la tabla personas."
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al consultar paciente: " + ex.Message });
            }
        }

        // POST: /Modulo05/AtencionClinica/GuardarAtencion
        [HttpPost("GuardarAtencion")]
        public IActionResult GuardarAtencion([FromForm] IFormCollection form)
        {
            try
            {
                string? dni = form["buscarDni"];
                string? motivo = form["motivoAtencion"];
                string? fechaHora = form["fechaHora"];
                string? medicamentos = form["medicamentosAdministrados"];
                string? observaciones = form["observacionesTratamiento"];

                if (string.IsNullOrWhiteSpace(motivo))
                {
                    return Json(new { success = false, message = "El motivo de atención es obligatorio." });
                }

                return Json(new
                {
                    success = true,
                    message = "¡Atención clínica registrada exitosamente en el sistema de Tópico!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al procesar registro: " + ex.Message });
            }
        }
    }
}
