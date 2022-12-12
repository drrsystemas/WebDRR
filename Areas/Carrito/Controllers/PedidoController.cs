using DRR.Core.DBEmpresaEjemplo.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LibreriaBase.Areas.Carrito;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace WebDRR.Areas.Carrito.Controllers
{
    [Area("Carrito")]
    [Route("[controller]/[action]")]
    public class PedidoController : Controller
    {

        #region Variables
        private readonly IRepositorioProducto _repositorioProducto;
        private readonly IRepositorioCliente _repositorioCliente;
        private readonly IRepositorioPedido _repositorioPedido;
        private SessionAcceso _session;
        private readonly IEnviarCorreo _enviarCorreo;
        #endregion

        #region Constructor
        public PedidoController(IRepositorioProducto repositorioProducto, IRepositorioCliente repositorioCliente, IRepositorioPedido repositorioPedido, IEnviarCorreo enviarCorreo, IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            _repositorioProducto = repositorioProducto;
            _repositorioProducto.DatosSistema = _session.Sistema;
            _repositorioProducto.ElementosPorPagina = 25;

            _repositorioCliente = repositorioCliente;
            _repositorioCliente.DatosSistema = _session.Sistema;

            _repositorioPedido = repositorioPedido;
            _repositorioPedido.DatosSistema = _session.Sistema;
            _repositorioPedido.ElementosPorPagina = 25;

            _enviarCorreo = enviarCorreo;
        }
        #endregion


        /// <summary>
        /// Listado de pedidos por usuario-
        /// </summary>
        public IActionResult ListarPedidos(FiltroPedido filtroPedido)
        {
            try
            {
                PedidoViewModel viewModel = new PedidoViewModel();


                var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);



                #region Filtro -- Pedidos

                if (filtroPedido.ModoLectura == false)
                {
                    filtroPedido.AlmaUserId = _session.Usuario.AlmaUserID ?? 0;
                    filtroPedido.WebUserId = _session.Usuario.IdAlmaWeb;

                    filtroPedido.VendedorId = _session.Usuario.Cliente_Vendedor_Id;

                    //parado por el momento 
                    filtroPedido.SectorId = _session.Sistema.SectorId;
                    filtroPedido.Sector = _session.Sistema.NombreRepresentada;

                    filtroPedido.TipoEmpresa = _session.Sistema.TipoEmpresa;

                    //Funciona de 10 lo paro porque no esta el FILTRO ACTIVADO.
                    if (carrito?.Cliente?.ClienteID > 0)
                    {
                        filtroPedido.ClienteId = carrito.Cliente.ClienteID;
                        filtroPedido.Cliente = carrito.Cliente.RazonSocial;
                    }
                    //}


                }
                else
                {
                    filtroPedido.ModoLectura = false;
                }

                if(_session.Usuario.Rol == (Int32)EnumRol.Cliente || _session.Usuario.Rol == (Int32)EnumRol.ClienteFidelizado)
                {
                    filtroPedido.DiferenciaSieteDias();
                }



                #endregion

                DRR.Core.DBAlmaNET.Models.Impuesto ingB = _repositorioCliente.getImpuestoAlmaNet(900);

                Dictionary<Int32, List<PedidoView>> diccionario = _repositorioPedido.ListarPedidos(filtroPedido, ingB);
                var query = diccionario.FirstOrDefault().Value;
                int catidad = diccionario.FirstOrDefault().Key;

                //if (query?.Count()>0)
                //{
                //    query = query.OrderByDescending(x => x.Fecha).ToList();
                //}

                ViewData["Rol"] = _session?.Usuario?.Rol;
                ViewData["Representada"] = _session?.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada ? true : false;



                viewModel.UrlRetorno = HttpContext.Request.UrlAtras();
                viewModel.Filtro = filtroPedido;
                viewModel.Lista = query.ToList();
                viewModel.Paginacion = new Paginacion
                {
                    PaginaActual = filtroPedido.PaginaActual,
                    ElementosPorPagina = _repositorioPedido.ElementosPorPagina,
                    Elementos = catidad
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View();
            }
        }



        /// <summary>
        /// Se registra el pedido en la base de datos es unos de los metodos mas criticos.<br/>
        /// Modificado: 14/09/2021
        /// Modificado: 14/01/2022
        /// </summary>
        public IActionResult FinalizarPedido()
        {
            try
            {
                NotificacionesViewModel model = new NotificacionesViewModel();




                if (_session.Usuario != null && _session.Usuario.IdAlmaWeb > 0)
                {
                    var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                    String urlAtras = HttpContext.Request.Headers["Referer"].ToString();


                    if (carrito.TotalItems() <= 0)
                    {
                        TempData["ErrorRepresentada"] = "No se puede guardar el pedido sin ningun producto";

                        return Redirect(urlAtras);
                    }

                    if (carrito.Lista?.Any(c => c.SubTotal == 0 && c.Producto.Bonificacion == 0) == true)
                    {
                        TempData["ErrorRepresentada"] = "En el pedido hay elementos con precio 0, elimine ese producto y repita el paso.";

                        return Redirect(urlAtras);
                    }

                    if (_session.Usuario.Rol == (Int32)EnumRol.Vendedor)
                    {
                        //ACA CONTROLAR.
                        if (carrito.Cliente == null || carrito.Cliente.ClienteID == 0)
                        {
                            TempData["ErrorRepresentada"] = "Necesita seleccionar 1 cliente.";

                            return Redirect(urlAtras);
                        }
                    }
                    else
                    {
                        //Aca ingresaria en el caso de que el envio y retiro este desactivado.
                        if (carrito.Envio == null)
                        {
                            RepositorioCliente repositorioCliente = new RepositorioCliente();
                            repositorioCliente.DatosSistema = _session.Sistema;
                            var configuracionUsuario = repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);

                            if (configuracionUsuario?.Documento > 0)
                            {
                                carrito.Envio = new ViewEnvio(configuracionUsuario.ApellidoyNombre, configuracionUsuario.Celular, (Int16)configuracionUsuario.CodigoPostal, configuracionUsuario.Direccion);
                            }


                        }
                    }



                    #region ver si capturo hs del clientes




                    #endregion


                    String obs = HttpContext.Request.Query["obs"];
                    String cel = HttpContext.Request.Query["cel"];
                    //validar si es numerico.
                    String cp = HttpContext.Request.Query["cp"];
                    String dir = HttpContext.Request.Query["dir"];

                    String date = HttpContext.Request.Query["dte"];

                    DateTime fechaPedido = DateTime.Now.FechaHs_Argentina();

              
                    if (!String.IsNullOrWhiteSpace(obs))
                    {
                        carrito.Observacion = obs;
                    }


                    if (carrito?.Envio != null)
                    {
                        carrito.Envio.Celular = cel;
                        if (!String.IsNullOrEmpty(dir))
                        {
                            carrito.Envio.Domicilio = dir;

                        }
                    }


                    carrito.SectorId = _session.Sistema.SectorId;
                    var usuario = _session.Usuario.XmlConfiguracion; //_repositorioProducto.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);


                    #region Se agrega el costo de envio al detalle de la factura

                    if (carrito.Envio?.IdTipoEnvio == (Int32)ConfEnvio.EnumEnvio.Envio)
                    {
                        //Se verifica que no exita-
                        Boolean existe = carrito.Lista.Any(c => c.Producto.ProductoId == carrito.Envio.IdEnvio);

                        if (_session.Configuracion.Flete != null)
                        {
                            if (existe == false)
                            {
                                ProductoMinimo itemEnvio = new ProductoMinimo();
                                itemEnvio.LeyendaAdicionalOferta = "envio";
                                itemEnvio.ProductoId = (Int32)_session.Configuracion.Flete.Valor;
                                itemEnvio.Cantidad = 1;
                                itemEnvio.NombreCompleto = carrito.Envio.NombreIdEnvio + " " + carrito.Envio.NombreIdTipoEnvio + " " + carrito.Envio.NombreIdSucursal;
                                itemEnvio.PrecioBruto = carrito.Envio.Costo;
                                itemEnvio.PrecioNeto = carrito.Envio.Costo.GetNeto(21);
                                itemEnvio.ListaPrecID = _session.getListaPrecio(this.HttpContext);
                                itemEnvio.Nombre = "_envio_";
                                carrito.AgregarItem(itemEnvio, 1);
                            }
                        }


                    }
                    #endregion






                    #region Mercado Pago.

                    if (carrito.Pago?.IdPago == (Int32)ConfMetodosPago.EnumTiposPagos.Mercado_Pago)
                    {
                        //Esto esta aca porque cuando se elege el pago con MP
                        //se puede dar el caso de que al final no se quiere pagar con MP 
                        // y le da a la tecla retroceso y ahi se hace un quilombo, con esa
                        // opcion se suliciona.

                        if (String.IsNullOrEmpty(carrito.Pago.DatosPago))
                        {
                            carrito.Guardar();
                            return RedirectToAction("PagarCarritoMp", "MercadoPago");
                        }


                    }
                    #endregion

                    #region Tarjeta de Credito/Debito

                    else if (carrito.Pago?.IdPago == (Int32)ConfMetodosPago.EnumTiposPagos.Tarjeta)
                    {
                        if (String.IsNullOrEmpty(carrito.Pago.DatosPago))
                        {
                            carrito.Guardar();
                            return RedirectToAction("FormularioDatos", "Decidir");
                        }
                    }

                    #endregion


                    #region Registrar el pedido - Metodo de pago que se coordina con el Vendedor.


                    var ConfEsquemaRegistroPedido = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == 13);

                    Int32 esquemaRegistroPedido = 1;

                    if (ConfEsquemaRegistroPedido?.Valor != 0)
                    {
                        esquemaRegistroPedido = (int)(ConfEsquemaRegistroPedido?.Valor);
                    }

                    var vendedorDto = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == 14);
                    Int32 vendedorId = 0;
                    if (vendedorDto?.Valor != 0)
                    {
                        vendedorId = (Int32)vendedorDto?.Valor;
                    }

                    IRepositorioPedido repositorioPedido = new RepositorioPedido();
                    repositorioPedido.DatosSistema = _session.Sistema;

                    Boolean seAgregoElPedido = false;


                    //27-06-2022
                    if(carrito.EstadoId == 20 && carrito.PedidoId > 0)
                    {
                        carrito.EstadoId = 0;
                        return RedirectToAction("ModificarPedido", "Pedido", new {estadoId=carrito.EstadoId});
                    }




                    ParametroPedido parametro = new ParametroPedido(carrito, usuario, esquemaRegistroPedido, vendedorId, fechaPedido);

                    seAgregoElPedido = repositorioPedido.AgregarPedido(parametro);


                    #endregion


                    if (seAgregoElPedido == true)
                    {

                        if(carrito.EstadoId != 20)
                        {
                            if (_session.Usuario.Rol == (Int32)EnumRol.Cliente || _session.Usuario.Rol == (Int32)EnumRol.ClienteFidelizado)
                            {
                                #region Envio de Correo.

                                String detallePedido = carrito.ToString();

                                List<String> listaCorreos = new List<string>();

                                DatoConfiguracion conCorreo = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (Int32)ConfPortal.EnumConfPortal.Aviso_por_Correo);
                                if (conCorreo?.Valor.MostrarEntero() == 1 && !String.IsNullOrEmpty(conCorreo?.Extra))
                                {
                                    listaCorreos.Add(conCorreo.Extra);
                                    listaCorreos.Add(conCorreo.ExtraDos);
                                }

                                listaCorreos.Add(_session.Usuario.Correo);


                                _enviarCorreo.Conectar((Int32)_session.Sistema.EmpresaId);
                                Boolean envio = _enviarCorreo.EnvioMultiple(listaCorreos, "Pedido web n° " + carrito.PedidoId, detallePedido);


                                #endregion
                            }
                        }




                        //12-07-2021
                        carrito.Guardar();

                        //Se guarda el carrito en session.
                        HttpContext.Session.SetJson("CarritoReg", carrito);


                        

                        //Se elimina el carrito 
                        Boolean vendedor = false;
                        if (_session.Usuario.Rol == (Int32)EnumRol.Vendedor)
                        {
                            vendedor = true;
                        }
                        carrito?.Clear(vendedor);


                        
                        if(_session.Usuario.Rol == (Int32)EnumRol.Vendedor)
                        {

                            //******* 
                            Utilidades.CalcularTotales(HttpContext, _session);
                            //*******

                        }
                        else
                        {
                            _session.Usuario.XmlConfiguracion.Carrito_Temporal = "";
                            _repositorioCliente.Guardar_ConfiguracionXml_UsuarioWeb_Async((Int32)_session.Usuario.IdAlmaWeb, _session.Usuario.XmlConfiguracion.GetXML());
                        }



                        //Redireccionamos a la accion pedido exitoso.
                        return RedirectToAction("PedidoExitoso");
                    }
                    else
                    {
                        model.Id = -1;
                        model.Mensaje = "El pedido no se pudo registrar";

                        String url = Url.Action("Notificaciones", "Home", model);

                        return Redirect(url);

                    }


                }
                else
                {
                    model.Id = -1;
                    model.Mensaje = "Para finalizar el pedido necesita registrarse, realize el registro y repita la operacion";

                    String url = Url.Action("Notificaciones", "Home", model);

                    return Redirect(url);
                }

            }
            catch (Exception ex)
            {
                NotificacionesViewModel model = new NotificacionesViewModel();
                model.Id = -1;
                model.Mensaje = "No se pudo registrar el pedido, en caso de ser posible, pongase en contacto con nosotros, gracias.";

                String url = Url.Action("Notificaciones", "Home", model);

                return Redirect(url);
            }


        }


        /// <summary>
        /// Se registra el pedido en la base de datos es unos de los metodos mas criticos.
        /// </summary>
        public IActionResult ModificarPedido([Optional] byte? estadoId)
        {
            NotificacionesViewModel model = new NotificacionesViewModel();

            if (_session.Usuario != null && _session.Usuario.IdAlmaWeb > 0)
            {
                var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                //En el caso de los temporales.
                if(estadoId!=null)
                {
                    carrito.EstadoId = (byte)estadoId;
                }


                String urlAtras = HttpContext.Request.Headers["Referer"].ToString();

                if (carrito.PedidoId <= 0)
                {
                    TempData["ErrorRepresentada"] = "No se puede modificar el pedido actual";

                    return Redirect(urlAtras);
                }

                if (carrito.TotalItems() <= 0)
                {
                    TempData["ErrorRepresentada"] = "No se puede guardar el pedido sin ningun producto";

                    return Redirect(urlAtras);
                }

                if (carrito.Lista?.Any(c => c.SubTotal == 0 && c.Producto.Bonificacion == 0) == true)
                {
                    TempData["ErrorRepresentada"] = "En el pedido hay elementos con precio 0, elimine ese producto y repita el paso.";

                    return Redirect(urlAtras);
                }

                if (_session.Usuario.Rol == 4)
                {
                    //ACA CONTROLAR.
                    if (carrito.Cliente == null || carrito.Cliente.ClienteID == 0)
                    {
                        TempData["ErrorRepresentada"] = "Necesita seleccionar 1 cliente.";

                        return Redirect(urlAtras);
                    }
                }

                #region ver si capturo hs del clientes




                #endregion


                String date = HttpContext.Request.Query["dte"];
                DateTime fechaPedido = DateTime.Now;
                if (!String.IsNullOrEmpty(date))
                {
                    try
                    {
                        string[] vector = date.Split(" ");
                        string[] fecha = vector[0].Split("/");
                        string[] hs = vector[1].Split(":");

                        fechaPedido = new DateTime(Convert.ToInt32(fecha[2]), Convert.ToInt32(fecha[1]), Convert.ToInt32(fecha[0]), Convert.ToInt32(hs[0]), Convert.ToInt32(hs[1]), 0);

                    }
                    catch (Exception)
                    {
                        fechaPedido = DateTime.Now;
                    }
                }




                carrito.SectorId = _session.Sistema.SectorId;
                var usuario = _repositorioProducto.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);

                #region Se agrega el costo de envio al detalle de la factura

                if (carrito.Envio?.IdTipoEnvio == (Int32)ConfEnvio.EnumEnvio.Envio)
                {
                    //Se verifica que no exita-
                    Boolean existe = carrito.Lista.Any(c => c.Producto.ProductoId == carrito.Envio.IdEnvio);

                    if (existe == false)
                    {
                        ProductoMinimo itemEnvio = new ProductoMinimo();
                        itemEnvio.LeyendaAdicionalOferta = "envio";
                        itemEnvio.ProductoId = (Int32)_session.Configuracion.Flete.Valor;
                        itemEnvio.Cantidad = 1;
                        itemEnvio.NombreCompleto = carrito.Envio.NombreIdEnvio + " " + carrito.Envio.NombreIdTipoEnvio + " " + carrito.Envio.NombreIdSucursal;
                        itemEnvio.PrecioBruto = carrito.Envio.Costo;
                        itemEnvio.PrecioNeto = carrito.Envio.Costo.GetNeto(21);
                        itemEnvio.ListaPrecID = _session.getListaPrecio(this.HttpContext);
                        itemEnvio.Nombre = "_envio_";
                        carrito.AgregarItem(itemEnvio, 1);
                    }

                }
                #endregion

                var ConfEsquemaRegistroPedido = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == 13);

                Int32 esquemaRegistroPedido = 1;

                if (ConfEsquemaRegistroPedido?.Valor != 0)
                {
                    esquemaRegistroPedido = (int)(ConfEsquemaRegistroPedido?.Valor);
                }

                var vendedorDto = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == 14);
                Int32 vendedorId = 0;
                if (vendedorDto?.Valor != 0)
                {
                    vendedorId = (Int32)vendedorDto?.Valor;
                }

                IRepositorioPedido repositorioPedido = new RepositorioPedido();
                repositorioPedido.DatosSistema = _session.Sistema;


                ParametroPedido parametro = new ParametroPedido(carrito, usuario, esquemaRegistroPedido, vendedorId, fechaPedido);


                Boolean ok = repositorioPedido.ModificarPedido(parametro);

                if (ok == true)
                {

                    if (carrito.Pago?.IdPago == (Int32)ConfMetodosPago.EnumTiposPagos.Mercado_Pago)
                    {
                        //Esto esta aca porque cuando se elege el pago con MP
                        //se puede dar el caso de que al final no se quiere pagar con MP 
                        // y le da a la tecla retroceso y ahi se hace un quilombo, con esa
                        // opcion se suliciona.
                        TempData["PedidoNuevo"] = carrito.PedidoId;

                        carrito.Guardar();
                        return RedirectToAction("PagarCarritoMp", "MercadoPago");
                    }
                    else
                    {
                        String detallePedido = "";

                        detallePedido = carrito.ToString();

                        List<String> listaCorreos = new List<string>();
                        //listaCorreos.Add("luisitosalvarezza@gmail.com");
                        //listaCorreos.Add("luissalvarezza@hotmail.com");
                        listaCorreos.Add(_session.Sistema.Correo);
                        listaCorreos.Add(_session.Usuario.Correo);


                        _enviarCorreo.Conectar((Int32)_session.Sistema.EmpresaId);

                        Boolean envio = _enviarCorreo.EnvioMultiple(listaCorreos, "Pedido web n° " + carrito.PedidoId, detallePedido);


                        //Se guarda el carrito en session.
                        HttpContext.Session.SetJson("CarritoReg", carrito);

                        //Se elimina el carrito 
                        Boolean vendedor = true;
                        if (_session.Usuario.Rol == 2)
                        {
                            vendedor = false;
                        }
                        carrito?.Clear(vendedor);

                        //******* 
                        if (_session.Usuario.Rol == (Int32)EnumRol.Vendedor)
                        {

                            //******* 
                            Utilidades.CalcularTotales(HttpContext, _session);
                            //*******

                        }
                        else
                        {
                            _session.Usuario.XmlConfiguracion.Carrito_Temporal = "";
                            _repositorioCliente.Guardar_ConfiguracionXml_UsuarioWeb_Async((Int32)_session.Usuario.IdAlmaWeb, _session.Usuario.XmlConfiguracion.GetXML());
                        }

                        //*******

                        //Redireccionamos a la accion pedido exitoso.
                        return RedirectToAction("PedidoExitoso");
                    }

                }
                else
                {
                    model.Id = -1;
                    model.Mensaje = "El pedido no se pudo registrar";

                    String url = Url.Action("Notificaciones", "Home", model);

                    return Redirect(url);
                }


            }
            else
            {
                model.Id = -1;
                model.Mensaje = "Para finalizar el pedido necesita registrarse, realize el registro y repita la operacion";

                String url = Url.Action("Notificaciones", "Home", model);

                return Redirect(url);
            }
        }




        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult CapturarFiltros(IFormCollection formCollection)
        {

            String json = formCollection["json"];
            Int32 operacion = Convert.ToInt32(formCollection["operacion"]);

            FiltroPedido filtroPedido = JsonConvert.DeserializeObject<FiltroPedido>(json);

            filtroPedido.ModoLectura = true;

            if (operacion == 1)
            {
                filtroPedido.SectorId = null;
                filtroPedido.Sector = "";
            }
            else if (operacion == 2)
            {
                filtroPedido.ClienteId = null;
                filtroPedido.Cliente = "";
            }
            else if (operacion == 3)
            {

                filtroPedido.FechaDesde = Convert.ToDateTime(formCollection["Filtro.FechaDesde"]);
                filtroPedido.FechaHasta = Convert.ToDateTime(formCollection["Filtro.FechaHasta"]);
                String valueTodo = formCollection["Filtro.Todos"].First();
                filtroPedido.Todos = valueTodo == "false" ? false : true;
            }



            return RedirectToAction("ListarPedidos", "Pedido", filtroPedido);
        }





        /// <summary>
        /// En esta pantalla se termina de completar el pedido es donde se guardan los datos de envio/retiro y pago-
        /// </summary>
        public IActionResult RegistrarPedido()
        {
            NotificacionesViewModel model = new NotificacionesViewModel();

            if (_session.Usuario != null && _session.Usuario.IdAlmaWeb > 0)
            {
                //ESTO ES PORQUE NO SE COMO SE LLEVA LA CUENTA DE ITEMS
                if (TempData.ContainsKey("PedidoNuevo"))
                {
                    var result = TempData["PedidoNuevo"];
                    if (result != null)
                    {
                        int idPedido = Convert.ToInt32(result);

                        return RedirectToAction("PedidoExitoso", "Pedido", new { id = idPedido });
                    }

                }



                SessionCarrito carrito = HttpContext.Session.GetJson<SessionCarrito>("Carrito") ?? new SessionCarrito();

                if (carrito.Lista.Count() == 0)
                {
                    return RedirectToAction("Productos", "Producto");
                }
                else
                {
                    #region Datos Envio......

                    //Aca ingresaria en el caso de que el envio y retiro este desactivado.
                    if (carrito.Envio == null)
                    {
                        RepositorioCliente repositorioCliente = new RepositorioCliente();
                        repositorioCliente.DatosSistema = _session.Sistema;
                        var configuracionUsuario = repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);

                        if (configuracionUsuario?.Documento > 0)
                        {
                            carrito.Envio = new ViewEnvio(configuracionUsuario.ApellidoyNombre, configuracionUsuario.Celular, (Int16)configuracionUsuario.CodigoPostal, configuracionUsuario.Direccion);
                        }

                    }


                    IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                    repositorioEmpresa.DatosSistema = _session.Sistema;
                    ConfEnvio conf = new ConfEnvio();
                    ViewBag.ListaTipoEnvio = conf.GetListaTiposEnvios();


                    //No mal..
                    var confenvios = repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);
                    ViewBag.ListaEnvio = confenvios?.ConfiguracionesEnvio;


                    var query = repositorioEmpresa.ListaSucursales(null);

                    List<SelectListItem> listaSuc = new List<SelectListItem>();

                    if (query?.Count() > 0)
                    {
                        foreach (var item in query)
                        {
                            SelectListItem elemento = new SelectListItem();
                            elemento.Value = item.SucursalId.ToString();
                            elemento.Text = item.DescripcionSucursal + " - " + item.Domicilio;

                            listaSuc.Add(elemento);
                        }
                    }


                    ViewBag.ListaSucursales = listaSuc;



                    #endregion

                    #region Datos Pago

                    ViewPago pago = new ViewPago();

                    var confpagos = repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);
                    ViewBag.ListaTipoPago = confpagos?.ConfiguracionesPago.Where(c => c.Valor == 1);


                    if (carrito.Pago == null)
                    {
                        carrito.Pago = new ViewPago();
                    }
                    #endregion


                    //if(carrito.EstadoId == 20)
                    //{
                    //    carrito.EstadoId = 0;
                    //    carrito.Guardar();

                    //    return RedirectToAction("ModificarPedido", "Pedido");
                    //}
                    //else
                    //{
                        return View(carrito);
                    //}

                    
                }


            }
            else
            {
                model.Id = -1;
                model.Mensaje = "Para registrar el pedido necesita registrarse, realize el registro y repita la operacion";

                String url = Url.Action("Notificaciones", "Home", model);

                return Redirect(url);
            }
        }


        /// <summary>
        /// Muestra que el pedido se registro con exito en la base de datos.
        /// Modificacion: 15/09/2021
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult PedidoExitoso()
        {
            String pedido = "";

            //Recuperamos de session el carrito
            var carritoP = HttpContext.Session.GetJson<LibreriaBase.Areas.Carrito.Clases.Carrito>("CarritoReg");

            if (carritoP != null)
            {


                

                //Se quita
                HttpContext.Session.Remove("CarritoReg");

                CarritoIndexViewModel civModel = new CarritoIndexViewModel(carritoP, "", _session.Sistema.TipoEmpresa);

                return View(civModel);
            }
            else
            {
                return RedirectToAction("Index", "Carrito");
            }
        }

        private void guardarEnvioPago(LibreriaBase.Areas.Carrito.Clases.Carrito carrito)
        {
            //Actualizar los datos en la base de datos.......
            DatosEnvioPago datosEnvioPago = new DatosEnvioPago();
            datosEnvioPago.Envio = carrito.Envio;
            datosEnvioPago.Pago = carrito.Pago;
            datosEnvioPago.UsuarioWeb = _repositorioProducto.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);
            //------------------------------------------------

            String datos = datosEnvioPago.GetXML();
            IRepositorioPedido repositorioPedido = new RepositorioPedido();
            repositorioPedido.DatosSistema = _session.Sistema;
            repositorioPedido.ActualizarDatosEnvioPago((Int32)carrito.PedidoId, datos);
            //------------------------------------------------
        }


        /// <summary>
        /// Genera el pdf del pedido.
        /// </summary>
        /// <param name="carrito"></param>
        /// <returns>pdf</returns>
        public IActionResult GenerarPdf(String codigo, String sector, String fecha, String cliente)
        {
            try
            {
                LibreriaBase.Areas.Carrito.Clases.Carrito carrito = null;
                IRepositorioPedido repositorioPedido = new RepositorioPedido();
                repositorioPedido.DatosSistema = _session.Sistema;


                if (!String.IsNullOrEmpty(codigo))
                {
                    Int32 pedidoId = Convert.ToInt32(codigo.DesEncriptar());



                    carrito = repositorioPedido.GetCarrito(pedidoId);
                }

                #region esto esta sin testear
                fecha = carrito.FechaPedido.Value.FechaFormateada();

                if (String.IsNullOrEmpty(sector))
                {
                    sector = "[" + _session?.Sistema?.SectorId + "] " + _session?.Sistema?.NombreRepresentada;
                }

                ViewCliente viewCliente = null;

                if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                {

                    IRepositorioCliente repCliente = new RepositorioCliente();
                    repCliente.DatosSistema = _session.Sistema;

                    viewCliente = repCliente.GetCliente((Int32)carrito.Cliente?.ClienteID);

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

                IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                repositorioEmpresa.DatosSistema = _session.Sistema;
                var empresaViewModel = repositorioEmpresa.GetEmpresa((int)_session.Sistema.EmpresaId);

                BaseFont helvetica = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, true);
                iTextSharp.text.Font titulo = new iTextSharp.text.Font(helvetica, 16f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));
                iTextSharp.text.Font subtitulo = new iTextSharp.text.Font(helvetica, 12f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));
                iTextSharp.text.Font parrafo = new iTextSharp.text.Font(helvetica, 10f, iTextSharp.text.Font.NORMAL, new BaseColor(0, 0, 0));
                iTextSharp.text.Font negrita = new iTextSharp.text.Font(helvetica, 9f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));
                iTextSharp.text.Font texto_blanco = new iTextSharp.text.Font(helvetica, 10f, iTextSharp.text.Font.BOLD, new BaseColor(255, 255, 255));
                iTextSharp.text.Font toinvoice = new iTextSharp.text.Font(helvetica, 20f, iTextSharp.text.Font.BOLD, new BaseColor(255, 255, 255));

                Document doc = new Document(PageSize.A4);
                doc.SetMargins(20f, 20f, 20f, 20f);
                MemoryStream ms = new MemoryStream();
                PdfWriter writer = PdfWriter.GetInstance(doc, ms);

                doc.AddAuthor(empresaViewModel.RazonSocial);
                doc.AddTitle("Pedido N° " + carrito.PedidoId);
                doc.Open();

                iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(empresaViewModel.Logo);
                logo.ScaleToFit(200, 200);
                doc.Add(logo);

                //Para el calculo de subtotales.......
                carrito.Cliente = viewCliente;

                #region Datos de cabecera
                Paragraph para1 = new Paragraph();
                Phrase ph2 = new Phrase();
                Paragraph mm1 = new Paragraph();

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("Reporte de Pedido", FontFactory.GetFont("Arial", 20, 2)));

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
                ph2.Add(new Chunk("Código de su pedido: " + carrito.PedidoId, FontFactory.GetFont("Arial", 10, 1)));

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("fecha: " + fecha, FontFactory.GetFont("Arial", 10, 1)));



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

                PdfPTable tabla = new PdfPTable(new float[] { 10f, 55f, 5f, 15f, 15f }) { WidthPercentage = 100f };
                PdfPCell c1 = new PdfPCell(new Phrase("Cantidad", negrita));
                PdfPCell c2 = new PdfPCell(new Phrase("Producto", negrita));
                PdfPCell cBonfi = new PdfPCell(new Phrase("Bonf.", negrita));
                PdfPCell c3 = new PdfPCell(new Phrase("Precio", negrita));
                PdfPCell c4 = new PdfPCell(new Phrase("Subtotal", negrita));

                c1.Border = 0;
                c2.Border = 0;
                cBonfi.Border = 0;
                c3.Border = 0;
                c4.Border = 0;

                c3.HorizontalAlignment = Element.ALIGN_RIGHT;
                c4.HorizontalAlignment = Element.ALIGN_RIGHT;

                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(cBonfi);
                tabla.AddCell(c3);
                tabla.AddCell(c4);
                #endregion

                #region se carga los datos de la tabla
                Boolean presiosFinales = true;

                var conf = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 12);
                if (conf != null)
                {
                    presiosFinales = conf.Valor == 0 ? true : false;
                }

                Int32 i = 0;
                for (i = 0; i < carrito.Lista.Count(); i++)
                {
                    if (i % 2 == 0)
                    {
                        c1.BackgroundColor = new BaseColor(204, 204, 204);
                        c2.BackgroundColor = new BaseColor(204, 204, 204);
                        cBonfi.BackgroundColor = new BaseColor(204, 204, 204);
                        c3.BackgroundColor = new BaseColor(204, 204, 204);
                        c4.BackgroundColor = new BaseColor(204, 204, 204);
                    }
                    else
                    {
                        c1.BackgroundColor = BaseColor.White;
                        c2.BackgroundColor = BaseColor.White;
                        cBonfi.BackgroundColor = BaseColor.White;
                        c3.BackgroundColor = BaseColor.White;
                        c4.BackgroundColor = BaseColor.White;
                    }

                    //Se obtiene el item del carrito
                    var item = carrito.Lista.ElementAt(i);

                    //Se crea una nueva fila con los datos-
                    if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                    {
                        c1.Phrase = new Phrase(item.Cantidad.MostrarEntero().ToString());
                    }
                    else
                    {
                        c1.Phrase = new Phrase(item.Cantidad.MostrarEntero().ToString());
                    }




                    if (_session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                    {
                        String codigoproveedor = item.Producto.CodigoProveedor;
                        if (string.IsNullOrWhiteSpace(codigoproveedor))
                        {
                            codigoproveedor = item.Producto.ProductoId.ToString();
                        }

                        c2.Phrase = new Phrase("(" + codigoproveedor + ") " + item.Producto.NombreCompleto);

                    }
                    else
                    {
                        c2.Phrase = new Phrase("(" + item.Producto.ProductoId.ToString() + ") " + item.Producto.NombreCompleto);

                    }

                    int bonf = item.Producto.Bonificacion.MostrarEntero();
                    if (bonf > 0)
                    {
                        cBonfi.Phrase = new Phrase(bonf.ToString());
                        //item.aplicarBonficacion(bonf);
                    }
                    else
                    {
                        cBonfi.Phrase = new Phrase("");
                    }

                    if (presiosFinales == true)
                    {
                        
                        c3.Phrase = new Phrase(item.Producto.PrecioBruto.FormatoMoneda());
                        c4.Phrase = new Phrase(item.SubTotal.FormatoMoneda());


                    }
                    else
                    {
                        c3.Phrase = new Phrase(item.Producto.PrecioNeto.FormatoMoneda());
                        c4.Phrase = new Phrase(item.SubTotalNeto.FormatoMoneda());


                    }

                    tabla.AddCell(c1);
                    tabla.AddCell(c2);
                    tabla.AddCell(cBonfi);
                    tabla.AddCell(c3);
                    tabla.AddCell(c4);
                }

                //c1.Colspan = 4;
                //c1.Phrase = new Phrase("");
                //c1.HorizontalAlignment = Element.ALIGN_RIGHT;

                //tabla.AddCell(c1);

                //c1.Colspan = 4;
                //if (presiosFinales == true)
                //{
                //    c1.Phrase = new Phrase(carrito.TotalCarrito().FormatoMoneda());
                //}
                //else
                //{
                //    c1.Phrase = new Phrase(carrito.TotalNetoCarrito().FormatoMoneda());
                //}

                //c1.HorizontalAlignment = Element.ALIGN_RIGHT;

                //tabla.AddCell(c1);
                #endregion


                doc.Add(tabla);



                #region esquema totales -- Se empieza a utilizar los subtotales -- 19-05-2021
                if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                {
                    Subtotales subTotales = carrito.Get_Totales();
                    decimal iibb = 0;

                    Paragraph paraTotales = new Paragraph();
                    Phrase phparaTotales = new Phrase();
                    Paragraph mmparaTotales = new Paragraph();

                    paraTotales.Add(new Chunk(Environment.NewLine));

                    if (!carrito.Descuento.IsNullOrValue(0))
                    {

                        paraTotales.Add(new Chunk("SUBTOTAL NETO: " + subTotales.SubTotalNeto.FormatoMoneda(), FontFactory.GetFont("Arial", 10, 2)));
                        paraTotales.Add(new Chunk(Environment.NewLine));

                        //paraTotales.Add(new Chunk("----------------------------"));
                        //paraTotales.Add(new Chunk(Environment.NewLine));
                        paraTotales.Add(new Chunk("DESC: %" + carrito.Descuento.MostrarEntero(), FontFactory.GetFont("Arial", 10, 2)));
                        paraTotales.Add(new Chunk(Environment.NewLine));

                    }

                    //paraTotales.Add(new Chunk("----------------------------"));
                    //paraTotales.Add(new Chunk(Environment.NewLine));

                    paraTotales.Add(new Chunk("NETO: " + subTotales.Neto.FormatoMoneda(), FontFactory.GetFont("Arial", 10, 2)));

                    paraTotales.Add(new Chunk(Environment.NewLine));
                    paraTotales.Add(new Chunk("IVA: " + subTotales.Iva.FormatoMoneda(), FontFactory.GetFont("Arial", 10, 2)));

                    //paraTotales.Add(new Chunk(Environment.NewLine));
                    //paraTotales.Add(new Chunk("BRUTO: " + carrito.TotalCarrito().FormatoMoneda(), FontFactory.GetFont("Arial", 10, 2)));

                    //paraTotales.Add(new Chunk(Environment.NewLine));
                    //paraTotales.Add(new Chunk("----------------------------"));
                    paraTotales.Add(new Chunk(Environment.NewLine));

                    if (viewCliente?.Impuesto?.ImpuestoID == 900)
                    {
                        decimal totalCarrito = carrito.TotalCarrito();

                        iibb = totalCarrito.Importe_Impuesto((decimal)viewCliente?.Impuesto?.PorcentajeAlicuota,
                               (decimal)viewCliente?.Impuesto?.ImporteMinimo);


                        paraTotales.Add(new Chunk("IIBB: " + subTotales.IIBB.FormatoMoneda(), FontFactory.GetFont("Arial", 10, 2)));
                        paraTotales.Add(new Chunk(Environment.NewLine));
                        paraTotales.Add(new Chunk("----------------------------"));
                        paraTotales.Add(new Chunk(Environment.NewLine));
                    }

                    paraTotales.Add(new Chunk("TOTAL: " + subTotales.Total.FormatoMoneda(), FontFactory.GetFont("Arial", 12, 2))); paraTotales.Add(new Chunk(Environment.NewLine));
                    paraTotales.Add(new Chunk(Environment.NewLine));

                    mmparaTotales.Alignment = Element.ALIGN_RIGHT;

                    phparaTotales.Add(paraTotales);
                    mmparaTotales.Add(phparaTotales);
                    doc.Add(mmparaTotales);
                }
                else
                {
                    Paragraph paraTotales = new Paragraph();
                    Phrase phparaTotales = new Phrase();
                    Paragraph mmparaTotales = new Paragraph();


                    paraTotales.Add(new Chunk("TOTAL: " + carrito.TotalCarrito().FormatoMoneda(), FontFactory.GetFont("Arial", 12, 2))); paraTotales.Add(new Chunk(Environment.NewLine));
                    paraTotales.Add(new Chunk(Environment.NewLine));

                    mmparaTotales.Alignment = Element.ALIGN_RIGHT;

                    phparaTotales.Add(paraTotales);
                    mmparaTotales.Add(phparaTotales);
                    doc.Add(mmparaTotales);
                }
                #endregion




                #region Observacion Carrito-
                if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                {
                    Paragraph paraObservaciones = new Paragraph();
                    Phrase phObservaciones = new Phrase();
                    Paragraph mmObservaciones = new Paragraph();


                    phObservaciones.Add(new Chunk(Environment.NewLine));
                    phObservaciones.Add(new Chunk("Observaciones Pedido: " + carrito.Observacion, FontFactory.GetFont("Arial", 10, 2)));
                    phObservaciones.Add(new Chunk(Environment.NewLine));


                    mmObservaciones.Add(phObservaciones);
                    paraObservaciones.Add(mmObservaciones);
                    doc.Add(paraObservaciones);
                }
                #endregion


                if (_session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    Int32 transporteId = repositorioPedido.ObtenerTransporteId(carrito.PedidoId ?? 0);

                    if (transporteId > 0)
                    {

                        var datosTrans = _repositorioProducto.ListarTransportes(transporteId)?.FirstOrDefault();

                        if (datosTrans != null)
                        {
                            Paragraph paraTransporte = new Paragraph();
                            Phrase phTransporte = new Phrase();
                            Paragraph mmTransporte = new Paragraph();


                            phTransporte.Add(new Chunk(Environment.NewLine));
                            phTransporte.Add(new Chunk("Transporte Seleccionado: " + datosTrans.Nombre, FontFactory.GetFont("Arial", 10, 2)));
                            phTransporte.Add(new Chunk(Environment.NewLine));


                            mmTransporte.Add(phTransporte);
                            paraTransporte.Add(mmTransporte);
                            doc.Add(paraTransporte);
                        }

                    }

                }




                #region Datos Envio
                //Ver clases -- ConfEnvio.cs  y  ViewEnvio.cs


                Paragraph para2 = new Paragraph();
                Phrase ph3 = new Phrase();
                Paragraph mm2 = new Paragraph();

                if (carrito.Envio != null)
                {
                    if (carrito.Envio.IdTipoEnvio > 0)
                    {
                        String infoEnvioRetiro = "[" + carrito.Envio.IdTipoEnvio + "] ";

                        if (carrito.Envio.IdTipoEnvio == 1)
                        {
                            infoEnvioRetiro += "Retiro por sucursal";
                        }
                        else
                        {
                            infoEnvioRetiro += "Envio a domicilio";
                        }

                        ph3.Add(new Chunk(Environment.NewLine));
                        ph3.Add(new Chunk("Tipo: " + infoEnvioRetiro, FontFactory.GetFont("Arial", 10, 2)));
                        ph3.Add(new Chunk(Environment.NewLine));
                    }



                    //ph3.Add(new Chunk(Environment.NewLine));
                    //ph3.Add(new Chunk(carrito.Envio.NombreIdEnvio, FontFactory.GetFont("Arial", 10, 2)));
                    //ph3.Add(new Chunk(Environment.NewLine));

                    //ph3.Add(new Chunk(Environment.NewLine));
                    if (carrito.Envio.IdTipoEnvio == 1)
                    {
                        String sucursal = "[" + carrito.Envio.IdSucursal + "] " + carrito.Envio.NombreIdSucursal;

                        ph3.Add(new Chunk("Sucursal: " + sucursal, FontFactory.GetFont("Arial", 10, 2)));
                        ph3.Add(new Chunk(Environment.NewLine));
                    }
                }

                mm2.Add(ph3);
                para2.Add(mm2);
                doc.Add(para2);


                #endregion




                Paragraph para3 = new Paragraph();
                Phrase ph4 = new Phrase();
                Paragraph mm3 = new Paragraph();

                if (carrito.Pago != null)
                {
                    //ph4.Add(new Chunk(Environment.NewLine));
                    ph4.Add(new Chunk("Pago: " + carrito.Pago.NombreIdPago, FontFactory.GetFont("Arial", 10, 2)));
                    ph4.Add(new Chunk(Environment.NewLine));

                    ph4.Add(new Chunk(carrito.Pago.DatosPago, FontFactory.GetFont("Arial", 10, 2)));
                    ph4.Add(new Chunk(Environment.NewLine));

                    if (!String.IsNullOrEmpty(carrito.Pago.Observacion))
                    {
                        ph4.Add(new Chunk("Obs: " + carrito.Pago.Observacion, FontFactory.GetFont("Arial", 10, 2)));
                        ph4.Add(new Chunk(Environment.NewLine));
                    }


                    if (carrito.Pago?.Transferencia != null)
                    {
                        ph4.Add(new Chunk("Datos Bancarios", FontFactory.GetFont("Arial", 10, 2)));
                        ph4.Add(new Chunk("Banco: " + carrito.Pago.Transferencia.Banco, FontFactory.GetFont("Arial", 10, 2)));
                        ph4.Add(new Chunk("CBU: " + carrito.Pago.Transferencia.CBU, FontFactory.GetFont("Arial", 10, 2)));
                        ph4.Add(new Chunk("Alias: " + carrito.Pago.Transferencia.Alias, FontFactory.GetFont("Arial", 10, 2)));
                    }

                }

                mm3.Add(ph4);
                para3.Add(mm3);
                doc.Add(para3);



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


        private void envioDeCorreosAviso(LibreriaBase.Areas.Carrito.Clases.Carrito carrito)
        {
            _enviarCorreo.Conectar((Int32)_session.Sistema.EmpresaId);


            Boolean enviado = _enviarCorreo.Enviar(_session.Sistema.Correo,
              "Pedido web n° " + carrito.PedidoId,
              carrito.ToString());

            //Correo al Usuario
            Boolean enviado1 = _enviarCorreo.Enviar(_session.Usuario.Correo,
                               "Pedido web n° " + carrito.PedidoId,
                               carrito.ToString());

            if (_session.Sistema.EmpresaId == 88)
            {
                //Control temporal para ver como viene el tema de los pedidos
                Boolean enviado3 = _enviarCorreo.Enviar("Nataliadenissehobus@gmail.com",
               "Pedido web n° " + carrito.PedidoId,
               carrito.ToString());
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="codigo"></param>
        /// <returns></returns>
        public IActionResult ObtenerPedido(String codigo, int tipoOperacion = (int)EnumTipoOperacion.Agregar)
        {

            if (!String.IsNullOrEmpty(codigo))
            {
                Int32 pedidoId = Convert.ToInt32(codigo.DesEncriptar());

                IRepositorioPedido repositorioPedido = new RepositorioPedido();
                repositorioPedido.DatosSistema = _session.Sistema;
                LibreriaBase.Areas.Carrito.Clases.Carrito carritoBaseDatos = repositorioPedido.GetCarrito(pedidoId);


                if (tipoOperacion == (int)EnumTipoOperacion.Eliminar)
                {
                    HttpContext.Session.SetJson("Carrito", carritoBaseDatos);
                }
                else
                {


                    IRepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = _session.Sistema;

                    //carritoBaseDatos.Cliente = repositorioCliente.GetCliente(carritoBaseDatos.Cliente.ClienteID);

                    ViewCliente viewCliente = repositorioCliente.GetCliente((Int32)carritoBaseDatos.Cliente?.ClienteID);

                    #region  Obtenr IIBB del cliente

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

                    carritoBaseDatos.Cliente = viewCliente;

                    if (tipoOperacion == (int)EnumTipoOperacion.Agregar)
                    {
                        carritoBaseDatos.PedidoId = null;
                    }

                    HttpContext.Session.SetJson("Carrito", carritoBaseDatos);
                }





            }


            return RedirectToAction("Index", "Carrito");
        }













        public IActionResult GenerarPdfListaPedidos(FiltroPedido filtro)
        {

            List<PedidoView> lista;

            DRR.Core.DBAlmaNET.Models.Impuesto ingB = _repositorioCliente.getImpuestoAlmaNet(900);

            filtro.Orden = FiltroPedido_Orden.Id_Asc;
            filtro.VendedorId = _session.Usuario.Cliente_Vendedor_Id;

            Dictionary<Int32, List<PedidoView>> diccionario = _repositorioPedido.ListarPedidos(filtro, ingB);
            lista = diccionario.FirstOrDefault().Value;
            int catidad = diccionario.FirstOrDefault().Key;





            //Secrea el documento
            iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4);
            doc.SetMargins(20f, 20f, 20f, 20f);

            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);

            //Las cuestiones basicas.
            doc.AddAuthor("Listado Pedidos");
            //doc.AddTitle("Cliente :" + cliente.RazonSocial);
            doc.Open();

            String infoPedido = "Listado de Pedidos";

            if (!String.IsNullOrEmpty(filtro.Cliente))
            {
                infoPedido += $"Cliente: {filtro.Cliente}";
            }

            if (filtro.Todos == false)
            {

                infoPedido += $" - Fechas: {filtro.FechaDesde.FechaCorta()} a {filtro.FechaHasta.FechaCorta()}";
            }



            #region Datos de cabecera
            Paragraph para1 = new Paragraph();
            Phrase ph2 = new Phrase();
            Paragraph mm1 = new Paragraph();

            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk(infoPedido, FontFactory.GetFont("Arial", 20, 2)));
            ph2.Add(new Chunk(Environment.NewLine));

            ph2.Add(new Chunk("Vendedor: " + _session.Usuario.Nombre, FontFactory.GetFont("Arial", 16, 2)));
            ph2.Add(new Chunk(Environment.NewLine));


            ph2.Add(new Chunk($"N° Pedidos: {lista.Count().ToString()} ", FontFactory.GetFont("Arial", 14, 2, BaseColor.Orange)));
            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk($"$ Total: {lista.Sum(c => c.Total).FormatoMoneda()} ", FontFactory.GetFont("Arial", 14, 2, BaseColor.Orange)));
            ph2.Add(new Chunk(Environment.NewLine));

            mm1.Add(ph2);
            para1.Add(mm1);
            doc.Add(para1);

            #endregion



            BaseFont helvetica = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, true);

            iTextSharp.text.Font negrita = new iTextSharp.text.Font(helvetica, 9f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));


            #region Esquema basico de la tabla

            PdfPTable tabla = new PdfPTable(new float[] { 10f, 60f, 15f, 15f }) { WidthPercentage = 100f };
            PdfPCell c1 = new PdfPCell(new Phrase("Código", negrita));
            PdfPCell c2 = new PdfPCell(new Phrase("Cliente", negrita));
            PdfPCell c3 = new PdfPCell(new Phrase("Fecha", negrita));
            PdfPCell c4 = new PdfPCell(new Phrase("Total", negrita));

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

            //Boolean presiosFinales = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == 12).Valor == 0 ? true : false;

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
                c1.Phrase = new Phrase(item.PedidoId.ToString());

                c2.Phrase = new Phrase(item.NombreCliente);

                c3.Phrase = new Phrase(item.Fecha.ToString("dd/MM H:mm"));

                c4.Phrase = new Phrase(item.Total.FormatoMoneda());


                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(c3);
                tabla.AddCell(c4);
            }

            #endregion
            doc.Add(tabla);





            writer.Close();
            doc.Close();
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms, "application/pdf");

        }




    }
}