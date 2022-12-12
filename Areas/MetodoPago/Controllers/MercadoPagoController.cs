using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.MetodoPago;
using LibreriaBase.Areas.MetodoPago.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using MercadoPago.DataStructures.Preference;
using MercadoPago.Resources;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Controllers
{
    [Area("MetodoPago")]
    [Route("[controller]/[action]")]
    public class MercadoPagoController : Controller
    {


        #region Variables
        private readonly IRepositorioEmpresa _repositorioEmpresa;
        IWebHostEnvironment _environment;
        #endregion


        #region Constructor
        //El constructor inyecta el repositorio de Cliente.
        //En dicho clase esta toda la comunicacion con la base de datos
        public MercadoPagoController(IRepositorioEmpresa repositorioEmpresa, IWebHostEnvironment environment)
        {
            _repositorioEmpresa = repositorioEmpresa;
            _environment = environment;


        }
        #endregion



        private string obtenerJsonEmpresas(out string ruta)
        {
            string[] filePaths = Directory.GetFiles(Path.Combine(_environment.WebRootPath, "files\\"));
            string archivo = Path.GetFileName(filePaths[0]);
            ruta = filePaths[0];

            //Primero hay que lleerlo.
            StreamReader reader = new StreamReader(ruta);
            string documento = reader.ReadToEnd();
            reader.Close();

            return documento;
        }




        public IActionResult Index(Boolean mp, string id, Int32? idEmpresa)
        {


            SessionAcceso session = HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            if (session == null)
            {
                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();


                //El primer ingreso hay que cargar la informacion.
                if (session == null)
                {
                    session = new SessionAcceso();
                    session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
                }
                else
                {
                    if (String.IsNullOrEmpty(session.Sistema?.Cn_Alma))
                    {
                        session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
                    }
                }

                if (idEmpresa == null || idEmpresa == 0)
                {
                    session.Sistema.EmpresaId = (Int32)LibreriaBase.Clases.DRREnviroment.EnumEmpresas.DrrSystemas;
                }
                else
                {
                    session.Sistema.EmpresaId = idEmpresa;
                }


                string ruta, documento;
                documento = obtenerJsonEmpresas(out ruta);
                var empresaDatos = documento?.ToObsect<List<DatosEmpresa>>()?.FirstOrDefault(c => c.IdEmpresa == (Int32)session.Sistema.EmpresaId);

                session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa_V2(empresaDatos.Nombre_BaseDatos);

                HttpContext.Session.SetJson("SessionAcceso", session);
            }

            _repositorioEmpresa.DatosSistema = session.Sistema;


            //Conecta con DrrSystemas


            //tira
            ViewBag.Empresa = session.Sistema.Nombre ?? "DRR Systemas";

            if (mp == true)
            {
                ViewBag.MP = true;
                ViewBag.Id = id;
            }

            ViewDatosPago viewData = new ViewDatosPago();

            return View(viewData);
        }


        //public IActionResult GenerarBoton(String title, String description, String unitPrice)
        public IActionResult GenerarBoton(ViewDatosPago viewData)
        {
            try
            {
                ViewBag.Error = "";


                if (String.IsNullOrEmpty(viewData.NyA))
                {
                    ViewBag.Error = "Ingrese su Nombre y Apellido";
                }


                if (String.IsNullOrEmpty(viewData.Detalle))
                {
                    ViewBag.Error = "Ingrese la descripcion, motivo del pago";
                }


                //if (String.IsNullOrEmpty(viewData.Importe.ToString()))
                //{



                //    ViewBag.Error = "Ingrese el importe";
                //}
                //else
                //{
                //    Boolean numero = viewData.Importe.EsNumerico();

                //    if(numero ==  false)
                //    {
                //        ViewBag.Error = "El formato no es correcto, ingreso un numero como por ejempo 120,50 (ciento veinte con 50 centavos)";
                //    }
                //}

                if (String.IsNullOrEmpty(ViewBag.Error))
                {
                    SessionAcceso session = HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
                    _repositorioEmpresa.DatosSistema = session.Sistema;


                    var lista = _repositorioEmpresa.CuentasMercadoPago();

                    if (lista?.Count() > 0)
                    {
                        MercadoPagoDatos mpd = lista.FirstOrDefault(c => c.Token != "");

                        HttpContext.Session.SetJson("MercadoPagoDatos", mpd);

                        MercadoPago.SDK sdk = new MercadoPago.SDK();

                        // procesar-pago

                        //El token lo necesito para procesar el pago.
                        sdk.ClientId = mpd.ClientId.Trim();//"1487233186955868";
                        sdk.ClientSecret = mpd.ClientSecret.Trim();// "MBz8Cqmd9JyAG1p9CuoC22xK86xDFsP1";

                        // Create a preference object
                        Preference preference = new Preference(sdk);

                        //preference.BackUrls = new BackUrls()
                        //{
                        //    Success = @"https://drrsystemas.azurewebsites.net/MercadoPago/AvisoMercadoPago"
                        //};
                        //preference.AutoReturn = MercadoPago.Common.AutoReturnType.approved;


                        preference.Items.Add(
                          new Item()
                          {

                              Title = viewData.NyA,
                              Description = viewData.Detalle,
                              Quantity = 1,
                              CurrencyId = MercadoPago.Common.CurrencyId.ARS,
                              UnitPrice = Convert.ToDecimal(viewData.Importe.Replace('.', ','))
                          }
                        );


                        preference.Save();
                        Boolean mp = false;
                        if (!String.IsNullOrEmpty(preference.Id))
                        {
                            mp = true;
                        }

                        //return RedirectToAction("Index", new { mp, preference.Id, session.Sistema.EmpresaId });
                        if (!String.IsNullOrEmpty(preference?.InitPoint))
                        {
                            return Redirect(preference.InitPoint);
                        }
                        else
                        {
                            ViewBag.Error = "No se detectaron generar el cupon de pago.";
                            return View();
                        }
                    }
                    else
                    {
                        ViewBag.Error = "No se detectaron cuentas de mercado pago.";
                        return View();
                    }

                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error con los datos de la cuenta de mercado pago.";
                return View();
            }


        }

        public IActionResult PagarQr()
        {
            MercadoPagoDatos mpd = HttpContext.Session.GetJson<MercadoPagoDatos>("MercadoPagoDatos");

            string token = mpd.Token;// "APP_USR-1487233186955868-042811-1f6751e8180bed60005e90421255e63d-120461613";

            LibreriaBase.Api.MercadoPago mercadoPago = new LibreriaBase.Api.MercadoPago(token);

            var req = mercadoPago.ListarPuntosdeVenta();

            LibreriaBase.Api.MercadoPago.PuntoVenta.Result result = req.results[req.paging.total - 1];


            return View(result);
        }

        public IActionResult InformacionPago(Int64 codigoPago)
        {
            MercadoPagoDatos mpd = HttpContext.Session.GetJson<MercadoPagoDatos>("MercadoPagoDatos");

            string token = mpd.Token;// "APP_USR-1487233186955868-042811-1f6751e8180bed60005e90421255e63d-120461613";

            LibreriaBase.Api.MercadoPago mercadoPago = new LibreriaBase.Api.MercadoPago(token);

            var req = mercadoPago.ObtenerPago(codigoPago);

            return View(req);
        }


        [HttpGet]
        public IActionResult AvisoMercadoPago()
        {
            Int32 codigo = 1;

            return View();
        }



        /// <summary>
        /// Mercado pago - se arma el pago de los elementos del carrito
        /// </summary>
        /// <returns></returns>
        public IActionResult PagarCarritoMp()
        {
            try
            {
                SessionAcceso session = HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
                _repositorioEmpresa.DatosSistema = session.Sistema;

                var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                var lista = _repositorioEmpresa.CuentasMercadoPago();

                if (lista?.Count() > 0)
                {
                    MercadoPagoDatos mpd = null;

                    if (session.Sistema?.SectorId > 0)
                    {
                        mpd = lista.FirstOrDefault(c => c.SectorId == session.Sistema.SectorId && c.Token != "");

                        if (mpd == null)
                        {
                            mpd = lista.FirstOrDefault(c => c.Token != "");
                        }
                    }
                    else
                    {
                        mpd = lista.FirstOrDefault(c => c.Token != "");
                    }


                    HttpContext.Session.SetJson("MercadoPagoDatos", mpd);

                    MercadoPago.SDK sdk = new MercadoPago.SDK();

                    // procesar-pago

                    //El token lo necesito para procesar el pago.
                    sdk.ClientId = mpd.ClientId.Trim();//"1487233186955868";
                    sdk.ClientSecret = mpd.ClientSecret.Trim();// "MBz8Cqmd9JyAG1p9CuoC22xK86xDFsP1";

                    // Create a preference object
                    Preference preference = new Preference(sdk);

                    preference.BackUrls = new BackUrls()
                    {
                        Success = @"https://" + HttpContext.Request.Host.Value + "/MercadoPago/RequestMercadoPago",
                        Pending = @"https://" + HttpContext.Request.Host.Value + "/MercadoPago/RequestMercadoPago",
                        Failure = @"https://" + HttpContext.Request.Host.Value + "/MercadoPago/RequestMercadoPago"

                    };

                    preference.AutoReturn = MercadoPago.Common.AutoReturnType.approved;

                    if (carrito.PedidoId == null || carrito.PedidoId == 0)
                    {
                        IRepositorioPedido repositorioPedido = new RepositorioPedido();
                        repositorioPedido.DatosSistema = session.Sistema;
                        int idProxPedido = repositorioPedido.Prox_PedidoID();
                        carrito.PedidoId = idProxPedido;

                    }
                    String titulo = "";

                    foreach (var item in carrito.Lista)
                    {
                        titulo += item.ToString() + " ";
                    }

                    String descripcion = "Compra online en " + session.Sistema.Nombre + " Codigo Pedido: " + carrito.PedidoId;


                    preference.Items.Add(
                        new Item()
                        {

                            Title = titulo,
                            Description = descripcion,
                            Quantity = 1,
                            CurrencyId = MercadoPago.Common.CurrencyId.ARS,
                            UnitPrice = carrito.TotalCarrito()
                        }
                    );


                    preference.Save();
                    Boolean mp = false;
                    if (!String.IsNullOrEmpty(preference.Id))
                    {
                        mp = true;
                    }

                    //return RedirectToAction("Index", new { mp, preference.Id, session.Sistema.EmpresaId });
                    if (!String.IsNullOrEmpty(preference?.InitPoint))
                    {
                        return Redirect(preference.InitPoint);
                    }
                    else
                    {
                        ViewBag.Error = "No se detectaron generar el cupon de pago.";
                        return View();
                    }
                }
                else
                {
                    ViewBag.Error = "No se detectaron cuentas de mercado pago.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorRepresentada"] = "No se detectaron cuentas de Mercado Pago, cambie la modalidad de pago.";
                return RedirectToAction("RegistrarPedido", "Pedido");


            }


        }



        public IActionResult RequestMercadoPago()
        {

            String respuesta = HttpContext.Request.QueryString.Value;

            respuesta = respuesta.Substring(1, respuesta.Length - 1);

            String[] array = respuesta.Split('&');

            if (array == null)
            {
                TempData["ErrorRepresentada"] = "El Pago fue CANCELADO, por cualquier duda revise su cuenta de mercado pago";
                return RedirectToAction("RegistrarPedido", "Pedido");
            }
            else
            {
                try
                {
                    string cod = array[0].Split('=')[1];
                    string estado = array[1].Split('=')[1];
                    string tipoPago = array[5].Split('=')[1];



                    if (String.IsNullOrEmpty(cod) || String.IsNullOrEmpty(estado))
                    {
                        TempData["ErrorRepresentada"] = "El Pago fue CANCELADO, por cualquier duda revise su cuenta de mercado pago";
                        return RedirectToAction("RegistrarPedido", "Pedido");
                    }
                    else
                    {
                        var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                        carrito.Pago.DatosPago = "Codigo: " + cod + " -- Estado: " + estado + " -- Tipo Pago: " + tipoPago;

                        carrito.Guardar();

                        return RedirectToAction("FinalizarPedido", "Pedido");
                    }


                }
                catch (Exception ex)
                {
                    TempData["ErrorRepresentada"] = "El Pago fue CANCELADO, por cualquier duda revise su cuenta de mercado pago";
                    return RedirectToAction("RegistrarPedido", "Pedido");
                }
            }

        }

    }
}