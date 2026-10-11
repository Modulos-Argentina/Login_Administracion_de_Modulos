using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Login_Administracion_de_Modulos.Areas.Modulo05.Models;

namespace Login_Administracion_de_Modulos.Areas.Modulo05.Controller
{
    [Area("Modulo05")]
    [Route("Modulo05/[controller]")]
    public class PsicologiaController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ExpedientePsicoRepository _repo;

        public PsicologiaController(IConfiguration configuration)
        {
            _configuration = configuration;
            string conn = _configuration.GetConnectionString("DefaultConnection") ?? "";
            _repo = new ExpedientePsicoRepository(conn);
        }

        // GET: /Modulo05/Psicologia o /Modulo05/Psicologia/Panel
        [HttpGet("")]
        [HttpGet("Panel")]
        public IActionResult Panel()
        {
            ViewData["Title"] = "Bienestar Psicológico";
            ViewData["Role"] = "Psicología";
            return View("~/Areas/Modulo05/Views/psicologia/panel_psicologia.cshtml");
        }

        // GET: /Modulo05/Psicologia/FormSesion o /Modulo05/Psicologia/Create
        [HttpGet("Create")]
        [HttpGet("FormSesion")]
        public IActionResult FormSesion(bool partial = false)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            ViewData["Title"] = "Nueva Sesión Psicológica";
            ViewData["Role"] = "Psicología";

            if (partial || isAjax)
            {
                return PartialView("~/Areas/Modulo05/Views/psicologia/form_sesion.cshtml");
            }

            return View("~/Areas/Modulo05/Views/psicologia/form_sesion.cshtml");
        }

        // GET: /Modulo05/Psicologia/ExpedienteAlumno
        [HttpGet("ExpedienteAlumno")]
        public IActionResult ExpedienteAlumno(string? dni = null)
        {
            ViewData["Title"] = "Expediente Confidencial del Alumno";
            ViewData["Role"] = "Psicología";
            var historial = !string.IsNullOrWhiteSpace(dni) 
                ? _repo.ObtenerExpedientePorDni(dni) 
                : new List<SesionPsicologicaItem>();

            ViewBag.DniBuscado = dni;
            return View("~/Areas/Modulo05/Views/psicologia/expediente_alumno.cshtml", historial);
        }

        // GET: /Modulo05/Psicologia/BuscarPaciente?dni=...
        [HttpGet("BuscarPaciente")]
        public IActionResult BuscarPaciente(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                return Json(new { success = false, message = "Ingrese un número de DNI válido." });
            }

            try
            {
                var alumno = _repo.BuscarEstudiantePorDni(dni);
                if (alumno != null)
                {
                    return Json(new
                    {
                        success = true,
                        personaId = alumno.PersonaId,
                        nombres = alumno.EstudianteNombres,
                        apellidos = alumno.EstudianteApellidos,
                        carrera = alumno.Carrera,
                        cicloTurno = alumno.CicloTurno
                    });
                }

                return Json(new
                {
                    success = false,
                    message = $"No se encontró registro para el DNI {dni} en la base de datos."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al consultar paciente: " + ex.Message });
            }
        }

        // POST: /Modulo05/Psicologia/GuardarAtencion o /Modulo05/Psicologia/GuardarSesion
        [HttpPost("GuardarAtencion")]
        [HttpPost("GuardarSesion")]
        public IActionResult GuardarAtencion([FromForm] IFormCollection form)
        {
            try
            {
                string? dni = form["dniBusqueda"].ToString();
                if (string.IsNullOrWhiteSpace(dni)) dni = form["dni"].ToString();

                string? motivoConsulta = form["motivoConsulta"].ToString();
                if (string.IsNullOrWhiteSpace(motivoConsulta)) motivoConsulta = form["motivo"].ToString();

                string? fechaHoraStr = form["fechaHora"].ToString();
                string? estadoClinico = form["estadoClinico"].ToString();
                if (string.IsNullOrWhiteSpace(estadoClinico)) estadoClinico = form["estado"].ToString();

                string? diagnosticoNotas = form["diagnosticoNotas"].ToString();
                if (string.IsNullOrWhiteSpace(diagnosticoNotas)) diagnosticoNotas = form["notas"].ToString();

                if (string.IsNullOrWhiteSpace(dni))
                {
                    return Json(new { success = false, message = "Debe buscar y validar un DNI primero." });
                }

                if (string.IsNullOrWhiteSpace(motivoConsulta))
                {
                    return Json(new { success = false, message = "El motivo de consulta es obligatorio." });
                }

                var alumno = _repo.BuscarEstudiantePorDni(dni);
                if (alumno == null)
                {
                    return Json(new { success = false, message = "El DNI no está registrado en el sistema." });
                }

                DateTime fechaHora = DateTime.Now;
                if (!string.IsNullOrEmpty(fechaHoraStr) && DateTime.TryParse(fechaHoraStr, out DateTime parsedDate))
                {
                    fechaHora = parsedDate;
                }

                var sesion = new SesionPsicologicaItem
                {
                    PersonaId = alumno.PersonaId,
                    PsicologoId = 1,
                    FechaHora = fechaHora,
                    MotivoConsulta = motivoConsulta,
                    ObservacionNotas = diagnosticoNotas,
                    EstadoAtencion = string.IsNullOrWhiteSpace(estadoClinico) ? "En Seguimiento" : estadoClinico
                };

                bool ok = _repo.RegistrarSesion(sesion);
                if (ok)
                {
                    return Json(new
                    {
                        success = true,
                        message = "¡Atención psicológica y notas confidenciales registradas exitosamente!"
                    });
                }

                return Json(new { success = false, message = "No se pudo registrar la sesión." });
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

            try
            {
                var sesiones = _repo.ObtenerExpedientePorDni(dni);
                var lista = new List<object>();
                foreach (var s in sesiones)
                {
                    lista.Add(new
                    {
                        fechaHora = s.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                        motivo = s.MotivoConsulta,
                        estado = s.EstadoAtencion ?? "Registrado",
                        notas = s.ObservacionNotas ?? ""
                    });
                }
                return Json(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al consultar historial: " + ex.Message });
            }
        }
    }
}
