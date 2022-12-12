using DRR.Core.DBEmpresaEjemplo.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Cobro;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace WebDRR.Areas.Cobro.Controllers
{
    [Area("Cobro")]
    [Route("[controller]/[action]")]
    public class CobroController : Controller
    {

        #region Variables
        private SessionAcceso _session;
        private IRepositorioCobro _repositorioCobro;
        #endregion


        #region Constructor
        //El constructor inyecta el repositorio de Cliente.
        //En dicho clase esta toda la comunicacion con la base de datos
        public CobroController(IRepositorioCobro repositorioCobro, IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioCobro = repositorioCobro;
            _repositorioCobro.DatosSistema = _session.Sistema;



        }
        #endregion


        // GET: CobroController
        public IActionResult Index(FiltroCobro filtro = null)
        {
            String info = "";

            if (_session.Usuario.CobradorId.IsNullOrValue(0))
            {
                Int32? cobradorId = _repositorioCobro.GetCobradorId((Int32)_session?.Usuario?.EntidadSucId, out info);

                if (cobradorId.IsNullOrValue(0))
                {

                    TempData["ErrorRepresentada"] = "Para porder operar con la cobranza, el Vendedor tiene que estar dado de alta como Cobrador.";

                    return RedirectToAction("Principal", "Home");
                }
                else
                {
                    _session.Usuario.CobradorId = cobradorId;

                    _session.GuardarSession(this.HttpContext);

                }
            }

            if (filtro == null)
            {
                filtro = new FiltroCobro();
                filtro.FechaDesde = DateTime.Now.FechaHs_Argentina();
                filtro.FechaHasta = DateTime.Now.FechaHs_Argentina();
            }
            else
            {
                if (filtro.FechaDesde == null && filtro.FechaHasta == null && filtro.Todos ==false)
                {
                    filtro.FechaDesde = DateTime.Now.FechaHs_Argentina();
                    filtro.FechaHasta = DateTime.Now.FechaHs_Argentina();
                }
            }

            filtro.SectorId = _session.Sistema.SectorId;
            filtro.CobradoId = _session.Usuario.CobradorId;


            if (filtro.ClienteId == null || filtro.ClienteId == 0)
            {
                ViewData["viewCliente"] = null;
            }
            else
            {
                IRepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = _session.Sistema;

                ViewData["viewCliente"] = repositorioCliente.GetCliente((Int32)filtro.ClienteId);
            }


            var listado = _repositorioCobro.Listar(filtro, out info);
            Boolean representada = false;
            if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
            {
                representada = true;
            }
            ViewData["Representada"] = representada;

            ListadoCobrosViewModel listadoCobrosViewModel = new ListadoCobrosViewModel();
            listadoCobrosViewModel.Filtro = filtro;
            listadoCobrosViewModel.Lista = listado;


            return View(listadoCobrosViewModel);
        }

        /// <summary>
        /// Nos lleva a la vista de agregar comprobantes.
        /// </summary>
        /// <param name="esquema">
        /// 1 Por Importe
        /// 2 Seleccion de Combrobantes
        /// </param>
        /// <param name="cobranza">
        /// Si viene nula es porque la operacion la incia un cobrador.
        /// Si es distinta de nula es porque la operacion la esta realizando un Repartidor.
        /// </param>
        /// <returns></returns>
        public ActionResult Agregar(int esquema, ViewSeleccionarCobranza cobranza = null)
        {
            Int32 tipo = 1;
            try
            {
                CobroViewModel viewModel = new CobroViewModel();

                if (cobranza.ToString() == "null")
                {
                    var confCobranza = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Activar_CodigoReparto_CobranzaVendedores);

                    if (confCobranza != null)
                    {
                        if (confCobranza?.Valor.MostrarEntero() == 1)
                        {
                            if (_session?.Usuario?.NumeroReparto.IsNullOrValue(0) == true)
                            {
                                tipo = 2;

                                throw new Exception("Del menu de opciones (rojo que esta arriba a la derecha), selecciona la opcion de: Asignar n° reparto");
                            }
                            else
                            {
                                IRepositorioPedido repositorioPedido = new RepositorioPedido();
                                repositorioPedido.DatosSistema = _session.Sistema;
                                Boolean verifica = repositorioPedido.ExisteReparto((int)(_session?.Usuario?.NumeroReparto));

                                if(verifica == false)
                                {
                                    throw new Exception("El N° de reparto ingresado no existe, controle el número que se ingreso.");
                                }

                            }

                        }
                    }


                    #region Sin N° Reparto

                    #region Verificaciones
                    var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                    FiltroCobro filtro = new FiltroCobro();

                    if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                    {
                        if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                        {
                            throw new Exception("Por favor seleccione la representada con la que quiere operar");
                        }
                        else
                        {
                            filtro.SectorId = _session?.Sistema?.SectorId;
                        }

                        if (carrito == null || carrito?.Cliente == null)
                        {
                            throw new Exception("Por favor seleccione el cliente");
                        }
                        else
                        {
                            filtro.ClienteId = carrito?.Cliente?.ClienteID;
                        }

                    }
                    else
                    {
                        if (carrito == null || carrito?.Cliente == null)
                        {
                            throw new Exception("Por favor seleccione el cliente");
                        }
                        else
                        {
                            filtro.ClienteId = carrito?.Cliente?.ClienteID;
                        }

                    }

                    //Paso el cobrador.
                    filtro.CobradoId = _session?.Usuario?.CobradorId;


                    //Que el cliente no tenga cobros sin reprecesar.
                    String info = "";
                    Boolean existeSinRepocesar = _repositorioCobro.ExisteCobroSinReprocesar(filtro, out info);

                    if (existeSinRepocesar == true)
                    {
                        tipo = 0;

                        throw new Exception("El cliente tiene cobros sin Reprocesar, en caso de necesitar realizar alguna modificación, puede editar el ultimo cobro.");
                    }


                    #endregion


                    //Ver url de atras si viene de verificar cobro entonces ahi tengo que tarar la session de viewcov

                    string url = "";
                    if (TempData.ContainsKey("UrlCobro"))
                    {
                        url = TempData["UrlCobro"].ToString();
                        TempData.Remove("UrlCobro");
                    }


                    if (url == "VerificarCobro")
                    {
                        viewModel = HttpContext.Session.GetJson<CobroViewModel>("CobroViewModel");


                        if (viewModel == null)
                        {
                            viewModel = new CobroViewModel();
                            viewModel.TipoOeracion = (byte)EnumTipoOperacion.Agregar;

                            viewModel.Esquema = esquema;
                            viewModel.CobroWeb.AlmaUserId = _session.Usuario.AlmaUserID;

                            if (carrito.Cliente != null)
                            {
                                viewModel.CobroWeb.ClienteId = carrito.Cliente?.ClienteID;
                                viewModel.CobroWeb.P_ClienteNombre = carrito.Cliente.RazonSocial;
                            }

                            viewModel.CobroWeb.SectorId = _session.Sistema.SectorId;


                            viewModel.CobroWeb.P_Representada = _session.Sistema.NombreRepresentada;
                            viewModel.CobroWeb.FechaComprobante = DateTime.UtcNow;



                            IRepositorioCliente repositorioCliente = new RepositorioCliente();
                            repositorioCliente.DatosSistema = _session.Sistema;
                            viewModel.ListaEstadoCuenta = repositorioCliente.GetSaldo((int)carrito?.Cliente?.EntidadSucId, (int)_session?.Sistema?.EmpresaId);


                            ////** PROBANDO.
                            //if(viewModel?.ListaEstadoCuenta?.Count()>0)
                            //{
                            //    foreach (var item in viewModel?.ListaEstadoCuenta)
                            //    {
                            //        if(!item.RegistroOperacionID.IsNullOrValue(0))
                            //        {
                            //            item.NumeroReparto = _repositorioCobro.GetNumeroReparto_Venta((Int32)item.RegistroOperacionID);
                            //        }
                            //    }
                            //}

                            //Se guarda en Session.
                            HttpContext.Session.SetJson("CobroViewModel", viewModel);
                        }

                    }
                    else
                    {
                        viewModel.TipoOeracion = (byte)EnumTipoOperacion.Agregar;

                        viewModel.Esquema = esquema;
                        viewModel.CobroWeb.AlmaUserId = _session.Usuario.AlmaUserID;

                        if (carrito.Cliente != null)
                        {
                            viewModel.CobroWeb.ClienteId = carrito.Cliente?.ClienteID;
                            viewModel.CobroWeb.P_ClienteNombre = carrito.Cliente.RazonSocial;
                        }

                        viewModel.CobroWeb.SectorId = _session.Sistema.SectorId;


                        viewModel.CobroWeb.P_Representada = _session.Sistema.NombreRepresentada;
                        viewModel.CobroWeb.FechaComprobante = DateTime.UtcNow;



                        IRepositorioCliente repositorioCliente = new RepositorioCliente();
                        repositorioCliente.DatosSistema = _session.Sistema;
                        viewModel.ListaEstadoCuenta = repositorioCliente.GetSaldo((int)carrito?.Cliente?.EntidadSucId, (int)_session?.Sistema?.EmpresaId);


                        ////** PROBANDO.
                        //if(viewModel?.ListaEstadoCuenta?.Count()>0)
                        //{
                        //    foreach (var item in viewModel?.ListaEstadoCuenta)
                        //    {
                        //        if(!item.RegistroOperacionID.IsNullOrValue(0))
                        //        {
                        //            item.NumeroReparto = _repositorioCobro.GetNumeroReparto_Venta((Int32)item.RegistroOperacionID);
                        //        }
                        //    }
                        //}

                        //Se guarda en Session.
                        HttpContext.Session.SetJson("CobroViewModel", viewModel);
                    }

                    #endregion
                }
                else
                {
                    #region Con N° Reparto  23/12/2021

                    #region Verificaciones
                    //var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                    FiltroCobro filtro = new FiltroCobro();

                    if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                    {
                        if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                        {
                            throw new Exception("Por favor seleccione la representada con la que quiere operar");
                        }
                        else
                        {
                            filtro.SectorId = _session?.Sistema?.SectorId;
                        }
                    }

                    String info = "";
                    //14/02/2022
                    if ((bool)(_session?.Usuario?.CobradorId.IsNullOrValue(0)))
                    {
                        Int32? cobradorId = _repositorioCobro.GetCobradorId((Int32)_session?.Usuario?.EntidadSucId, out info);

                        if (!cobradorId.IsNullOrValue(0))
                        {
                            _session.Usuario.CobradorId = cobradorId;
                            _session.GuardarSession(HttpContext);
                        }
                    }


                    //Paso el cobrador.
                    filtro.CobradoId = _session?.Usuario?.CobradorId;
                    filtro.ClienteId = cobranza.ClienteID;


                    //Que el cliente no tenga cobros sin reprecesar.

                    Boolean existeSinRepocesar = _repositorioCobro.ExisteCobroSinReprocesar(filtro, out info);

                    if (existeSinRepocesar == true)
                    {
                        tipo = 2;


                    #region Lo nuevo 21-04-2022
                    //Podria abrir el -- Modificar el ultimo cobro del cliente......

                        if(filtro.CobroWebID.IsNullOrValue(0)==false)
                        {
                            var routeValues = new RouteValueDictionary {
                                  { "cobroWebId", Convert.ToInt32(filtro.CobroWebID) }
                                };
                            return RedirectToAction("Editar", "Cobro", routeValues);
                        }

                    //https://localhost:44308/Cobro/Editar?cobroWebId=99
                        //-****---------------------------------------------------------
                        #endregion



                        //throw new Exception("El cliente tiene cobros sin Reprocesar, en caso de necesitar realizar alguna modificación, puede editar el ultimo cobro.");
                    }


                    #endregion



                    //Ver url de atras si viene de verificar cobro entonces ahi tengo que tarar la session de viewcov

                    string url = "";
                    if (TempData.ContainsKey("UrlCobro"))
                    {
                        url = TempData["UrlCobro"].ToString();
                        TempData.Remove("UrlCobro");
                    }


                    if (url == "VerificarCobro")
                    {
                        viewModel = HttpContext.Session.GetJson<CobroViewModel>("CobroViewModel");

                        if(viewModel == null)
                        {
                            viewModel = new CobroViewModel();

                            viewModel.TipoOeracion = (byte)EnumTipoOperacion.Agregar;

                            viewModel.Esquema = esquema;
                            viewModel.CobroWeb.AlmaUserId = _session.Usuario.AlmaUserID;

                            viewModel.CobroWeb.ClienteId = cobranza.ClienteID;
                            viewModel.CobroWeb.P_ClienteNombre = cobranza.Cliente;

                            viewModel.CobroWeb.SectorId = _session.Sistema.SectorId;


                            viewModel.CobroWeb.P_Representada = _session.Sistema.NombreRepresentada;
                            viewModel.CobroWeb.FechaComprobante = DateTime.UtcNow;

                            viewModel.UrlRetorno = HttpContext.Request.UrlAtras();


                            IRepositorioCliente repositorioCliente = new RepositorioCliente();
                            repositorioCliente.DatosSistema = _session.Sistema;


                            viewModel.ListaEstadoCuenta = repositorioCliente.GetSaldo(cobranza.EntidadSucId, (int)_session?.Sistema?.EmpresaId);

                            //Se guarda en Session.
                            HttpContext.Session.SetJson("CobroViewModel", viewModel);
                        }
                    }
                    else
                    {
                        viewModel.TipoOeracion = (byte)EnumTipoOperacion.Agregar;

                        viewModel.Esquema = esquema;
                        viewModel.CobroWeb.AlmaUserId = _session.Usuario.AlmaUserID;

                        viewModel.CobroWeb.ClienteId = cobranza.ClienteID;
                        viewModel.CobroWeb.P_ClienteNombre = cobranza.Cliente;

                        viewModel.CobroWeb.SectorId = _session.Sistema.SectorId;


                        viewModel.CobroWeb.P_Representada = _session.Sistema.NombreRepresentada;
                        viewModel.CobroWeb.FechaComprobante = DateTime.UtcNow;

                        viewModel.UrlRetorno = HttpContext.Request.UrlAtras();


                        IRepositorioCliente repositorioCliente = new RepositorioCliente();
                        repositorioCliente.DatosSistema = _session.Sistema;


                        viewModel.ListaEstadoCuenta = repositorioCliente.GetSaldo(cobranza.EntidadSucId, (int)_session?.Sistema?.EmpresaId);

                        //Se guarda en Session.
                        HttpContext.Session.SetJson("CobroViewModel", viewModel);
                    }

                    #endregion
                }







                return View(viewModel);
            }
            catch (Exception ex)
            {

                NotificacionesViewModel notificaciones = new NotificacionesViewModel();
                notificaciones.Mensaje = ex.Message;
                if (tipo == 1)
                {
                    //notificaciones.UrlIr = Url.Action("ListarClientes", "Cliente");
                    //notificaciones.UrlTexto = "Seleccionar Cliente";
                    TempData["ErrorRepresentada"] = notificaciones.Mensaje;

                    return RedirectToAction("ListarClientes", "Cliente");
                }
                else if (tipo == 2)
                {
                    //notificaciones.UrlIr = Url.Action("DetalleReparto", "Reparto");
                    //notificaciones.UrlTexto = "Modificar Cobro";
                    TempData["ErrorRepresentada"] = notificaciones.Mensaje;

                    return RedirectToAction("DetalleReparto", "Reparto");
                }
                else
                {
                    TempData["ErrorRepresentada"] = notificaciones.Mensaje;

                    return RedirectToAction("Index", "Cobro");
                }

            }
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AgregarQuitarCombrobantesSeleccionados(String comprobante, Boolean activo)
        {

            CobroViewModel viewModel = new CobroViewModel();

            viewModel = HttpContext.Session.GetJson<CobroViewModel>("CobroViewModel");

            if (!String.IsNullOrEmpty(comprobante))
            {
                if (activo == true)
                {
                    //Agregar
                    foreach (var item in viewModel.ListaEstadoCuenta)
                    {
                        if (item.Comprobante == comprobante)
                        {
                            item.Selecionada = true;
                        }
                    }
                }
                else
                {
                    //Quitar
                    foreach (var item in viewModel.ListaEstadoCuenta)
                    {
                        if (item.Comprobante == comprobante)
                        {
                            item.Selecionada = false;
                        }
                    }
                }

                HttpContext.Session.SetJson("CobroViewModel", viewModel);
            }



            Decimal factura = viewModel.ListaEstadoCuenta.Where(c => c.Selecionada == true).Sum(c => c.SaldoCtaCte);
            Decimal cbro = viewModel.ListaEstadoCuenta.Where(c => c.Selecionada == true).Sum(c => c.Adelanto);
            int cantidadComprobantes = viewModel.ListaEstadoCuenta.Where(c => c.Selecionada == true).Count();

            Generica respuesta = new Generica();
            respuesta.Nombre = (factura + cbro).FormatoMoneda();
            respuesta.Id = cantidadComprobantes;

            return new JsonResult(respuesta);


        }


        // POST: CobroController/Create
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult VerificarCobro(IFormCollection collection)
        {
            try
            {

                ViewData["TipoEmpresa"] = _session.Sistema.TipoEmpresa;

                CobroViewModel viewModel = new CobroViewModel();

                viewModel = HttpContext.Session.GetJson<CobroViewModel>("CobroViewModel");
                Int32 esquema = Convert.ToInt32(collection["tipo"]);
                viewModel.Esquema = esquema;

                if (viewModel.Esquema == 2)
                {
                    Decimal importe = Convert.ToDecimal(collection["ImporteEntregado"]);
                    viewModel.ImporteEntregado = importe;
                    //viewModel.CobroWeb.TotalCobro = importe;

                    Exception exception = null;

                    viewModel.GenerarEsquemaPago(out exception);


                    viewModel.CobroWeb.TotalCobro = importe;

                    //Se guarda el cobro
                    HttpContext.Session.SetJson("CobroViewModel", viewModel);
                }
                else
                {
                    Int32 indice = 1;
                    Decimal importeTotal = 0;
                    foreach (var item in viewModel.ListaEstadoCuenta)
                    {

                        if (item.Selecionada == true)
                        {
                            var importe = collection["txtCobranza_" + indice];
                            try
                            {
                                importeTotal += Convert.ToDecimal(importe);
                                item.SeCobra = Convert.ToDecimal(importe);
                            }
                            catch (Exception)
                            {

                            }

                        }

                        indice += 1;
                    }

                    var importeAdelanto = collection["txtAdelanto"];
                    if (!String.IsNullOrEmpty(importeAdelanto))
                    {
                        decimal adelanto = 0;
                        bool castAdelanto = Decimal.TryParse(importeAdelanto, out adelanto);
                        if (castAdelanto == true)
                        {

                            ViewEstadoCuenta vec_adelanto = new ViewEstadoCuenta();
                            vec_adelanto.TipoOperacion = "Adelanto";

                            importeTotal += Convert.ToDecimal(adelanto);

                            vec_adelanto.SeCobra = adelanto;
                            vec_adelanto.Selecionada = true;
                            viewModel.ListaEstadoCuenta.Add(vec_adelanto);
                        }

                    }

                    Exception exception = null;

                    viewModel.GenerarEsquemaPago(out exception);

                    viewModel.CobroWeb.TotalCobro = importeTotal;
                    viewModel.CobroWeb.FechaComprobante = (DateTime)DateTime.Now.FechaHs_Argentina();

                    HttpContext.Session.SetJson("CobroViewModel", viewModel);
                    TempData["UrlCobro"] = "VerificarCobro";
                }



                return View(viewModel);
            }
            catch (Exception ex)
            {

                return View();
            }
        }



        // POST: CobroController/Create
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult GuardarCobro(IFormCollection collection)
        {
            Boolean btnError = true;

            try
            {

                CobroViewModel viewModel = new CobroViewModel();
                viewModel = CobroViewModel.RecuperarSession(HttpContext); 

                var fechaR = collection["CobroWeb.FechaComprobante"];
                var observacion = collection["CobroWeb.Observacion"];

                viewModel.CobroWeb.FechaComprobante = Convert.ToDateTime(fechaR);
                viewModel.CobroWeb.FechaImputacion = DateTime.Now;
                viewModel.CobroWeb.TalonarioId = 1;
                viewModel.CobroWeb.NroComprobante = 1;
                viewModel.CobroWeb.Observacion = observacion;

                //Momentaneo.
                if (_session?.Usuario?.NumeroReparto.IsNullOrValue(0) == false)
                {
                    viewModel.CobroWeb.MovId = _session?.Usuario?.NumeroReparto;
                }

                viewModel.CobroWeb.CobradorId = _session?.Usuario?.CobradorId;

                String informacion = "";

                if (_session?.Sistema.EmpresaId == 45)
                {
                    viewModel.CobroWeb.AlmaUserId = null;
                }

                Tuple<Boolean, Generica>? respuesta = null;

                if (viewModel.TipoOeracion == (int)EnumTipoOperacion.Agregar)
                {
                    respuesta = _repositorioCobro.AgregarCobro(viewModel.CobroWeb);
                }
                else if (viewModel.TipoOeracion == (int)EnumTipoOperacion.Modificar)
                {
                    respuesta = _repositorioCobro.ModificarCobro(viewModel);
                }

                if (respuesta?.Item1 == true)
                {
                    //******* 
                    Utilidades.CalcularTotales(HttpContext, _session);
                    //*******

                    HttpContext.Session.Remove("CobroViewModel");

                    TempData["ErrorRepresentada"] = "El cobro se guardo con exito";
                    TempData["ErrorRepresentadaColor"] = "Verde";
                }
                else
                {
                    TempData["ErrorRepresentada"] = "No se pudo guarda el cobro. Detalle: " + informacion;
                }


                if (!String.IsNullOrEmpty(viewModel?.UrlRetorno))
                {
                    return Redirect(viewModel?.UrlRetorno);
                }
                else
                {
                    return RedirectToAction("Index", "Cobro");
                }

            }
            catch (Exception ex)
            {
                NotificacionesViewModel notificaciones = new NotificacionesViewModel();
                notificaciones.Mensaje = ex.Message;
                if (btnError == true)
                {
                    notificaciones.UrlIr = Url.Action("ListarClientes", "Cliente");
                    notificaciones.UrlTexto = "Seleccionar Cliente";
                }


                TempData["Error"] = notificaciones.ToJson();

                return RedirectToAction("Index", "Cobro");
            }
        }



        /// <summary>
        /// El controlar es diferente para simplificar un poco mas el codigo
        /// pero la vista es la misma que la de agregar.
        /// </summary>
        /// <param name="cobroWebId"></param>
        /// <returns></returns>
        public IActionResult Editar(Int32 cobroWebId)
        {
            try
            {
                //Obtener el cobro.
                #region Verificaciones
                //var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                {
                    if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                    {
                        throw new Exception("Por favor seleccione la representada con la que quiere operar");
                    }

                    //if (carrito == null || carrito?.Cliente == null)
                    //{
                    //    throw new Exception("Por favor seleccione el cliente");
                    //}
                }

                #endregion

                String info = "";
                OperacionCobroWeb entidad = _repositorioCobro.GetCobroWeb(cobroWebId, out info);


                entidad.TotalCobro = Math.Round(entidad.TotalCobro, 2);

                if (entidad == null)
                {
                    throw new Exception("Ocurrio un error al intenar seleccionar el cobro: " + info);
                }

                if (entidad.FechaHoraReproceso != null)
                {
                    throw new Exception("El cobro seleccionado ya se reproceso, no puede modificarse");
                }
                else
                {
                    //Podemos editaro.
                    String esquemaString = entidad?.OperacionCobroWebItem?.FirstOrDefault().Detalle;
                    Int32 esquema = 2;

                    if (esquemaString.Contains("Generado Automaticamente"))
                    {
                        esquema = 1;
                    }



                    CobroViewModel viewModel = new CobroViewModel();
                    viewModel.Esquema = esquema;
                    viewModel.CobroWeb = entidad;


                    IRepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = _session.Sistema;

                    viewModel.ListaEstadoCuenta = repositorioCliente.GetSaldo((int)entidad?.Cliente?.EntidadSucId, (int)_session?.Sistema?.EmpresaId);

                    //Se guarda en Session.
                    
                    viewModel.TipoOeracion = (byte)EnumTipoOperacion.Modificar;
                    HttpContext.Session.SetJson("CobroViewModel", viewModel);
                    
                    return View("Agregar", viewModel);

                    //return RedirectToAction("Index", "Cobro");

                }

            }
            catch (Exception ex)
            {
                NotificacionesViewModel notificaciones = new NotificacionesViewModel();
                notificaciones.Mensaje = ex.ErrorException();
                //notificaciones.UrlIr = Url.Action("ListarClientes", "Cliente");
                //notificaciones.UrlTexto = "Seleccionar Cliente";

                TempData["Error"] = notificaciones.ToJson();

                return RedirectToAction("Index", "Cobro");
            }
        }


        public IActionResult Eliminar(Int32 cobroWebId)
        {
            try
            {
                //Obtener el cobro.
                #region Verificaciones
                //var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                {
                    if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                    {
                        throw new Exception("Por favor seleccione la representada con la que quiere operar");
                    }

                    //if (carrito == null || carrito?.Cliente == null)
                    //{
                    //    throw new Exception("Por favor seleccione el cliente");
                    //}
                }

                #endregion

                var res = _repositorioCobro.EliminarCobro(cobroWebId);

return RedirectToAction("Index", "Cobro");

                

            }
            catch (Exception ex)
            {
                NotificacionesViewModel notificaciones = new NotificacionesViewModel();
                notificaciones.Mensaje = ex.ErrorException();
                //notificaciones.UrlIr = Url.Action("ListarClientes", "Cliente");
                //notificaciones.UrlTexto = "Seleccionar Cliente";

                TempData["Error"] = notificaciones.ToJson();

                return RedirectToAction("Index", "Cobro");
            }
        }







        public IActionResult GenerarPdf(int cobroId)
        {
            try
            {
                String info = "";
                OperacionCobroWeb entidad = _repositorioCobro.GetCobroWeb(cobroId, out info);



                BaseFont helvetica = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, true);
                iTextSharp.text.Font titulo = new iTextSharp.text.Font(helvetica, 16f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));
                iTextSharp.text.Font subtitulo = new iTextSharp.text.Font(helvetica, 12f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));
                iTextSharp.text.Font parrafo = new iTextSharp.text.Font(helvetica, 10f, iTextSharp.text.Font.NORMAL, new BaseColor(0, 0, 0));
                iTextSharp.text.Font negrita = new iTextSharp.text.Font(helvetica, 12f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));
                iTextSharp.text.Font texto_blanco = new iTextSharp.text.Font(helvetica, 10f, iTextSharp.text.Font.BOLD, new BaseColor(255, 255, 255));
                iTextSharp.text.Font toinvoice = new iTextSharp.text.Font(helvetica, 20f, iTextSharp.text.Font.BOLD, new BaseColor(255, 255, 255));

                Document doc = new Document(PageSize.A4);
                doc.SetMargins(20f, 20f, 20f, 20f);

                MemoryStream ms = new MemoryStream();
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);


                IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                repositorioEmpresa.DatosSistema = _session.Sistema;
                var empresaViewModel = repositorioEmpresa.GetEmpresa((int)_session.Sistema.EmpresaId);


                #region esto esta sin testear
                String sector = "[" + _session?.Sistema?.SectorId + "] " + _session?.Sistema?.NombreRepresentada;

                String cliente = "";
                ViewCliente viewCliente = null;

                if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                {

                    IRepositorioCliente repCliente = new RepositorioCliente();
                    repCliente.DatosSistema = _session.Sistema;

                    viewCliente = repCliente.GetCliente((Int32)entidad.Cliente?.ClienteId);

                    #region  Obtenr IIBB del cliente

                    RepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = _session.Sistema;
                    var impuestoCliente = repositorioCliente.Obtener_Impuestos(viewCliente.EntidadSucId);
                    Int16 iibb = 900;

                    if (impuestoCliente?.Count() > 0)
                    {
                        EntidadSucursalImpuesto impuestoIIBB = impuestoCliente.FirstOrDefault(c => c.ImpuestoId == iibb);

                        if (impuestoIIBB != null)
                        {

                            if (impuestoIIBB.EximidoHastaFecha != null)
                            {

                                if (impuestoIIBB.EximidoHastaFecha < DateTime.Now)
                                {
                                    var impDatos = repositorioCliente.getImpuestoAlmaNet(impuestoIIBB.ImpuestoId);

                                    viewCliente.Impuesto = new LibreriaBase.Clases.Impuesto();
                                    viewCliente.Impuesto.ImpuestoID = impDatos.ImpuestoId;
                                    viewCliente.Impuesto.Nombre = impDatos.DescripcionImpuesto;
                                    viewCliente.Impuesto.PorcentajeAlicuota = impDatos.PocentajeDeducir ?? 0;
                                    viewCliente.Impuesto.ImporteMinimo = impDatos.MontoMinimo ?? 0;
                                }

                            }
                            else
                            {
                                var impDatos = repositorioCliente.getImpuestoAlmaNet(impuestoIIBB.ImpuestoId);

                                viewCliente.Impuesto = new LibreriaBase.Clases.Impuesto();
                                viewCliente.Impuesto.ImpuestoID = impDatos.ImpuestoId;
                                viewCliente.Impuesto.Nombre = impDatos.DescripcionImpuesto;
                                viewCliente.Impuesto.PorcentajeAlicuota = impDatos.PocentajeDeducir ?? 0;
                                viewCliente.Impuesto.ImporteMinimo = impDatos.MontoMinimo ?? 0;
                            }

                        }

                    }

                    #endregion



                    if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                    {
                        cliente = "[" + viewCliente?.NroClienteAsignado + "] " + viewCliente?.RazonSocial;
                    }
                    else
                    {
                        cliente = "[" + viewCliente?.ClienteID + "] " + viewCliente?.RazonSocial;
                    }


                }
                #endregion


                String vendedor = "[" + _session?.Usuario?.Cliente_Vendedor_Id + "] " + _session?.Usuario?.Nombre;











                //Empresa-
                doc.AddAuthor(empresaViewModel.RazonSocial);
                //Cobro-
                doc.AddTitle("Cobro N° " + entidad.CobroWebId);
                doc.Open();

                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(empresaViewModel.Logo);
                logo.ScaleToFit(200, 200);
                doc.Add(logo);




                #region Datos de cabecera
                Paragraph para1 = new Paragraph();
                Phrase ph2 = new Phrase();
                Paragraph mm1 = new Paragraph();

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("Reporte de Cobro", FontFactory.GetFont("Arial", 20, 2)));

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk(empresaViewModel.CorreoElectronico, FontFactory.GetFont("Arial", 10, 1)));

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("Dirección: " + empresaViewModel.Direccion, FontFactory.GetFont("Arial", 10, 1)));
                ph2.Add(new Chunk(Environment.NewLine));


                if (_session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    ph2.Add(new Chunk(Environment.NewLine));
                    ph2.Add(new Chunk("Representada: " + sector, FontFactory.GetFont("Arial", 10, 1)));
                }


                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("Código de su cobro: " + entidad.CobroWebId, FontFactory.GetFont("Arial", 10, 1)));

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("Fecha: " + entidad.FechaComprobante, FontFactory.GetFont("Arial", 10, 1)));





                if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                {
                    ph2.Add(new Chunk(Environment.NewLine));
                    ph2.Add(new Chunk("Cliente: " + cliente, FontFactory.GetFont("Arial", 10, 1)));

                    ph2.Add(new Chunk(Environment.NewLine));
                    ph2.Add(new Chunk("Vendedor: " + vendedor, FontFactory.GetFont("Arial", 10, 1)));
                }
                else
                {
                    ph2.Add(new Chunk(Environment.NewLine));
                    ph2.Add(new Chunk("Cliente: " + vendedor, FontFactory.GetFont("Arial", 10, 1)));
                }

                mm1.Add(ph2);
                para1.Add(mm1);
                doc.Add(para1);

                #endregion



                Chunk barra = new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(5f, 30f, new BaseColor(0, 69, 161), Element.ALIGN_RIGHT, -1));
                doc.Add(barra);


                doc.Add(new Phrase(" "));




                #region Esquema basico de la tabla

                PdfPTable tabla = new PdfPTable(new float[] { 30f, 50f, 20f }) { WidthPercentage = 100f };
                PdfPCell c1 = new PdfPCell(new Phrase("Comprobante", negrita));
                PdfPCell c2 = new PdfPCell(new Phrase("Detalle", negrita));
                PdfPCell c3 = new PdfPCell(new Phrase("Importe", negrita));

                c1.Border = 0;
                c2.Border = 0;
                c3.Border = 0;

                c3.HorizontalAlignment = Element.ALIGN_RIGHT;

                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(c3);
                #endregion

                #region se carga los datos de la tabla
                Boolean presiosFinales = true;


                Int32 i = 0;
                for (i = 0; i < entidad.OperacionCobroWebItem.Count(); i++)
                {
                    if (i % 2 == 0)
                    {
                        c1.BackgroundColor = new BaseColor(204, 204, 204);
                        c2.BackgroundColor = new BaseColor(204, 204, 204);
                        c3.BackgroundColor = new BaseColor(204, 204, 204);
                    }
                    else
                    {
                        c1.BackgroundColor = BaseColor.White;
                        c2.BackgroundColor = BaseColor.White;
                        c3.BackgroundColor = BaseColor.White;
                    }

                    //Se obtiene el item del carrito
                    var item = entidad.OperacionCobroWebItem.ElementAt(i);
                    String comprobante = "";
                    String error = "";
                    if (item?.TipoOperacionId != null)
                    {
                        var dta = _repositorioCobro.InfoComprobante((int)item.TipoOperacionId, (int)item.RegistroOperacionId, (int)entidad.ClienteId, out error);
                        if (dta != null)
                        {
                            comprobante = dta.ToString();
                        }
                    }
                    else
                    {
                        comprobante = "Adelanto";
                    }

                    String Detalle = item.Detalle;
                    String Importe = item.ImportePago.FormatoMoneda();


                    //Se crea una nueva fila con los datos-
                    c1.Phrase = new Phrase(comprobante);
                    c2.Phrase = new Phrase(Detalle);
                    c3.Phrase = new Phrase(Importe);

                    tabla.AddCell(c1);
                    tabla.AddCell(c2);
                    tabla.AddCell(c3);
                }

                c1.Colspan = 2;
                c1.Phrase = new Phrase("");
                c1.HorizontalAlignment = Element.ALIGN_RIGHT;
                tabla.AddCell(c1);

                c1.Colspan = 2;
                c1.Phrase = new Phrase(entidad?.TotalCobro.FormatoMoneda());
                c1.HorizontalAlignment = Element.ALIGN_RIGHT;

                tabla.AddCell(c1);
                #endregion


                doc.Add(tabla);



                #region Observacion Carrito-
                Paragraph paraObservaciones = new Paragraph();
                Phrase phObservaciones = new Phrase();
                Paragraph mmObservaciones = new Paragraph();


                phObservaciones.Add(new Chunk(Environment.NewLine));
                phObservaciones.Add(new Chunk("Observacion: " + entidad.Observacion, FontFactory.GetFont("Arial", 10, 2)));
                phObservaciones.Add(new Chunk(Environment.NewLine));


                mmObservaciones.Add(phObservaciones);
                paraObservaciones.Add(mmObservaciones);
                doc.Add(paraObservaciones);
                #endregion




                writer.Close();
                doc.Close();
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, "application/pdf");

            }
            catch (Exception ex)
            {
                NotificacionesViewModel model = new NotificacionesViewModel();

                model.Id = -1;
                model.Mensaje = "No se pudo generar el pdf. Error: " + ex.Message;

                String url = Url.Action("Notificaciones", "Home", model);

                return Redirect(url);
            }
        }



        public IActionResult GenerarLiquidacion(String filtro)
        {
            FiltroCobro filtroCob = filtro.ToObsect<FiltroCobro>();

            List<OperacionCobroWeb> lista = new List<OperacionCobroWeb>();
            String info = "";

            lista = _repositorioCobro.Listar(filtroCob, out info)?.ToList();

            //Secrea el documento
            iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4);
            doc.SetMargins(20f, 20f, 20f, 20f);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);

            doc.AddAuthor(_session.Usuario.Nombre);
            doc.Open();


            #region Datos de cabecera
            Paragraph para1 = new Paragraph();
            Phrase ph2 = new Phrase();
            Paragraph mm1 = new Paragraph();

            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk("Listado de Cobros", FontFactory.GetFont("Arial", 20, 2)));
            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk(Environment.NewLine));

            if (_session.Sistema.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
            {
                ph2.Add(new Chunk("Representada: " + _session.Sistema.NombreRepresentada, FontFactory.GetFont("Arial", 16, 2)));
            }




            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk(Environment.NewLine));
            mm1.Add(ph2);
            para1.Add(mm1);
            doc.Add(para1);

            #endregion



            BaseFont helvetica = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, true);

            iTextSharp.text.Font negrita = new iTextSharp.text.Font(helvetica, 9f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));


            #region Esquema basico de la tabla

            PdfPTable tabla = new PdfPTable(new float[] { 10f, 15f, 15f, 60f }) { WidthPercentage = 100f };
            PdfPCell c1 = new PdfPCell(new Phrase("CobroID", negrita));
            PdfPCell c2 = new PdfPCell(new Phrase("Fecha", negrita));
            PdfPCell c3 = new PdfPCell(new Phrase("Total", negrita));
            PdfPCell c4 = new PdfPCell(new Phrase("Cliente", negrita));

            c1.Border = 0;
            c2.Border = 0;
            c3.Border = 0;
            c4.Border = 0;


            c4.HorizontalAlignment = Element.ALIGN_RIGHT;

            tabla.AddCell(c1);
            tabla.AddCell(c2);
            tabla.AddCell(c3);
            tabla.AddCell(c4);
            #endregion

            #region se carga los datos de la tabla

            Int32 i = 0;
            for (i = 0; i < lista.Count(); i++)
            {
                if (i % 2 == 0)
                {
                    c1.BackgroundColor = new BaseColor(204, 204, 204);
                    c2.BackgroundColor = new BaseColor(204, 204, 204);
                    c3.BackgroundColor = new BaseColor(204, 204, 204);
                    c4.BackgroundColor = new BaseColor(204, 204, 204);
                }
                else
                {
                    c1.BackgroundColor = BaseColor.White;
                    c2.BackgroundColor = BaseColor.White;
                    c3.BackgroundColor = BaseColor.White;
                    c4.BackgroundColor = BaseColor.White;
                }

                //Se obtiene el item del carrito
                var item = lista.ElementAt(i);


                //Se crea una nueva fila con los datos-
                c1.Phrase = new Phrase(item.CobroId.ToString());
                c2.Phrase = new Phrase(item.FechaComprobante.FechaCorta());

                c3.Phrase = new Phrase(item.TotalCobro.FormatoMoneda());

                c4.Phrase = new Phrase(item?.Cliente?.EntidadSuc?.Entidad?.RazonSocial);



                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(c3);
                tabla.AddCell(c4);
            }


            #region TOTALES

            Paragraph paraTotales = new Paragraph();
            Phrase phparaTotales = new Phrase();
            Paragraph mmparaTotales = new Paragraph();


            paraTotales.Add(new Chunk("TOTAL Cobranza: " + lista.Sum(c => c.TotalCobro).FormatoMoneda(), FontFactory.GetFont("Arial", 14, 2)));
            paraTotales.Add(new Chunk(Environment.NewLine));
            paraTotales.Add(new Chunk(Environment.NewLine));

            mmparaTotales.Alignment = Element.ALIGN_LEFT;

            phparaTotales.Add(paraTotales);
            mmparaTotales.Add(phparaTotales);
            doc.Add(mmparaTotales);

            #endregion


            //tabla.AddCell(c2);
            #endregion
            doc.Add(tabla);

            writer.Close();
            doc.Close();
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms, "application/pdf");
        }



        [HttpGet]
        public IActionResult AsignarNumeroRepartoCobrador()
        {
            try
            {
                AsignarNumeroReparto model = new AsignarNumeroReparto();
                return PartialView("_frmAsignarRepartoCobrador", model);
            }
            catch
            {
                return Content("");
            }
        }

        [HttpPost]
        public IActionResult AsignarNumeroRepartoCobrador(AsignarNumeroReparto objeto)
        {
            try
            {

                if (objeto?.Numero.IsNullOrValue(0) == false)
                {
                    _session.Usuario.NumeroReparto = objeto.Numero;
                    _session.GuardarSession(HttpContext);
                    TempData["ErrorRepresentada"] = "Se guardo el numero de reparto";

                }
                return RedirectToAction("Index", "Cobro");
            }
            catch (Exception ex)
            {
                NotificacionesViewModel notificaciones = new NotificacionesViewModel();
                notificaciones.Mensaje = ex.Message;
                notificaciones.UrlTexto = "No se pudo asignar el n° de reparto";

                TempData["ErrorRepresentada"] = notificaciones.Mensaje;

                return RedirectToAction("Index", "Cobro");
            }
        }

        protected async Task<string> RenderViewAsync(string viewName = null, object model = null)
        {
            // Primero, intentamos localizar la vista...
            var actionContext = new ActionContext(
                    HttpContext, RouteData, ControllerContext.ActionDescriptor, ModelState);
            var viewEngine = HttpContext.RequestServices.GetService<ICompositeViewEngine>();

            var viewEngineResult = viewEngine.FindView(actionContext, viewName, isMainPage: false);

            if (!viewEngineResult.Success)
            {
                var searchedLocations = string.Join(", ", viewEngineResult.SearchedLocations);
                throw new InvalidOperationException(
                    $"Couldn't find view '{viewName}', " +
                    $"searched locations: [{searchedLocations}]");
            }

            // Hemos encontrado la vista, vamos a renderizarla...
            using (var sw = new StringWriter())
            {
                // Preparamos el contexto de la vista
                var viewData = new ViewDataDictionary(ViewData) { Model = model };
                var helperOptions = HttpContext.RequestServices
                        .GetService<IOptions<HtmlHelperOptions>>()
                        .Value;
                var viewContext = new ViewContext(
                        actionContext, viewEngineResult.View, viewData, TempData, sw, helperOptions);

                // Y voila!
                await viewEngineResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

    }
}
