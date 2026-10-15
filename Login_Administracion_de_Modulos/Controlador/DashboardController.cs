using Microsoft.AspNetCore.Mvc;
using Login_Administracion_de_Modulos.Seguridad;

namespace Login_Administracion_de_Modulos.Controlador
{
    public class DashboardController : Controller
    {
        [HttpGet("/Dashboard/Topico")]
        [ProtegerPanel("Enfermería (Tópico)", "Tópico", "Enfermeria")]
        public IActionResult Topico()
        {
            return View("~/Vista/Dashboard/Topico.cshtml");
        }

        [HttpGet("/Dashboard/Psicologia")]
        [ProtegerPanel("Psicología", "Psicologia")]
        public IActionResult Psicologia()
        {
            return View("~/Vista/Dashboard/Psicologia.cshtml");
        }

        [HttpGet("/Dashboard/Docente")]
        [ProtegerPanel("Docente", "Profesor")]
        public IActionResult Docente()
        {
            return View("~/Vista/Dashboard/Docente.cshtml");
        }

        [HttpGet("/Dashboard/Soporte")]
        [ProtegerPanel("Jefe de Soporte", "Soporte")]
        public IActionResult Soporte()
        {
            return View("~/Vista/Dashboard/Soporte.cshtml");
        }

        [HttpGet("/Dashboard/Alumno")]
        [ProtegerPanel("Alumno", "Estudiante")]
        public IActionResult Alumno()
        {
            return View("~/Vista/Dashboard/Alumno.cshtml");
        }

        [HttpGet("/Dashboard/Practicante")]
        [ProtegerPanel("Practicante")]
        public IActionResult Practicante()
        {
            return View("~/Vista/Dashboard/Practicante.cshtml");
        }

        [HttpGet("/Dashboard/Secretaria")]
        [ProtegerPanel("Secretaría Académica", "Secretaria")]
        public IActionResult Secretaria()
        {
            return View("~/Vista/Dashboard/Secretaria.cshtml");
        }

        [HttpGet("/Dashboard/Administrador")]
        [ProtegerPanel("Administrador", "Admin")]
        public IActionResult Administrador()
        {
            return View("~/Vista/Dashboard/Administrador.cshtml");
        }

        [HttpGet("/Dashboard/JefeGeneral")]
        [HttpGet("/Dashboard/Jefe")]
        [ProtegerPanel("Jefe General", "Almacen")]
        public IActionResult JefeGeneral()
        {
            return View("~/Vista/Dashboard/JefeGeneral.cshtml");
        }

        [HttpGet("/Dashboard/JefeComputacion")]
        [HttpGet("/Dashboard/TallerComputacion")]
        [ProtegerPanel("Jefe de Taller Computación", "Computacion")]
        public IActionResult JefeComputacion()
        {
            return View("~/Vista/Dashboard/JefeComputacion.cshtml");
        }

        [HttpGet("/Dashboard/JefeAdministracion")]
        [HttpGet("/Dashboard/TallerAdministracion")]
        [ProtegerPanel("Jefe de Taller Administración", "Administracion")]
        public IActionResult JefeAdministracion()
        {
            return View("~/Vista/Dashboard/JefeAdministracion.cshtml");
        }

        [HttpGet("/Dashboard/JefeContabilidad")]
        [HttpGet("/Dashboard/TallerContabilidad")]
        [ProtegerPanel("Jefe de Taller Contabilidad", "Contabilidad")]
        public IActionResult JefeContabilidad()
        {
            return View("~/Vista/Dashboard/JefeContabilidad.cshtml");
        }

        [HttpGet("/Dashboard/EncargadoInventario")]
        [HttpGet("/Dashboard/Inventario")]
        [ProtegerPanel("Encargado de Inventario", "Inventario")]
        public IActionResult EncargadoInventario()
        {
            return View("~/Vista/Dashboard/EncargadoInventario.cshtml");
        }

        [HttpGet("/Dashboard/Tesoreria")]
        [HttpGet("/Dashboard/Tesorero")]
        [ProtegerPanel("Tesorería", "Tesoreria")]
        public IActionResult Tesoreria()
        {
            return View("~/Vista/Dashboard/Tesoreria.cshtml");
        }
    }
}
