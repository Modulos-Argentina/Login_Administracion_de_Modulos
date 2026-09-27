using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

namespace Login_Administracion_de_Modulos.Controlador
{
    public class DashboardController : Controller
    {
        [HttpGet("/Dashboard/Topico")]
        public IActionResult Topico()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UsuarioNombre") ?? "Lic. Rosa Fernández";
            ViewBag.UserRole = HttpContext.Session.GetString("UsuarioRol") ?? "Enfermería (Tópico)";
            
            string name = ViewBag.UserName as string ?? "Rosa Fernández";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            ViewBag.UserInitials = parts.Length >= 2 
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper() 
                : (name.Length > 0 ? name[0].ToString().ToUpper() : "RF");

            return View("~/Vista/Dashboard/Topico.cshtml");
        }

        [HttpGet("/Dashboard/Psicologia")]
        public IActionResult Psicologia()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UsuarioNombre") ?? "Lic. Juan Psicólogo";
            ViewBag.UserRole = HttpContext.Session.GetString("UsuarioRol") ?? "Psicología";
            return View("~/Areas/Modulo05/Views/psicologia.cshtml");
        }

        [HttpGet("/Dashboard/Docente")]
        public IActionResult Docente()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UsuarioNombre") ?? "Prof. Carlos Docente";
            ViewBag.UserRole = HttpContext.Session.GetString("UsuarioRol") ?? "Docente";
            return View("~/Vista/Dashboard/Docente.cshtml");
        }

        [HttpGet("/Dashboard/Soporte")]
        public IActionResult Soporte()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UsuarioNombre") ?? "Ing. Mario Soporte";
            ViewBag.UserRole = HttpContext.Session.GetString("UsuarioRol") ?? "Jefe de Soporte";
            return View("~/Vista/Dashboard/Soporte.cshtml");
        }
    }
}
