using DRR.Core.DBEmpresaEjemplo.Models;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Home;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Areas.Visor;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RtfPipe;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using WebDRR.Clases;

namespace WebDRR.Controllers
{
    public class HomeController : ControllerDrrSystemas
    {
        #region Variables
        private IRepositorioEmpresa _repositorioEmpresa;
        private SessionAcceso _session;
        private IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        #endregion

       

        #region Constructor
        public HomeController(IRepositorioEmpresa repositorioEmpresa, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, IConfiguration configuration)
        {
            //Se obtiene el 
            _repositorioEmpresa = repositorioEmpresa;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _environment = environment;

            _configuration = configuration;

        }
        #endregion


        /// <summary>
        /// Esta pagina tiene 2 vistas.
        /// 1) Si no hay empresa, se muestra una pagina principal donde el usuario 
        /// selecciona la empresa con la que se va a trabajar.
        /// 2) Hay una empresa, se realiza el login como usuario Anonimo y se muestra
        /// los datos que el usuario anonimo puede visualizar.
        /// </summary>
        /// <param name="idEmpresa">
        /// (int?) El identificador de la empresa en caso de ser 0 o null ingresa por 1
        /// caso contrario 2.
        /// </param>
        /// <returns>
        /// Para 1 se va redirecciona a el metodo de accion - principal portal.
        /// caso contrario se muestra la web de la empresa con los permisos del usuario anonimo.
        /// </returns>
        public IActionResult Index(Int32? idEmpresa)
        {
            try
            {
                //
                TempData.Clear();

                //Caso 1
                if (idEmpresa == null || idEmpresa == 0)
                {
                    if (_session == null)
                    {
                        //se envia a la pantalla donde el usuario selecciona la empresa.
                        return RedirectToAction("Empresas", "Home");
                    }
                    else
                    {
                        _repositorioEmpresa.DatosSistema = _session.Sistema;

                        EmpresaViewModel empresaViewModel = _repositorioEmpresa.GetEmpresa_SQL((int)_session.Sistema.EmpresaId);

                        _session.Sistema.Correo = empresaViewModel.CorreoElectronico;
                        _session.Sistema.Logo = empresaViewModel.Logo_Html;

                        if (_session.Configuracion == null)
                        {
                            //Guardo en sesion los datos. 
                            if (!String.IsNullOrEmpty(empresaViewModel.Configuracion))
                            {
                                _session.Configuracion = empresaViewModel.Configuracion.GetObjectOfXml<ConfiguracionAdminEmpresa>();
                            }

                            HttpContext.Session.SetJson("SessionAcceso", _session);
                        }

                        if (_session.Sistema?.SectorId != null && _session.Sistema?.SectorId != 0)
                        {
                            //-- Hay que controlar, redirecciono a productos porque no tiene sentido mosmtrar los datos del sector.

                            var sector = _repositorioEmpresa.GetSector((int)_session.Sistema.SectorId);
                            if (sector != null)
                            {
                                empresaViewModel = new EmpresaViewModel();
                                empresaViewModel.RazonSocial = sector.Descripcion;
                                return View(empresaViewModel);
                            }
                            else
                            {
                                return View(empresaViewModel);
                            }

                            //--PARCHE QUE SE ARMO PARA ZAFAR Y LLEVA MESES Se paro 10/09/2020
                            //return RedirectToAction("Productos", "Producto");
                        }
                        else
                        {
                            return View(empresaViewModel);
                        }

                    }
                }
                //Caso 2
                else
                {
                    DRREnviroment enviroment = new DRREnviroment();

                    //El primer ingreso hay que cargar la informacion.
                    if (_session == null)
                    {
                        _session = new SessionAcceso();
                        _session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(_session.Sistema?.Cn_Alma))
                        {
                            _session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
                        }
                    }

                    _session.Sistema.EmpresaId = idEmpresa;
                    string ruta, documento;
                    documento = obtenerJsonEmpresas(out ruta);
                    var empresaDatos = documento?.ToObsect<List<DatosEmpresa>>()?.FirstOrDefault(c => c.IdEmpresa == idEmpresa
                    && c.Activa == true);

                    if (empresaDatos == null)
                    {
                        throw new Exception("Servidor fuera de servicio, para mas informacion comunicarse al tel: 03751-420850");


                    }

                    _session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa_V2(empresaDatos.Nombre_BaseDatos);


                    _repositorioEmpresa.DatosSistema = _session.Sistema;
                    var empresa = _repositorioEmpresa.ObtenerEmpresa_AlmaNet_SQL((int)_session.Sistema.EmpresaId);

                    //if(empresa.EmpresaTipoConfigId && "Multiple")
                    Int32 tipoemp = empresa.EmpresaTipoConfigId ?? 0;
                    String cadena = tipoemp.DevolverBinario().Reverso();

                    //Ojito
                    EmpresaViewModel empresaViewModel = _repositorioEmpresa.GetEmpresa_SQL((int)_session.Sistema.EmpresaId);

                    //Guardo en sesion los datos. 
                    if (!String.IsNullOrEmpty(empresaViewModel?.Configuracion))
                    {
                        _session.Configuracion = empresaViewModel.Configuracion.GetObjectOfXml<ConfiguracionAdminEmpresa>();
                    }



                    #region Testing Hosting

                    var activarHosting = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Hosting_Asignado);

                    if (activarHosting != null)
                    {
                        if (activarHosting.Valor.MostrarEntero() == 1)
                        {

                            Byte valorServidor = 0;

                            Boolean sepuede = Byte.TryParse(activarHosting.Extra, out valorServidor);

                            if (sepuede == true)
                            {

                                string urlVerifica = verificarHosting(valorServidor);
                                if (!string.IsNullOrEmpty(urlVerifica))
                                {
                                    return Redirect(urlVerifica);
                                }

                            }

                        }
                    }
                    #endregion






                    if (cadena.Length == 9)
                    {
                        Int32 pos = (Int32)Char.GetNumericValue(cadena[8]);
                        if (pos == 1)
                        {
                            _session.Sistema.TipoEmpresa = 256;
                            empresaViewModel.ListadoSectores = _repositorioEmpresa.ListaRepresentadas().ToList();
                            empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                        }

                    }
                    else if (cadena.Length == 10)
                    {
                        Int32 pos = (Int32)Char.GetNumericValue(cadena[9]);
                        if (pos == 1)
                        {
                            _session.Sistema.TipoEmpresa = 512;
                            var query = _repositorioEmpresa.ListaRepresentadas();

                            //-Es asi-

                            if (query?.Count() > 0)
                            {
                                DatoConfiguracion conSectores = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == (int)ConfPortal.EnumConfPortal.Sectores_Empresa);

                                if (conSectores?.Valor.MostrarEntero() == 1)
                                {
                                    String[] sectoresActivos = conSectores.Extra.Split(' ');

                                    empresaViewModel.ListadoSectores = new List<Sector>();
                                    foreach (var sector in query)
                                    {
                                        string id = sector.SectorId.ToString();
                                        Boolean existe = sectoresActivos.Any(c => c == id);
                                        if (existe)
                                        {
                                            empresaViewModel.ListadoSectores.Add(sector);
                                        }
                                    }
                                }
                                else
                                {
                                    empresaViewModel.ListadoSectores = query.Where(x => x.EmpresaId != null).ToList();
                                }



                            }

                            empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                        }

                    }
                    else
                    {
                        //Para las empresas que no tienen multiples sectores o trabajan con representadas.
                        _session.Sistema.TipoEmpresa = 1;
                        empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                    }





                    _session.Sistema.Nombre = empresaViewModel.RazonSocial;
                    _session.Sistema.Logo = empresaViewModel.Logo_Html;
                    _session.Sistema.Correo = empresaViewModel.CorreoElectronico?.Trim();

                    string correo = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 5)?.Extra;
                    if (!String.IsNullOrEmpty(correo))
                    {
                        _session.Sistema.Correo = correo;

                    }


                    //Este esquema eslo nuevo de momento se implementa en empresas que operan con 1 sector.
                    if (_session.Sistema.SectorId == null)
                    {
                        String rutaWS = @"https://wa.me/" + _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 6)?.Extra;
                        _session.Sistema.WhatsappSector = rutaWS;
                    }


                    var tipoVisualizacion = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == 16);
                    if (tipoVisualizacion != null)
                    {
                        _session.TipoVisualizacionProductos = Convert.ToInt32(tipoVisualizacion.Valor);
                    }

                    HttpContext.Session.SetJson("SessionAcceso", _session);



                    return View(empresaViewModel);

                }


            }
            catch (Exception ex)
            {
                //var routeValues = new RouteValueDictionary
                //{
                //    { "Id", -1 },
                //    { "Mensaje", ex.Message }
                //};

                //String url = Url.Action("Notificaciones", "Home", routeValues);

                //return Redirect(url);
                NotificacionesViewModel notificacionesView = new NotificacionesViewModel();
                notificacionesView.LayoutRoot = true;
                notificacionesView.Mensaje = ex.Message;
                notificacionesView.Id = 123;

                return RedirectToAction("Notificaciones", notificacionesView);
            }

        }

       
        public IActionResult Representada(Int16? sectorId, String nombre, string urlAtras = "")
        {
            if (sectorId == null || sectorId == 0)
            {
                _repositorioEmpresa.DatosSistema = _session.Sistema;

                if (String.IsNullOrEmpty(urlAtras))
                {
                    urlAtras = HttpContext.Request.UrlAtras();
                }
                ViewBag.urlAtras = urlAtras;

                if (_session.Sistema.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                {
                    var listaRep = _repositorioEmpresa.ListaRepresentadas();
                    ViewBag.Titulo = "Representadas";
                    ViewBag.Representada = true;
                    return View(listaRep);
                }
                else
                {
                    List<Sector> lista = new List<Sector>();

                    var query = _repositorioEmpresa.ListaRepresentadas();

                    if (query?.Count() > 0)
                    {
                        DatoConfiguracion conSectores = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == (int)ConfPortal.EnumConfPortal.Sectores_Empresa);

                        if (conSectores?.Valor.MostrarEntero() == 1)
                        {
                            String[] sectoresActivos = conSectores.Extra.Split(' ');

                            foreach (var sector in query)
                            {
                                string id = sector.SectorId.ToString();
                                Boolean existe = sectoresActivos.Any(c => c == id);
                                if (existe)
                                {
                                    lista.Add(sector);
                                }
                            }
                        }
                        else
                        {
                            lista = query.Where(x => x.EmpresaId != null).ToList();
                        }

                    }


                    ViewBag.Titulo = "Sectores";

                    return View(lista?.Where(c => c.EmpresaId != null));
                }

            }
            else
            {
                ////Probando esto se va
                //SessionCarrito carrito = HttpContext.Session?.GetJson<SessionCarrito>("Carrito");
                //if (carrito != null)
                //{
                //    carrito.Clear();
                //}
                HttpContext.Session?.Remove("Carrito");

                _session.Sistema.NombreRepresentada = nombre;

                //Si quedo algo de los productos en session
                HttpContext.Session.Remove("ProductoMinimoViewModel");

                _session.Sistema.SectorId = sectorId;


                #region Selecciono la lista de precios de 1 cliente


                if (_session?.Sistema?.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada)
                {
                    if (_session?.Usuario?.Rol == (Int32)EnumRol.Cliente || _session?.Usuario?.Rol == (Int32)EnumRol.ClienteFidelizado)
                    {
                        RepositorioCliente repositorioCliente = new RepositorioCliente();
                        repositorioCliente.DatosSistema = _session.Sistema;

                        var conf = repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session?.Usuario.IdAlmaWeb);

                        int idCliente = repositorioCliente.GetClienteVendedor(8, (int)conf?.EntidadSucID);
                        _session.Usuario.EntidadSucId = conf?.EntidadSucID;
                        _session.Usuario.Cliente_Vendedor_Id = idCliente;

                        var entidadCrep = repositorioCliente.GetClienteRespresentada((int)_session.Usuario.Cliente_Vendedor_Id, (int)sectorId);
                        if (entidadCrep != null)
                        {
                            _session.Usuario.IdListaPrecio = entidadCrep.ListaPrecId;
                        }

                    }
                }

                #endregion


                //Por el momento se tiene que mejorar----------------------
                _repositorioEmpresa.DatosSistema = _session.Sistema;
                var sectores = _repositorioEmpresa.ListaRepresentadas();
                if (sectores?.Count() > 0)
                {
                    var sec = sectores.FirstOrDefault(c => c.SectorId == sectorId);
                    if (sec != null)
                    {



                        String rutaWS = @"https://wa.me/" + sec.DenominacionAdicional;
                        _session.Sistema.WhatsappSector = rutaWS;

                        if (sec.Logo != null)
                        {
                            _session.Sistema.Logo = sec.Logo.RutaImagenJpg();

                        }


                    }
                }

                string correo = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 5)?.Extra;
                if (!String.IsNullOrEmpty(correo))
                {
                    _session.Sistema.Correo = correo;

                }



                var tipoVisualizacion = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == 16);
                if (tipoVisualizacion != null)
                {
                    _session.TipoVisualizacionProductos = Convert.ToInt32(tipoVisualizacion.Valor);
                }

                HttpContext.Session.SetJson("SessionAcceso", _session);



                //if (String.IsNullOrEmpty(urlAtras))
                //{
                //    return RedirectToAction("Index", "Home");
                //}
                //else
                //{
                //    return Redirect(urlAtras);
                //}

                var irA = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 17);

                if (irA != null)
                {
                    if (!String.IsNullOrEmpty(irA.Extra))
                    {
                        if (irA.Extra == "Carrito")
                        {
                            return RedirectToAction("Index", "Carrito");
                        }
                        else if (irA.Extra == "Principal")
                        {
                            return RedirectToAction("Principal", "Home");
                        }
                        else if (irA.Extra == "Producto")
                        {
                            return RedirectToAction("Productos", "Producto");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
        }





        /// <summary>
        /// Me permite ingresar directamente a un sector o representada.
        /// Por ahora los productos no se mezclan sino que visualiza lo de ese sector.
        /// </summary>
        /// <param name="idEmpresa"></param>
        /// <param name="idSector"></param>
        /// <param name="url">Este campo me va a permitir redireccionar a 1 determinada pagina, ej: ingresar directamente a la parte de productos.</param>
        /// <returns></returns>
        /// <modificado>06/05/2020</modificado>
        public IActionResult IngresoRepresentada(Int32 idEmpresa, Int16 idSector, String url = "")
        {
            try
            {

                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();

                _session = new SessionAcceso();
                _session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
                _session.Sistema.EmpresaId = idEmpresa;


                string ruta, documento;
                documento = obtenerJsonEmpresas(out ruta);
                var empresaDatos = documento?.ToObsect<List<DatosEmpresa>>()?.FirstOrDefault(c => c.IdEmpresa == idEmpresa
 && c.Activa == true);

                if (empresaDatos == null)
                {
                    throw new Exception("Servidor fuera de servicio, para mas informacion comunicarse al tel: 03751-420850");


                }

                _session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa_V2(empresaDatos.Nombre_BaseDatos);



                _repositorioEmpresa.DatosSistema = _session.Sistema;
                var empresa = _repositorioEmpresa.ObtenerEmpresa_AlmaNet_SQL((int)_session.Sistema.EmpresaId);



                //if(empresa.EmpresaTipoConfigId && "Multiple")
                Int32 tipoemp = empresa.EmpresaTipoConfigId ?? 0;
                String cadena = tipoemp.DevolverBinario().Reverso();


                if (cadena.Length == 9)
                {
                    Int32 pos = (Int32)Char.GetNumericValue(cadena[8]);
                    if (pos == 1)
                    {
                        _session.Sistema.TipoEmpresa = (Int32)EnumTiposEmpresas.Representada;
                    }
                }
                else if (cadena.Length == 10)
                {
                    Int32 pos = (Int32)Char.GetNumericValue(cadena[9]);
                    if (pos == 1)
                    {
                        _session.Sistema.TipoEmpresa = (Int32)EnumTiposEmpresas.EmpresaMultisector;
                    }

                }
                else
                {
                    _session.Sistema.TipoEmpresa = (Int32)EnumTiposEmpresas.Empresas;
                }


                var conf = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa(idEmpresa);
                //Guardo en sesion los datos. 
                _session.Configuracion = conf;

                _session.Sistema.SectorId = idSector;




                #region Testing Hosting

                var activarHosting = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Hosting_Asignado);

                if (activarHosting != null)
                {
                    if (activarHosting.Valor.MostrarEntero() == 1)
                    {
                        Byte valorServidor = 0;

                        Boolean sepuede = Byte.TryParse(activarHosting.Extra, out valorServidor);
                        if (sepuede == true)
                        {
                            string urlVerifica = verificarHosting(valorServidor);
                            if (!string.IsNullOrEmpty(urlVerifica))
                            {
                                return Redirect(urlVerifica);
                            }
                        }
                    }
                }
                #endregion







                var sector = _repositorioEmpresa.GetSector_SQL((int)_session.Sistema.SectorId);

                if (sector?.SectorId > 0)
                {
                    _session.Sistema.NombreRepresentada = sector.Descripcion;

                    String rutaWS = @"https://wa.me/" + sector.DenominacionAdicional;
                    _session.Sistema.WhatsappSector = rutaWS;

                    if (sector.Logo != null)
                    {
                        _session.Sistema.Logo = sector.Logo.RutaImagenJpg();

                    }
                    else
                    {
                        //La empresa----

                    }

                }

                string correo = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 5)?.Extra;
                if (!String.IsNullOrEmpty(correo))
                {
                    _session.Sistema.Correo = correo;

                }

                var tipoVisualizacion = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == 16);
                if (tipoVisualizacion != null)
                {
                    _session.TipoVisualizacionProductos = Convert.ToInt32(tipoVisualizacion.Valor);
                }

                //Guardo en sesion los datos. 
                HttpContext.Session.SetJson("SessionAcceso", _session);

                //Boolean esIrAProducto = false;
                if (!String.IsNullOrEmpty(url))
                {

                    #region CASO AUTOVALLE -- TEMPORAl
                    if (_session.Sistema.EmpresaId == 88)
                    {
                        return RedirectToAction("Index", "Home", new { idEmpresa = 88 });
                    }
                    #endregion

                    return Redirect(url);
                }
                else
                {
                    var irA = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 17);

                    if (irA != null)
                    {
                        if (!String.IsNullOrEmpty(irA.Extra))
                        {
                            if (irA.Extra == "Carrito")
                            {
                                return RedirectToAction("Index", "Carrito");
                            }
                            else if (irA.Extra == "Principal")
                            {
                                return RedirectToAction("Principal", "Home");
                            }
                            else if (irA.Extra == "Producto")
                            {
                                return RedirectToAction("Productos", "Producto");
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(url))
                        {
                            return Redirect(url);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }


                    }
                }



            }
            catch (Exception ex)
            {
                //var routeValues = new RouteValueDictionary
                //{
                //    { "Id", -1 },
                //    { "Mensaje", ex.Message }
                //};

                //String urlNotificaciones = Url.Action("Notificaciones", "Home", routeValues);

                //return Redirect(urlNotificaciones);

                NotificacionesViewModel notificacionesView = new NotificacionesViewModel();
                notificacionesView.LayoutRoot = true;
                notificacionesView.Mensaje = ex.Message;
                notificacionesView.Id = 123;

                return RedirectToAction("Notificaciones", notificacionesView);
            }

        }


        [HttpGet]
        public IActionResult Setting(String dato)
        {
            HttpContext.Session.Clear();

            //momento
            String cdrr = "2" + "-" + "0";
            cdrr = cdrr.Encriptar();

            String autovalle = "88" + "-" + "0";
            autovalle = autovalle.Encriptar();


            String cperfume = "12" + "-" + "0";
            cperfume = cperfume.Encriptar();

            String comercial = "117" + "-" + "0";
            comercial = comercial.Encriptar();

            String dm = "118" + "-" + "0";
            dm = dm.Encriptar();

            String agroAzul = "120" + "-" + "0";
            agroAzul = agroAzul.Encriptar();


            String contruccion = "125" + "-" + "0";
            comercial = contruccion.Encriptar();

            String mimen = "23" + "-" + "0";
            mimen = mimen.Encriptar();

            Setting entidad = new Setting();
            if (!String.IsNullOrEmpty(dato))
            {
                entidad.Codigo = dato;
            }

            return View(entidad);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Setting(Setting model)
        {

            try
            {
                String codigo = model.Codigo;


                String[] vector = codigo.DesEncriptar().Split('-');

                Int32 empresa = Convert.ToInt32(vector[0]);
                Int32 sector = Convert.ToInt32(vector[1]);

                if (sector > 0)
                {
                    return RedirectToAction("IngresoRepresentada", new { idEmpresa = empresa, idSector = sector });
                }
                else
                {
                    return RedirectToAction("Index", new { idEmpresa = empresa });
                }

            }
            catch (Exception)
            {
                return View();
            }

        }


        public IActionResult Ingreso(String dato)
        {
            try
            {
                String codigo = dato;


                String[] vector = codigo.DesEncriptar().Split('-');

                Int32 empresa = Convert.ToInt32(vector[0]);
                Int32 sector = Convert.ToInt32(vector[1]);

                if (sector > 0)
                {
                    return RedirectToAction("IngresoRepresentada", new { idEmpresa = empresa, idSector = sector });
                }
                else
                {
                    return RedirectToAction("Index", new { idEmpresa = empresa });
                }
            }
            catch (Exception)
            {
                return RedirectToAction("Setting");
            }

        }



        public IActionResult MenuEmpresas()
        {


            return View();
        }


        /// <summary>
        /// Este action muestra las empresas activa
        /// </summary>
        /// <param name="clave">verificacion de acceso. Solo personal de DrrSystemas tiene acceso </param>
        /// <returns>View</returns>
        public IActionResult Empresas(String clave)
        {
            try
            {
                if (String.IsNullOrEmpty(clave))
                {
                    return RedirectToAction("MenuEmpresas", "Home");
                }
                else if (clave == "drr_systemas")
                {
                    HttpContext.Session.Clear();

                    string error = "";

                    SeleccionarEmpresaViewModel seleccionarEmpresa = new SeleccionarEmpresaViewModel();
                    //DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();

                    string ruta, documento;
                    documento = obtenerJsonEmpresas(out ruta);

                    ConfiguracionEmpresas configuracionEmpresas = new ConfiguracionEmpresas();

                    List<DatosEmpresa> lista = new List<DatosEmpresa>();

                    if (String.IsNullOrEmpty(documento))
                    {

                        //no hay nada se tiene que genera el archivo,
                        lista = configuracionEmpresas.GenerarEsquemaDefecto(out error);

                        string datoGenericos = lista.ToJson();
                        StreamWriter sw = new StreamWriter(ruta, false, Encoding.ASCII);
                        sw.Write(datoGenericos);
                        sw.Close();
                    }
                    else
                    {
                        lista = documento.ToObsect<List<DatosEmpresa>>();
                    }

                    seleccionarEmpresa.ListaEmpresas = lista?.Where(c => c.Activa == true).ToList();

                    return View(seleccionarEmpresa);

                }
                else
                {
                    TempData["ErrorRepresentada"] = "La clave ingresada no es correcta";

                    return RedirectToAction("MenuEmpresas", "Home");
                }

            }
            catch (Exception ex)
            {
                return View(null);
            }
        }

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


        public IActionResult ConfVista(int? opcion)
        {
            try
            {
                if (opcion == null)
                {
                    return View();
                }
                else
                {

                    HttpContext.Session.SetJson("SessionAcceso", _session);

                    return RedirectToAction("Index", new { _session.Sistema.EmpresaId });
                }

            }
            catch (Exception ex)
            {
                return View(null);
            }
        }


        /// <summary>
        /// Muestra la pagina principal del portal, dependiendo el rol del usuario.
        /// </summary>
        /// <modificacion>10/01/2022</modificacion>
        /// <returns></returns>
        public IActionResult Principal()
        {
            _repositorioEmpresa.DatosSistema = _session.Sistema;

            #region Control - revisar
            if (_session?.Sistema?.TipoEmpresa != (Int32)EnumTiposEmpresas.Empresas)
            {
                if (_session?.Sistema?.SectorId == null || _session?.Sistema?.SectorId == 0)
                {
                    String urlAtras = Url.Action("Principal", "Home");

                    String url = Url.Action("Representada", "Home", new { urlAtras = urlAtras });

                    return Redirect(url);
                }
            }
            #endregion


            #region Si es Vendedor - Muestra si hay un cliente seleccionado
            if (_session?.Usuario?.Rol == (int)EnumRol.Vendedor)
            {
               var sessionDiaTrabajoVendedor = SessionDiaTrabajoVendedor.RecuperarSession(HttpContext);
                ViewData["DiaTrabajoVendedor"] = sessionDiaTrabajoVendedor;

                var carrito = SessionCarrito.ObtenerCarrito(this.HttpContext.RequestServices);

                if (carrito?.Cliente?.ClienteID > 0)
                {
                    ViewData["Cliente"] = carrito?.Cliente?.RazonSocial;
                    ViewData["ClienteId"] = carrito?.Cliente?.ClienteID;
                }
            }
            #endregion


            #region Configuracion de Filtro y Buscador

            Boolean filtro33Activo = false;
            Boolean verBuscardor = false;

            try
            {
                DatoConfiguracion confFiltroProducto = _session.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault
    (c => c.Codigo == (Int32)ConfViewDatosProductos.EnumConfViewDatosProductos.InicializacionFiltroProducto);

                if (confFiltroProducto?.Valor.MostrarEntero() == 1)
                {
                    filtro33Activo = true;
                }

                DatoConfiguracion confBuscador = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo ==
(int)LibreriaBase.Areas.Usuario.ConfPortal.EnumConfPortal.Ver_Buscador_en_PaginaPrincipal);

                if (confBuscador?.Valor.MostrarEntero() == 1)
                {
                    verBuscardor = true;
                }

            }
            catch (Exception ex)
            {
                filtro33Activo = false;
                verBuscardor = false;
            }


            ViewData["Filtro33"] = filtro33Activo;
            ViewData["VerBuscador"] = verBuscardor;

            #endregion


            #region Productos Vedette

            //if (_session?.Usuario?.IdAlmaWeb == null || _session?.Usuario?.IdAlmaWeb == 0)
            //{
            //    List<Publicidad> listaPub = _repositorioEmpresa.ListarPublicidades();
            //    listaPub = listaPub?.Where(c => c.SectorId == null || c.SectorId == _session.Sistema.SectorId).ToList();
            //    ViewBag.listaPublicidad = listaPub;

            //    //Productos Vedettes.
            //    var prodVedettes = _session?.Configuracion?.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Productos_Vedette);

            //    if (prodVedettes != null && prodVedettes.Valor.Activo_Inactivo() == "Activo")
            //    {
            //        if (!String.IsNullOrEmpty(prodVedettes.Extra))
            //        {
            //            FiltroProducto filtro = new FiltroProducto();
            //            filtro.ListaProductosVedette = new List<int>();
            //            filtro.SectorId = _session.Sistema?.SectorId;
            //            filtro.TipoEmpresa = _session.Sistema?.TipoEmpresa;
            //            filtro.ListaPrecID = _session.getListaPrecio(this.HttpContext);
            //            filtro.ListaPrecioOferta = _session.getListaPrecioOferta();
            //            filtro.VerProductosSinStock = _session.getMostrarProductosStockCero();


            //            String[] prod = prodVedettes.Extra.Split("_");

            //            if (prod.Count() > 0)
            //            {
            //                foreach (var item in prod)
            //                {
            //                    String[] separar = item.Split("-");

            //                    if (separar[1] != null)
            //                    {
            //                        string valor = separar[1];

            //                        if (valor.EsNumerico())
            //                        {
            //                            short? idSector = Convert.ToInt16(valor);
            //                            if (_session.Sistema.SectorId == idSector)
            //                            {
            //                                //Aca agregar a la lista filtro.......

            //                                if (separar[0].ToString().EsNumerico() == true)
            //                                {
            //                                    filtro.ListaProductosVedette.Add(Convert.ToInt32(separar[0]));
            //                                }
            //                                //filtro.
            //                            }
            //                        }
            //                    }
            //                    else
            //                    {
            //                        if (separar[0].ToString().EsNumerico() == true)
            //                        {
            //                            filtro.ListaProductosVedette.Add(Convert.ToInt32(separar[0]));
            //                        }
            //                    }
            //                }
            //            }

            //            if (filtro.ListaProductosVedette.Count() > 0)
            //            {
            //                RepositorioProducto repositorioProducto = new RepositorioProducto();
            //                repositorioProducto.ElementosPorPagina = 6;
            //                repositorioProducto.DatosSistema = _session.Sistema;
            //                var request = repositorioProducto.ListaProductosV3(filtro);

            //                ViewBag.listaProductos = request?.FirstOrDefault().Value;
            //            }

            //        }
            //    }

            //}
            #endregion


            #region Publicidades - Notificaciones

            var conf_modPubliciad = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.NoticiasInternas); //50

            if (conf_modPubliciad?.Valor.MostrarEntero() == 1)
            {
                DateTime fecha = DateTime.Now.AddDays(-45);

                AdminVisor adminVisor = new AdminVisor(HttpContext);
                IServicioVisor servicioVisor = new ServicioVisor();
                servicioVisor.DatosSistema = _session.Sistema;

                List<Noticia> listaNoticias = adminVisor.Get_NoticiasInternas_BD(servicioVisor, fecha);

                if (listaNoticias?.Count() > 0)
                {
                    //listaNoticias = listaNoticias.Where(c => c.Fecha.Date <= fecha && (c.SectorId == null || c.SectorId == _session?.Sistema.SectorId)).ToList();
                    ViewData["ListaNoticias"] = listaNoticias;
                }


            }

            #endregion


            #region Leyenda

            var leyenda_bp = _session?.Configuracion?.ConfiguracionesViewDatosProductos?.FirstOrDefault(c => c.Codigo == 21);

            if (leyenda_bp != null)
            {
                ViewData["leyenda_bp"] = leyenda_bp.Extra;

            }
            else
            {
                ViewData["leyenda_bp"] = "";
            }
            #endregion


            return View(_session);
        }


        public IActionResult VerRepresentada(int sectorId, String urlRetorno)
        {
            _repositorioEmpresa.DatosSistema = _session.Sistema;

            Boolean representada = _session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada ? true : false;

            ViewRepresentada proveedorRepresentada = _repositorioEmpresa.GetProveedor(sectorId, representada);
            if (!String.IsNullOrEmpty(urlRetorno))
            {
                proveedorRepresentada.UrlRetorno = urlRetorno;
            }
            else
            {
                proveedorRepresentada.UrlRetorno = HttpContext.Request.UrlAtras();
            }
            if (!String.IsNullOrEmpty(proveedorRepresentada.Datos))
            {
                var html = Rtf.ToHtml(proveedorRepresentada.Datos);
                proveedorRepresentada.Datos = html;
            }




            return View(proveedorRepresentada);
        }




        #region Pagina de Error -- Por el momento esta desactivada hay que mejorar el modelo de error y los msj

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Notificaciones(NotificacionesViewModel vmodel, Int32? codigoEstado)
        {
            NotificacionesViewModel notificaciones = null;

            if (codigoEstado != null)
            {
                notificaciones = new NotificacionesViewModel
                {
                    Id = codigoEstado,
                    Mensaje = "Se produjo un error al procesar su solicitud",
                };

                return View(notificaciones);
            }
            else
            {
                if (vmodel == null)
                {
                    var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                    if (exceptionFeature != null)
                    {
                        notificaciones = new NotificacionesViewModel
                        {
                            Id = 500,
                            Mensaje = exceptionFeature.Error.Message,
                        };
                    }

                    return View(notificaciones);
                }
                else
                {
                    if (!String.IsNullOrEmpty(vmodel?.UrlIr))
                    {
                        TempData["UrlIr"] = vmodel.UrlIr;
                    }

                    return View(vmodel);
                }

            }

        }

        #endregion










        /// <summary>
        /// Este enfoque de ingreso esta especialemente diseñado para AGRO-AZUL hoy adaptado para todos.
        /// </summary>
        /// <param name="idEmpresa">Identificador de la empresa</param>
        /// <param name="idRubro">La familia que se va a mostrar </param>
        /// <param name="pagina">por defecto es 1</param>
        /// <param name="retorno">La url al sitio de donde se llamo al metodo.</param>
        /// <returns></returns>
        /// <modificacion>14-03-2022</modificacion>
        public IActionResult IngresoReducido(Int32? idEmpresa, Int32 idRubro, int pagina, string retorno)
        {
            try
            {
                Boolean esquemaAgroAzul = false;



                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();

                if (HttpContext.Session.Keys.Contains("SessionAcceso"))
                {
                    _session = HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
                }
                else
                {
                    _session = new SessionAcceso();
                }

                _session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();

                _session.Sistema.EmpresaId = idEmpresa;


                string ruta, documento;
                documento = obtenerJsonEmpresas(out ruta);
                var empresaDatos = documento?.ToObsect<List<DatosEmpresa>>()?.FirstOrDefault(c => c.IdEmpresa == idEmpresa);

                _session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa_V2(empresaDatos.Nombre_BaseDatos);


                _repositorioEmpresa.DatosSistema = _session.Sistema;
                var empresa = _repositorioEmpresa.ObtenerEmpresa_AlmaNet_SQL((int)_session.Sistema.EmpresaId);
                IRepositorioProducto repositorioProducto = new RepositorioProducto();
                repositorioProducto.DatosSistema = _session.Sistema;






                #region Esquema a medida para AgroAzul
                if (idEmpresa == (Int32)DRREnviroment.EnumEmpresas.AgroAzul)
                {
                    esquemaAgroAzul = true;

                    int idFamilia = 9;

                    if (retorno.Contains("hidroponia"))
                    {
                        idFamilia = 72;
                    }

                    var familia = repositorioProducto.GetProductoFamilia(idFamilia, null);

                    if (familia != null)
                    {
                        _session.Familia_ModoRaiz = familia;
                    }

                    //La seleccion Actual.
                    if (idRubro > 0)
                    {
                        _session.RubroId = idRubro;
                    }


                }
                else
                {
                    if (idRubro > 0)
                    {
                        var familia = repositorioProducto.GetProductoFamilia(idRubro, null);
                        if (familia != null)
                        {
                            _session.Familia_ModoRaiz = familia;
                            _session.RubroId = idRubro;
                        }
                    }


                }
                #endregion


                #region Buscar

                String datoBusqueda = "";

                if (HttpContext.Request.Query.ContainsKey("buscar"))
                {
                    datoBusqueda = HttpContext.Request.Query["buscar"];

                }


                String datoclasificacionId = "";
                //Nuevo ingresa por clasificacion a la web.-inica Dm
                if (HttpContext.Request.Query.ContainsKey("clasificacion"))
                {
                    datoclasificacionId = HttpContext.Request.Query["clasificacion"];



                }


                #endregion


                _session.ElementosPagina = pagina;


                //if(empresa.EmpresaTipoConfigId && "Multiple")
                Int32 tipoemp = empresa.EmpresaTipoConfigId ?? 0;
                String cadena = tipoemp.DevolverBinario().Reverso();

                //Ojito
                EmpresaViewModel empresaViewModel = _repositorioEmpresa.GetEmpresa_SQL((int)_session.Sistema.EmpresaId);

                if (cadena.Length == 9)
                {
                    Int32 pos = (Int32)Char.GetNumericValue(cadena[8]);
                    if (pos == 1)
                    {
                        _session.Sistema.TipoEmpresa = 256;
                        empresaViewModel.ListadoSectores = _repositorioEmpresa.ListaRepresentadas().ToList();
                        empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                    }

                }
                else if (cadena.Length == 10)
                {
                    Int32 pos = (Int32)Char.GetNumericValue(cadena[9]);
                    if (pos == 1)
                    {
                        _session.Sistema.TipoEmpresa = 512;
                        var query = _repositorioEmpresa.ListaRepresentadas();
                        if (query?.Count() > 0)
                        {
                            empresaViewModel.ListadoSectores = query.Where(x => x.EmpresaId != null).ToList();
                        }

                        empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                    }

                }
                else
                {
                    //Para las empresas que no tienen multiples sectores o trabajan con representadas.
                    _session.Sistema.TipoEmpresa = 1;
                    empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                }



                //Guardo en sesion los datos. 
                if (!String.IsNullOrEmpty(empresaViewModel.Configuracion))
                {
                    _session.Configuracion = empresaViewModel.Configuracion.GetObjectOfXml<ConfiguracionAdminEmpresa>();
                }




                #region Testing Hosting

                var activarHosting = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Hosting_Asignado);

                if (activarHosting != null)
                {
                    if (activarHosting.Valor.MostrarEntero() == 1)
                    {
                        Byte valorServidor = 0;

                        Boolean sepuede = Byte.TryParse(activarHosting.Extra, out valorServidor);
                        if (sepuede == true)
                        {
                            string urlVerifica = verificarHosting(valorServidor);
                            if (!string.IsNullOrEmpty(urlVerifica))
                            {
                                return Redirect(urlVerifica);
                            }
                        }
                    }
                }
                #endregion










                Int32 idRubroUbicacion = 0;

                if (!String.IsNullOrEmpty(retorno))
                {
                    //Por el momento hasta que se mejore el esquemita
                    DatoConfiguracion confUbicacionCliente = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Cliente_CFinal_Deposito_Defecto);

                    if (confUbicacionCliente?.Valor.Activo_Inactivo() == "Activo")
                    {
                        if (!String.IsNullOrEmpty(confUbicacionCliente.Extra))
                        {
                            String[] partes = confUbicacionCliente.Extra.Split("_");
                            //Verifiacar si hay mas de 1
                            if (partes[0] == retorno)
                            {
                                idRubroUbicacion = Convert.ToInt32(partes[1]);
                                _session.ListaRubroUbicacioId = new List<int>();
                                _session.ListaRubroUbicacioId.Add(idRubroUbicacion);
                            }
                        }

                        if (!String.IsNullOrEmpty(confUbicacionCliente.ExtraDos))
                        {
                            String[] partes = confUbicacionCliente.ExtraDos.Split("_");
                            //Verifiacar si hay mas de 1
                            if (partes[0] == retorno)
                            {
                                idRubroUbicacion = Convert.ToInt32(partes[1]);
                                _session.ListaRubroUbicacioId = new List<int>();
                                _session.ListaRubroUbicacioId.Add(idRubroUbicacion);
                            }
                        }
                    }


                    _session.UrlRetornoPaginaEmpresa = retorno;
                }



                _session.Sistema.Nombre = empresaViewModel.RazonSocial;
                _session.Sistema.Logo = empresaViewModel.Logo_Html;
                _session.Sistema.Correo = empresaViewModel.CorreoElectronico?.Trim();

                string correo = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 5)?.Extra;
                if (!String.IsNullOrEmpty(correo))
                {
                    _session.Sistema.Correo = correo;

                }


                //Este esquema eslo nuevo de momento se implementa en empresas que operan con 1 sector.
                if (_session.Sistema.SectorId == null)
                {
                    String rutaWS = @"https://wa.me/" + _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 6)?.Extra;
                    _session.Sistema.WhatsappSector = rutaWS;
                }


                var tipoVisualizacion = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == 16);



                if (tipoVisualizacion != null)
                {
                    _session.TipoVisualizacionProductos = Convert.ToInt32(tipoVisualizacion.Valor);
                }

                _session.GuardarSession(HttpContext);



                #region Ingreso Catalogo - Automatico

                if (HttpContext.Request.Query.ContainsKey("catalogo"))
                {
                    DatosUsuario datos = new DatosUsuario()
                    {
                        Correo = "catalogo@catalogo.com",
                        Clave = "catalogo"
                    };
                    return RedirectToAction("GotoCatalogo", "Acceso", datos);

                }

                #endregion


                String ofertas = HttpContext.Request.Query["Ofertas"];


                FiltroProducto filtro = new FiltroProducto();

                if (!String.IsNullOrEmpty(ofertas))
                {
                    filtro.Ofertas = true;
                }


                if (esquemaAgroAzul == true)
                {


                    if (idRubro > 0)
                    {
                        filtro.FiltroRubro = true;
                        filtro.FamiliaId = idRubro;

                        filtro.FiltroRaizFamilia = true;
                        filtro.Familia_Raiz_Id = _session.Familia_ModoRaiz.FamiliaId;
                    }
                    else if (idRubro < 0)
                    {

                        filtro.MarcaId = Math.Abs(idRubro);
                        filtro.FiltroMarca = true;

                        var marca = repositorioProducto.GetMarca((Int32)filtro.MarcaId);
                        if (marca != null)
                        {
                            filtro.NombreMarca = marca.Nombre;
                        }
                        //18/02/2022
                        _session.Familia_ModoRaiz = null;


                    }

                    if (!String.IsNullOrEmpty(datoBusqueda))
                    {
                        filtro.Dato = datoBusqueda;
                    }

                    if (!String.IsNullOrEmpty(datoclasificacionId))
                    {
                        filtro.ClasificacionId = Convert.ToInt32(datoclasificacionId);
                    }

                    _session.GuardarSession(HttpContext);

                    return RedirectToAction("Productos", "Producto", filtro);
                }
                else
                {
                    if (idRubro > 0)
                    {
                        filtro.FiltroRubro = true;
                        filtro.FamiliaId = idRubro;

                        filtro.FiltroRaizFamilia = false;
                        filtro.Familia_Raiz_Id = null;
                    }
                    else if (idRubro < 0)
                    {

                        filtro.MarcaId = Math.Abs(idRubro);
                        filtro.FiltroMarca = true;

                        var marca = repositorioProducto.GetMarca((Int32)filtro.MarcaId);
                        if (marca != null)
                        {
                            filtro.NombreMarca = marca.Nombre;
                        }
                        //18/02/2022
                        _session.Familia_ModoRaiz = null;


                    }

                    if (!String.IsNullOrEmpty(datoBusqueda))
                    {

                        filtro.Dato = datoBusqueda;

                    }

                    if (!String.IsNullOrEmpty(datoclasificacionId))
                    {
                        filtro.ClasificacionId = Convert.ToInt32(datoclasificacionId);
                    }

                    _session.GuardarSession(HttpContext);
                    return RedirectToAction("Productos", "Producto", filtro);
                }




            }
            catch (Exception ex)
            {
                var routeValues = new RouteValueDictionary
                {
                    { "Id", -1 },
                    { "Mensaje", ex.Message }
                };

                String redireccionar = Url.Action("Notificaciones", "Home", routeValues);

                return Redirect(redireccionar);
            }

        }


        private string verificarHosting(byte servidorConfiguracion)
        {
            #region Testing Hosting

            //Este dato se obtiene de la configuracion
            byte servidorH = servidorConfiguracion;

            string urlBaseLocalDepuracion = ConfiguracionHosting.getUrlHosting((byte)ConfiguracionHosting.Servidores.Local);

            //Arma la url actual
            string urlActualBase = HttpContext.Request.Scheme + @"://" + HttpContext.Request.Host + "/";


            //Este control evita que se redireccione en el momento de DEPURACION.
            if (urlBaseLocalDepuracion != urlActualBase)
            {


                String urlBase = "";


                if (servidorH == (byte)ConfiguracionHosting.Servidores.Produccion)
                {
                    urlBase = ConfiguracionHosting.getUrlHosting((byte)ConfiguracionHosting.Servidores.Produccion);

                }
                else if (servidorH == (byte)ConfiguracionHosting.Servidores.Testing)
                {
                    urlBase = ConfiguracionHosting.getUrlHosting((byte)ConfiguracionHosting.Servidores.Testing);

                }



                if (urlBase != urlActualBase)
                {
                    urlBase += HttpContext.Request.Path + HttpContext.Request.QueryString;
                    return urlBase;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }



            #endregion
        }



        public IActionResult UrlAfipQr()
        {
            return View();
        }

        public IActionResult VerDatosFacturaQR(String urlAfip)
        {
            String jSonDecodificado = "";
            try
            {
                String url = urlAfip;
                ViewData["url"] = url;
                String codigo = urlAfip.Replace(@"https://www.afip.gob.ar/fe/qr/?p=", "");

                byte[] decbuff = Convert.FromBase64String(codigo);
                jSonDecodificado = System.Text.Encoding.UTF8.GetString(decbuff);
            }
            catch
            {
                //si se envia una cadena si codificación base64, mandamos vacio
                jSonDecodificado = "";
            }

            DatosQr dato = JsonConvert.DeserializeObject<DatosQr>(jSonDecodificado);

            return View(dato);
        }





        public IActionResult Testing()
        {
            #region Testeos 

            //var datosDecidir = LibreriaCoreDRR.Decidir.LibreriaDecidir.ConfiguracionDatosEmpresas().
            //    FirstOrDefault(c => c.IdEmpresa == 88);

            //LibreriaCoreDRR.Decidir.LibreriaDecidir libreriaDecidir = new LibreriaCoreDRR.Decidir.LibreriaDecidir(datosDecidir);



            //String error = "";
            //Int32 binNaranja = 402918;
            //var request = libreriaDecidir.GetDatosTarjeta(binNaranja, out error);


            //var req2 = libreriaDecidir.GetPagos(null, true);

            #endregion



            LibreriaCoreDRR.GoogleSheets.GoogleSheets googleSheets = new LibreriaCoreDRR.GoogleSheets.GoogleSheets();


            googleSheets.Service = googleSheets.Authorize_AcountService();

            var listado = googleSheets.Leer_Cliente(googleSheets.Service, "producto!A2:Ak10");

            List<LibreriaCoreDRR.GoogleSheets.Cliente> lista = listado.ToList();
            return View(lista);




            // var routeValues = new RouteValueDictionary
            //{
            //    { "Id", 37 },
            //    { "Mensaje", "Se realizo el testing." }
            //};

            //String urlNotificaciones = Url.Action("Notificaciones", "Home", routeValues);

            //return Redirect(urlNotificaciones);
        }



        [HttpPost]
        public IActionResult AbrirFormularioContacto()
        {
            FormularioWp model = new FormularioWp();
            model.Titulo = "Consultas/Reclamos";
            model.Id = 2;
            model.Cliente = _session?.Usuario?.Nombre;
            model.Url = _session?.Sistema?.WhatsappSector;

            return PartialView("_formularioWhatsApp", model);
        }







        public IActionResult FullCargaDatos()
        {
            try
            {

                SessionFull_Clientes_Productos sessionFull_Clientes_Productos = new SessionFull_Clientes_Productos();

                FiltroCliente filtroCliente = new FiltroCliente();
                filtroCliente.Todos = true;
                filtroCliente.VendedorId = _session.Usuario.Cliente_Vendedor_Id;

                String error = "";

                IRepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = _session.Sistema;
                var resp = repositorioCliente.ClientesVendedorV2(filtroCliente, out error);

                if(resp?.First()!=null)
                {
                    sessionFull_Clientes_Productos.Clientes = resp?.First().Value ?? null;
                }

                //------------------------------------------------------------------**

                FiltroProducto filtroProducto = new FiltroProducto();
                filtroProducto.Todos = true;
                filtroProducto.EsVendedor = _session?.Usuario?.Rol == (Int32)EnumRol.Vendedor ? true : false;
                filtroProducto.SectorId = _session.Sistema?.SectorId;
                filtroProducto.TipoEmpresa = _session.Sistema?.TipoEmpresa;
                filtroProducto.ListaPrecID = _session.getListaPrecio(this.HttpContext);
                filtroProducto.ListaPrecioOferta = _session.getListaPrecioOferta();
                filtroProducto.VerProductosSinStock = _session.getMostrarProductosStockCero();
                filtroProducto.SucursalId = 1;
                filtroProducto.VerTodos = true;

                IRepositorioProducto repositorioProducto = new RepositorioProducto();
                repositorioProducto.DatosSistema = _session.Sistema;
                var respprod = repositorioProducto.ListaProductosV3(filtroProducto);

                if (respprod?.First() != null)
                {
                    sessionFull_Clientes_Productos.Productos = respprod?.First().Value ?? null;
                }

                //se guarda la carga.
                sessionFull_Clientes_Productos.GuardarSession(HttpContext);

                TempData["ErrorRepresentada"] = "Se activo las tablas en modo Full - esto implica que no se veran las actualizaciones de manera automatica, pero se ganar en velocidad, ya que los clientes y productos ahora estan en el teléfono.";

                return RedirectToAction("Principal");
            }
            catch (Exception ex)
            {

                throw;
            }
        }



        [HttpGet]
        public IActionResult links(Int32? idEmpresa, string data, Int32 idRubro, Int32 marca, int pagina, string retorno)
        {
            try
            {
                Boolean esquemaAgroAzul = false;


                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();

                if (HttpContext.Session.Keys.Contains("SessionAcceso"))
                {
                    _session = HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
                }
                else
                {
                    _session = new SessionAcceso();
                }

                _session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();

                _session.Sistema.EmpresaId = idEmpresa;


                string ruta, documento;
                documento = obtenerJsonEmpresas(out ruta);
                var empresaDatos = documento?.ToObsect<List<DatosEmpresa>>()?.FirstOrDefault(c => c.IdEmpresa == idEmpresa);

                _session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa_V2(empresaDatos.Nombre_BaseDatos);


                _repositorioEmpresa.DatosSistema = _session.Sistema;
                var empresa = _repositorioEmpresa.ObtenerEmpresa_AlmaNet_SQL((int)_session.Sistema.EmpresaId);
                IRepositorioProducto repositorioProducto = new RepositorioProducto();
                repositorioProducto.DatosSistema = _session.Sistema;






                #region Esquema a medida para AgroAzul
                if (idEmpresa == (Int32)DRREnviroment.EnumEmpresas.AgroAzul)
                {
                    esquemaAgroAzul = true;

                    int idFamilia = 9;

                    if (retorno.Contains("hidroponia"))
                    {
                        idFamilia = 72;
                    }

                    var familia = repositorioProducto.GetProductoFamilia(idFamilia, null);

                    if (familia != null)
                    {
                        _session.Familia_ModoRaiz = familia;
                    }

                    //La seleccion Actual.
                    if (idRubro > 0)
                    {
                        _session.RubroId = idRubro;
                    }


                }
                else
                {
                    if (idRubro > 0)
                    {
                        var familia = repositorioProducto.GetProductoFamilia(idRubro, null);
                        if (familia != null)
                        {
                            //_session.Familia_ModoRaiz = familia;
                            _session.RubroId = idRubro;
                        }
                    }


                }
                #endregion


                #region Buscar

                String datoBusqueda = data;



                String datoclasificacionId = "";
                //Nuevo ingresa por clasificacion a la web.-inica Dm
                if (HttpContext.Request.Query.ContainsKey("clasificacion"))
                {
                    datoclasificacionId = HttpContext.Request.Query["clasificacion"];



                }


                #endregion


                _session.ElementosPagina = 48;


                //if(empresa.EmpresaTipoConfigId && "Multiple")
                Int32 tipoemp = empresa.EmpresaTipoConfigId ?? 0;
                String cadena = tipoemp.DevolverBinario().Reverso();

                //Ojito
                EmpresaViewModel empresaViewModel = _repositorioEmpresa.GetEmpresa_SQL((int)_session.Sistema.EmpresaId);

                if (cadena.Length == 9)
                {
                    Int32 pos = (Int32)Char.GetNumericValue(cadena[8]);
                    if (pos == 1)
                    {
                        _session.Sistema.TipoEmpresa = 256;
                        empresaViewModel.ListadoSectores = _repositorioEmpresa.ListaRepresentadas().ToList();
                        empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                    }

                }
                else if (cadena.Length == 10)
                {
                    Int32 pos = (Int32)Char.GetNumericValue(cadena[9]);
                    if (pos == 1)
                    {
                        _session.Sistema.TipoEmpresa = 512;
                        var query = _repositorioEmpresa.ListaRepresentadas();
                        if (query?.Count() > 0)
                        {
                            empresaViewModel.ListadoSectores = query.Where(x => x.EmpresaId != null).ToList();
                        }

                        empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                    }

                }
                else
                {
                    //Para las empresas que no tienen multiples sectores o trabajan con representadas.
                    _session.Sistema.TipoEmpresa = 1;
                    empresaViewModel.TipoEmpresa = _session.Sistema.TipoEmpresa;
                }



                //Guardo en sesion los datos. 
                if (!String.IsNullOrEmpty(empresaViewModel.Configuracion))
                {
                    _session.Configuracion = empresaViewModel.Configuracion.GetObjectOfXml<ConfiguracionAdminEmpresa>();
                }




                #region Testing Hosting

                var activarHosting = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Hosting_Asignado);

                if (activarHosting != null)
                {
                    if (activarHosting.Valor.MostrarEntero() == 1)
                    {
                        Byte valorServidor = 0;

                        Boolean sepuede = Byte.TryParse(activarHosting.Extra, out valorServidor);
                        if (sepuede == true)
                        {
                            string urlVerifica = verificarHosting(valorServidor);
                            if (!string.IsNullOrEmpty(urlVerifica))
                            {
                                return Redirect(urlVerifica);
                            }
                        }
                    }
                }
                #endregion










                Int32 idRubroUbicacion = 0;

                if (!String.IsNullOrEmpty(retorno))
                {
                    //Por el momento hasta que se mejore el esquemita
                    DatoConfiguracion confUbicacionCliente = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Cliente_CFinal_Deposito_Defecto);

                    if (confUbicacionCliente?.Valor.Activo_Inactivo() == "Activo")
                    {
                        if (!String.IsNullOrEmpty(confUbicacionCliente.Extra))
                        {
                            String[] partes = confUbicacionCliente.Extra.Split("_");
                            //Verifiacar si hay mas de 1
                            if (partes[0] == retorno)
                            {
                                idRubroUbicacion = Convert.ToInt32(partes[1]);
                                _session.ListaRubroUbicacioId = new List<int>();
                                _session.ListaRubroUbicacioId.Add(idRubroUbicacion);
                            }
                        }

                        if (!String.IsNullOrEmpty(confUbicacionCliente.ExtraDos))
                        {
                            String[] partes = confUbicacionCliente.ExtraDos.Split("_");
                            //Verifiacar si hay mas de 1
                            if (partes[0] == retorno)
                            {
                                idRubroUbicacion = Convert.ToInt32(partes[1]);
                                _session.ListaRubroUbicacioId = new List<int>();
                                _session.ListaRubroUbicacioId.Add(idRubroUbicacion);
                            }
                        }
                    }


                    _session.UrlRetornoPaginaEmpresa = retorno;
                }



                _session.Sistema.Nombre = empresaViewModel.RazonSocial;
                _session.Sistema.Logo = empresaViewModel.Logo_Html;
                _session.Sistema.Correo = empresaViewModel.CorreoElectronico?.Trim();

                string correo = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 5)?.Extra;
                if (!String.IsNullOrEmpty(correo))
                {
                    _session.Sistema.Correo = correo;

                }


                //Este esquema eslo nuevo de momento se implementa en empresas que operan con 1 sector.
                if (_session.Sistema.SectorId == null)
                {
                    String rutaWS = @"https://wa.me/" + _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == 6)?.Extra;
                    _session.Sistema.WhatsappSector = rutaWS;
                }


                var tipoVisualizacion = _session?.Configuracion?.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == 16);



                if (tipoVisualizacion != null)
                {
                    _session.TipoVisualizacionProductos = Convert.ToInt32(tipoVisualizacion.Valor);
                }

                _session.GuardarSession(HttpContext);



                #region Ingreso Catalogo - Automatico

                if (HttpContext.Request.Query.ContainsKey("catalogo"))
                {
                    DatosUsuario datos = new DatosUsuario()
                    {
                        Correo = "catalogo@catalogo.com",
                        Clave = "catalogo"
                    };
                    return RedirectToAction("GotoCatalogo", "Acceso", datos);

                }

                #endregion


                String ofertas = HttpContext.Request.Query["Ofertas"];


                FiltroProducto filtro = new FiltroProducto();

                if (!String.IsNullOrEmpty(ofertas))
                {
                    filtro.Ofertas = true;
                }


                if (esquemaAgroAzul == true)
                {


                    if (idRubro > 0)
                    {
                        filtro.FiltroRubro = true;
                        filtro.FamiliaId = idRubro;

                        filtro.FiltroRaizFamilia = true;
                        filtro.Familia_Raiz_Id = _session.Familia_ModoRaiz.FamiliaId;
                    }


                    if (marca < 0)
                    {

                        filtro.MarcaId = marca;
                        filtro.FiltroMarca = true;

                        var marcas = repositorioProducto.GetMarca((Int32)filtro.MarcaId);
                        if (marcas != null)
                        {
                            filtro.NombreMarca = marcas.Nombre;
                        }
                        //18/02/2022
                        _session.Familia_ModoRaiz = null;


                    }

                    if (!String.IsNullOrEmpty(data))
                    {
                        filtro.Dato = data;
                    }

                    if (!String.IsNullOrEmpty(datoclasificacionId))
                    {
                        filtro.ClasificacionId = Convert.ToInt32(datoclasificacionId);
                    }

                    _session.GuardarSession(HttpContext);

                    return RedirectToAction("Productos", "Producto", filtro);
                }
                else
                {
                    if (idRubro > 0)
                    {
                        filtro.FiltroRubro = true;
                        filtro.FamiliaId = idRubro;

                        filtro.FiltroRaizFamilia = false;
                        filtro.Familia_Raiz_Id = null;
                    }
                    else if (idRubro < 0)
                    {

                        filtro.MarcaId = marca;
                        filtro.FiltroMarca = true;

                        var marcas = repositorioProducto.GetMarca((Int32)filtro.MarcaId);
                        if (marcas != null)
                        {
                            filtro.NombreMarca = marcas.Nombre;
                        }
                        //18/02/2022
                        _session.Familia_ModoRaiz = null;


                    }

                    if (!String.IsNullOrEmpty(datoBusqueda))
                    {

                        filtro.Dato = datoBusqueda;

                    }

                    if (!String.IsNullOrEmpty(datoclasificacionId))
                    {
                        filtro.ClasificacionId = Convert.ToInt32(datoclasificacionId);
                    }

                    _session.GuardarSession(HttpContext);
                    return RedirectToAction("Productos", "Producto", filtro);
                }




            }
            catch (Exception ex)
            {
                var routeValues = new RouteValueDictionary
                {
                    { "Id", -1 },
                    { "Mensaje", ex.Message }
                };

                String redireccionar = Url.Action("Notificaciones", "Home", routeValues);

                return Redirect(redireccionar);
            }

        }
    }
}
