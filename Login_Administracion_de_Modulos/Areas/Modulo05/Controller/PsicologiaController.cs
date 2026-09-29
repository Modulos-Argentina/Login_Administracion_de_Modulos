using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using Microsoft.AspNetCore.Http;

namespace Login_Administracion_de_Modulos.Areas.Modulo05.Controller
{
    [Area("Modulo05")]
    [Route("Modulo05/[controller]")]
    public class PsicologiaController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IConfiguration _configuration;

        public PsicologiaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /Modulo05/Psicologia/BuscarPaciente?dni=...
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
                                    message = "No se encontró registro para el DNI " + dni + " en la base de datos."
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

        // POST: /Modulo05/Psicologia/GuardarAtencion
        [HttpPost("GuardarAtencion")]
        public IActionResult GuardarAtencion([FromForm] IFormCollection form)
        {
            try
            {
                string? dni = form["dniBusqueda"];
                string? fechaHoraStr = form["fechaHora"];
                string? estadoClinico = form["estadoClinico"];
                string? motivoConsulta = form["motivoConsulta"];
                string? diagnosticoNotas = form["diagnosticoNotas"];

                if (string.IsNullOrWhiteSpace(dni))
                {
                    return Json(new { success = false, message = "Debe buscar y validar un DNI primero." });
                }

                if (string.IsNullOrWhiteSpace(motivoConsulta))
                {
                    return Json(new { success = false, message = "El motivo de consulta es obligatorio." });
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

                    // Insertar atención psicológica
                    string queryInsert = @"
                        INSERT INTO atenciones_psicologia 
                        (persona_id, psicologo_id, fecha_hora, motivo_consulta, observacion_notas, estado_atencion)
                        VALUES (@persona_id, @psicologo_id, @fecha_hora, @motivo, @notas, @estado)";

                    using (var cmdInsert = new NpgsqlCommand(queryInsert, connection))
                    {
                        cmdInsert.Parameters.AddWithValue("@persona_id", idPersona);
                        cmdInsert.Parameters.AddWithValue("@psicologo_id", 1); // Mocked user ID
                        
                        DateTime fechaHora = DateTime.Now;
                        if (!string.IsNullOrEmpty(fechaHoraStr) && DateTime.TryParse(fechaHoraStr, out DateTime parsedDate))
                        {
                            fechaHora = parsedDate;
                        }
                        cmdInsert.Parameters.AddWithValue("@fecha_hora", fechaHora);
                        cmdInsert.Parameters.AddWithValue("@motivo", motivoConsulta);
                        cmdInsert.Parameters.AddWithValue("@notas", string.IsNullOrEmpty(diagnosticoNotas) ? (object)DBNull.Value : diagnosticoNotas);
                        cmdInsert.Parameters.AddWithValue("@estado", string.IsNullOrEmpty(estadoClinico) ? (object)DBNull.Value : estadoClinico);

                        cmdInsert.ExecuteNonQuery();
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "¡Atención psicológica registrada exitosamente!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al procesar registro: " + ex.Message });
            }
        }
        // GET: /Modulo05/Psicologia/Historial?dni=...
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
                        SELECT a.fecha_hora, a.motivo_consulta, a.estado_atencion, a.observacion_notas
                        FROM atenciones_psicologia a
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
                                    estado = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    notas = reader.IsDBNull(3) ? "" : reader.GetString(3)
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
