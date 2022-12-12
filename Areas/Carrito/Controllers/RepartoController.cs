using LibreriaBase.Areas.Carrito;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Carrito.Controllers
{
    [Area("Carrito")]
    [Route("[controller]/[action]")]
    public class RepartoController : Controller
    {

        #region Variables
        private readonly IRepositorioPedido _repositorioPedido;
        private readonly IRepositorioCliente _repositorioCliente;
        private SessionAcceso _session;
        #endregion

        #region Constructor
        public RepartoController(IRepositorioPedido repositorioPedido, IHttpContextAccessor httpContextAccessor)
        {
            _repositorioPedido = repositorioPedido;

            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioPedido.DatosSistema = _session.Sistema;
            _repositorioPedido.ElementosPorPagina = 25;

            _repositorioCliente = new RepositorioCliente();
            _repositorioCliente.DatosSistema = _session.Sistema;

        }
        #endregion









        // GET: RepartoController
        public ActionResult Index(Generica parametro)
        {
            if (_session.Usuario.NumeroReparto.IsNullOrValue(0))
            {
                if (parametro?.Id == 1)
                {
                    //Selecciona 1 Reparto.
                    ViewData["Seleccionar"] = true;
                }

                var query = _repositorioPedido.ListarRepartos();

                return View(query);
            }
            else
            {
                int numero = (int)_session.Usuario.NumeroReparto;
                return RedirectToAction("DetalleReparto", new { numeroReparto = numero });
            }

        }


        public ActionResult DetalleReparto(Int32 numeroReparto)
        {
            if (numeroReparto == 0)
            {
                numeroReparto = _session.Usuario.NumeroReparto ?? 0;
            }

            if (numeroReparto == 0)
            {
                TempData["ErrorRepresentada"] = "Seleccione 1 numero de reparto.";
                Generica parametro = new Generica() { Id = 1 };
                return RedirectToAction("Index", "Reparto", parametro);
            }
            else
            {
                _session.Usuario.NumeroReparto = (int)numeroReparto;
                _session.GuardarSession(HttpContext);
                //---//
                ViewReparto view = new ViewReparto();
                view.Listado = _repositorioPedido.DetalleReparto(numeroReparto).ToList();

                //query.First().Mov.es
                return View(view);
            }


        }


        public IActionResult abrirFrmEntregarReparto(Int32 id, String detalle)
        {
            ViewEnviarReparto itemView = new ViewEnviarReparto()
            {
                Id = id,
                Observacion = detalle,
                ParametroString = "Entregar Pedido",
                Fecha = new DateTime().FechaHs_Argentina()
            };

            return PartialView("_frmEntregarReparto", itemView);
        }


        [HttpPost]
        public IActionResult MarcarComoEntregado(Int32 id, String detalle, String fecha)
        {
            String infoError = "";
            ViewEnviarReparto item = new ViewEnviarReparto()
            {
                Id = id,
                Fecha = Convert.ToDateTime(fecha),
                Observacion = detalle
            };
            int guardo = _repositorioPedido.Marcar_Reparto_ComoEntregado(item, out infoError);


            return Content(item.Observacion);
        }



        public ActionResult MapaReparto(Int32 numeroReparto)
        {
            if (numeroReparto == 0)
            {
                TempData["ErrorRepresentada"] = "Seleccione 1 numero de reparto.";
                Generica parametro = new Generica() { Id = 1 };
                return RedirectToAction("Index", "Reparto", parametro);
            }
            else
            {
                var query = _repositorioPedido.DetalleReparto(numeroReparto);

                return View(new List<ViewCliente>());
            }


        }


        [HttpPost]
        public IActionResult abrirFrmDetalleFactura(Int32 ventaId)
        {

            try
            {
                var query = _repositorioPedido.DetalleVenta_ModoRemito(ventaId);

                //if(query?.Count()>1)
                //{
                //    query = query.OrderBy(c => c.Item);
                //}

                return PartialView("_tablaDetalleVenta_Remito", query);
            }
            catch (Exception ex)
            {
                return PartialView("");
            }


        }



        public IActionResult SalirReparto()
        {
            _session.Usuario.NumeroReparto = null;
            _session.GuardarSession(HttpContext);

            Generica generica = new Generica();
            generica.Id = 1;

            return RedirectToAction("Index", generica);
        }

    }
}
