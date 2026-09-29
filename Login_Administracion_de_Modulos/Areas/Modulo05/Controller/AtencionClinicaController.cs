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
                string? fechaHoraStr = form["fechaHora"];
                string? medicamentos = form["medicamentosAdministrados"];
                string? observaciones = form["observacionesTratamiento"];
                string? tipoSeguroSalud = form["tipoSeguroSalud"];
                bool esAlergico = !string.IsNullOrEmpty(form["reportaAlergias"]);
                string? detalleAlergia = form["especificarAlergias"];

                if (string.IsNullOrWhiteSpace(dni))
                {
                    return Json(new { success = false, message = "Debe buscar y validar un DNI primero." });
                }

                if (string.IsNullOrWhiteSpace(motivo))
                {
                    return Json(new { success = false, message = "El motivo de atención es obligatorio." });
                }

                string? connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Obtener id_persona
                    int idPersona = 0;
                    using (var cmdPersona = new NpgsqlCommand("SELECT id_persona FROM personas WHERE numero_documento = @dni LIMIT 1", connection))
                    {
                        cmdPersona.Parameters.AddWithValue("@dni", dni.Trim());
                        var result = cmdPersona.ExecuteScalar();
                        if (result == null)
                        {
                            return Json(new { success = false, message = "El DNI no está registrado en el sistema." });
                        }
                        idPersona = Convert.ToInt32(result);
                    }

                    // Insertar atención de tópico
                    string queryInsert = @"
                        INSERT INTO atenciones_topico 
                        (persona_id, atendido_por_id, fecha_hora, motivo, es_alergico, detalle_alergia, seguro_salud, observacion)
                        VALUES (@persona_id, @atendido_por_id, @fecha_hora, @motivo, @es_alergico, @detalle_alergia, @seguro_salud, @observacion)";

                    using (var cmdInsert = new NpgsqlCommand(queryInsert, connection))
                    {
                        cmdInsert.Parameters.AddWithValue("@persona_id", idPersona);
                        cmdInsert.Parameters.AddWithValue("@atendido_por_id", 1); // Mocked user ID
                        
                        DateTime fechaHora = DateTime.Now;
                        if (!string.IsNullOrEmpty(fechaHoraStr) && DateTime.TryParse(fechaHoraStr, out DateTime parsedDate))
                        {
                            fechaHora = parsedDate;
                        }
                        cmdInsert.Parameters.AddWithValue("@fecha_hora", fechaHora);
                        cmdInsert.Parameters.AddWithValue("@motivo", motivo);
                        cmdInsert.Parameters.AddWithValue("@es_alergico", esAlergico);
                        cmdInsert.Parameters.AddWithValue("@detalle_alergia", string.IsNullOrEmpty(detalleAlergia) ? (object)DBNull.Value : detalleAlergia);
                        cmdInsert.Parameters.AddWithValue("@seguro_salud", string.IsNullOrEmpty(tipoSeguroSalud) ? (object)DBNull.Value : tipoSeguroSalud);
                        
                        string observacionFinal = observaciones ?? "";
                        if (!string.IsNullOrWhiteSpace(medicamentos)) 
                        {
                            observacionFinal = "Medicamentos: " + medicamentos + "\n" + observacionFinal;
                        }

                        cmdInsert.Parameters.AddWithValue("@observacion", string.IsNullOrEmpty(observacionFinal) ? (object)DBNull.Value : observacionFinal);

                        cmdInsert.ExecuteNonQuery();
                    }
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
        // GET: /Modulo05/AtencionClinica/Historial?dni=...
        [HttpGet("Historial")]
        public IActionResult Historial(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                return Json(new { success = false, message = "DNI no proporcionado." });
            }

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT a.fecha_hora, a.motivo, a.seguro_salud, a.observacion, a.es_alergico, a.detalle_alergia
                        FROM atenciones_topico a
                        INNER JOIN personas p ON a.persona_id = p.id_persona
                        WHERE p.numero_documento = @dni
                        ORDER BY a.fecha_hora DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@dni", dni.Trim());
                        using (var reader = command.ExecuteReader())
                        {
                            var atenciones = new System.Collections.Generic.List<object>();
                            while (reader.Read())
                            {
                                atenciones.Add(new
                                {
                                    fechaHora = reader.IsDBNull(0) ? "" : reader.GetDateTime(0).ToString("dd/MM/yyyy HH:mm"),
                                    motivo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    seguroSalud = reader.IsDBNull(2) ? "Ninguno" : reader.GetString(2),
                                    observacion = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    esAlergico = reader.IsDBNull(4) ? false : reader.GetBoolean(4),
                                    detalleAlergia = reader.IsDBNull(5) ? "" : reader.GetString(5)
                                });
                            }
                            return Json(new { success = true, data = atenciones });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al consultar historial: " + ex.Message });
            }
        }
    }
}
