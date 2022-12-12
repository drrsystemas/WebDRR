using LibreriaBase.Areas.Home;
using LibreriaBase.Clases;
using Microsoft.AspNetCore.Mvc;
using WebDRR.Areas.Pruebas.Models;

namespace WebDRR.Areas.Pruebas.Controllers
{
    [Area("Pruebas")]
    [Route("[controller]/[action]")]
    public class JugandoController : Controller
    {
        public IActionResult Index()
        {
            ViewJugando viewJugando = new ViewJugando();
            viewJugando.Titulo = "La idea es el empezar a probar codigo con vue.js";
            return View(viewJugando);
        }



        public IActionResult Contacto()
        {
            ViewContacto view = new ViewContacto();
            return View(view);
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult EnviarCorreo(ViewContacto view)
        {


            EnviarCorreoDirecto enviarCorreoDirecto = new EnviarCorreoDirecto(118);
            Boolean correo = enviarCorreoDirecto.enviarEmail(view.CorreoElectronico,"", "Contacto", view.Consulta);


            var routeValues = new RouteValueDictionary
            {
                { "Id", 1 },
                { "Mensaje", "Se envio el correo: "+correo }
            };

            String urlNotificaciones = Url.Action("Notificaciones", "Home", routeValues);

            return Redirect(urlNotificaciones);
        }

    }
}
