using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Login_Administracion_de_Modulos.Controlador;

namespace Login_Administracion_de_Modulos.Seguridad
{
    /// <summary>
    /// Filtro de autorización para proteger paneles y módulos.
    /// Valida que exista una sesión activa (autenticada por DNI, Google o Demo Local)
    /// y que el rol del usuario coincida con los permisos requeridos.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ProtegerPanelAttribute : ActionFilterAttribute
    {
        private readonly string[] _rolesPermitidos;

        public ProtegerPanelAttribute(params string[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos ?? Array.Empty<string>();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            int? usuarioId = session.GetInt32("UsuarioId");
            string? usuarioRol = session.GetString("UsuarioRol");
            string? usuarioNombre = session.GetString("UsuarioNombre");

            // Determinar si la petición es AJAX o solicita JSON
            bool esPeticionAjaxOJson =
                context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                (context.HttpContext.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

            // 1. Validar si existe sesión activa
            if (!usuarioId.HasValue || string.IsNullOrWhiteSpace(usuarioRol))
            {
                if (esPeticionAjaxOJson)
                {
                    context.Result = new JsonResult(new { success = false, message = "Sesión no iniciada." })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                    return;
                }

                // Navegación convencional en navegador -> Redirigir al Login
                context.Result = new RedirectResult("/Login/Index");
                return;
            }

            // 2. Si hay roles permitidos definidos, verificar coincidencia
            if (_rolesPermitidos.Length > 0)
            {
                // El rol "Administrador" posee acceso global a todos los módulos y paneles
                bool esAdministrador = usuarioRol.Contains("Administrador", StringComparison.OrdinalIgnoreCase) ||
                                       usuarioRol.Contains("Admin", StringComparison.OrdinalIgnoreCase);

                bool rolAutorizado = esAdministrador || _rolesPermitidos.Any(r =>
                    usuarioRol.Contains(r, StringComparison.OrdinalIgnoreCase) ||
                    r.Contains(usuarioRol, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r, usuarioRol, StringComparison.OrdinalIgnoreCase)
                );

                if (!rolAutorizado)
                {
                    if (esPeticionAjaxOJson)
                    {
                        context.Result = new JsonResult(new { success = false, message = "Acceso denegado: rol no autorizado." })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }

                    // Navegación normal: Redirigir a su propio dashboard correspondiente
                    string rutaPropia = LoginController.ObtenerRutaDashboardPorRol(usuarioRol);
                    context.Result = new RedirectResult(rutaPropia);
                    return;
                }
            }

            // 3. Sesión válida y autorizada: Inyectar datos en ViewBag del controlador
            if (context.Controller is Controller controller)
            {
                string nombre = string.IsNullOrWhiteSpace(usuarioNombre) ? "Usuario Institucional" : usuarioNombre;
                controller.ViewBag.UserName = nombre;
                controller.ViewBag.UserRole = usuarioRol;

                var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                controller.ViewBag.UserInitials = partes.Length >= 2
                    ? $"{partes[0][0]}{partes[1][0]}".ToUpper()
                    : (nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "US");
            }

            base.OnActionExecuting(context);
        }
    }
}
