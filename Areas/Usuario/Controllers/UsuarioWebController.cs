using DRR.Core.DBAlmaNET.Models;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Usuario.Controllers
{
    [Area("Usuario")]
    [Route("[controller]/[action]")]
    public class UsuarioWebController : Controller
    {
        #region Variables
        private SessionAcceso _session;
        IRepositorioUsuarioWeb _repositorioUsuarioWeb;
        #endregion


        #region Constructor
        public UsuarioWebController(IRepositorioUsuarioWeb repositorioUsuarioWeb, IHttpContextAccessor httpContextAccessor)
        {
            //Se obtiene el 
            _repositorioUsuarioWeb = repositorioUsuarioWeb;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            _repositorioUsuarioWeb.DatosSistema = _session?.Sistema;

            _repositorioUsuarioWeb.ElementosPorPagina = 24;



        }
        #endregion




        // GET: UsuarioWebController
        public ActionResult UsuariosWeb()
        {
            //Testing para los que es el root
            ViewData["Rol"] = (byte)_session?.Usuario?.Rol;



            FiltroWebUserId filtro = new FiltroWebUserId();
            filtro.EmpresaId = (int)_session.Sistema.EmpresaId;

            var query = _repositorioUsuarioWeb.Lista(filtro);
            return View(query);
        }



        // POST: UsuarioWebController/Create
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UsuarioWebController/Edit/5
        public ActionResult Crud(int webUserId, int tipoOperacion)
        {

            ViewData["Rol"] = (byte)_session?.Usuario?.Rol;

            UsuarioWeb usuario = null;

            if (webUserId > 0)
            {
                usuario = _repositorioUsuarioWeb.GetUsuarioWeb(webUserId);
            }
            else
            {
                //Para mas control
                if (tipoOperacion == (Int32)EnumTipoOperacion.Agregar)
                {
                    usuario = new UsuarioWeb();
                }
            }



            if (tipoOperacion == 10)
            {
                ViewData["TipoOperacion"] = 1;
                ViewData["Representada"] = _session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada ? true : false;

                if (TempData.ContainsKey("usuarioweb"))
                {
                    string usuariow = TempData["usuarioweb"].ToString();
                    if (!String.IsNullOrEmpty(usuariow))
                    {
                        usuario = usuariow.ToObsect<UsuarioWeb>();
                    }
                    TempData.Remove("usuarioweb");
                }

                return View(usuario);
            }
            else
            {
                ViewData["TipoOperacion"] = tipoOperacion;
                ViewData["Representada"] = _session.Sistema.TipoEmpresa == (int)EnumTiposEmpresas.Representada ? true : false;
                return View(usuario);
            }


        }


        /// <summary>
        /// Se modifica el: 15/06/2022
        /// </summary>
        /// <param name="webUserId"></param>
        /// <param name="entidad"></param>
        /// <returns></returns>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Edit(int webUserId, UsuarioWeb entidad)
        {
            try
            {
                RepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = _session.Sistema;

                //Se pasa la entidad asi laburo menos.

                //Tengo el formulario 
                IFormCollection collection = HttpContext.Request.Form;

                int tipoOperacion = Convert.ToInt32(collection["tipoOperacion"]);

                #region AGREGAR
                if (tipoOperacion == (int)EnumTipoOperacion.Agregar)
                {

                    #region Verifico que el correo Electronico ya no exista

                    Boolean? existeCorreo = repositorioCliente.ExisteCorreoElectronico(entidad.Email, (Int32)_session.Sistema.EmpresaId);

                    #endregion

                    if (existeCorreo == true)
                    {
                        entidad.P_Error = "El correo electronico ya existe en la base de datos";
                        TempData["usuarioweb"] = entidad.ToJson();
                        //return View("~/Areas/Usuario/Views/UsuarioWeb/Crub.cshtml", entidad);
                        RouteValueDictionary parametro = new RouteValueDictionary();
                        parametro.Add("webUserId", 0);
                        parametro.Add("tipoOperacion", 10);
                        return RedirectToAction("Crud", "UsuarioWeb", parametro);
                    }
                    else
                    {

                        //entity.WebUserId = GenerarIdentificador_UsuarioWeb();
                        //entity.almauserID
                        String datosIdSuc = collection["entidadSucId"].ToString();
                        if (!String.IsNullOrEmpty(datosIdSuc))
                        {
                            string[] datosIdSucPartes = datosIdSuc.Split("-");

                            Int32 idSucursal = Convert.ToInt32(datosIdSucPartes[0]);
                            String nombre = datosIdSucPartes[1].ToString();
                            Int64 cuit = 0;
                            try
                            {
                                 cuit = Convert.ToInt64(datosIdSucPartes[2]);
                            }
                            catch (Exception)
                            {

                            }
                            

                            if (idSucursal > 0)
                            {
                                entidad.EntidadSucId = idSucursal;
                            }
                            if (cuit > 0)
                            {
                                entidad.NroIdentificacion = cuit;
                            }
                        }


                        #region Rol del usuario -- es bastante importante 

                        byte tipoUsuario = Convert.ToByte(collection["configuracion.TipoUsuario"]);
                        if (tipoUsuario == 2)
                        {
                            entidad.TipoAccesoId = (byte?)(tipoUsuario - 1);
                        }
                        else
                        {
                            entidad.TipoAccesoId = tipoUsuario;
                        }
                        #endregion

                        UsuarioWeb_Configuracion usuarioWeb_Configuracion = new UsuarioWeb_Configuracion();
                        if (tipoUsuario == (Int32)EnumRol.Cliente || tipoUsuario == (Int32)EnumRol.Vendedor )
                        {
                            //Obtenemos todos los datos del formulario.
                            usuarioWeb_Configuracion.ApellidoyNombre = collection["configuracion.ApellidoyNombre"];
                            usuarioWeb_Configuracion.Celular = collection["configuracion.Celular"];

                            Int32 cp = 0;
                            Boolean sepudoCast = Int32.TryParse(collection["configuracion.CodigoPostal"], out cp);
                            if (sepudoCast == true)
                            {
                                usuarioWeb_Configuracion.CodigoPostal = cp;
                            }

                            usuarioWeb_Configuracion.Localidad = collection["configuracion.Localidad"];
                            usuarioWeb_Configuracion.Direccion = collection["configuracion.Direccion"];
                            var fechaN = collection["configuracion.FechaNacimiento"];
                            var fechaR = collection["configuracion.FechaRegistro"];


                            String data = collection["configuracion.VendedorVisualizaTodosLosCliente"];
                            if (!String.IsNullOrEmpty(data))
                            {
                                if (data.Equals("false"))
                                {
                                    usuarioWeb_Configuracion.VendedorVisualizaTodosLosCliente = false;
                                }
                                else
                                {
                                    usuarioWeb_Configuracion.VendedorVisualizaTodosLosCliente = true;
                                }
                            }

                        }


                        #region AlmaUserID - generico
                        if (entidad.AlmaUserId == null || entidad.AlmaUserId == 0)
                        {
                            DatoConfiguracion conf_almaUserId_Generico = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(x => x.Codigo == (int)ConfPortal.EnumConfPortal.AlmaUserId_Empresa);

                            //15-01-2021
                            if (!String.IsNullOrEmpty(conf_almaUserId_Generico?.Extra))
                            {
                                entidad.AlmaUserId = Convert.ToInt32(conf_almaUserId_Generico?.Extra);
                            }
                        }
                        #endregion




                        //Tipo entidad id maneja juan y yo manejo el xml y tipo acceso.
                        entidad.TipoEntidadId = entidad.TipoAccesoId;

                        entidad.EmpresaId = (int)_session.Sistema.EmpresaId;
                        entidad.Contrasena = entidad.Contrasena.Encriptar();


                        usuarioWeb_Configuracion.Documento = entidad.NroIdentificacion;
                        usuarioWeb_Configuracion.AlmaUserID = entidad.AlmaUserId;
                        usuarioWeb_Configuracion.TipoUsuario = (short)tipoUsuario;





                        //Generar el XML -

                        if (usuarioWeb_Configuracion.FechaRegistro == null)
                        {
                            usuarioWeb_Configuracion.FechaRegistro = DateTime.Now;
                        }

                        entidad.Configuracion = usuarioWeb_Configuracion.GetXML();


                        Int32 cambios = repositorioCliente.AgregarUsuarioWeb(entidad);


                        return RedirectToAction("UsuariosWeb", "UsuarioWeb");
                    }



                }
                #endregion

                #region MODIFICAR - 07/12/2021
                else if (tipoOperacion == (int)EnumTipoOperacion.Modificar)
                {

                    UsuarioWeb_Configuracion usuarioWeb_Configuracion = entidad.Configuracion.GetObjectOfXml<UsuarioWeb_Configuracion>();


                    byte tipoUsuario = Convert.ToByte(collection["configuracion.TipoUsuario"]);

                    if (tipoUsuario == (int)EnumRol.Vendedor || tipoUsuario == (int)EnumRol.ClienteFidelizado)
                    {

                        entidad.TipoAccesoId = tipoUsuario;
                        entidad.TipoEntidadId = entidad.TipoAccesoId;


                        Int32 vendedorClienteID = 0;

                        if (collection.ContainsKey("idClienteVendedor"))
                        {
                            try
                            {
                                vendedorClienteID = Convert.ToInt32(collection["idClienteVendedor"]);
                            }
                            catch (Exception ex)
                            {
                                vendedorClienteID = 0;
                            }

                        }

                        if (vendedorClienteID > 0)
                        {
                            usuarioWeb_Configuracion.Cliente_Vendedor_Id = vendedorClienteID;
                        }


                        String datosIdSuc = collection["entidadSucId"].ToString();


                        if (!String.IsNullOrEmpty(datosIdSuc))
                        {
                            string[] datosIdSucPartes = datosIdSuc.Split("-");


                            Int32 idSucursal = Convert.ToInt32(datosIdSucPartes[0]);

                            String nombre = datosIdSucPartes[1].ToString();
                            Int64 cuit = Convert.ToInt64(datosIdSucPartes[2]);

                            if (idSucursal > 0)
                            {
                                entidad.EntidadSucId = idSucursal;
                                usuarioWeb_Configuracion.EntidadSucID = idSucursal;
                            }

                            entidad.NroIdentificacion = cuit;


                        }

                        //Control de seguridad....
                        if (!String.IsNullOrEmpty(entidad.Email))
                        {
                            entidad.AlmaUserId = null;
                        }
                        else
                        {
                            usuarioWeb_Configuracion.AlmaUserID = entidad.AlmaUserId;
                        }



                        String data = collection["configuracion.VendedorVisualizaTodosLosCliente"];
                        if (!String.IsNullOrEmpty(data))
                        {
                            if (data.Equals("false"))
                            {
                                usuarioWeb_Configuracion.VendedorVisualizaTodosLosCliente = false;
                            }
                            else
                            {
                                usuarioWeb_Configuracion.VendedorVisualizaTodosLosCliente = true;
                            }
                        }
                        //usuarioWeb_Configuracion.VendedorVisualizaTodosLosCliente = Convert.ToBoolean();




                        entidad.Configuracion = usuarioWeb_Configuracion.GetXML();

                        Int32 cambios = repositorioCliente.ModificarUsuarioWeb(entidad);

                    }
                    else if (tipoUsuario == (int)EnumRol.Cliente)
                    {


                        Int32 cambios = repositorioCliente.ModificarUsuarioWeb(entidad);

                    }

                    return RedirectToAction("UsuariosWeb", "UsuarioWeb");


                }
                #endregion

                else
                {
                    return View(entidad);
                }


            }
            catch (Exception ex)
            {
                return View(entidad);
            }
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Buscar(String dato, int rol)
        {
            FiltroCliente filtro = new FiltroCliente();
            filtro.Codigo = dato;
            filtro.Rol = rol;

            Boolean modoVendedor = false;
            if (rol == 4)
            {
                modoVendedor = true;
            }

            //ViewData["Vendedor"] = modoVendedor;

            List<ViewCliente> lista = _repositorioUsuarioWeb.Buscar_ClientesFidelizados_Vendedores(filtro, modoVendedor);

            return PartialView("_tablaClientesRoles", lista);
        }




        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult BuscarCliente(String dato, int rol)
        {
            FiltroCliente filtro = new FiltroCliente();
            filtro.Codigo = dato;
            filtro.Rol = rol;
            Boolean modoVendedor = false;
            if (rol == 4)
            {
                modoVendedor = true;
            }

            ViewData["Vendedor"] = modoVendedor;

            List<ViewCliente> lista = _repositorioUsuarioWeb.Buscar_ClientesFidelizados_Vendedores(filtro, modoVendedor);

            return PartialView("_tablaClientesMinima", lista);
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult BuscarDni(int dni)
        {
            if (dni > 0)
            {
                LibreriaBase.WebServices.LibreriaAfip libreriaAfip = new LibreriaBase.WebServices.LibreriaAfip();
                UsuarioWeb_Configuracion con = new UsuarioWeb_Configuracion();
                con.Documento = dni;
                con.Sexo = 1;

                con = libreriaAfip.getDatosWebService(con);

                if (con == null || con?.Cuit <= 0)
                {
                    con.Sexo = 2;
                    con = libreriaAfip.getDatosWebService(con);
                }

                con.Referencias = con?.FechaNacimiento.FechaCorta();

                return new JsonResult(con);
            }
            else
            {
                return new JsonResult("");
            }

        }
    }
}
