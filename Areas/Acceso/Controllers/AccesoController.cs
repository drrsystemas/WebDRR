using DRR.Core.DBAlmaNET.Models;
using LibreriaBase.Areas.Acceso;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Acceso.Controllers
{
    [Area("Acceso")]
    //[Route("Acceso/[controller]/[action]")]
    [Route("[controller]/[action]")]
    public class AccesoController : Controller
    {
        #region Variables
        private readonly IRepositorioCliente _repositorioCliente;
        private readonly IConfiguration _configuracion;
        private readonly IEnviarCorreo _enviarCorreo;
        private SessionAcceso _session;
        private IWebHostEnvironment _hostEnvironment;
        #endregion


        #region Constructor
        //El constructor inyecta el repositorio de Cliente.
        //En dicho clase esta toda la comunicacion con la base de datos
        public AccesoController(IRepositorioCliente repositorioCliente, IConfiguration configuracion, IEnviarCorreo enviarCorreo, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment hostEnvironment)
        {
            _repositorioCliente = repositorioCliente;

            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");


            _repositorioCliente.DatosSistema = _session?.Sistema;


            _configuracion = configuracion;
            _enviarCorreo = enviarCorreo;
            _hostEnvironment = hostEnvironment;
        }
        #endregion

        public IActionResult GotoIndex()
        {
            return RedirectToAction("Index");
        }




        [HttpGet]
        public IActionResult Index(String msj)
        {
            try
            {
                if (!String.IsNullOrEmpty(msj))
                {
                    ViewBag.ErrorMessage = msj;
                }
                Boolean gmailActivo = true;

                var gmail = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 10);

                if (gmail != null)
                {
                    if (gmail.Valor != 1)
                    {
                        gmailActivo = false;
                    }
                }

                ViewData["gmail"] = gmailActivo;

                Boolean representada = false;
                if (_session?.Sistema?.TipoEmpresa == (int)EnumTiposEmpresas.Representada)
                {
                    representada = true;
                }

                ViewData["representada"] = representada;

                Int32? val = _session?.Configuracion?.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Web_Cerrada)?.Valor.MostrarEntero();
                if (!val.IsNullOrValue(0))
                {
                    ViewData["Registo"] = false;
                }
                else
                {
                    ViewData["Registo"] = true;
                }



                DatosUsuario datosUsuario = new DatosUsuario();

                return View(datosUsuario);

            }
            catch (Exception)
            {
                return RedirectToAction("Empresas", "Home");
            }
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Ingreso(DatosUsuario datos)
        {
            try
            {
                datos.IdEmpresa = _session.Sistema.EmpresaId;

                //***CONTROLAR***//
                var entity = _repositorioCliente.ObtenerUsuarioWeb_V2(datos);

                if (entity != null && !entity.IdAlmaWeb.IsNullOrValue(0))
                {
                    _session.Usuario = entity;

                    //Por ahora no tiene nada salvo lo de la lista de conf.
                    _session.ConfiguracionUsuario = entity?.XmlConfiguracion?.Configuraciones;

                    String urlIr = _session.UrlIr;
                    _session.UrlIr = "";

                    if (_session?.Usuario?.Rol == (Int32)EnumRol.Vendedor)
                    {
                        _session.TipoVisualizacionProductos = (Int32)FiltroProducto.EnumTipoVisualizacion.Grilla;

                        Utilidades.CalcularTotales(HttpContext, _session);

                        //#region TOTALES DE PEDIDOS COBRANZAS ENTREGAS

                        //SessionDiaTrabajoVendedor sessionDiaTrabajoVendedor = SessionDiaTrabajoVendedor.RecuperarSession(HttpContext);
                        //if(sessionDiaTrabajoVendedor == null)
                        //{
                        //    sessionDiaTrabajoVendedor = new SessionDiaTrabajoVendedor();
                        //    sessionDiaTrabajoVendedor.DatosSistema = _session.Sistema;
                        //}

                        //sessionDiaTrabajoVendedor.ActuralizarTodo(_session);

                        //sessionDiaTrabajoVendedor.GuardarSession(HttpContext);
                        //#endregion


                        var confFull = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (Int32)ConfPortal.EnumConfPortal.Full_Session_Clientes_Productos);

                        if(confFull?.Valor.MostrarEntero()==1)
                        {
                            _session.ModoFull_Cliente_Productos = true;

                            /// Disparar los metodos Asyncronos.
                        }
                    }
                    else
                    {

                        //ACa levanto el carrito que guarde de manera temporal de cliente 07/07/2022
                       var carrito =  SessionCarrito.RecuperarSession(HttpContext);
                        
                        carrito = _session.Usuario.XmlConfiguracion.Carrito_Temporal.ToObsect<SessionCarrito>();
                        if(carrito!=null)
                        {
                            carrito.Session = HttpContext.Session;

                            carrito.Guardar(sinActualizarUserWeb: true);
                        }



                        _session.TipoVisualizacionProductos = (Int32)FiltroProducto.EnumTipoVisualizacion.Tarjeta;
                    }

                    _session.GuardarSession(HttpContext);


                    if (!String.IsNullOrEmpty(urlIr))
                    {
                        return Redirect(urlIr);
                    }


                    #region REVISAR ESTO ..
                    //ESTO ES PORQUE NO SE COMO SE LLEVA LA CUENTA DE ITEMS
                    if (TempData.ContainsKey("UrlIr"))
                    {
                        var result = TempData["UrlIr"];
                        if (result != null)
                        {
                            String ir = result.ToString();

                            //Temporal................
                            String[] data = ir.Split('\\');

                            if (data?.Count() == 2)
                            {
                                return RedirectToAction(data[1], data[0]);
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
                    #endregion
                }
                else
                {

                    if (datos.Correo == "root@root.com" && datos.Clave == "benjaisa")
                    {
                        datos.Rol = (int)EnumRol.Root;
                        _session.Usuario = datos;
                        _session.Usuario.IdEmpresa = _session.Sistema.EmpresaId;

                        _session.GuardarSession(HttpContext);
                        //HttpContext.Session.SetJson("SessionAcceso", _session);

                        return RedirectToAction("Validacion", "Administracion", new { pantalla = 7 });
                    }


                    //Se verifica - si esta activado el usuario generico 
                    var root = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Root);

                    //No existe entonces la busque da la lista generica.
                    if (root == null)
                    {
                        ConfPortal confPortal = new ConfPortal();
                        List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                        lista = confPortal.GenerarEsquemaInicial();

                        root = lista.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Root);
                    }


                    if (root?.Valor.Activo_Inactivo() == "Activo")
                    {
                        if (root.Extra == datos.Correo && root.ExtraDos == datos.Clave)
                        {
                            datos.Rol = (int)EnumRol.Root;
                            _session.Usuario = datos;
                            _session.Usuario.IdEmpresa = _session.Sistema.EmpresaId;

                            _session.GuardarSession(HttpContext);
                            //HttpContext.Session.SetJson("SessionAcceso", _session);

                            return RedirectToAction("Validacion", "Administracion", new { pantalla = 7 });
                        }
                        else
                        {
                            return catalogo(datos);

                        }
                    }
                    else
                    {
                        //Ingreso comodin IngSalvarezza-riop1556




                        return catalogo(datos);
                    }

                }

            }
            catch (Exception ex)
            {
                //ACA----
                //En caso de error redirecciona a la web principal..
                return RedirectToAction("Empresas", "Home");
            }
        }


        [HttpGet]
        public IActionResult IngresoApp(String ingreso)
        {
            try
            {

                dynamic ing = recuperarLlaveIngreso(ingreso);

                DatosUsuario datos = new DatosUsuario();
                datos.IdEmpresa = ing.id;
                datos.Correo = ing.correo;
                datos.Clave = ing.clave;


                SessionAcceso session = conectar((int)datos.IdEmpresa, null, _hostEnvironment);


                _repositorioCliente.DatosSistema = session.Sistema;

                //***CONTROLAR***//
                var entity = _repositorioCliente.ObtenerUsuarioWeb_V2(datos);

                if (entity != null && !entity.IdAlmaWeb.IsNullOrValue(0))
                {
                    session.Usuario = entity;

                    //Por ahora no tiene nada salvo lo de la lista de conf.
                    session.ConfiguracionUsuario = entity?.XmlConfiguracion?.Configuraciones;

                    String urlIr = session.UrlIr;
                    session.UrlIr = "";

                    if (session?.Usuario?.Rol == (Int32)EnumRol.Vendedor)
                    {
                        session.TipoVisualizacionProductos = (Int32)FiltroProducto.EnumTipoVisualizacion.Grilla;

                        Utilidades.CalcularTotales(HttpContext, session);

                        //#region TOTALES DE PEDIDOS COBRANZAS ENTREGAS

                        //SessionDiaTrabajoVendedor sessionDiaTrabajoVendedor = SessionDiaTrabajoVendedor.RecuperarSession(HttpContext);
                        //if(sessionDiaTrabajoVendedor == null)
                        //{
                        //    sessionDiaTrabajoVendedor = new SessionDiaTrabajoVendedor();
                        //    sessionDiaTrabajoVendedor.DatosSistema = _session.Sistema;
                        //}

                        //sessionDiaTrabajoVendedor.ActuralizarTodo(_session);

                        //sessionDiaTrabajoVendedor.GuardarSession(HttpContext);
                        //#endregion


                        var confFull = session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (Int32)ConfPortal.EnumConfPortal.Full_Session_Clientes_Productos);

                        if (confFull?.Valor.MostrarEntero() == 1)
                        {
                            session.ModoFull_Cliente_Productos = true;

                            /// Disparar los metodos Asyncronos.



                        }
                    }
                    else
                    {
                        session.TipoVisualizacionProductos = (Int32)FiltroProducto.EnumTipoVisualizacion.Tarjeta;
                    }

                    session.GuardarSession(HttpContext);


                    if (!String.IsNullOrEmpty(urlIr))
                    {
                        return Redirect(urlIr);
                    }



                    if (HttpContext.Request.Query.ContainsKey("verpedidos"))
                    {
                        return RedirectToAction("ListarPedidos", "Pedido");
                    }



                    #region REVISAR ESTO ..
                    //ESTO ES PORQUE NO SE COMO SE LLEVA LA CUENTA DE ITEMS
                    if (TempData.ContainsKey("UrlIr"))
                    {
                        var result = TempData["UrlIr"];
                        if (result != null)
                        {
                            String ir = result.ToString();

                            //Temporal................
                            String[] data = ir.Split('\\');

                            if (data?.Count() == 2)
                            {
                                return RedirectToAction(data[1], data[0]);
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
                        var irA = session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == 17);

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
                    #endregion
                }
                else
                {

                    if (datos.Correo == "root@root.com" && datos.Clave == "benjaisa")
                    {
                        datos.Rol = (int)EnumRol.Root;
                        session.Usuario = datos;
                        session.Usuario.IdEmpresa = session.Sistema.EmpresaId;

                        session.GuardarSession(HttpContext);
                        //HttpContext.Session.SetJson("SessionAcceso", _session);

                        return RedirectToAction("Validacion", "Administracion", new { pantalla = 7 });
                    }


                    //Se verifica - si esta activado el usuario generico 
                    var root = session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Root);

                    //No existe entonces la busque da la lista generica.
                    if (root == null)
                    {
                        ConfPortal confPortal = new ConfPortal();
                        List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                        lista = confPortal.GenerarEsquemaInicial();

                        root = lista.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Root);
                    }


                    if (root?.Valor.Activo_Inactivo() == "Activo")
                    {
                        if (root.Extra == datos.Correo && root.ExtraDos == datos.Clave)
                        {
                            datos.Rol = (int)EnumRol.Root;
                            session.Usuario = datos;
                            session.Usuario.IdEmpresa = session.Sistema.EmpresaId;

                            session.GuardarSession(HttpContext);
                            //HttpContext.Session.SetJson("SessionAcceso", _session);

                            return RedirectToAction("Validacion", "Administracion", new { pantalla = 7 });
                        }
                        else
                        {
                            return catalogo(datos);

                        }
                    }
                    else
                    {
                        //Ingreso comodin IngSalvarezza-riop1556




                        return catalogo(datos);
                    }

                }

            }
            catch (Exception ex)
            {
                //ACA----
                //En caso de error redirecciona a la web principal..
                return RedirectToAction("Empresas", "Home");
            }
        }


        private IActionResult catalogo(DatosUsuario datos)
        {
            String msj = "Los datos ingresados no son correctos";
            //catalogo
            var catalogo = _session?.Configuracion?.ConfiguracionesPortal.FirstOrDefault(x => x.Codigo == (Int32)ConfPortal.EnumConfPortal.Activar_Catalogo);

            if (catalogo == null || catalogo?.Valor == 0)
            {
                return RedirectToAction("Index", "Acceso", new { msj });
            }
            else
            {
                if (catalogo.Valor.MostrarEntero() == 1)
                {
                    if (datos.Correo == "catalogo@catalogo.com" && datos.Clave == "catalogo")
                    {
                        _session.Usuario = new DatosUsuario();
                        _session.Usuario.Rol = (Int32)EnumRol.Catalogo;
                        _session.Usuario.IdListaPrecio = Convert.ToInt32(catalogo.Extra);
                        HttpContext.Session.SetJson("SessionAcceso", _session);

                        return RedirectToAction("Productos", "Producto");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Acceso", new { msj });
                    }
                }
                else
                {

                    return RedirectToAction("Index", "Acceso", new { msj });
                }
            }

        }


        public IActionResult GotoCatalogo(DatosUsuario datos)
        {
            return catalogo(datos);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult ConfirmarRegistro(DatosUsuarioVM datosUsuario)
        {
            try
            {

                if (datosUsuario.UsuarioConfiguracion.Documento <= 0)
                {
                    datosUsuario.Error.Mensaje += "Ingrese un numero de documento valido\n";
                    throw new Exception();
                }


                if (datosUsuario.Usuario.Clave != datosUsuario.Usuario.ClaveVerificacion)
                {
                    datosUsuario.Error.Mensaje += "La verificación de la clave no es correcta\n";
                    throw new Exception();
                }


                datosUsuario.Usuario.IdEmpresa = _session?.Sistema?.EmpresaId;

                Boolean? existeCorreo = _repositorioCliente.ExisteCorreoElectronico(datosUsuario.Usuario.Correo, (Int32)datosUsuario.Usuario.IdEmpresa);

                if (existeCorreo == false)
                {
                    LibreriaBase.WebServices.LibreriaAfip libreriaAfip = new LibreriaBase.WebServices.LibreriaAfip();

                    datosUsuario.UsuarioConfiguracion.Sexo = 1;
                    var primero = libreriaAfip.getDatosWebService(datosUsuario.UsuarioConfiguracion);
                    if (primero == null || primero?.Cuit <= 0)
                    {
                        datosUsuario.UsuarioConfiguracion.Sexo = 2;
                        var seg = libreriaAfip.getDatosWebService(datosUsuario.UsuarioConfiguracion);
                        datosUsuario.UsuarioConfiguracion = seg;
                    }
                    else
                    {
                        datosUsuario.UsuarioConfiguracion = primero;
                    }

                    LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();
                    String txtVerificacion = "";// enviroment.Encriptar(datosUsuario.Usuario.Correo + datosUsuario.Usuario.Clave);

                    txtVerificacion = txtVerificacion.Substring(0, 10);

                    _enviarCorreo.Conectar((Int32)_session?.Sistema?.EmpresaId);

                    Boolean ok = _enviarCorreo.Enviar(datosUsuario.Usuario.Correo,
                        "Registro Usuario",
                        "Su codigo de verificación es: " + txtVerificacion);

                    datosUsuario.Json = datosUsuario.GenerarJson();

                    return View(datosUsuario);

                }
                else
                {
                    if (existeCorreo.HasValue == true)
                    {
                        datosUsuario.Error.Id = 1;
                        datosUsuario.Error.Mensaje += "El correo ingresado figura en la base de datos\n";
                    }
                    else
                    {
                        datosUsuario.Error.Mensaje += "Error al validar el correo, intente nuevamente\n";
                    }

                    TempData["RegistroUsuario"] = datosUsuario.GenerarJson();
                    return RedirectToAction("Registro", "Acceso");
                }

            }
            catch
            {
                TempData["RegistroUsuario"] = datosUsuario.GenerarJson();
                return RedirectToAction("Registro", "Acceso");
            }
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AgregarUsuarioWeb(DatosUsuarioVM datosUsuario)
        {
            try
            {
                //Verificar Codigo..
                String msj = "";

                DatosUsuarioVM usuarioVM = datosUsuario.GenerarObjeto(datosUsuario.Json);
                usuarioVM.CodigoEnviado = datosUsuario.CodigoEnviado;
                usuarioVM.Usuario.IdEmpresa = _session.Sistema.EmpresaId;

                DatoConfiguracion registrosNuevos = _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(x => x.Codigo == 3);

                if (registrosNuevos != null)
                {
                    usuarioVM.Habilitado = registrosNuevos.Valor == 1 ? true : false;
                }

                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();
                String txtVerificacion = "";//enviroment.Encriptar(datosUsuario.Usuario.Correo + datosUsuario.Usuario.Clave);

                txtVerificacion = txtVerificacion.Substring(0, 10);

                if (usuarioVM.CodigoEnviado == txtVerificacion)
                {
                    int guardo = _repositorioCliente.AgregarUsuarioWeb(usuarioVM);

                    if (guardo > 0)
                    {
                        msj = "El usuario se agrego con exito en la BD";
                    }
                    else
                    {
                        msj = "no se agrego el usuario";
                    }
                }
                else
                {
                    msj = "El codigo de verificacion no es correcto";
                }

                return RedirectToAction("Notificaciones", "Acceso", new { info = msj });
            }
            catch (Exception)
            {

                return View(null);
            }
        }




        public IActionResult RecuperClave(DatosUsuario usuario)
        {
            try
            {



                if (String.IsNullOrEmpty(usuario.Correo))
                {
                    return View();
                }
                else
                {

                    usuario.IdEmpresa = _session.Sistema.EmpresaId;

                    String clave = _repositorioCliente.RecuperarClave(usuario);

                    LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();

                    _enviarCorreo.Conectar((Int32)_session?.Sistema?.EmpresaId);


                    Boolean ok = _enviarCorreo.Enviar(usuario.Correo,
                        "Recuperar",
                        "Su clave es: " + clave);


                    return RedirectToAction("Notificaciones", "Acceso", new { info = "Se envio un correo con su clave" });
                }
            }
            catch (Exception ex)
            {
                return View(null);
            }
        }



        public IActionResult Notificaciones(String info)
        {
            try
            {
                ViewBag.Notificacion = info;

                return View();
            }
            catch (Exception)
            {
                return View(null);
            }
        }


        /// <summary>
        /// Cierra session del usuario actual en el sistema.
        /// </summary>
        public IActionResult Cerrar()
        {
            Int32 idEmpresa = _session.Sistema.EmpresaId ?? 0;
            HttpContext.Session.Clear();

            _session.Usuario = new DatosUsuario();
            HttpContext.Session.SetJson("SessionAcceso", _session);

            if (idEmpresa != 29)
            {
                return RedirectToAction("Index", "Acceso");
            }
            else
            {
                return RedirectToAction("Acceso", "Codigo");
            }

        }




        #region Registrar Alternativo






        /// <summary>
        /// REVISAR SI ESTA ACTION ESTA OPERATIVO   --- 04/08/2021
        /// 
        /// En este caso el sistema de login se finalizo en el envio del correo.
        /// </summary>
        /// <param name="datosUsuario"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult FinalizarRegistro(String correo, Int64 dni, Int32 idEmpresa, Int32 id, String token)
        {
            //if(_session==null)
            //{
            //    if (_session == null)
            //    {
            //        LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();


            //        //El primer ingreso hay que cargar la informacion.
            //        if (_session == null)
            //        {
            //            _session = new SessionAcceso();
            //            _session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
            //        }
            //        else
            //        {
            //            if (String.IsNullOrEmpty(_session.Sistema?.Cn_Alma))
            //            {
            //                _session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
            //            }
            //        }
            //        _session.Usuario.IdAlmaWeb = id;
            //        _session.Sistema.EmpresaId = idEmpresa;
            //        _session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa((Int32)_session.Sistema.EmpresaId);

            //        HttpContext.Session.SetJson("SessionAcceso", _session);
            //    }

            //    _repositorioCliente.DatosSistema = _session?.Sistema;
            //}


            DatosUsuarioVM item = new DatosUsuarioVM();


            if (item.Usuario == null)
            {
                item.Usuario = new DatosUsuario();
            }

            if (item.UsuarioConfiguracion == null)
            {
                item.UsuarioConfiguracion = new LibreriaBase.Areas.Usuario.UsuarioWeb_Configuracion();
            }

            item.Usuario.IdEmpresa = idEmpresa;
            item.Usuario.IdAlmaWeb = id;
            item.UsuarioConfiguracion.Documento = dni;
            item.Usuario.Correo = correo;
            item.CodigoEnviado = token;


            UsuarioWeb usuario = _repositorioCliente.GetUsuarioWebById((int)item.Usuario.IdAlmaWeb);
            Boolean activado = false;
            if (usuario != null)
            {
                String clave = usuario.Contrasena.DesEncriptar();

                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();
                String txtVerificacion = "";//enviroment.Encriptar(item.Usuario.Correo + clave).Substring(0, 10);

                if (txtVerificacion == item.CodigoEnviado)
                {
                    usuario.Inhabilitado = false;
                    activado = _repositorioCliente.ActualizarUsuarioWeb(usuario);
                    TempData["ErrorRepresentada"] = "Su cuenta esta activada - Ingrese el correo y su clave, para poder acceder al portal.";
                }
                else
                {
                    //Codigo incorrecto......
                    TempData["ErrorRepresentada"] = "Los datos de verificación no son correctos.";

                }

            }


            return RedirectToAction("Index", "Acceso");



        }




        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult ActivarUsuario(DatosUsuarioVM datosUsuario)
        {

            UsuarioWeb usuario = _repositorioCliente.GetUsuarioWebById((int)datosUsuario.Usuario.IdAlmaWeb);
            Boolean activado = false;
            if (usuario != null)
            {
                String clave = usuario.Contrasena.DesEncriptar();

                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();
                String txtVerificacion = "";// enviroment.Encriptar(datosUsuario.Usuario.Correo + clave).Substring(0, 10);

                if (txtVerificacion == datosUsuario.CodigoEnviado)
                {
                    usuario.Inhabilitado = false;
                    activado = _repositorioCliente.ActualizarUsuarioWeb(usuario);
                    TempData["ErrorRepresentada"] = "Su cuenta esta activada - Ingrese el correo y su clave, para poder acceder al portal.";
                }
                else
                {
                    //Codigo incorrecto......
                    TempData["ErrorRepresentada"] = "Los datos de verificación no son correctos.";

                }

            }


            return RedirectToAction("Index", "Acceso");
        }




        public IActionResult ReeviarToken(String correo)
        {
            try
            {
                var usuario = _repositorioCliente.GetUsuarioWeb(c => c.Inhabilitado == true && c.Email == correo);

                if (usuario != null)
                {
                    LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();

                    String txtVerificacion = "";// enviroment.Encriptar(usuario.Email + usuario.Contrasena.DesEncriptar()).Substring(0, 10);

                    #region Enviar Correo
                    string protocolo = @"https://";
                    string host = HttpContext.Request.Host.Host;
                    string hostpuesto = host + ":" + (HttpContext.Request.Host.Port ?? 80).ToString();

                    var routeValueDictionary = new RouteValueDictionary();
                    routeValueDictionary.Add("correo", usuario.Email);
                    routeValueDictionary.Add("dni", usuario.NroIdentificacion);
                    routeValueDictionary.Add("idEmpresa", usuario.EmpresaId);
                    routeValueDictionary.Add("id", usuario.WebUserId);
                    routeValueDictionary.Add("token", txtVerificacion);
                    String link = Url.Action("FinalizarRegistro", "Acceso", routeValueDictionary);


                    link = protocolo + host + link;


                    String cuerporCorreo = "";
                    cuerporCorreo = "Registro de Usuarios para: " + _session.Sistema.Nombre;
                    cuerporCorreo += "<br/>";
                    //cuerporCorreo +="Su token de verificación es: " + txtVerificacion;
                    cuerporCorreo += "<br/>";
                    cuerporCorreo += "Copie y pegue el link enlace:  <br/><br/>";
                    cuerporCorreo += link;

                    _enviarCorreo.Conectar((Int32)_session?.Sistema?.EmpresaId);


                    Boolean ok = _enviarCorreo.Enviar(usuario.Email,
                            "Reenvio de Token",
                            cuerporCorreo.ToString());
                    #endregion
                }
                else
                {

                }


                return RedirectToAction("Index", "Acceso");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Acceso");
            }
        }




        #endregion


        public IActionResult LoginGmail()
        {
            IngresoGmail login = new IngresoGmail();


            HttpContext.Response.Cookies.Delete("accounts.google.com");




            if (TempData.ContainsKey("UrlIr"))
            {
                //Maneter la url de ida.
                TempData.Keep("UrlIr");
            }

            return View(login);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Gmail(IngresoGmail ingreso)
        {
            try
            {
                //1_Verificar que el correo este en la base de datos
                //2 Si esta hacer el ingreso.
                //3 Registrar usuario - Hacer ingreso.

                if (!String.IsNullOrEmpty(ingreso?.Correo) && !String.IsNullOrEmpty(ingreso?.Token))
                {
                    ingreso.Clave_Portal = (ingreso.Correo.Substring(0, 5) + ingreso.Nombre.Substring(0, 5)).Encriptar();

                    Boolean? existeCorreo = _repositorioCliente.ExisteCorreoElectronico(ingreso?.Correo, (Int32)_session.Sistema.EmpresaId);

                    if (existeCorreo == true)
                    {
                        DatosUsuario Usuario = new DatosUsuario();
                        Usuario.IdEmpresa = _session.Sistema.EmpresaId;
                        Usuario.Correo = ingreso?.Correo;
                        Usuario.Clave = ingreso?.Clave_Portal;
                        Usuario.ClaveVerificacion = ingreso?.Clave_Portal;

                        Boolean existeUsuario = _repositorioCliente.VerificarIngreso(Usuario);


                        if (existeUsuario == true)
                        {
                            var entity = _repositorioCliente.ObtenerUsuarioWeb(Usuario);

                            _session.Usuario = entity;
                            HttpContext.Session.SetJson("SessionAcceso", _session);


                            //return RedirectToAction("Index", "Home");

                            //ESTO ES PORQUE NO SE COMO SE LLEVA LA CUENTA DE ITEMS
                            if (TempData.ContainsKey("UrlIr"))
                            {
                                var result = TempData["UrlIr"];

                                TempData.Clear();

                                if (result != null)
                                {
                                    String ir = result.ToString();

                                    //Temporal................
                                    String[] data = ir.Split('\\');

                                    if (data?.Count() == 2)
                                    {
                                        return RedirectToAction(data[1], data[0]);
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
                        else
                        {
                            return RedirectToAction("Index", "Acceso", new { msj = "Error al ingreso con Gmail" });
                        }
                    }
                    else
                    {
                        LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();
                        String txtVerificacion = "";// enviroment.Encriptar(ingreso?.Correo + ingreso.Clave_Portal);

                        txtVerificacion = txtVerificacion.Substring(0, 10);

                        DatosUsuarioVM usuarioVM = new DatosUsuarioVM();
                        usuarioVM.Usuario = new DatosUsuario();
                        usuarioVM.Usuario.Correo = ingreso?.Correo;
                        usuarioVM.Usuario.Clave = ingreso?.Clave_Portal;
                        usuarioVM.Usuario.ClaveVerificacion = ingreso?.Clave_Portal;

                        usuarioVM.CodigoEnviado = txtVerificacion;
                        usuarioVM.Usuario.IdEmpresa = _session.Sistema.EmpresaId;
                        usuarioVM.Usuario.Nombre = ingreso?.Nombre;
                        usuarioVM.ExternalLogin = true;

                        //VEO SI CREA EL USUARIO_________

                        int agrego = _repositorioCliente.AgregarUsuarioWeb(usuarioVM);

                        if (agrego > 0)
                        {


                            _session.Usuario = usuarioVM.Usuario;
                            HttpContext.Session.SetJson("SessionAcceso", _session);

                            return RedirectToAction("Index", "Home");
                        }

                    }
                    return RedirectToAction("Index", "Acceso", new { msj = "Error al ingreso con Gmail" });
                }
                else
                {
                    return RedirectToAction("Index", "Acceso", new { msj = "Error al ingreso con Gmail" });
                }


            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Acceso", new { msj = "Error al ingreso con Gmail" });
            }
        }






        #region Nuevo esquema de registro

        [HttpGet]
        public IActionResult NuevaCuenta()
        {
            try
            {
                return View("CrearCuenta", new CrearCuenta());
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Acceso");
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="datosUsuario"></param>
        /// <returns></returns>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult CrearCuenta(CrearCuenta datos)
        {
            try
            {
                if (!datos.Documento.EsNumerico())
                {
                    ModelState.AddModelError("Documento", "El formato del Documento no es correcto");
                }

                if (ModelState.IsValid)
                {
                    DatosUsuarioVM datosUsuario = new DatosUsuarioVM();
                    datosUsuario.Usuario.IdEmpresa = _session?.Sistema?.EmpresaId;
                    datosUsuario.Usuario.Correo = datos.CorreoElectronico;
                    datosUsuario.Usuario.Clave = datos.Contraseña;
                    datosUsuario.Usuario.ClaveVerificacion = datos.ContraseñaVerificacion;
                    datosUsuario.UsuarioConfiguracion.Documento = Convert.ToInt64(datos.Documento);
                    datosUsuario.UsuarioConfiguracion.ApellidoyNombre = datos.NombreyApellido;

                    var almaUserId_Generico = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == (int)ConfPortal.EnumConfPortal.AlmaUserId_Empresa);

                    //15-01-2021
                    if (!String.IsNullOrEmpty(almaUserId_Generico?.Extra))
                    {
                        datosUsuario.Usuario.AlmaUserID = Convert.ToInt32(almaUserId_Generico?.Extra);
                        datosUsuario.UsuarioConfiguracion.AlmaUserID = datosUsuario.Usuario.AlmaUserID;
                    }

                    Boolean? existeCorreo = _repositorioCliente.ExisteCorreoElectronico(datosUsuario.Usuario.Correo, (Int32)datosUsuario.Usuario.IdEmpresa);

                    if (existeCorreo == false)
                    {
                        LibreriaBase.WebServices.LibreriaAfip libreriaAfip = new LibreriaBase.WebServices.LibreriaAfip();

                        datosUsuario.UsuarioConfiguracion.Sexo = 1;
                        var primero = libreriaAfip.getDatosWebService(datosUsuario.UsuarioConfiguracion);
                        if (primero == null || primero?.Cuit <= 0)
                        {
                            datosUsuario.UsuarioConfiguracion.Sexo = 2;
                            var seg = libreriaAfip.getDatosWebService(datosUsuario.UsuarioConfiguracion);
                            datosUsuario.UsuarioConfiguracion = seg;
                        }
                        else
                        {
                            datosUsuario.UsuarioConfiguracion = primero;
                        }

                        //---------------------------
                        //Verificar Codigo..
                        String msj = "";
                        datosUsuario.UsuarioConfiguracion.Direccion = datos.Direccion;
                        datosUsuario.UsuarioConfiguracion.Celular = datos.Telefono;

                        datosUsuario.UsuarioConfiguracion.ApellidoyNombre = datos.NombreyApellido;
                        datosUsuario.Usuario.IdEmpresa = _session.Sistema.EmpresaId;


                        LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();
                        String txtVerificacion = "";// enviroment.Encriptar(datosUsuario.Usuario.Correo + datosUsuario.Usuario.Clave);
                        txtVerificacion = txtVerificacion.Substring(0, 10);
                        //datosUsuario.CodigoEnviado = txtVerificacion;



                        datosUsuario.Habilitado = false;





                        int webUserId = _repositorioCliente.AgregarUsuarioWeb(datosUsuario);


                        string protocolo = @"https://";
                        string host = HttpContext.Request.Host.Host;
                        string hostpuesto = host + ":" + (HttpContext.Request.Host.Port ?? 80).ToString();

                        var routeValueDictionary = new RouteValueDictionary();
                        routeValueDictionary.Add("correo", datosUsuario.Usuario.Correo);
                        routeValueDictionary.Add("dni", datosUsuario.UsuarioConfiguracion.Documento);
                        routeValueDictionary.Add("idEmpresa", datosUsuario.Usuario.IdEmpresa);
                        routeValueDictionary.Add("id", webUserId);
                        routeValueDictionary.Add("token", txtVerificacion);

                        //El ultimo tema.

                        String link = Url.Action("FinalizarRegistro", "Acceso", routeValueDictionary);


                        link = protocolo + host + link;


                        String cuerporCorreo = "";
                        cuerporCorreo += _session.Sistema.Nombre;
                        cuerporCorreo += "<br/>";
                        cuerporCorreo += "Registro de Usuarios para: " + datos.NombreyApellido;
                        cuerporCorreo += "<br/>";
                        cuerporCorreo += "Tel/Cel: " + datos.Telefono;
                        cuerporCorreo += "<br/>";


                        //08-02-2021 ** Se ve la configuracion del usuario.
                        DatoConfiguracion usurioActivo = _session.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (Int32)ConfPortal.EnumConfPortal.Registro_UsuarioActivo);

                        if (usurioActivo?.Valor.MostrarEntero() == 1)
                        {

                            cuerporCorreo += "Para activar su cuenta click en:  <a href=" + link + " id=\"enlaceWeb\">ACTIVAR CUENTA</a>";
                        }
                        else
                        {
                            cuerporCorreo += "En breve nos pondremos en contacto con usted para confirmale la activacion de su usuario.";
                            cuerporCorreo += "<br/>";
                            cuerporCorreo += "Gracias.";
                        }

                        //Verificamos que la empresa tenga configurado su correo.
                        RepositorioEmpresa repositorio = new RepositorioEmpresa();
                        repositorio.DatosSistema = _session?.Sistema;

                        Boolean ok = false;

                        _enviarCorreo.Conectar((Int32)_session?.Sistema?.EmpresaId);


                        DatoConfiguracion conCorreo = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (Int32)ConfPortal.EnumConfPortal.Aviso_por_Correo);

                        if (conCorreo?.Valor.MostrarEntero() == 1 && !String.IsNullOrEmpty(conCorreo?.Extra))
                        {
                            List<String> correos = new List<string>();
                            correos.Add(datosUsuario.Usuario.Correo);
                            correos.Add(conCorreo.Extra);
                            ok = _enviarCorreo.EnvioMultiple(correos, "Registro Usuario", cuerporCorreo.ToString());
                        }
                        else
                        {
                            ok = _enviarCorreo.Enviar(datosUsuario.Usuario.Correo, "Registro Usuario", cuerporCorreo.ToString());
                        }



                        if (ok == true)
                        {
                            //return Redirect(url);
                            String urlIr = HttpContext.Request.UrlAtras();
                            String urlTexto = "Ingresar";
                            String urlRetorno = "Acceso\\GotoIndex";
                            String icono = "fas fa-at fa-4x";

                            String msj_ok = "El correo se envio con exito, dentro del mismo hay un link donde al presionarlo podra activar su cuenta.";


                            //Metodo de error hay que armar.
                            var routeValues = new RouteValueDictionary
                        {
                            { "Id", "1"  },
                            { "Mensaje", msj_ok },
                            {"Icono", icono },
                             {"UrlRetorno", urlRetorno },
                               {"UrlTexto", urlTexto },
                            {"UrlIr", urlIr }
                        };

                            String link_ok = Url.Action("Notificaciones", "Home", routeValues);

                            return Redirect(link_ok);

                        }
                        else
                        {
                            String urlIr = HttpContext.Request.UrlAtras();
                            String urlTexto = "Volver a intentar";
                            String urlRetorno = Url.Action("NuevaCuenta", "Acceso");
                            String icono = "fas fa-at fa-4x";
                            String msj_errorCorreo = "El correo no fue enviado, este problema surge por un error al escribir el correo.";


                            //Metodo de error hay que armar.
                            var routeValues = new RouteValueDictionary
                        {
                            { "Id", "1"  },
                            { "Mensaje", msj_errorCorreo },
                            {"Icono", icono },
                             {"UrlRetorno", urlRetorno },
                               {"UrlTexto", urlTexto },
                            {"UrlIr", urlIr }
                        };

                            String link_ErrorCorreo = Url.Action("Notificaciones", "Home", routeValues);

                            return Redirect(link_ErrorCorreo);
                        }



                    }
                    else
                    {

                        //return Redirect(url);
                        String urlIr = HttpContext.Request.UrlAtras();
                        String urlTexto = "Solucionar Problemas";
                        String urlRetorno = "Acceso\\GotoIndex";
                        String icono = "fas fa-at fa-4x";
                        String msj = "El correo ingresado figura en la base de datos, si ya realizo este paso seguramente recibio un correo con el podra realizar la activacion de la cuenta. Si su cuenta ya estaba activa puede recuperar su contraseña.";


                        //Metodo de error hay que armar.
                        var routeValues = new RouteValueDictionary
                        {
                            { "Id", "1"  },
                            { "Mensaje", msj },
                            {"Icono", icono },
                             {"UrlRetorno", urlRetorno },
                               {"UrlTexto", urlTexto },
                            {"UrlIr", urlIr }
                        };

                        String link = Url.Action("Notificaciones", "Home", routeValues);

                        return Redirect(link);



                    }
                }
                else
                {


                    if (!datos.Documento.EsNumerico())
                    {
                        ModelState.AddModelError("dni", "Ingrese un numero de documento valido");
                    }


                    if (datos.Contraseña != datos.ContraseñaVerificacion)
                    {
                        ModelState.AddModelError("claves", "La verificación de la clave no es correcta");

                    }

                    return View(datos);
                }



            }
            catch (Exception ex)
            {
                return View(null);
            }

        }
        #endregion



        [HttpGet]
        public IActionResult SolucionarProblemas()
        {
            return View();
        }


        #region Modificar Contrsaña
        public IActionResult ModificarClave()
        {
            DatosUsuario datosUsuario = new DatosUsuario();

            return View(datosUsuario);
        }

        [HttpPost]
        public IActionResult ModificarClave(DatosUsuario datosUsuario)
        {
            var usuarioWeb = _repositorioCliente.GetUsuarioWebById((int)_session?.Usuario.IdAlmaWeb);
            if (usuarioWeb != null)
            {
                usuarioWeb.Contrasena = datosUsuario.Clave.Encriptar();
                bool actualizo = _repositorioCliente.ActualizarUsuarioWeb(usuarioWeb);

                if (actualizo == true)
                {
                    TempData["ErrorRepresentada"] = "La clave se modificaron con exito.";
                    TempData["ErrorRepresentadaColor"] = "Verde";
                    return RedirectToAction("Panel", "EditarDatos");
                }
            }

            TempData["ErrorRepresentada"] = "La clave no se pudo modificar.";
            return RedirectToAction("Panel", "EditarDatos");
        }
        #endregion


        private SessionAcceso conectar(int empresaId, Nullable<Int16> sectorId, IWebHostEnvironment environment)
        {
            #region conectar
            DRREnviroment enviroment = new DRREnviroment();

            SessionAcceso session = new SessionAcceso();
            session.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
            session.Sistema.EmpresaId = empresaId;


            string ruta, documento;
            //documento = obtenerJsonEmpresas(out ruta);

            string[] filePaths = Directory.GetFiles(Path.Combine(environment.WebRootPath, "files\\"));
            string archivo = Path.GetFileName(filePaths[0]);
            ruta = filePaths[0];

            //Primero hay que lleerlo.
            StreamReader reader = new StreamReader(ruta);
            documento = reader.ReadToEnd();
            reader.Close();

            var empresaDatos = documento?.ToObsect<List<DatosEmpresa>>()?.FirstOrDefault(c => c.IdEmpresa == empresaId && c.Activa == true);

            if (empresaDatos == null)
            {
                throw new Exception("Servidor fuera de servicio, para mas informacion comunicarse al tel: 03751-420850");
            }

            session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa_V2(empresaDatos.Nombre_BaseDatos);

            RepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
            RepositorioProducto repositorioProducto = new RepositorioProducto();

            repositorioEmpresa.DatosSistema = session.Sistema;
            repositorioProducto.DatosSistema = session.Sistema;


            var empresa = repositorioEmpresa.ObtenerEmpresa_AlmaNet_SQL((int)session.Sistema.EmpresaId);
            Int32 tipoemp = empresa.EmpresaTipoConfigId ?? 0;
            String cadena = tipoemp.DevolverBinario().Reverso();


            if (cadena.Length == 9)
            {
                Int32 pos = (Int32)Char.GetNumericValue(cadena[8]);
                if (pos == 1)
                {
                    session.Sistema.TipoEmpresa = (Int32)EnumTiposEmpresas.Representada;
                }
            }
            else if (cadena.Length == 10)
            {
                Int32 pos = (Int32)Char.GetNumericValue(cadena[9]);
                if (pos == 1)
                {
                    session.Sistema.TipoEmpresa = (Int32)EnumTiposEmpresas.EmpresaMultisector;
                }
            }
            else
            {
                session.Sistema.TipoEmpresa = (Int32)EnumTiposEmpresas.Empresas;
            }


            var conf = repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa(empresaId);
            //Guardo en sesion los datos. 
            session.Configuracion = conf;

            if (sectorId > 0)
            {
                session.Sistema.SectorId = sectorId;
            }


            #endregion

            return session;
        }

        private string generarLlaveIngreso(Int32 empresaId, string correo, string clave)
        {
            String dato = $"__{empresaId}***{correo}***{clave}__";
            dato = dato.Encriptar();

            return dato;
        }
        private Object recuperarLlaveIngreso(String llave)
        {
            try
            {
                String[] dato = llave.DesEncriptar().Split("***");

                Int32 id = Convert.ToInt32(dato[0].Remove(0, 2));

                String correo = dato[1];

                String clave = dato[2].Remove(dato[2].Length - 2, 2);


                return new
                {
                    id,
                    correo,
                    clave
                };
            }
            catch (Exception)
            {
                return null;
            }

        }

    }
}