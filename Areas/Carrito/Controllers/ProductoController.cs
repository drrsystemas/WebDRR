using iTextSharp.text;
using iTextSharp.text.pdf;
using LibreriaBase.Areas.Carrito;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RtfPipe;

namespace WebDRR.Areas.Carrito.Controllers
{
    [Area("Carrito")]
    [Route("[controller]/[action]")]

    public class ProductoController : Controller
    {

        #region Variables
        private IRepositorioProducto _repositorioProducto;
        SessionAcceso _session;

        private LibreriaBase.Areas.Carrito.Clases.Carrito _carrito;
        #endregion


        #region Constructor
        public ProductoController(IRepositorioProducto repositorioProducto, LibreriaBase.Areas.Carrito.Clases.Carrito carritoServicio, IHttpContextAccessor httpContextAccessor)
        {
            _repositorioProducto = repositorioProducto;
            //verificar sesion
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioProducto.DatosSistema = _session?.Sistema;

            if (_session?.Familia_ModoRaiz?.FamiliaId > 0)
            {
                _repositorioProducto.ElementosPorPagina = _session.ElementosPagina;
            }
            else
            {
                _repositorioProducto.ElementosPorPagina = _session.getPaginaProducto();
            }


            _carrito = carritoServicio;
        }
        #endregion



        #region Lista de Productos 
        /// <summary>
        /// 
        /// Modificado: 16/09/2021
        /// </summary>
        /// <param name="filtro"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Productos(FiltroProducto filtro)
        {
            Int32 codigoError = 0;

            try
            {
                Boolean representada = false;

                //18-12-2020
                if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    representada = true;
                    if (_carrito?.Cliente?.TipoBonificProducto > 0)
                    {
                        ViewBag.TipoBonificProducto = _carrito?.Cliente?.TipoBonificProducto;
                    }
                }

                if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
                {
                    ViewData["viewCliente"] = _carrito?.Cliente;

                    var confPresentacionDefecto = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.ProductoPresentacion_Defecto_Web);//27
                    
                    if(confPresentacionDefecto?.Valor.MostrarEntero()==1)
                    {
                        Int16 presDef = 0;
                        Boolean ok = Int16.TryParse(confPresentacionDefecto.Extra, out presDef);
                        if(ok == true)
                        {
                            filtro.PresentacionIdDefecto = presDef;
                        }
                        
                    }

                }

                Boolean btnComprarEstado = btnComprar_Estado();
                ViewData["BtnComprar"] = btnComprarEstado;


                if (filtro.Filtro_33 == true)
                {
                    DatoConfiguracion con33 = _session?.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault(c => c.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.InicializacionFiltroProducto);

                    if (!String.IsNullOrEmpty(con33.Extra))
                    {
                        String[] dato = con33.Extra.Split("|");

                        if (!String.IsNullOrEmpty(dato[0]))
                        {
                            filtro.MarcaId = Convert.ToInt32(dato[0]);
                            filtro.FiltroMarca = true;
                        }

                        if (!String.IsNullOrEmpty(dato[1]))
                        {
                            filtro.FamiliaId = Convert.ToInt32(dato[0]);
                            filtro.FiltroRubro = true;
                        }

                        if (!String.IsNullOrEmpty(con33.ExtraDos))
                        {
                            filtro.Dato = con33.ExtraDos;
                        }
                    }
                }


                //Hasta armar.
                if (filtro.SucursalId == 0)
                {
                    filtro.SucursalId = 1;
                }

                //control de error paginacion
                if (filtro.PaginaActual == 0)
                {
                    filtro.PaginaActual = 1;
                }

                if (filtro.FiltroRubro == false)
                {
                    filtro.FamiliaId = 0;
                }
                if (filtro.FiltroMarca == false)
                {
                    filtro.MarcaId = 0;
                }


                if (filtro.FamiliaId == 0)
                {
                    filtro.NombreFamilia = "";
                }
                if (filtro.MarcaId == 0)
                {
                    filtro.NombreMarca = "";
                }

                //01-04-2022
                if (filtro.VerTodos == false)
                {
                    var confTodos = _session.Configuracion.ConfiguracionesViewDatosProductos.FirstOrDefault(c => c.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.Desactivar_Paginacion_Producto);
                    if (confTodos != null)
                    {
                        if (confTodos?.Valor.MostrarEntero() == 1)
                        {
                            //Se quita la paginacion.
                            filtro.VerTodos = true;

                        }
                    }
                }


                #region CAMBIAR COMPORTAMIENTO EN VENDEDOR
                //var confAbrirEditar = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(c => c.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.AbrirEditarProducto_Agregar_ModoVendedor);
                //int abrePantalla = 0;
                //if (confAbrirEditar != null)
                //{
                //    abrePantalla = (int)(confAbrirEditar?.Valor.MostrarEntero());
                //}
                //ViewData["ModoEdicionProducto"] = abrePantalla == 1 ? true : false;

                ViewData["ModoEdicionProducto"] = _session.getModoEdicionProducto();
                #endregion



                //Muy buena -- Agro Azul -- 09/02/2021 se recontra modifico.
                verificarUbicaciones_DepositoVendedor(filtro);

                #region Verificaciones

                if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                {
                    if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                    {
                        codigoError = 2;
                        throw new Exception("Por favor seleccione la representada con la que quiere operar");
                    }
                }
                else if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.EmpresaMultisector)
                {
                    if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                    {
                        codigoError = 3;
                        throw new Exception("Por favor seleccione el sector de la empresa con la que quiere operar");
                    }
                }


                #endregion


                #region Ocultar - Determinadas Clasificaciones
                //11-02-2021
                var ocultarClasificacion = _session?.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault(c => c.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.Ocultar_Clasificacion_Clientes);
                if (ocultarClasificacion?.Valor.Activo_Inactivo() == "Activo")
                {
                    if (!String.IsNullOrEmpty(ocultarClasificacion.Extra) && ocultarClasificacion.Extra.EsNumerico() == true)
                    {
                        if(!String.IsNullOrEmpty( ocultarClasificacion.Extra))
                        {
                            List<String> vector = ocultarClasificacion.Extra.Split('-').ToList();
                            vector.RemoveAll(c => c == "");

                            if(vector?.Count > 0)
                            {
                                for (int i = 0; i < vector?.Count; i++)
                                {
                                    filtro.ClasificacionID_Oculta_Cliente.Add(Convert.ToInt32(vector[i]));
                                }
                            }
                        }
                      
                    }
                }
                #endregion


                #region Buscar con or
                var buscarOr = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(c => c.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.Buscar_Productos_Usando_Or_Logico);

                if (buscarOr != null)
                {
                    if (buscarOr?.Valor.MostrarEntero() == 1)
                    {
                        filtro.BuscarUsando_Or_Logico = true;
                    }
                }

                #endregion


                filtro.EsVendedor = _session?.Usuario?.Rol == (Int32)EnumRol.Vendedor ? true : false;
                filtro.SectorId = _session.Sistema?.SectorId;
                filtro.TipoEmpresa = _session.Sistema?.TipoEmpresa;

                filtro.ListaPrecID = _session.getListaPrecio(this.HttpContext);

                filtro.ListaPrecioOferta = _session.getListaPrecioOferta();
                filtro.VerProductosSinStock = _session.getMostrarProductosStockCero();




                //La lista general ---

                #region Porcentaje - Sugerido - Tomando en cuenta la configuracion del Usuario.
                var confUsuarioUno = _session?.ConfiguracionUsuario?.FirstOrDefault(x => x.Codigo == 1);

                if (confUsuarioUno != null && confUsuarioUno.Valor.Activo_Inactivo() == "Activo")
                {
                    if (confUsuarioUno.Extra != null && confUsuarioUno.Extra.EsNumerico())
                    {
                        filtro.PorcentajeSugerido = Convert.ToDecimal(confUsuarioUno.Extra);
                    }
                    else
                    {
                        var porcentajeGeneral = _session?.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault(x => x.Codigo == 2);
                        if (porcentajeGeneral != null && porcentajeGeneral.Valor > 0)
                        {
                            filtro.PorcentajeSugerido = porcentajeGeneral.Valor;
                        }
                    }
                }
                else
                {
                    var porcentajeGeneral = _session?.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault(x => x.Codigo == 2);
                    if (porcentajeGeneral != null && porcentajeGeneral.Valor > 0)
                    {
                        filtro.PorcentajeSugerido = porcentajeGeneral.Valor;
                    }
                }
                #endregion

                //ViewBag.url = _session.Sistema?.WhatsappSector;
                ViewData["UrlWp"] = _session.Sistema?.WhatsappSector;

                #region Ordenamiento ... 

                if (filtro.Ordenamiento == 0)
                {
                    if (_session.Ordenamiento == 0)
                    {
                        var orden = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.Ordenamiento_Productos);
                        if (orden != null)
                        {
                            _session.Ordenamiento = (byte)orden.Valor;
                            _session.GuardarSession(HttpContext);
                            //HttpContext.Session.SetJson("SessionAcceso", _session);
                        }

                    }

                    filtro.Ordenamiento = _session.Ordenamiento;
                }
                else
                {
                    _session.Ordenamiento = filtro.Ordenamiento;
                    _session.GuardarSession(HttpContext);
                    //HttpContext.Session.SetJson("SessionAcceso", _session);
                }
                #endregion


                #region CAMBIAS OPCIONES DE CONFIGURACION
                if (!String.IsNullOrEmpty(filtro.DatoAuxiliar))
                {
                    if (filtro.DatoAuxiliar.Equals("OpcionesFiltro"))
                    {
                        _session.EsconderPrecios = filtro.EsconderPrecio;

                        if (!String.IsNullOrEmpty(filtro.Dato))
                        {
                            if (filtro.Dato.EsNumerico())
                            {
                                _session.ElementosPagina = Convert.ToInt32(filtro.Dato);
                                _repositorioProducto.ElementosPorPagina = _session.ElementosPagina;
                                filtro.Dato = "";
                                filtro.PaginaActual = 1;
                            }
                        }

                        _session.OcultarImagenes = filtro.OcultarImagenes;
                        _session.TipoVisualizacionProductos = filtro.TipoVisualizacion;

                        _session.GuardarSession(HttpContext);
                        //HttpContext.Session.SetJson("SessionAcceso", _session);
                    }
                }
                else
                {
                    filtro.EsconderPrecio = _session.EsconderPrecios;
                    filtro.OcultarImagenes = _session.OcultarImagenes;

                    filtro.TipoVisualizacion = _session.TipoVisualizacionProductos;

                }
                #endregion


                #region Visualizacion

                if (filtro.TipoVisualizacion == 0)
                {
                    filtro.TipoVisualizacion = _session.TipoVisualizacionProductos;
                }

                //Esta solo ingresa la 1era vez.
                if (filtro.TipoVisualizacion == 0)
                {

                    if (filtro.EsVendedor == true)
                    {
                        filtro.TipoVisualizacion = 2;
                        _session.TipoVisualizacionProductos = filtro.TipoVisualizacion;
                        //HttpContext.Session.SetJson("SessionAcceso", _session);
                        _session.GuardarSession(HttpContext);
                    }
                    else
                    {
                        //LoNuevo: esto agregue para que solo el cliente vea en modo tarjeta.
                        if (_session?.Usuario?.Rol == (int)EnumRol.Cliente || _session?.Usuario?.Rol == (int)EnumRol.ClienteFidelizado)
                        {
                            filtro.TipoVisualizacion = 1;
                            _session.TipoVisualizacionProductos = filtro.TipoVisualizacion;
                            //HttpContext.Session.SetJson("SessionAcceso", _session);
                            _session.GuardarSession(HttpContext);
                        }
                        else
                        {
                            if (_session?.TipoVisualizacionProductos == 0)
                            {
                                var tipoVisualizacion = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == 16);
                                if (tipoVisualizacion != null)
                                {
                                    if (tipoVisualizacion.Valor.MostrarEntero() == 0)
                                    {
                                        filtro.TipoVisualizacion = 1;
                                        tipoVisualizacion.Valor = 1;
                                    }
                                    _session.TipoVisualizacionProductos = Convert.ToInt32(tipoVisualizacion.Valor);
                                    //HttpContext.Session.SetJson("SessionAcceso", _session);
                                    _session.GuardarSession(HttpContext);
                                }
                            }
                        }

                    }


                }

                #endregion


                //Control
                if (filtro.TipoEmpresa == null)
                {
                    filtro.TipoEmpresa = _session?.Sistema?.TipoEmpresa ?? 1;
                }


                if(_session?.Usuario?.SectorID.IsNullOrValue(0)==false)
                {
                    filtro.SectorId = (short?)(_session?.Usuario?.SectorID);
                }


                var ocultarS = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.Ocultar_Productos_Stock_Cero);

                if (ocultarS?.Valor.MostrarEntero() == 1)
                {
                    filtro.VerProductosSinStock = false;
                }
                else
                {
                    filtro.VerProductosSinStock = true;
                }



                //VIEJO -- Funcional
                ProductoMinimoViewModel item = cargarProductoMinimoViewModel(filtro);

                //Aparentemente el viewData tambien se accede desde las vistas parciales.
                ViewData["EsconderPrecio"] = filtro?.EsconderPrecio ?? false;
                ViewData["Conf_VP"] = _session.Configuracion?.ConfiguracionesViewDatosProductos;
                ViewData["Rol"] = _session.Usuario?.Rol;


                //ESTO ES PORQUE NO SE COMO SE LLEVA LA CUENTA DE ITEMS
                if (TempData.ContainsKey("CantidadCarrito"))
                {
                    var result = TempData["CantidadCarrito"];
                    if (result != null)
                    {
                        //la idea es que si esta muestre.
                        item.Filtro.CantidadCarrito = Convert.ToDecimal(result);
                    }
                    else
                    {
                        //para que no muestre el disparador de se agrego un producto con exito.
                        item.Filtro.CantidadCarrito = 0;
                    }
                }

                ViewData["Session"] = _session;
                ViewData["Representada"] = representada;
                return View(item);

            }
            catch (Exception ex)
            {
                #region Errores

                String urlTexto = "";
                String urlRetorno = "";
                String icono = "";

                if (codigoError == 1)
                {
                    urlRetorno = "Acceso\\Index";
                    urlTexto = "Iniciar Session";
                    icono = "fas fa-sign-in-alt fa-4x";
                }
                else if (codigoError == 2)
                {
                    urlRetorno = "Home\\Representada";
                    urlTexto = "Seleccionar Representada";
                    icono = "fas fa-briefcase fa-4x";
                }
                else if (codigoError == 3)
                {
                    urlRetorno = "Home\\Representada";
                    urlTexto = "Seleccionar Sector";
                    icono = "fas fa-briefcase fa-4x";
                }

                //Metodo de error hay que armar.
                var routeValues = new RouteValueDictionary
                {
                    { "Id", codigoError },
                    { "Mensaje", ex.Message },
                    {"Icono", icono },
                     {"UrlRetorno", urlRetorno },
                       {"UrlTexto", urlTexto }
                };

                String url = Url.Action("Notificaciones", "Home", routeValues);

                return Redirect(url);

                #endregion
            }


        }



        /// <summary>
        /// Si es true se muestra el boton en caso contrario el boton se oculta.
        /// </summary>
        /// <returns></returns>
        private Boolean btnComprar_Estado()
        {
            try
            {
                Boolean estado = true;

                var modoCarritoW = _session.Configuracion?.ConfiguracionesPortal.
           FirstOrDefault(c => c.Codigo == (int)LibreriaBase.Areas.Usuario.
           ConfPortal.EnumConfPortal.ModoCarritoWhatsApp);

                if (modoCarritoW?.Valor.Activo_Inactivo() == "Activo")
                {
                    estado = false;
                }
                else
                {
                    /*QuePaso: no se bien que carajo era esto - Sigo sin saber */
                    estado = _session.getEstaActivoPago_Carrito();
                }

                return estado;
            }
            catch (Exception)
            {
                return true;
            }

        }

        private void verificarUbicaciones_DepositoVendedor(FiltroProducto filtro)
        {
            //Esto es para que los vendedores vean un determinado stock -- no el total-
            if (_session.Usuario?.Rol == (int)EnumRol.Vendedor)
            {


                //Verificar si esta activo.
                DatoConfiguracion dconf = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Vendedor_Deposito_Defecto);

                if (!String.IsNullOrEmpty(dconf?.Extra))
                {
                    var lista = dconf.Extra.Split('-');


                    if (lista?.Count() > 0)
                    {
                        if (filtro.ListaRubroUbicacioId == null)
                        {
                            filtro.ListaRubroUbicacioId = new List<int>();
                        }

                        foreach (String id in lista)
                        {
                            filtro.ListaRubroUbicacioId.Add(Convert.ToInt32(id));
                        }
                    }

                }
            }
            else
            {

                #region Rubro -- Deposito Stock......

                #endregion



            }
        }



        /// <summary>
        /// 
        /// Modificado:16/09/2021
        /// </summary>
        /// <param name="filtro"></param>
        /// <param name="item"></param>
        private ProductoMinimoViewModel cargarProductoMinimoViewModel(FiltroProducto filtro)
        {

            ProductoMinimoViewModel viewModels = ProductoMinimoViewModel.RecuperarSession(HttpContext);
            Boolean recupera = false;


            if (viewModels != null)
            {
                recupera = viewModels.Filtro.SonIgualesLosObjectos(filtro);
            }


            if (recupera == true)
            {
                estaEnElCarrito(viewModels.Lista);

                return viewModels;
            }
            else
            {
                viewModels = new ProductoMinimoViewModel();

                Dictionary<Int32, List<ProductoMinimo>> consulta = new Dictionary<int, List<ProductoMinimo>>();

                Boolean ocultarImagenes = false;

                #region Filtro - RAIZ

                if (_session.Familia_ModoRaiz?.FamiliaId > 0)
                {
                    filtro.FiltroRaizFamilia = true;
                    filtro.Familia_Raiz_Id = _session.Familia_ModoRaiz?.FamiliaId;
                    filtro.Familia_Raiz_Orden = _session?.Familia_ModoRaiz?.Orden;

                    if (_session?.RubroId > 0)
                    {
                        filtro.FamiliaId = _session.RubroId;
                    }

                    _session.RubroId = 0;

                    _session.GuardarSession(HttpContext);
                }


                #endregion

                //filtro.IncluirIImagenWeb = true;

                consulta = _repositorioProducto.ListaProductosV3(filtro);
                ocultarImagenes = _session.OcultarImagenes;


                Int32 cantidadRegistros = consulta?.FirstOrDefault().Key ?? 0;

                if (cantidadRegistros > 0)
                {
                    estaEnElCarrito(consulta?.FirstOrDefault().Value);

                }
                else
                {
                    TempData["ErrorRepresentada"] = "No se encontro ningun producto";
                }


                viewModels.Lista = consulta?.FirstOrDefault().Value;
                viewModels.Filtro = filtro;

                Int32 elementos = cantidadRegistros;

                viewModels.Paginacion = new Paginacion
                {
                    PaginaActual = filtro.PaginaActual,
                    ElementosPorPagina = _repositorioProducto.ElementosPorPagina,
                    Elementos = elementos
                };


                viewModels.GuardarSession(HttpContext);

                //HttpContext.Session.SetJson("ProductoMinimoViewModel", item);

                return viewModels;
            }



        }
        #endregion



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult CargarImagenes_pagina(Int32[] lista)
        {
            try
            {
                List<GetImagen> listaImagenes = _repositorioProducto.ListaRutaImagene_Web(lista.ToList());
                String json = "";

                if (listaImagenes != null)
                {
                    json = JsonConvert.SerializeObject(listaImagenes.Select(c => new { c.Id }));
                }

                return new JsonResult(json);

            }
            catch (Exception ex)
            {

                return new JsonResult("");
            }
        }

        private void estaEnElCarrito(List<ProductoMinimo> lista)
        {
            Boolean bonificacionProductoCliente = false;
            if (_carrito?.Cliente?.TipoBonificProducto > 0)
            {
                bonificacionProductoCliente = true;
            }

            if (lista != null)
            {
                foreach (var elemtos in lista)
                {
                    if (_session.Sistema.TipoEmpresa == 256)
                    {
                        elemtos.Representada = true;
                    }

                    var estaCarrito = _carrito?.Lista?.FirstOrDefault(c => c.Producto.ProductoId == elemtos.ProductoId);
                    if (estaCarrito != null)
                    {
                        elemtos.EstaCarrito = true;
                        elemtos.CantidadCarrito = estaCarrito.Cantidad;
                        elemtos.IdItemCarrito = estaCarrito.IdItemCarrito;
                    }
                }
            }

        }




        /// <summary>
        /// Modificado 22/02/2022
        /// </summary>
        /// <param name="codigo"></param>
        /// <param name="representada"></param>
        /// <param name="urlRetorno"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult VerProducto(String codigo, Boolean representada, String urlRetorno)
        {

            ProductoMinimo item = null;

            //Recupero 
            ProductoMinimoViewModel productoViewModel = ProductoMinimoViewModel.RecuperarSession(HttpContext);
            //HttpContext.Session.GetJson<ProductoMinimoViewModel>("ProductoMinimoViewModel");

            FiltroProducto filtro;

            if (productoViewModel == null)
            {
                filtro = getFiltro(codigo);

                //filtro.Dato = codigo;
                Int32 enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Normal;
                if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    if (representada == false)
                    {
                        enumCasoEspecial = (int)FiltroProducto.EnumCasosEspeciales.Representada_SinCodigo;
                    }
                }

                filtro.Enum_CasoEspeciale = enumCasoEspecial;
                //filtro.ModoVerProducto = true;

                var ubicacionesStock = _session.Configuracion.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == (int)ConfViewDatosProductos.EnumConfViewDatosProductos.VerUbicacionStock);

                if (ubicacionesStock?.Valor.Activo_Inactivo() == "Activo")
                {
                    filtro.MostrarUbicacionStock = true;

                }


                Dictionary<Int32, List<ProductoMinimo>> resultado = new Dictionary<int, List<ProductoMinimo>>();

                resultado = _repositorioProducto.ListaProductosV3(filtro);

                item = resultado?.Values?.FirstOrDefault().FirstOrDefault();
            }
            else
            {
                filtro = productoViewModel.Filtro;

                int codigoCastiado = 0;

                bool casting = Int32.TryParse(codigo, out codigoCastiado);

                if (casting)
                {
                    item = productoViewModel.Lista.FirstOrDefault(c => c.ProductoId == codigoCastiado);
                }

            }


            item.Cantidad = 1;//Cantidad minima por el momento....





            //if (item != null)
            //{
            //    String error = "";


            //    //LoNuevo: - Aca se genera los productos relacionados - y tb los elaborados.

            //    var listaRel = _repositorioProducto.ListaProductosRelacionados(item.ProductoId, out error);
            //    ViewData["ListaProdRel"] = listaRel?.ToJson();

            //    short valor = 4;
            //    short esEleborado = ((short)(item?.ProductoTipoId & valor));
            //    Boolean elabora = false;
            //    if(esEleborado == 4)
            //    {
            //        elabora = true;
            //        var listaProdElaborados = _repositorioProducto.ListaProductosElaborados(item.ProductoPresentacionId, out error);
            //        ViewData["ListaProdElaborados"] = listaProdElaborados;
            //    }

            //    ViewData["Elaborado"] = elabora;




            //    List<ViewImagen> listaImagenes= _repositorioProducto.ListaRutaImagenes_Sql(item.ProductoBaseId);

            //    //Cargar Imagen principal y la secuendaria.
            //    if(listaImagenes?.Count()>0)
            //    {
            //        foreach (var img in listaImagenes)
            //        {
            //            img.Imagen = img.ImagenByte.RutaImagenJpg();

            //            //Test 
            //            System.Drawing.Image imagenOriginal = TrabajandoConImagenes.ByteArrayToImage(img.ImagenByte);
            //            System.Drawing.Image miniatura = TrabajandoConImagenes.RedimencionarPorPorcentaje(imagenOriginal, 75);
            //            Byte[] imgMini = TrabajandoConImagenes.ImageToByte(miniatura);

            //            img.ImagenMiniatura = imgMini.RutaImagenJpg();
            //        }

            //        item.ListaImagenes = listaImagenes;
            //    }


            //    item.Rtf = _repositorioProducto.GetObservacion(item.ProductoBaseId);

            //    //Tengo que volver a cargar en un metodo separado.
            //    if (!String.IsNullOrEmpty(item.Rtf))
            //    {
            //        try
            //        {
            //            var html = Rtf.ToHtml(item.Rtf);
            //            item.Rtf = html;
            //        }
            //        catch (Exception exRtf)
            //        {
            //            item.Rtf = "***";
            //        }

            //    }
            //}


            if (!String.IsNullOrEmpty(urlRetorno))
            {
                ViewBag.UrlRetorno = urlRetorno;
            }
            else
            {
                ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();
            }


            if (item != null)
            {
                ViewBag.EsconderPrecio = filtro.EsconderPrecio;
                ViewData["Conf_VP"] = _session?.Configuracion?.ConfiguracionesViewDatosProductos;
                ViewData["Conf_Portal"] = _session?.Configuracion?.ConfiguracionesPortal;
                ViewData["Rol"] = _session.Usuario.Rol;
                ViewBag.wp = _session.Sistema?.WhatsappSector;

                var modoCarritoW = _session.Configuracion?.ConfiguracionesPago.
                                    FirstOrDefault(c => c.Codigo == (int)LibreriaBase.Areas.Usuario.
                                    ConfPortal.EnumConfPortal.ModoCarritoWhatsApp);


                Boolean btnComprarEstado = btnComprar_Estado();
                ViewData["BtnComprar"] = btnComprarEstado;


                //18-12-2020
                if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    if (_carrito?.Cliente?.TipoBonificProducto > 0)
                    {
                        ViewBag.TipoBonificProducto = _carrito?.Cliente?.TipoBonificProducto;
                    }
                }



                return View(item);
            }
            else
            {
                string url = Url.Action("Productos", "Producto");
                return Redirect(url);
            }

        }


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



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult CargarImagenes_Producto(Int32 idProductoBase)
        {
            try
            {
                var listaImg = _repositorioProducto.ListaRutaImagenes_Sql(idProductoBase);

                String json = JsonConvert.SerializeObject(listaImg);

                return new JsonResult(json);
            }
            catch (Exception ex)
            {

                return new JsonResult("");
            }
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ContentResult CargarObservacion_Producto(Int32 idProductoBase)
        {
            try
            {
                String observacion = _repositorioProducto.GetObservacion(idProductoBase);

                observacion = Rtf.ToHtml(observacion);

                return Content(observacion, "text/html");
            }
            catch (Exception ex)
            {

                return Content("", "text/html");
            }
        }




        public IActionResult SeleccionarFamilia(String dato)
        {
            if (!String.IsNullOrEmpty(dato))
            {
                ViewBag.dato = dato;
            }
            return View();
        }




        public IActionResult BuscadorProductos()
        {
            FiltroProducto filtro = new FiltroProducto();

            return View(filtro);
        }


        [HttpPost]
        public IActionResult GenerarUrl(Int32 id)
        {
            String urlProducto = "/Producto/VerProducto?codigo=" + id;

            String enlace = "https://" + this.Request.Host.Value + "/Home/IngresoRepresentada?idEmpresa=" + _session.Sistema.EmpresaId + "&idSector=" + _session.Sistema.SectorId + "&url=" + urlProducto;

            return new JsonResult(enlace);
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GenerarUrlProducto(string dato, Int32? familiaId, Int32? marcaId, Int32? paginaActual)
        {
            Boolean primerParametro = false;
            String urlProducto = "/home/links?idEmpresa=" + _session.Sistema.EmpresaId;

            urlProducto += "&data=" + dato;
            urlProducto += "&idRubro=" + familiaId;
            urlProducto += "&marca=" + marcaId;
            urlProducto += "&pagina=" + paginaActual;

            String enlace = "https://" + this.Request.Host.Value + urlProducto;

            return new JsonResult(enlace);
        }





        public IActionResult GenerarPdfListaProductos(FiltroProducto filtro)
        {

            List<ProductoMinimo> lista;
            //FiltroProducto filtro = new FiltroProducto();
            filtro.PaginaActual = 1;
            filtro.SectorId = _session.Sistema?.SectorId;

            filtro.ListaPrecID = _session.getListaPrecio(this.HttpContext);
            filtro.ListaPrecioOferta = _session.getListaPrecioOferta();

            var confUsuarioUno = _session?.ConfiguracionUsuario?.FirstOrDefault(x => x.Codigo == 1);

            if (confUsuarioUno != null && confUsuarioUno.Valor.Activo_Inactivo() == "Activo")
            {
                if (confUsuarioUno.Extra != null && confUsuarioUno.Extra.EsNumerico())
                {
                    filtro.PorcentajeSugerido = Convert.ToDecimal(confUsuarioUno.Extra);
                }
            }
            else
            {
                var porcentajeGeneral = _session?.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault(x => x.Codigo == 2);
                if (porcentajeGeneral != null && porcentajeGeneral.Valor > 0)
                {
                    filtro.PorcentajeSugerido = porcentajeGeneral.Valor;
                }
            }
            filtro.TipoVisualizacion = _session.TipoVisualizacionProductos;
            filtro.Ordenamiento = _session.Ordenamiento;
            filtro.TipoEmpresa = _session?.Sistema?.TipoEmpresa ?? 1;
            filtro.VerTodos = true;

            var diccionario = _repositorioProducto.ListaProductosV3(filtro);

            lista = diccionario?.FirstOrDefault().Value;


            //Secrea el documento
            iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4);
            doc.SetMargins(20f, 20f, 20f, 20f);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);

            //Las cuestiones basicas.
            doc.AddAuthor("Listado Productos");
            //doc.AddTitle("Cliente :" + cliente.RazonSocial);
            doc.Open();



            #region Datos de cabecera
            Paragraph para1 = new Paragraph();
            Phrase ph2 = new Phrase();
            Paragraph mm1 = new Paragraph();

            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk("Listado Productos", FontFactory.GetFont("Arial", 20, 2)));
            ph2.Add(new Chunk(Environment.NewLine));

            ph2.Add(new Chunk("Vendedor: " + _session.Usuario.Nombre, FontFactory.GetFont("Arial", 16, 2)));
            ph2.Add(new Chunk(Environment.NewLine));
            mm1.Add(ph2);
            para1.Add(mm1);
            doc.Add(para1);

            #endregion



            BaseFont helvetica = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, true);

            iTextSharp.text.Font negrita = new iTextSharp.text.Font(helvetica, 9f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));


            #region Esquema basico de la tabla

            PdfPTable tabla = new PdfPTable(new float[] { 10f, 15f, 60f, 15f }) { WidthPercentage = 100f };
            PdfPCell c1 = new PdfPCell(new Phrase("Código", negrita));
            PdfPCell c2 = new PdfPCell(new Phrase("Cod. Barras", negrita));
            PdfPCell c3 = new PdfPCell(new Phrase("Descripción", negrita));
            PdfPCell c4 = new PdfPCell(new Phrase("Precio", negrita));

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


                String codigoProducto = "";

                if (item.Representada == false)
                {
                    codigoProducto = item.ProductoId.ToString();
                }
                else
                {
                    codigoProducto = item.CodigoProveedor;
                }


                //Se crea una nueva fila con los datos-
                c1.Phrase = new Phrase(codigoProducto);

                String cb = !String.IsNullOrEmpty(item.CodigoBarras) ? item.CodigoBarras : "    ";
                c2.Phrase = new Phrase(cb);


                String esquemaNombre = item.Nombre;
                if (item.Cantidad > 0)
                {
                    esquemaNombre += " {x" + item.Cantidad.MostrarEntero() + "}";
                }

                c3.Phrase = new Phrase(esquemaNombre);



                //if(presiosFinales==true)
                //{
                c4.Phrase = new Phrase(item.PrecioBruto.FormatoMoneda());
                //}
                //else
                //{
                //    c4.Phrase = new Phrase(item.PrecioNeto.FormatoMoneda());
                //}


                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(c3);
                tabla.AddCell(c4);
            }





            //c2.Colspan = 5;
            //c2.Phrase = new Phrase("");
            //c2.HorizontalAlignment = Element.ALIGN_RIGHT;
            //c2.Phrase = new Phrase(lista.Sum(c => c.SaldoCtaCte + c.Adelanto).FormatoMoneda());

            //tabla.AddCell(c2);
            #endregion
            doc.Add(tabla);

            writer.Close();
            doc.Close();
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms, "application/pdf");

        }





        public IActionResult ColectorDatos()
        {
            return View();
        }





        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult CargarProductoRelacionados(String json)
        {
            try
            {
                List<Int32> listaProdIds = json.ToObsect<List<Int32>>();

                #region Filtro

                ProductoMinimoViewModel productoViewModel = HttpContext.Session.GetJson<ProductoMinimoViewModel>("ProductoMinimoViewModel");

                FiltroProducto filtro;

                if (productoViewModel == null)
                {
                    filtro = getFiltro("");
                }
                else
                {
                    filtro = productoViewModel.Filtro;
                    filtro.Dato = "";
                }

                filtro.ModoVerProducto = false;
                filtro.ListaProductosVedette = listaProdIds;
                filtro.IncluirIImagenWeb = true;
                var consulta = _repositorioProducto.ListaProductosV3(filtro);
                var listaProd = consulta?.FirstOrDefault().Value;
                #endregion

                return PartialView("_carouselProductosRelacionados", listaProd);
            }
            catch (Exception ex)
            {

                return new JsonResult("");
            }
        }


        /// <summary>
        /// Este metodo se usa en el principal de producto para la carga en segundo plano de las imagenes.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult CargarImagenesSegundoPlano()
        {
            try
            {
                var itemRecupera = HttpContext.Session.
                    GetJson<ProductoMinimoViewModel>("ProductoMinimoViewModel");

                if (itemRecupera != null)
                {
                    if (itemRecupera.Lista != null)
                    {
                        List<int> listaImg = itemRecupera.Lista.
                            Select(x => x.ProductoBaseId).ToList();

                        var listado = _repositorioProducto.
                            ListaRutaImagene_Web(listaImg);


                        return new JsonResult(listado);
                    }
                    else
                    {
                        return new JsonResult("");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }






        /// <summary>
        /// 22/02/2022
        /// </summary>
        /// <param name="productoBase"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult verImagenes(Int32 productoBase, int productoId)
        {
            try
            {

                List<ViewImagen> listaImagenes = _repositorioProducto.ListaRutaImagenes_Sql(productoBase);

                if (listaImagenes?.Count() > 0)
                {
                    foreach (var img in listaImagenes)
                    {
                        img.Imagen = img.ImagenByte.RutaImagenJpg();

                        //Test 
                        System.Drawing.Image imagenOriginal = TrabajandoConImagenes.ByteArrayToImage(img.ImagenByte);
                        System.Drawing.Image miniatura = TrabajandoConImagenes.RedimencionarPorPorcentaje(imagenOriginal, 75);
                        Byte[] imgMini = TrabajandoConImagenes.ImageToByte(miniatura);

                        img.ImagenMiniatura = imgMini.RutaImagenJpg();
                    }
                }
                else
                {
                    var itemRecupera = ProductoMinimoViewModel.RecuperarSession(HttpContext);
                    if (itemRecupera != null)
                    {
                        var prod = itemRecupera.Lista.FirstOrDefault(c => c.ProductoId == productoId);

                        ViewImagen imagen = new ViewImagen();
                        imagen.Imagen = prod.Imagen.RutaImagenJpg();

                        System.Drawing.Image imagenOriginal = TrabajandoConImagenes.ByteArrayToImage(prod.Imagen);
                        System.Drawing.Image miniatura = TrabajandoConImagenes.RedimencionarPorPorcentaje(imagenOriginal, 75);
                        Byte[] imgMini = TrabajandoConImagenes.ImageToByte(miniatura);

                        imagen.ImagenMiniatura = imgMini.RutaImagenJpg();

                        listaImagenes = new List<ViewImagen>();
                        listaImagenes.Add(imagen);
                    }

                }

                return PartialView("_galeriaImagenes", listaImagenes);
            }
            catch (Exception ex)
            {

                return new JsonResult("");
            }
        }

    }
}