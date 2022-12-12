using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Carrito.Controllers
{
    [Area("Carrito")]
    [Route("[controller]/[action]")]
    public class MarcaController : Controller
    {
        #region Variables
        private IRepositorioProducto _repositorioProducto;
        SessionAcceso _session;

        #endregion


        #region Constructor
        public MarcaController(IRepositorioProducto repositorioProducto, IHttpContextAccessor httpContextAccessor)
        {


            _repositorioProducto = repositorioProducto;
            //verificar sesion
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioProducto.DatosSistema = _session?.Sistema;

            if (_session.Sistema.TipoEmpresa == 256 || _session.Sistema.TipoEmpresa == 512)
            {
                _repositorioProducto.ElementosPorPagina = 48;
            }
            else
            {
                _repositorioProducto.ElementosPorPagina = 24;
            }

        }
        #endregion

        public IActionResult Listado()
        {

            var listaMarcas = _repositorioProducto.ListaMarcasQueTienenProductosWeb_conImagen(null);

            return View(listaMarcas);
        }
    }
}