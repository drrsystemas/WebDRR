using LibreriaBase.Areas.Carrito;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using WebDRR.Clases;

namespace WebDRR.Areas.Carrito.Controllers
{
    /// <summary>
    /// El codigo que se ve comentedo hace referencia a una implementacion anterior-
    /// Como net core da la posibilidad de implementar multiples patrones voy probando y si anda va quedando.
    /// </summary>
    [Area("Carrito")]
    [Route("[controller]/[action]")]
    public class CarritoController : ControllerDrrSystemas
    {

        #region Variables
        private readonly IRepositorioProducto _repositorioProducto;
        private SessionAcceso _session;

        private LibreriaBase.Areas.Carrito.Clases.Carrito _carrito;
        #endregion


        #region Constructor
        public CarritoController(IRepositorioProducto repositorioProducto, LibreriaBase.Areas.Carrito.Clases.Carrito carritoServicio, IHttpContextAccessor httpContextAccessor)
        {
            _repositorioProducto = repositorioProducto;

            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioProducto.DatosSistema = _session.Sistema;
            _repositorioProducto.ElementosPorPagina = 25;

            httpContextAccessor.HttpContext.Session.Remove("VolverCarrito");


            _carrito = carritoServicio;
            _carrito.Rol =Convert.ToByte(_session?.Usuario?.Rol ?? 0); 
        }
        #endregion


        #region Metodos

        private FiltroProducto getFiltro(String codigo)
        {
            FiltroProducto filtro = new FiltroProducto();
            filtro.Dato = codigo;
            filtro.TipoEmpresa = _session.Sistema.TipoEmpresa;
            filtro.TipoBusquedaDato = (Int32)FiltroProducto.EnumTipoBusquedaDato.Codigo;
            filtro.SectorId = _session.Sistema.SectorId;
            filtro.ListaPrecID = _session.getListaPrecio(this.HttpContext);
            filtro.ListaPrecioOferta = _session.getListaPrecioOferta();
            filtro.ModoVerProducto = true;



            if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
            {

                var confPresentacionDefecto = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.ProductoPresentacion_Defecto_Web);//27

                if (confPresentacionDefecto?.Valor.MostrarEntero()==1)
                {
                    Int16 presDef = 0;
                    Boolean ok = Int16.TryParse(confPresentacionDefecto.Extra, out presDef);
                    if (ok == true)
                    {
                        filtro.PresentacionIdDefecto = presDef;
                    }

                }

            }

            return filtro;
        }


        private void restablecerCarrito()
        {
            var cliente = _carrito.Cliente;
            Boolean vendedor = true;
            if (_session.Usuario.Rol == 2)
            {
                vendedor = false;
            }
            _carrito?.Clear(vendedor);
            // _carrito.Cliente = cliente;
            _carrito.Guardar();
        }


        private ProductoMinimo getProgucto(String codigo, Int32 enum_CasoEspecial = 0)
        {
            FiltroProducto filtro = new FiltroProducto();
            filtro =  getFiltro(codigo);

            filtro.Enum_CasoEspeciale = enum_CasoEspecial;
            filtro.IncluirIImagenWeb = true;


            var query = _repositorioProducto.ListaProductosV3(filtro);

            var producto = query?.Values.FirstOrDefault().FirstOrDefault();

            return producto;
        }



        #endregion

        public IActionResult Index(string urlRetorno)
        {
            try
            {
                DatoConfiguracion envioRetiro = null;
                DatoConfiguracion pago = null;
                #region VALIDACIONES

                if (_session.Usuario.Rol != (Int32)EnumRol.Vendedor)
                {
                    envioRetiro = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Activar_Envio_Retiro_CarritoCompras);

                    pago = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Activar_Pagos_CarritoCompras);

                }




                if (_session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    Exception exception;

                    if (_session.Usuario?.IdAlmaWeb == null || _session.Usuario?.IdAlmaWeb == 0)
                    {
                        exception = new Exception("Para acceder a pedidos, necesita iniciar sesion");
                        exception.HResult = 1;

                        throw exception;
                    }

                    if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                    {

                        exception = new Exception("Por favor seleccione la representada con la que quiere operar");
                        exception.HResult = 2;

                        throw exception;

                    }





                }
                else if (_session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.EmpresaMultisector)
                {
                    Exception exception;


                    if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                    {

                        exception = new Exception("Por favor seleccione el sector de la empresa con el que  quiere operar");
                        exception.HResult = 3;

                        throw exception;

                    }

                }
                #endregion



                if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                {
                    DatoConfiguracion confBuscarClientePrimiero = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 15);

                    if (confBuscarClientePrimiero?.Valor == 1)
                    {
                        if (_carrito.Cliente == null || _carrito.Cliente.ClienteID == 0)
                        {
                            FiltroCliente filtro = new FiltroCliente();
                            filtro.BusquedaCliente = true;
                            filtro.UrLRetorno = Url.Action("Index", "Carrito");

                            TempData["ErrorRepresentada"] = "Seleccione el cliente";

                            return RedirectToAction("ListarClientes", "Cliente", filtro);

                        }
                    }

                    if (_session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                    {
                        var transporte = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo ==
                            (int)LibreriaBase.Areas.Usuario.ConfPortal.EnumConfPortal.Mostrar_SeleccionarTransporte);

                        if (transporte?.Valor.MostrarEntero() == 1)
                        {
                            ViewData["ListaTransporte"] = _repositorioProducto.ListarTransportes();
                        }
                    }
                }

                if (String.IsNullOrEmpty(urlRetorno) || urlRetorno == "/")
                {
                    urlRetorno = Url.Action("Productos", "Producto");
                }

                Boolean contiene = urlRetorno.Contains("Producto");
                if (contiene == true)
                {
                    TempData["ListaProducto"] = true;
                }

                ViewBag.wp = _session.Sistema?.WhatsappSector;


                CarritoIndexViewModel carritoIndexView = new CarritoIndexViewModel(
                    carrito: _carrito,
                    urlRetorno: urlRetorno,
                    tipoEmpresa: _session.Sistema.TipoEmpresa);

                return View(carritoIndexView);


            }
            catch (Exception ex)
            {
                String urlTexto = "";
                String url = "";
                String icono = "";

                if (ex.HResult == 1)
                {
                    url = "Acceso\\GotoIndex";
                    urlTexto = "Iniciar Session";
                    icono = "fas fa-sign-in-alt fa-4x";
                }
                else if (ex.HResult == 2)
                {
                    url = "Home\\Representada";
                    urlTexto = "Seleccionar Representada";
                    icono = "fas fa-briefcase fa-4x";
                }
                else if (ex.HResult == 3)
                {
                    url = "Home\\Representada";
                    urlTexto = "Seleccionar Sector";
                    icono = "fas fa-briefcase fa-4x";
                }

                //Metodo de error hay que armar.
                var routeValues = new RouteValueDictionary
                {
                    { "Id", ex.HResult  },
                    { "Mensaje", ex.Message },
                    {"Icono", icono },
                     {"UrlRetorno", url },
                       {"UrlTexto", urlTexto }
                };

                String link = Url.Action("Notificaciones", "Home", routeValues);

                return Redirect(link);
            }
        }


        #region Agregar

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AgregarItemCarrito_Codigo(String codigo, string urlRetorno)
        {
            Int32 tipoBusqueda = (Int32)FiltroProducto.EnumTipoBusquedaDato.Codigo; //Codigo

            if (!String.IsNullOrEmpty(codigo))
            {
                string[] partes = codigo.Split("*");
                Int32 cantidad = 1;
                String cod = "";
                try
                {
                    if (partes.Count() > 1)
                    {
                        cantidad = Convert.ToInt32(partes[0]?.ToString());
                        cod = partes[1];
                    }
                    else
                    {
                        cod = partes[0];
                    }

                }
                catch (Exception)
                {
                    cantidad = 1;
                }


                Int16 resultado = (Int16)enumAccionesPedido.Ninguno;

                FiltroProducto filtro = new FiltroProducto();
                filtro = getFiltro(cod);


                Dictionary<Int32, List<ProductoMinimo>> query = new Dictionary<int, List<ProductoMinimo>>();

                query = _repositorioProducto.ListaProductosV3(filtro);

                Int32? cantidadProductos = 0;

                try
                {
                    cantidadProductos = query?.Values?.FirstOrDefault().Count ?? 0;
                }
                catch (Exception)
                {
                    cantidadProductos = null;
                }


                if (cantidadProductos == 1)
                {
                    var producto = query?.Values?.FirstOrDefault()?.FirstOrDefault();

                    if (producto != null)
                    {
                        _carrito.AgregarComoNuevoItem(producto, cantidad);
                    }
                    else
                    {

                        TempData["ErrorRepresentada"] = "No se encontro en producto buscado";
                        resultado = (Int16)enumAccionesPedido.NoEncontroCodigoProducto;
                    }

                    return RedirectToAction("Index", new { urlRetorno });
                }
                else if (cantidad == 0)
                {
                    TempData["ErrorRepresentada"] = "No se encontro en producto buscado";
                    resultado = (Int16)enumAccionesPedido.NoEncontroCodigoProducto;

                    return RedirectToAction("Index", new { urlRetorno });
                }
                else
                {
                    TempData["ErrorRepresentada"] = "Coincidencias de su busqueda";
                    HttpContext.Session.Remove("ProductoMinimoViewModel");

                    filtro.ModoVerProducto = false;

                    //Empiezo a usar la session porque se hace imposible con parametros.
                    Generica genericaVolverCarrito = new Generica();
                    genericaVolverCarrito.Id = 100;
                    genericaVolverCarrito.UrlRetorno = Url.Action("Index", "Carrito");
                    HttpContext.Session.SetJson("VolverCarrito", genericaVolverCarrito);

                    return RedirectToAction("Productos", "Producto", filtro);
                }

            }
            else
            {
                TempData["ErrorRepresentada"] = "Ingrese un código";
                return RedirectToAction("Index", new { urlRetorno });
            }

        }





        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AgregarItemCarrito(string codigo, Boolean representada, string urlRetorno)
        {
            Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
            if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
            {
                if (representada == false)
                {
                    enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                }
            }

            var producto = getProgucto(codigo, enumCasoEspecial);

            if (producto != null)
            {
                _carrito.AgregarComoNuevoItem(producto, 1);
            }


            TempData["CantidadCarrito"] = _carrito.Lista.Count();
            TempData["ListaProducto"] = true;

            return Redirect(urlRetorno);
        }

        public IActionResult AgregarItemCarrito_Cantidad(int productoId, decimal cantidad, string urlRetorno)
        {
            //Boolean representada = false;
            //if(_session.Sistema.TipoEmpresa == 256)
            //{
            //    representada = true;
            //}

            FiltroProducto filtro = new FiltroProducto();
            filtro =  getFiltro(productoId.ToString());


            if (cantidad == 0)
            {
                cantidad = 1;
            }

            Dictionary<Int32, List<ProductoMinimo>> query = new Dictionary<int, List<ProductoMinimo>>();

            query = _repositorioProducto.ListaProductosV3(filtro);

            var producto = query?.Values.FirstOrDefault().FirstOrDefault();

            if (producto != null)
            {
                _carrito.AgregarComoNuevoItem(producto, cantidad);
            }


            TempData["CantidadCarrito"] = _carrito.Lista.Count();
            TempData["ListaProducto"] = true;

            return Redirect(urlRetorno);

        }



        public RedirectToActionResult Agregar(int productoId, decimal cantidad, string urlRetorno, Int32 tipoOperacion = 1)
        {
            String msj = "La cantidad no puede ser menor que 0";

            if (cantidad <= 0)
            {

                ModelState.AddModelError(nameof(cantidad), msj);
            }

            if (ModelState.IsValid)
            {
                FiltroProducto filtro = new FiltroProducto();
                filtro = getFiltro(productoId.ToString());


                Dictionary<Int32, List<ProductoMinimo>> query = new Dictionary<int, List<ProductoMinimo>>();

                query = _repositorioProducto.ListaProductosV3(filtro);


                var producto = query?.Values.FirstOrDefault().FirstOrDefault();

                if (producto != null)
                {
                    _carrito.AgregarComoNuevoItem(producto, cantidad);
                }

                if (tipoOperacion == 2)
                {
                    return RedirectToAction("DatosDeEnvio");
                }
                else
                {
                    return RedirectToAction("Index", new { urlRetorno });

                }
            }
            else
            {

                TempData["ErrorRepresentada"] = msj;
                return RedirectToAction("Index", new { urlRetorno });
            }

        }


        public RedirectToActionResult AgregarDos(AgregarCarrito entidad)
        {
            String msj = "La cantidad no puede ser menor que 0";

            if (entidad.Cantidad_I <= 0)
            {

                ModelState.AddModelError(nameof(entidad.Cantidad_I), msj);
            }

            if (ModelState.IsValid)
            {

                Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
                if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    if (entidad.Representada == false)
                    {
                        enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                    }
                }

                var producto = getProgucto(entidad.ProductoId, enumCasoEspecial);

                if (producto != null)
                {
                    _carrito.AgregarComoNuevoItem(producto, entidad.Cantidad_I);
                }

                if (entidad.TipoOperacion == 2)
                {
                    return RedirectToAction("DatosDeEnvio");
                }
                else
                {
                    return RedirectToAction("Index", new { entidad.UrlRetorno });

                }
            }
            else
            {

                TempData["ErrorRepresentada"] = msj;
                return RedirectToAction("Index", new { entidad.UrlRetorno });
            }

        }







        /// <summary>
        /// 
        /// </summary>
        /// <param name="idproducto"></param>
        /// <param name="idItemCarrito"></param>
        /// <param name="tipoOp"></param>
        /// <param name="cantidad"></param>
        /// <param name="representada"></param>
        /// <returns></returns>
        /// <fecha>
        /// 19-12-2020 - Se agrega la opcion de buscar 1ero el producto directamente de la session. (la paro para revisar bien)
        /// </fecha>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Agregar_Carrito_Json(String idproducto, Int32 idItemCarrito, Boolean representada)
        {

            ViewItemCarrito viewItemCarrito = new ViewItemCarrito();

            if (String.IsNullOrEmpty(viewItemCarrito?.Error))
            {
                LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
_carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == idItemCarrito);

                Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
                if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    if (representada == false)
                    {
                        enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                    }
                }


                if (itemBuscado != null)
                {
                    itemBuscado.Cantidad = 1;

                    _carrito.Guardar();
                }
                else
                {


                    ProductoMinimo viewProducto = new ProductoMinimo();

                    viewProducto = getProgucto(idproducto, enumCasoEspecial);


                    _carrito.AgregarItem(viewProducto, 1);

                    itemBuscado = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == viewProducto.ProductoId);
                }




                var confPrecios = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 12);


                viewItemCarrito.ItemCarrito = itemBuscado;
                viewItemCarrito.TotalesCarrito = new Generica();
                viewItemCarrito.TotalesCarrito.Id = (int)_carrito.TotalItems();

                if (confPrecios?.Valor == 1)
                {
                    viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalNetoCarrito().FormatoMoneda();
                }
                else
                {
                    viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalCarrito().FormatoMoneda();
                }


                viewItemCarrito.TotalesCarrito.UrlRetorno = _carrito.TotalNetoCarrito().FormatoMoneda();


            }

            return new JsonResult(viewItemCarrito);

        }

        #endregion


        #region Modificar
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public RedirectToActionResult Modificar(LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito item, Int16 tipoOp, String urlRetorno)
        {

            LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
                _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == item.Producto.ProductoId && c.IdItemCarrito == item.IdItemCarrito);


            if (tipoOp == 1)
            {
                if (itemBuscado != null)
                {
                    itemBuscado.Cantidad = item.Cantidad + 1;
                }

                _carrito.Guardar();
            }
            else if (tipoOp == 2)
            {
                if (itemBuscado != null)
                {
                    itemBuscado.Cantidad = item.Cantidad - 1;
                }

                if (itemBuscado.Cantidad == 0)
                {
                    _carrito.QuitarItem(itemBuscado);
                }
                else
                {
                    _carrito.Guardar();
                }
            }
            else //Agregar descripcion extra a los nombres 
            {
                itemBuscado.Producto.DetallaCliente = item.Producto.DetallaCliente;
                _carrito.Guardar();
            }

            //if (TempData.ContainsKey("urlRetorno"))
            //{
            //    TempData.Keep();
            //}

            return RedirectToAction("Index", new { urlRetorno });
        }



        [HttpPost]
        ////[ValidateAntiForgeryToken]
        public JsonResult ModificarCantidadJson(Int32 idProducto, Int32 idItemCarrito, decimal cantidad, String urlRetorno)
        {
            if (cantidad > 0)
            {
                LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
       _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == idProducto && c.IdItemCarrito == idItemCarrito);

                if (itemBuscado != null)
                {
                    itemBuscado.Cantidad = cantidad;
                }

                _carrito.Guardar();

                String json = JsonConvert.SerializeObject(_carrito);

                return new JsonResult(json);
            }
            else
            {
                if (_session?.Usuario?.Rol == (Int32)EnumRol.Vendedor)
                {
                    TempData["ErrorRepresentada"] = "La cantidad no puede ser 0, " +
    "si quiere eliminar el producto, utilice el icono de la papelera";
                }
                else
                {
                    TempData["ErrorRepresentada"] = "La cantidad no puede ser menor que 1, " +
    "si quiere eliminar el producto, utilice el icono de la papelera";
                }

                return new JsonResult("");
            }
        }



        /// <summary>
        /// Nos abre la pantalla para modificar o aplicar bonificaciones al producto
        /// 28/04/2022
        /// </summary>
        /// <param name="idItemCarrito"></param>
        /// <param name="idProducto"></param>
        /// <returns></returns>
        public IActionResult ModificarItemCarrito(Int32 idItemCarrito, Int32 idProducto = 0)
        {
            LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito item = null;
            ProductoMinimo prod = null;

            if (idProducto == 0)
            {
                item = _carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == idItemCarrito);
            }
            else
            {
                if (idItemCarrito!=0)
                {
                    if (_session?.Sistema?.TipoEmpresa != (Int32)EnumTiposEmpresas.Representada)
                    {
                        item = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == idProducto);
                    }
                    else
                    {
                        item = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == idProducto);

                        if (item == null)
                        {
                            item = _carrito.Lista.FirstOrDefault(c => c.Producto.CodigoProveedor == idProducto.ToString());
                        }
                    }
                }
                else
                {
                    //idProducto esta cargado y iditemCarrito es 0
                    ProductoMinimoViewModel viewModels = ProductoMinimoViewModel.RecuperarSession(HttpContext);

                    prod = viewModels.Lista.FirstOrDefault(c => c.ProductoId == idProducto);

                    //Solucionar un error 
                    if (prod.PrecioNetoSinDescuento == 0)
                    {
                        prod.PrecioNetoSinDescuento = prod.PrecioNeto;
                    }

                }
            }



            int rol = _session?.Usuario?.Rol ?? 0;

            Boolean visible = false;

            if (rol == 4)
            {
                visible = true;
            }

            LibreriaBase.Areas.Carrito.ViewActualizarItem actualizarItem;
            if (item != null)
            {
                actualizarItem =
                    new LibreriaBase.Areas.Carrito.ViewActualizarItem
                    {
                        Nombre = item.Producto.NombreCompleto,
                        IdItemCarrito = item.IdItemCarrito,
                        BonificacionVisible = visible,
                        Bonificacion = item.Producto.Bonificacion.MostrarEntero(),
                        Cantidad = item.Cantidad,
                        Detalle = item.Producto.DetallaCliente,
                        ProductoId = item.Producto.ProductoId,
                        FechaActualizacion = item.Producto.FechaActualizacion,

                        UrlRetorno = HttpContext.Request.UrlAtras(),
                        Rol = _session?.Usuario?.Rol ?? (Int32)EnumRol.Cliente
                    };
            }
            else
            {
                actualizarItem =
                        new LibreriaBase.Areas.Carrito.ViewActualizarItem
                        {
                            Nombre = prod.NombreCompleto,
                            IdItemCarrito = 0,
                            BonificacionVisible = visible,
                            Bonificacion = 0,
                            Cantidad = 1,
                            Detalle = "",
                            ProductoId = prod.ProductoId,
                            FechaActualizacion = prod.FechaActualizacion,

                            UrlRetorno = HttpContext.Request.UrlAtras(),
                            Rol = _session?.Usuario?.Rol ?? (Int32)EnumRol.Cliente
                        };
            }



            if (item!=null)
            {
                if (item.Producto.PrecioNetoSinDescuento == 0)
                {
                    item.Producto.PrecioNetoSinDescuento = item.Producto.PrecioNeto;
                }

                if (item.Producto.PrecioBrutoSinDescuento == 0)
                {
                    item.Producto.PrecioBrutoSinDescuento = item.Producto.PrecioBruto;
                }

                actualizarItem.PrecioNeto = item.Producto.PrecioNetoSinDescuento;


                actualizarItem.PrecioBruto = item.Producto.PrecioBrutoSinDescuento;


                if (item.PresentacionId == 0)
                {
                    actualizarItem.Presentacion = "Bulto";
                }
                else
                {
                    actualizarItem.Presentacion = "Unidad";
                }
            }
            else
            {

                if (prod.PrecioNetoSinDescuento == 0)
                {
                    prod.PrecioNetoSinDescuento = prod.PrecioNeto;
                }

                if (prod.PrecioBrutoSinDescuento == 0)
                {
                    prod.PrecioBrutoSinDescuento = prod.PrecioBruto;
                }


                actualizarItem.PrecioNeto = prod.PrecioNetoSinDescuento;
                actualizarItem.PrecioBruto = prod.PrecioBrutoSinDescuento;

                if (prod.PresentacionId == 0)
                {
                    actualizarItem.Presentacion = "Bulto";
                }
                else
                {
                    actualizarItem.Presentacion = "Unidad";
                }
            }



            var dato = _session?.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault(x => x.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.Activar_Producto_Generico);

            if (dato?.Valor == 1)
            {
                if (dato.Extra.EsNumerico())
                {
                    int cod = Convert.ToInt32(dato.Extra);

                    if (cod == actualizarItem.ProductoId)
                    {
                        actualizarItem.EsGenerico = true;
                        if (item !=null)
                        {
                            actualizarItem.PrecioNeto = item.Producto.PrecioNeto;
                        }
                        else
                        {
                            actualizarItem.PrecioNeto = prod.PrecioNeto;
                        }

                    }
                }
            }


            return View(actualizarItem);

        }


        public async Task<IActionResult> ModificarItemCarrito_Ajax(Int32 idItemCarrito, Int32 idProducto = 0)
        {
            try
            {
                return RedirectToAction("ModificarItemCarrito", new { idItemCarrito = idItemCarrito, idProducto = idProducto });
            }
            catch (Exception ex)
            {

                throw;
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <modificado>22/10/2021</modificado>
        [HttpPost]
        //[ValidateAntiF/orgeryToken]/
        public IActionResult ModificarItemCarrito(ViewActualizarItem item)
        {
            IFormCollection frm = this.HttpContext.Request.Form;
            item.Cantidad = Convert.ToDecimal(frm["Cantidad"]);

            if (item.IdItemCarrito == 0)
            {
                ProductoMinimoViewModel viewModels = ProductoMinimoViewModel.RecuperarSession(HttpContext);

                var prod = viewModels.Lista.FirstOrDefault(c => c.ProductoId == item.ProductoId);

                _carrito.AgregarItem(prod, item.Cantidad);

            }


            if (item?.Cantidad > 0)
            {
                LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado = null;

                if (item.IdItemCarrito != 0)
                {
                    itemBuscado= _carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == item.IdItemCarrito);
                }
                else
                {
                    itemBuscado = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == item.ProductoId);
                }



                if (itemBuscado != null)
                {
                    Int16 idPresentacion = 0;
                    if (item.Presentacion == "Unidad")
                    {
                        idPresentacion = 1;
                    }

                    String cambio = "NO";

                    if (frm.ContainsKey("cambialaPresentacion"))
                    {
                        cambio = frm["cambialaPresentacion"].ToString();
                    }

                    if (!String.IsNullOrEmpty(cambio) && cambio == "SI")
                    {
                        if (itemBuscado.PresentacionId != idPresentacion)
                        {
                            //Cambiar los datos en el carrito
                            //Vovler a la misma vista.
                            Int16 listaPrecio = (short)_session.getListaPrecio(this.HttpContext);

                            var otraPresentacion = _repositorioProducto.GetPresentacionProducto((int)(itemBuscado?.Producto?.ProductoId), idPresentacion, listaPrecio, null);

                            if (otraPresentacion != null)
                            {
                                item.Presentacion = otraPresentacion.Presentacion;
                                item.PrecioNeto = otraPresentacion.PrecioNeto;
                                item.PrecioBruto = otraPresentacion.PrecioBruto;




                                if (cambio == "SI")
                                {
                                    return View(item);
                                }
                            }
                        }
                    }




                    itemBuscado.Cantidad = item.Cantidad;
                    itemBuscado.PresentacionId = idPresentacion;
                    itemBuscado.Producto.PresentacionId = idPresentacion;



                    itemBuscado.Producto.DetallaCliente = item.Detalle;

                    #region GENERICO
                    if (item.EsGenerico == true)
                    {
                        itemBuscado.Producto.NombreCompleto = item.Nombre;
                        try
                        {
                            itemBuscado.Producto.PrecioBruto = Convert.ToDecimal(item.PrecioGenerico);
                        }
                        catch (Exception)
                        {
                            itemBuscado.Producto.PrecioBruto = 0;
                        }

                        itemBuscado.Producto.PrecioNeto = itemBuscado.Producto.PrecioBruto.GetNeto((Decimal)itemBuscado.Producto.PorcImpuesto);


                    }
                    else
                    {
                        itemBuscado.Producto.PrecioNeto = item.PrecioNeto;
                        itemBuscado.Producto.PrecioBruto = item.PrecioBruto;

                    }
                    #endregion


                    itemBuscado.Producto.PrecioNetoSinDescuento = itemBuscado.Producto.PrecioNeto;
                    itemBuscado.Producto.PrecioBrutoSinDescuento = itemBuscado.Producto.PrecioBruto;


                    itemBuscado.aplicarBonficacion(item.Bonificacion);



                    //Cambio: Si no guarda no modifico lo cambios en el carrito para revisar.....
                    _carrito.Guardar();
                }

                if (!String.IsNullOrEmpty(item?.UrlRetorno))
                {
                    return Redirect(item.UrlRetorno);
                }
                else
                {
                    return RedirectToAction("Index");
                }

            }
            else
            {
                //Eliminar
                //  ModelState.AddModelError(nameof(item.Cantidad), "La cantidad tiene que ser mayor que 0");

                return View();
            }





        }

        #endregion


        #region Eliminar

        public IActionResult EliminarItemCarrito(int productoId, int idItemCarrito, string urlRetorno)
        {
            LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
                _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == productoId && c.IdItemCarrito == idItemCarrito);


            if (itemBuscado != null)
            {
                _carrito.QuitarItem(itemBuscado);

            }

            if (!String.IsNullOrEmpty(urlRetorno))
            {
                return Redirect(urlRetorno);
            }
            else
            {
                return RedirectToAction("Index", new { urlRetorno });
            }

        }


        public RedirectToActionResult EliminarCarrito(string urlRetorno)
        {
            Int32 idPedido = _carrito.PedidoId ?? 0;


            if (_session?.Usuario?.Rol == (Int32)EnumRol.Cliente || _session?.Usuario?.Rol == (Int32)EnumRol.ClienteFidelizado)
            {
                _session.Usuario.XmlConfiguracion.Carrito_Temporal = "";

                IRepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = _session.Sistema;
                repositorioCliente.Guardar_ConfiguracionXml_UsuarioWeb_Async((Int32)_session.Usuario.IdAlmaWeb, _session.Usuario.XmlConfiguracion.GetXML());
            }


            if (idPedido == 0)
            {
                restablecerCarrito();

                return RedirectToAction("Index", new { urlRetorno });
            }
            else
            {
                IRepositorioPedido repositorioPedido = new RepositorioPedido();
                repositorioPedido.DatosSistema = _session.Sistema;

                var res = repositorioPedido.EliminarPedido(idPedido);

                restablecerCarrito();

                return RedirectToAction("ListarPedidos", "Pedido");
            }

        }
        #endregion



        #region Observacion
        public IActionResult Observacion(string observacion, String urlRetorno)
        {
            try
            {
                string dato = HttpContext.Request.PathAndQuery();

                _carrito.Observacion = observacion;
                _carrito.Guardar();


                return RedirectToAction("Index");
                //return Redirect(urlRetorno);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpPost]
        ////[ValidateAntiForgeryToken]
        public JsonResult ObservacionJson(string observacion)
        {
            try
            {
                string dato = HttpContext.Request.PathAndQuery();

                _carrito.Observacion = observacion;
                _carrito.Guardar();

                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        public IActionResult CarritoObservacion(string urlRetorno)
        {
            Generica generica = new Generica();
            generica.Nombre = _carrito.Observacion;
            generica.UrlRetorno = urlRetorno;
            return View(generica);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult EditObservacion(Generica generica)
        {
            _carrito.Observacion = generica.Nombre;
            _carrito.Guardar();

            return Redirect(generica.UrlRetorno);
        }


        [HttpPost]
        ////[ValidateAntiForgeryToken]
        public IActionResult ActualizarObs(string dato)
        {
            try
            {
                _carrito.Observacion = dato;
                _carrito.Guardar();

                return new JsonResult(_carrito.Observacion);
            }
            catch (Exception ex)
            {
                return new JsonResult("");
            }

        }



        #endregion



        #region Bonificacion
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Bonificar(Decimal bonifica)
        {
            _carrito.Guardar();

            return RedirectToAction("Index");
        }




        [HttpPost]
        ////[ValidateAntiForgeryToken]
        public JsonResult ModificarBonificacionJson(Int32 idProducto, Int32 idItemCarrito, Decimal bonificacion, String urlRetorno)
        {
            LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
                _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == idProducto && c.IdItemCarrito == idItemCarrito);

            if (itemBuscado != null)
            {
                itemBuscado.aplicarBonficacion(bonificacion);
            }

            _carrito.Guardar();

            String json = JsonConvert.SerializeObject(_carrito);

            return new JsonResult(json);
        }
        #endregion

        #region Datos de Envio

        /// <summary>
        /// configuran la opciones de retiro/envio.
        /// Modificado: 20/09/2021
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult DatosDeEnvio(string url = "")
        {

            ///Esto esta para el analisis...


            NotificacionesViewModel model = new NotificacionesViewModel();

            if (_session.Usuario != null && _session.Usuario.IdAlmaWeb > 0)
            {
                if (_session.Usuario.Rol == (int)EnumRol.Cliente || _session.Usuario.Rol == (int)EnumRol.ClienteFidelizado || _session.Usuario.Rol == 0 || _session.Usuario.Rol == null)
                {
                    bool activo = _session.getEstaActivoEnvioyRetiro_Carrito();

                    if (activo == false)
                    {

                        //Cargo la el mensaje de error que se va a mostrar en login.
                        TempData["ErrorRepresentada"] = "La opciones de envio/retiro no estan activadas, las mismas se coordinan con el vendedor.";

                        //Redirecciona la login.
                        return RedirectToAction("Index", "Carrito");
                    }

                }


                if (_carrito.TotalItems() > 0)
                {
                    //Se usan el rep empresa-
                    IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                    repositorioEmpresa.DatosSistema = _session.Sistema;
                    //Se usan el rep Cliente
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = _session.Sistema;

                    /*Hay que tener una UnidadTrabajo para ver los rep.*/


                    //Ya arranca cargado con los datos del cliente..
                    if (_carrito.Envio == null)
                    {

                        var configuracionUsuario = repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);

                        if (configuracionUsuario?.Documento > 0)
                        {
                            _carrito.Envio = new ViewEnvio(configuracionUsuario.ApellidoyNombre, configuracionUsuario.Celular, (Int16)configuracionUsuario.CodigoPostal, configuracionUsuario.Direccion);
                        }


                    }
                    else
                    {
                        if (String.IsNullOrEmpty(_carrito.Envio?.Clientes))
                        {
                            var configuracionUsuario = repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);

                            if (configuracionUsuario?.Documento > 0)
                            {
                                _carrito.Envio = new ViewEnvio(configuracionUsuario.ApellidoyNombre, configuracionUsuario.Celular, (Int16)configuracionUsuario.CodigoPostal, configuracionUsuario.Direccion);
                            }

                        }
                    }





                    if (_carrito.Envio != null)
                    {
                        #region ViewBag

                        ViewBag.rol = _session.Usuario.Rol;

                        ConfEnvio conf = new ConfEnvio();

                        ViewBag.ListaTipoEnvio = conf.ListaTiposEnvios();

                        if (_carrito.Envio.IdTipoEnvio == 0)
                        {
                            _carrito.Envio.IdTipoEnvio = (Byte)ConfEnvio.EnumEnvio.Retiro;
                        }

                        var confenvios = repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);
                        ViewBag.ListaEnvio = confenvios?.ConfiguracionesEnvio.Where(c => c.Descripcion != "");


                        ViewBag.ListaSucursales = repositorioEmpresa.Lista_ViewSucursales();
                        #endregion

                        _carrito.Envio.UrlRetorno = url;

                        return View(_carrito.Envio);
                    }
                    else
                    {

                        //Aviso....
                        TempData.Add("ErrorRepresentada", "Necesitamos que ingrese los siguientes datos, esto es por única vez.");
                        TempData.Add("ValDatos", "Carrito\\DatosDeEnvio");
                        return RedirectToAction("BuscarDatos", "Panel");
                    }

                }
                else
                {
                    TempData["ErrorRepresentada"] = "No hay ningun producto en el seleccionado";

                    string urlError = Request.UrlAtras();
                    return Redirect(urlError);
                }


            }
            else
            {
                #region Vs Anterior
                ////model.Id = -1;
                ////model.Mensaje = "Para finalizar el pedido necesita registrarse, realize el registro y repita la operacion";

                ////String url = Url.Action("Notificaciones", "Home", model);

                ////return Redirect(url);
                //String urlIr = "Carrito\\DatosDeEnvio";
                //String urlTexto = "Iniciar Sesion";
                //String url = "Acceso\\GotoIndex";
                //String icono = "fas fa-sign-in-alt fa-4x";
                //String msj = "Para finalizar el pedido necesita registrarse, realize el registro y repita la operacion";



                ////Metodo de error hay que armar.
                //var routeValues = new RouteValueDictionary
                //{
                //    { "Id", "1"  },
                //    { "Mensaje", msj },
                //    {"Icono", icono },
                //     {"UrlRetorno", url },
                //       {"UrlTexto", urlTexto },
                //    {"UrlIr", urlIr }
                //};

                //String link = Url.Action("Notificaciones", "Home", routeValues);

                //return Redirect(link);
                #endregion

                //Si el login es correcto tiene que ir a esta url.
                _session.UrlIr = Url.Action("DatosDeEnvio", "Carrito");
                //Guardo el dato en la session.
                HttpContext.Session.SetJson("SessionAcceso", _session);
                //Cargo la el mensaje de error que se va a mostrar en login.
                TempData["ErrorRepresentada"] = "Para finalizar el pedido necesita registrarse, realize el registro y repita la operacion";

                //Redirecciona la login.
                return RedirectToAction("GotoIndex", "Acceso");
            }

        }

        /// <summary>
        /// Guarda los datos de retiro/envio en el carrito
        /// </summary>
        /// <param name="dato"></param>
        /// <returns></returns>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult DatosDeEnvio(ViewEnvio dato)
        {
            //Comparativa...
            IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
            repositorioEmpresa.DatosSistema = _session.Sistema;

            var confenvios = repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

            if (confenvios?.EnvioGratis?.Valor > 0)
            {
                if (_carrito.TotalCarrito() >= confenvios?.EnvioGratis?.Valor)
                {
                    dato.Costo = 0;
                    _carrito.Envio = dato;
                }
                else
                {
                    _carrito.Envio = dato;
                }
                _carrito.Guardar();
            }
            else
            {
                _carrito.Envio = dato;
                _carrito.Guardar();
            }




            if (_session.Usuario?.Rol == (Int32)EnumRol.Cliente || _session.Usuario?.Rol == (Int32)EnumRol.ClienteFidelizado || _session.Usuario?.Rol == 0)
            {
                if (!String.IsNullOrEmpty(dato.UrlRetorno))
                {
                    return Redirect(dato.UrlRetorno);
                }
                else
                {

                    var metodoP = _session.Configuracion.ConfiguracionesPortal.
                        FirstOrDefault(c => c.Codigo == (Int32)ConfPortal.EnumConfPortal.Activar_Pagos_CarritoCompras);

                    if (metodoP.Valor.MostrarEntero() == 1)
                    {
                        return RedirectToAction("MetodoDePago");
                    }
                    else
                    {
                        return RedirectToAction("RegistrarPedido", "Pedido");
                    }



                }
            }
            else
            {
                if (!String.IsNullOrEmpty(dato.UrlRetorno))
                {
                    return Redirect(dato.UrlRetorno);

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
        }


        #endregion


        #region Metodos de Pago


        [HttpGet]
        public IActionResult MetodoDePago(String url = "")
        {

            NotificacionesViewModel model = new NotificacionesViewModel();

            if (_session.Usuario != null && _session.Usuario.IdAlmaWeb > 0)
            {

                if (_session.Usuario.Rol == (int)EnumRol.Cliente || _session.Usuario.Rol == (int)EnumRol.ClienteFidelizado || _session.Usuario.Rol == 0 || _session.Usuario.Rol == null)
                {
                    bool activo = _session.getEstaActivoPago_Carrito();

                    if (activo == false)
                    {

                        //Cargo la el mensaje de error que se va a mostrar en login.
                        TempData["ErrorRepresentada"] = "La opciones de pago no estan activadas, las mismas se coordinan con el vendedor.";

                        //Redirecciona la login.
                        return RedirectToAction("Index", "Carrito");
                    }

                }





                IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                repositorioEmpresa.DatosSistema = _session.Sistema;

                #region Cuentas Bancarias 

                //Este despues se tienen que hacer un Ajax Asincrono asi en caso de seleccionar la tb se haga el llmado a la base de datos.
                if (_session?.Sistema?.SectorId != null)
                {
                    var cuentasBancarias = repositorioEmpresa.ListaCuentasBancarias((Int32)_session.Sistema.SectorId);

                    if (cuentasBancarias?.Count() > 0)
                    {
                        var cta = cuentasBancarias[0];
                        TransferenciaBancaria transferencia = new TransferenciaBancaria();
                        transferencia.Banco = cta.DescripcionCuentaBanco;
                        transferencia.CBU = cta.BancoCbu;
                        transferencia.Alias = cta.BancoAliasCbu;

                        ViewBag.TransferenciaB = transferencia;
                    }
                }

                #endregion

                ViewPago pago = new ViewPago();

                if (String.IsNullOrEmpty(url))
                {
                    pago.Url = url;
                }


                var confenvios = repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);
                ViewBag.ListaTipoPago = confenvios?.ConfiguracionesPago?.Where(c => c.Valor == 1);


                if (_carrito.Pago != null)
                {
                    pago = _carrito.Pago;
                }

                return View(pago);
            }
            else
            {
                model.Id = -1;

                model.Mensaje = "Para finalizar el pedido necesita registrarse, realize el registro y repita la operacion";

                String urlError = Url.Action("Notificaciones", "Home", model);

                return Redirect(urlError);
            }

        }




        /// <summary>
        /// Actualiza los datos de pago en el carrito.
        /// </summary>
        /// <param name="dato"></param>
        /// <returns></returns>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult MetodoDePago(ViewPago dato)
        {
            //Se pasan los datos 
            _carrito.Pago = dato;
            _carrito.Guardar();

            if (String.IsNullOrEmpty(dato.Url))
            {
                return RedirectToAction("RegistrarPedido", "Pedido");
            }
            else
            {
                return Redirect(dato.Url);
            }
        }


        #endregion





        [HttpPost]
        ////[ValidateAntiForgeryToken]
        public JsonResult AntesAgregar(int productoId, string urlRetorno)
        {
            RespuestaModelo respuesta = new RespuestaModelo()
            {
                Respuesta = true,
                UrlRedirect = "/Carrito/AgregarItemCarrito/?productoId=" + productoId + "&urlRetorno=" + urlRetorno,
                Error = ""
            };

            #region obtenerProducto
            FiltroProducto filtro = new FiltroProducto();
            filtro = getFiltro(productoId.ToString());


            //Cambio V2 -- V3 -- OJO
            var query = _repositorioProducto.ListaProductosV3(filtro);

            var producto = query?.Values.FirstOrDefault().FirstOrDefault();
            #endregion

            var semaforo = _session.Configuracion.ConfiguracionesViewDatosProductos?.FirstOrDefault(c => c.Codigo == 9);

            if (semaforo?.Valor == 1)
            {
                var permitirVtasRojo = _session.Configuracion.ConfiguracionesViewDatosProductos?.FirstOrDefault(c => c.Codigo == 15);

                if (permitirVtasRojo.Valor == 0)
                {
                    if (!String.IsNullOrEmpty(semaforo.Extra))
                    {

                        String[] data = semaforo.Extra.Split('|');

                        Int32 valorR = Convert.ToInt32(data[0]);

                        if (producto.Stock <= valorR)
                        {
                            // ModelState.AddModelError("Pro", "Este producto esta con Stock en Rojo no se puede agregar al carrito");
                            respuesta.Respuesta = false;
                            respuesta.Error = "Este producto esta con Stock en Rojo no se puede agregar al carrito";
                        }
                    }
                }
            }
            else
            {
                //Stock mayor de que hay aca hay que armar algo.
            }

            return Json(respuesta);
        }

        #region Sumar  Restar

        /// <summary>
        /// Se usa esta action para los botones de - y + en el carrito.
        /// </summary>
        /// <param name="idItemCarrito"></param>
        /// <param name="tipoOp">1 Suma _ 2 Resta</param>
        /// <param name="urlRetorno">Vuelve al lugar de donde se origino el llamado</param>
        /// <returns></returns>
        public IActionResult Sumar_Restar(Int32 idproducto, Int32 idItemCarrito, Int16 tipoOp, String urlRetorno)
        {

            LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
                _carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == idItemCarrito);


            if (tipoOp == 1)
            {
                if (itemBuscado != null)
                {
                    itemBuscado.Cantidad = itemBuscado.Cantidad + 1;
                }
                else
                {
                    var pord = getProgucto(idproducto.ToString());
                    _carrito.AgregarItem(pord, 1);
                }

                _carrito.Guardar();
            }
            else if (tipoOp == 2)
            {
                if (itemBuscado != null)
                {
                    itemBuscado.Cantidad = itemBuscado.Cantidad - 1;
                }

                if (itemBuscado.Cantidad == 0)
                {
                    _carrito.QuitarItem(itemBuscado);
                }
                else
                {
                    _carrito.Guardar();
                }
            }


            return Redirect(urlRetorno);
        }



        /// <summary>
        /// Modificado: 18/09/2021
        /// </summary>
        /// <param name="idproducto"></param>
        /// <param name="idItemCarrito"></param>
        /// <param name="tipoOp"></param>
        /// <param name="representada"></param>
        /// <returns></returns>
        [HttpPost]
        ////[ValidateAntiForgeryToken]
        public IActionResult Sumar_Restar_Json(String idproducto, Int32 idItemCarrito, Int16 tipoOp, Boolean representada)
        {
            if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
            {

                #region Validacion 22-04-2021
                DatoConfiguracion confBuscarClientePrimiero = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 15);

                ViewItemCarrito viewItemCarrito = new ViewItemCarrito();

                if (confBuscarClientePrimiero?.Valor == 1)
                {
                    if (_carrito.Cliente == null || _carrito.Cliente.ClienteID == 0)
                    {
                        FiltroCliente filtro = new FiltroCliente();
                        filtro.BusquedaCliente = true;
                        //filtro.UrLRetorno = Url.Action("Index", "Carrito");
                        filtro.UrLRetorno = HttpContext.Request.UrlAtras();
                        TempData["ErrorRepresentada"] = "Seleccione el cliente";

                        String url = Url.Action("ListarClientes", "Cliente", filtro);

                        viewItemCarrito.Url = url;
                        viewItemCarrito.Error = "Necesita seleccionar primero el cliente";
                    }
                }
                #endregion

                Boolean modoModal = false;
                Int32 id = 0;

                if (String.IsNullOrEmpty(viewItemCarrito?.Error))
                {
                    LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
        _carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == idItemCarrito);

                    Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
                    if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                    {
                        if (representada == false)
                        {
                            enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                        }
                    }

                    if (tipoOp == 1)
                    {
                        if (itemBuscado != null)
                        {
                            itemBuscado.Cantidad = itemBuscado.Cantidad + 1;
                        }
                        else
                        {
                            modoModal = true;

                            var pord = getProgucto(idproducto, enumCasoEspecial);

                            _carrito.AgregarItem(pord, 1);

                            itemBuscado = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == pord.ProductoId);
                        }

                        _carrito.Guardar();
                    }
                    else if (tipoOp == 2)
                    {
                        if (itemBuscado != null)
                        {
                            itemBuscado.Cantidad = itemBuscado.Cantidad - 1;
                        }

                        if (itemBuscado.Cantidad == 0)
                        {
                            _carrito.QuitarItem(itemBuscado);
                        }
                        else
                        {
                            _carrito.Guardar();
                        }
                    }
                    var confPrecios = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 12);



                    viewItemCarrito.ItemCarrito = itemBuscado;
                    viewItemCarrito.TotalesCarrito = new Generica();
                    viewItemCarrito.TotalesCarrito.Id = (int)_carrito.TotalItems();

                    if (confPrecios?.Valor == 1)
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalNetoCarrito().FormatoMoneda();
                    }
                    else
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalCarrito().FormatoMoneda();
                    }


                    viewItemCarrito.TotalesCarrito.UrlRetorno = _carrito.TotalNetoCarrito().FormatoMoneda();


                }



                return new JsonResult(viewItemCarrito);
            }
            else
            {
                #region Cliente

                ViewItemCarrito viewItemCarrito = new ViewItemCarrito();

                if (String.IsNullOrEmpty(viewItemCarrito?.Error))
                {
                    LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
        _carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == idItemCarrito);

                    Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
                    if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                    {
                        if (representada == false)
                        {
                            enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                        }
                    }

                    if (tipoOp == 1)
                    {
                        if (itemBuscado != null)
                        {
                            itemBuscado.Cantidad = itemBuscado.Cantidad + 1;
                        }
                        else
                        {
                            var pord = getProgucto(idproducto, enumCasoEspecial);

                            _carrito.AgregarItem(pord, 1);

                            itemBuscado = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == pord.ProductoId);
                        }

                        _carrito.Guardar();
                    }
                    else if (tipoOp == 2)
                    {
                        if (itemBuscado != null)
                        {
                            itemBuscado.Cantidad = itemBuscado.Cantidad - 1;
                        }

                        if (itemBuscado.Cantidad == 0)
                        {
                            _carrito.QuitarItem(itemBuscado);
                        }
                        else
                        {
                            _carrito.Guardar();
                        }
                    }
                    var confPrecios = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 12);



                    viewItemCarrito.ItemCarrito = itemBuscado;
                    viewItemCarrito.TotalesCarrito = new Generica();
                    viewItemCarrito.TotalesCarrito.Id = (int)_carrito.TotalItems();

                    if (confPrecios?.Valor == 1)
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalNetoCarrito().FormatoMoneda();
                    }
                    else
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalCarrito().FormatoMoneda();
                    }


                    viewItemCarrito.TotalesCarrito.UrlRetorno = _carrito.TotalNetoCarrito().FormatoMoneda();


                }

                return new JsonResult(viewItemCarrito);
                #endregion
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="idproducto"></param>
        /// <param name="idItemCarrito"></param>
        /// <param name="tipoOp"></param>
        /// <param name="cantidad"></param>
        /// <param name="representada"></param>
        /// <returns></returns>
        /// <fecha>
        /// 19-12-2020 - Se agrega la opcion de buscar 1ero el producto directamente de la session. (la paro para revisar bien)
        /// </fecha>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Sumar_Restar_Json_Dos(String idproducto, Int32 idItemCarrito, Int16 tipoOp, Int32 cantidad, Boolean representada)
        {
            if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
            {
                #region VENDEDOR
                #region Validacion 22-04-2021
                DatoConfiguracion confBuscarClientePrimiero = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 15);

                ViewItemCarrito viewItemCarrito = new ViewItemCarrito();

                if (confBuscarClientePrimiero?.Valor == 1)
                {
                    if (_carrito.Cliente == null || _carrito.Cliente.ClienteID == 0)
                    {
                        FiltroCliente filtro = new FiltroCliente();
                        filtro.BusquedaCliente = true;

                        filtro.UrLRetorno = HttpContext.Request.UrlAtras();

                        TempData["ErrorRepresentada"] = "Seleccione el cliente";

                        String url = Url.Action("ListarClientes", "Cliente", filtro);

                        viewItemCarrito.Url = url;
                        viewItemCarrito.Error = "Necesita seleccionar primero el cliente";
                    }
                }
                #endregion

                if (String.IsNullOrEmpty(viewItemCarrito?.Error))
                {
                    LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
    _carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == idItemCarrito);

                    Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
                    if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                    {
                        if (representada == false)
                        {
                            enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                        }
                    }

                    if (tipoOp == 1)
                    {
                        if (itemBuscado != null)
                        {
                            itemBuscado.Cantidad = cantidad;
                        }
                        else
                        {
                            ProductoMinimo viewProducto = new ProductoMinimo();

                            viewProducto = getProgucto(idproducto, enumCasoEspecial);

                            _carrito.AgregarItem(viewProducto, cantidad);

                            itemBuscado = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == viewProducto.ProductoId);
                        }

                        _carrito.Guardar();
                    }
                    else if (tipoOp == 2)
                    {

                        _carrito.QuitarItem(itemBuscado);

                    }

                    var confPrecios = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 12);


                    viewItemCarrito.ItemCarrito = itemBuscado;
                    viewItemCarrito.TotalesCarrito = new Generica();
                    viewItemCarrito.TotalesCarrito.Id = (int)_carrito.TotalItems();

                    if (confPrecios?.Valor == 1)
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalNetoCarrito().FormatoMoneda();
                    }
                    else
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalCarrito().FormatoMoneda();
                    }


                    viewItemCarrito.TotalesCarrito.UrlRetorno = _carrito.TotalNetoCarrito().FormatoMoneda();

                }

                return new JsonResult(viewItemCarrito);
                #endregion


            }
            else
            {
                #region Cliente

                ViewItemCarrito viewItemCarrito = new ViewItemCarrito();

                if (String.IsNullOrEmpty(viewItemCarrito?.Error))
                {
                    LibreriaBase.Areas.Carrito.Clases.Carrito.ItemCarrito itemBuscado =
    _carrito.Lista.FirstOrDefault(c => c.IdItemCarrito == idItemCarrito);

                    Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
                    if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                    {
                        if (representada == false)
                        {
                            enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                        }
                    }

                    if (tipoOp == 1)
                    {
                        if (itemBuscado != null)
                        {
                            itemBuscado.Cantidad = cantidad;
                        }
                        else
                        {

                            ProductoMinimo viewProducto = new ProductoMinimo();

                            viewProducto = getProgucto(idproducto, enumCasoEspecial);

                            _carrito.AgregarItem(viewProducto, cantidad);

                            itemBuscado = _carrito.Lista.FirstOrDefault(c => c.Producto.ProductoId == viewProducto.ProductoId);
                        }

                        _carrito.Guardar();
                    }
                    else if (tipoOp == 2)
                    {

                        _carrito.QuitarItem(itemBuscado);

                    }

                    var confPrecios = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 12);


                    viewItemCarrito.ItemCarrito = itemBuscado;
                    viewItemCarrito.TotalesCarrito = new Generica();
                    viewItemCarrito.TotalesCarrito.Id = (int)_carrito.TotalItems();

                    if (confPrecios?.Valor == 1)
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalNetoCarrito().FormatoMoneda();
                    }
                    else
                    {
                        viewItemCarrito.TotalesCarrito.Nombre = _carrito.TotalCarrito().FormatoMoneda();
                    }


                    viewItemCarrito.TotalesCarrito.UrlRetorno = _carrito.TotalNetoCarrito().FormatoMoneda();


                }

                return new JsonResult(viewItemCarrito);
                #endregion
            }


        }



        #endregion




        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult BonificacionGeneral(int bonificacion)
        {
            //
            if (_carrito.TotalItems() > 0)
            {
                foreach (var item in _carrito.Lista)
                {
                    item.aplicarBonficacion(bonificacion);
                }
            }

            _carrito.Guardar();

            return new JsonResult("");
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Descuento(int descuento)
        {
            _carrito.descuento(descuento);
            _carrito.Guardar();

            return new JsonResult("");

        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult GuardarIdTransporte(Int32 idTransporte)
        {
            _carrito.TransporteId = idTransporte;
            _carrito.Guardar();

            return new JsonResult("");
        }





        [HttpPost]
        public IActionResult AbrirFormularioWp()
        {
            FormularioWp model = new FormularioWp();
            model.Titulo = "Pedido WhatsApp";
            model.Id = 1;
            model.Cliente = _session?.Usuario?.Nombre;
            model.Url = _session?.Sistema?.WhatsappSector;

            return PartialView("_formularioWhatsApp", model);
        }


        [HttpPost]
        public IActionResult CambiarComprobante()
        {
            try
            {
                String resultado = "true";
                
                _carrito.OtrosComprobantes = !_carrito.OtrosComprobantes;

                _carrito.Guardar();

                return Content(resultado);
            }
            catch (Exception ex)
            {
                return Content("error");
            }

        }









        #region Local Storage

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GuardarCarritoLocalStorage()
        {
            ////24/06/2022
            //_carrito.Temporal = true;

            return new JsonResult(_carrito);
        }

        public IActionResult GuardarPedidoTemporal()
        {

            if(_carrito.EstadoId == 20 && !_carrito.PedidoId.IsNullOrValue(0))
            {
                return RedirectToAction("ModificarPedido", "Pedido");
            }
            else
            {
                if(_carrito.PedidoId.IsNullOrValue(0))
                {
                    //Pedido temporal no se puedo reprocesar.
                    _carrito.EstadoId = 20;

                    _carrito.Guardar();

                    return RedirectToAction("FinalizarPedido", "Pedido");
                }
            }


            return RedirectToAction("Principal", "Home");
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult RecuperarCarritoLocalStorage(String micarrito)
        {
            IRepositorioPedido repositorioPedido = new RepositorioPedido();
            repositorioPedido.DatosSistema = _session.Sistema;
            IRepositorioCliente repositorioCliente = new RepositorioCliente();
            repositorioCliente.DatosSistema = _session.Sistema;
            //Verificar que no haya ningun pedido temporal.

            FiltroPedido filtro = new FiltroPedido();
            filtro.AlmaUserId = _session.Usuario.AlmaUserID ?? 0;
            filtro.WebUserId = _session.Usuario.IdAlmaWeb;
            filtro.Estado = 20; //Temporal.

            #region Esquema Para Modificar -- 25/06/2022

            Int32 idEntidadSuc = 0;
            if (!_session.Usuario.EntidadSucId.IsNullOrValue(0))
            {
                idEntidadSuc = (int)_session.Usuario.EntidadSucId;
            }
            else
            {
                var entSuc = repositorioCliente.GetUsuario((int)_session.Usuario.AlmaUserID);
                if(entSuc!=null)
                {
                    idEntidadSuc = entSuc.EntidadSucId ??0;
                }
            }

            Int32 clienteid = repositorioCliente.GetClienteVendedor((Int32)EnumRol.ClienteFidelizado, idEntidadSuc);

            filtro.ClienteId = clienteid;
            #endregion


            filtro.SinPaginacion = true;
            filtro.Todos = true;


            DRR.Core.DBAlmaNET.Models.Impuesto ingB = repositorioCliente.getImpuestoAlmaNet(900);
            Dictionary<int,List<PedidoView>> query = repositorioPedido.ListarPedidos(filtro, ingB);

            if(query!=null && query.Count()>0)
            {

                if(query.FirstOrDefault().Value!=null)
                {
                    PedidoView pedido = query.FirstOrDefault().Value.FirstOrDefault();

                    if(pedido!=null)
                    {
                        _carrito = repositorioPedido.GetCarrito(pedido.PedidoId);
                    }
                    
                }


                //Transformar el pedido web en carrito..

            }
            else
            {
                _carrito = micarrito.ToObsect<LibreriaBase.Areas.Carrito.Clases.Carrito>();
               
            }

            HttpContext.Session.SetJson("Carrito", _carrito);

            return new JsonResult("");
        }


        #endregion

    }
}