using DRR.Core.DBEmpresaEjemplo.Enumerados;
using LibreriaBase.Areas.Visor;
using LibreriaBase.Clases;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Visor.Controllers
{
    [Area("Visor")]
    [Route("[controller]/[action]")]
    public class VisorController : Controller
    {
        #region Variables
        private IServicioVisor _servicioVisor;
        private SessionAcceso _session;
        #endregion


        #region Constructor
        public VisorController(IServicioVisor servicioVisor, IHttpContextAccessor httpContextAccessor)
        {
            //Se obtiene el 
            _servicioVisor = servicioVisor;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _servicioVisor.DatosSistema = _session.Sistema;
        }
        #endregion





        public ActionResult Index()
        {
            return View();
        }

        public ActionResult VerPublicidad()
        {
            var lista = _servicioVisor.Listar((Int32)EnumPublicidad_PublicidadTipoId.Imagen_Interna);
            //var listaVideo = _servicioVisor.Listar((Int32)EnumPublicidad_PublicidadTipoId.Video_Interno);

            return View(lista);
        }


        public IActionResult Publicidades()
        {
            var lista = _servicioVisor.Listar((Int32)EnumPublicidad_PublicidadTipoId.Imagen_Interna);
            return View(lista);
        }

        public ActionResult VerOfertas()
        {
            String error = "";
            var lista = _servicioVisor.Listar_Ofertas_Sql(100, 1, null, out error);

            return View(lista);
        }


        public ActionResult VerNoticias()
        {
            String error = "";
            var lista = _servicioVisor.Listar_Noticias_Dos(100, 1, null, out error);

            return View(lista);
        }



        public ActionResult VerVideos()
        {
            List<ViewVisorContenido> lista = new List<ViewVisorContenido>();
            lista.Add(new ViewVisorContenido { RutaVideo = "https://www.youtube.com/watch?v=JdYvvYEkP-0&t=475s" });
            lista.Add(new ViewVisorContenido { RutaVideo = "https://www.youtube.com/watch?v=TX-Qn-Yh5p0" });
            lista.Add(new ViewVisorContenido { RutaVideo = "https://www.youtube.com/watch?v=6xUnSVTh8fI" });
            return View(lista);
        }
    }
}
