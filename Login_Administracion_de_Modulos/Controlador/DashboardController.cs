using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

namespace Login_Administracion_de_Modulos.Controlador
{
    public class DashboardController : Controller
    {
        private void CargarDatosSesion(string nombrePorDefecto, string rolPorDefecto)
        {
            string? userName = HttpContext.Session.GetString("UsuarioNombre");
            string? userRole = HttpContext.Session.GetString("UsuarioRol");

            if (string.IsNullOrEmpty(userName))
            {
                userName = nombrePorDefecto;
                HttpContext.Session.SetString("UsuarioNombre", userName);
            }

            if (string.IsNullOrEmpty(userRole))
            {
                userRole = rolPorDefecto;
                HttpContext.Session.SetString("UsuarioRol", userRole);
            }

            if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
            {
                HttpContext.Session.SetInt32("UsuarioId", 999);
                HttpContext.Session.SetString("EsDemo", "true");
            }

            ViewBag.UserName = userName;
            ViewBag.UserRole = userRole;

            var parts = userName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            ViewBag.UserInitials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : (userName.Length > 0 ? userName[0].ToString().ToUpper() : "US");
        }

        [HttpGet("/Dashboard/Topico")]
        public IActionResult Topico()
        {
            CargarDatosSesion("Lic. Rosa Fernández", "Enfermería (Tópico)");
            return View("~/Vista/Dashboard/Topico.cshtml");
        }

        [HttpGet("/Dashboard/Psicologia")]
        public IActionResult Psicologia()
        {
            CargarDatosSesion("Lic. Juan Psicólogo", "Psicología");
            return View("~/Vista/Dashboard/Psicologia.cshtml");
        }

        [HttpGet("/Dashboard/Docente")]
        public IActionResult Docente()
        {
            CargarDatosSesion("Prof. Carlos Docente", "Docente");
            return View("~/Vista/Dashboard/Docente.cshtml");
        }

        [HttpGet("/Dashboard/Soporte")]
        public IActionResult Soporte()
        {
            CargarDatosSesion("Ing. Mario Soporte", "Jefe de Soporte");
            return View("~/Vista/Dashboard/Soporte.cshtml");
        }

        [HttpGet("/Dashboard/Alumno")]
        public IActionResult Alumno()
        {
            CargarDatosSesion("Estudiante Ana Torres", "Alumno");
            return View("~/Vista/Dashboard/Alumno.cshtml");
        }

        [HttpGet("/Dashboard/Practicante")]
        public IActionResult Practicante()
        {
            CargarDatosSesion("Téc. Luis Practicante", "Practicante");
            return View("~/Vista/Dashboard/Practicante.cshtml");
        }

        [HttpGet("/Dashboard/Secretaria")]
        public IActionResult Secretaria()
        {
            CargarDatosSesion("Lic. Carmen Secretaría", "Secretaría Académica");
            return View("~/Vista/Dashboard/Secretaria.cshtml");
        }

        [HttpGet("/Dashboard/Administrador")]
        public IActionResult Administrador()
        {
            CargarDatosSesion("Administrador del Sistema", "Administrador");
            return View("~/Vista/Dashboard/Administrador.cshtml");
        }
    }
}
