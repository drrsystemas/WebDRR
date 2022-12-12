using LibreriaBase.Areas.Carrito;
using LibreriaBase.Areas.MetodoPago;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using LibreriaCoreDRR.Decidir;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.MetodoPago.Controllers
{
    [Area("MetodoPago")]
    [Route("[controller]/[action]")]
    public class DecidirController : Controller
    {

        #region Variables
        private readonly IRepositorioEmpresa _repositorioEmpresa;
        SessionAcceso _session;
        private LibreriaBase.Areas.Carrito.Clases.Carrito _carrito;
        #endregion


        #region Constructor
        public DecidirController(IRepositorioEmpresa repositorioEmpresa, IHttpContextAccessor httpContextAccessor, LibreriaBase.Areas.Carrito.Clases.Carrito carritoServicio)
        {
            _repositorioEmpresa = repositorioEmpresa;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioEmpresa.DatosSistema = _session.Sistema;

            _carrito = carritoServicio;
        }





        #endregion

        #region Formulario Datos


        public IActionResult FormularioDatos(ViewModelDatosTarjeta datosPago)
        {
            if (datosPago?.DatosTarjeta?.TokenAutorizacion == null)
            {
                datosPago = new ViewModelDatosTarjeta();
                String error = "";

                //var listaOFP = _repositorioEmpresa.ListaOperacionesFormaPagoDesgloce(null, out error);
                datosPago.ListaFormasPago = _repositorioEmpresa.ListarFormaPago_Tarjeta(out error);


                return View(datosPago);

            }
            else
            {
                return View(datosPago);
            }

        }




        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GuardarDatosTarjeta(ViewModelDatosTarjeta datosPago)
        {

            #region Validaciones para chequear que es lo que el cliente dice


            #endregion


            if (_carrito.Pago == null)
            {
                ViewPago pago = new ViewPago();
                pago.IdPago = 5;//Por el momento despues se tiene que seleccionar de la lista de pagos.
                pago.NombreIdPago = "Tarjeta Credito/Debito";
                pago.DatosTarjeta = datosPago.DatosTarjeta;
                _carrito.Pago = pago;
            }
            else
            {
                _carrito.Pago.DatosTarjeta = datosPago.DatosTarjeta;
            }

            _carrito.Guardar();

            return RedirectToAction("FormularioPago", "Decidir");


        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult BinTarjeta(String bin)
        {



            String error = "";
            Int32 apiBin = Convert.ToInt32(bin);

            var datosDecidir = LibreriaCoreDRR.Decidir.LibreriaDecidir.ConfiguracionDatosEmpresas().
    FirstOrDefault(c => c.IdEmpresa == _session.Sistema.EmpresaId);

            LibreriaCoreDRR.Decidir.LibreriaDecidir libreriaDecidir = new LibreriaCoreDRR.Decidir.LibreriaDecidir(datosDecidir);

            var request = libreriaDecidir.GetDatosTarjeta(apiBin, out error);

            String data = "";

            if (request != null)
            {
                data = request.bank.name + " " + request.country.name + " " + request.type + " " + request.scheme;
            }


            return new JsonResult(data);
        }

        #endregion

        #region Formulario Pago


        public IActionResult FormularioPago()
        {
            ViewModelFormaPagoTarjeta viewModel = new ViewModelFormaPagoTarjeta();
            viewModel.DatosPago = new DatosPago();

            String error = "";


            ///Forma de pago desgloce para tener mas claridad hay que generar un patron fijo.
            /*
             DEBITO - lo que es debito.
             MERCADO PAGO - para mercado pago - si hay o no incremento al utilizar dicho medio de pago.
             */
            bool credito = true;
            if (_carrito.Pago.DatosTarjeta.TarjetaTipo != 1)
            {
                credito = false;
            }

            viewModel.FormasPago = _repositorioEmpresa.FormaPago_Tarjeta_Desgloce(_carrito.Pago.DatosTarjeta.FormaPagoId, credito, out error);

            if (viewModel?.FormasPago?.Desgloce?.Count() > 0)
            {
                viewModel.FormasPago.Desgloce = viewModel.FormasPago.Desgloce.Where(c => c.Cuota > 0).ToList();

                foreach (var item in viewModel.FormasPago.Desgloce)
                {
                    String data = "";

                    Decimal nuevoTotal = _carrito.TotalCarrito().IncrementarImporte(item.Interes);
                    item.Total = nuevoTotal;

                    if (item.Cuota == 1)
                    {
                        data = " de : " + nuevoTotal.FormatoMoneda();
                        item.ImporteCuota = nuevoTotal;
                    }
                    else
                    {
                        decimal subT = nuevoTotal / item.Cuota;
                        item.ImporteCuota = subT;
                        data = " de : " + subT.FormatoMoneda() + "  -- Total de: " + nuevoTotal.FormatoMoneda();
                    }

                    item.Descripcion += " " + data;
                }
            }



            viewModel.DatosPago.TotalOperacion = _carrito.TotalCarrito();
            viewModel.DatosPago.NumeroCuotas = 1;



            return View(viewModel);
        }


        #endregion


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult RealizarPago(ViewModelFormaPagoTarjeta vmPago)
        {
            #region Token Decidir

            Boolean modoTesting = true;

            var datoTarjeta = _carrito.Pago.DatosTarjeta;

            var datosDecidir = LibreriaCoreDRR.Decidir.LibreriaDecidir.ConfiguracionDatosEmpresas().
    FirstOrDefault(c => c.IdEmpresa == _session.Sistema.EmpresaId);

            if (!String.IsNullOrEmpty(datosDecidir?.KeyPrivada))
            {
                modoTesting = false;
            }

            LibreriaDecidir libreriaDecidir = new LibreriaDecidir(datosDecidir);

            LibreriaDecidir.SolicitudPago.Solicitud solicitud = new LibreriaDecidir.SolicitudPago.Solicitud();

            solicitud.card_number = datoTarjeta?.NumeroTarjeta;
            solicitud.security_code = datoTarjeta?.CodigoSeguridad.ToString();
            solicitud.card_expiration_month = Convert.ToInt32(datoTarjeta.MesVto).ToString("00");
            solicitud.card_expiration_year = Convert.ToInt32(datoTarjeta.AñoVto).ToString("00");
            solicitud.card_holder_identification = new LibreriaDecidir.SolicitudPago.CardHolderIdentification();
            solicitud.card_holder_identification.type = "dni";
            solicitud.card_holder_identification.number = datoTarjeta.NumeroDocumento;
            solicitud.card_holder_name = datoTarjeta.Titular;


            var respuestaToken = libreriaDecidir.GenerarTokenPago(solicitud, modoTesting);

            #endregion

            #region Realizar el Pago

            IFormCollection collection = this.HttpContext.Request.Form;
            String json = collection["desgleJson"];
            List<ViewFormaPagoDesgloce> lisDesgloce = json.ToObsect<List<ViewFormaPagoDesgloce>>();

            ViewFormaPagoDesgloce opSeleccionada = lisDesgloce?.FirstOrDefault(c => c.FormaPagoDesgloseId == vmPago.DatosPago.IdTransaccion);


            IRepositorioPedido repositorioPedido = new RepositorioPedido();
            repositorioPedido.DatosSistema = _session.Sistema;
            int idProxPedido = repositorioPedido.Prox_PedidoID();

            int codigoEvitaErrores = DateTime.Now.FechaHs_Argentina().Hour + DateTime.Now.FechaHs_Argentina().Minute;

            LibreriaDecidir.EjecucionPago.Solicitud pago = new LibreriaDecidir.EjecucionPago.Solicitud();

            pago.site_transaction_id = "Pedido_Web_" + idProxPedido + "_" + codigoEvitaErrores;

            pago.token = respuestaToken.id;
            pago.payment_type = "single";

            //Esto esta relacionado con los metodos de pago de descidir
            pago.payment_method_id = 1;
            pago.sub_payments = new object[0];


            pago.bin = respuestaToken.bin;
            pago.currency = "ARS";
            //Cuotas
            pago.installments = opSeleccionada.Cuota;//2 cuotas

            Decimal dato = Math.Round(opSeleccionada.Total, 2);
            String datoNumero = dato.ToString();
            String formato = datoNumero.ToString().Replace(",", "");
            Int32 impoteFormateado = 0;

            Boolean ok = Int32.TryParse(formato, out impoteFormateado);

            //Importe
            pago.amount = impoteFormateado;//100 pesos - Importe total.
                                           // pago.sub_payments = new Array[0];
            #endregion

            var reqPAgo = libreriaDecidir.EfectuarPago(pago, modoTesting);

            if (reqPAgo != null)
            {
                if (reqPAgo.status == "approved")
                {
                    _carrito.Pago.DatosPago = "Estado: " + reqPAgo.status + " - Ticket: " + reqPAgo.status_details.ticket +
                        " - Transacción: " + reqPAgo.site_transaction_id + " - Id: " + reqPAgo.id + " - Tarjeta: " + reqPAgo.card_brand + " - Bin: " + reqPAgo.bin;

                    _carrito.Guardar();

                    return RedirectToAction("FinalizarPedido", "Pedido");
                }
                else
                {
                    TempData["ErrorRepresentada"] = "El Pago fue CANCELADO";

                    return RedirectToAction("RegistrarPedido", "Pedido");

                }
            }
            else
            {
                TempData["ErrorRepresentada"] = "El Pago fue CANCELADO";

                return RedirectToAction("RegistrarPedido", "Pedido");
            }


        }





        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult FormularioDatosTarjeta(DatosPago datosPago)
        {

            //if (datosPago?.DatosTarjeta?.TokenAutorizacion == null)
            //{
            //    datosPago = new DatosPago();
            //    String error = "";

            //    //var listaOFP = _repositorioEmpresa.ListaOperacionesFormaPagoDesgloce(null, out error);
            //    datosPago.ListaFormasPago = _repositorioEmpresa.ListarFormaPago_Tarjeta(out error);


            return PartialView("_datosTarjetaCredito", datosPago);

            //}
            //else
            //{
            //    return PartialView("_datosTarjetaCredito", datosPago);
            //}

        }




        /// <summary>
        /// Muestra los movimientos de la tarjeta.
        /// </summary>
        /// <returns></returns>
        public IActionResult VerMovimientos()
        {

            Boolean modoTesting = true;

            var datosDecidir = LibreriaCoreDRR.Decidir.LibreriaDecidir.ConfiguracionDatosEmpresas().FirstOrDefault(c => c.IdEmpresa == _session.Sistema.EmpresaId);

            if (!String.IsNullOrEmpty(datosDecidir?.KeyPrivada))
            {
                modoTesting = false;
            }

            LibreriaDecidir libreriaDecidir = new LibreriaDecidir(datosDecidir);

            LibreriaDecidir.OperacionesTransacciones.ListaPagos lista = libreriaDecidir.GetPagos(null, modoTesting);

            List<Payment> respuesta = libreriaDecidir.CastResult(lista.results);


            return View(respuesta);

        }

    }
}