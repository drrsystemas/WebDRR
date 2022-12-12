using Castle.Core.Internal;
using LibreriaBase.Areas.ColectorDatos;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebDRR.Clases;

namespace WebDRR.Areas.Usuario.Controllers
{
    [Area("Usuario")]
    [Route("[controller]/[action]")]
    public class PanelController : ControllerDrrSystemas
    {
        private enum EnumTipoOperacion
        {
            SinDatos = 1,
            Visualizar = 2,
            Ediciones = 3
        }


        #region Variables
        private SessionAcceso _session;
        IRepositorioCliente _repositorioCliente;
        #endregion


        #region Constructor
        public PanelController(IRepositorioCliente repositorioCliente, IHttpContextAccessor httpContextAccessor)
        {
            //Se obtiene el 
            _repositorioCliente = repositorioCliente;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioCliente.DatosSistema = _session?.Sistema;
        }
        #endregion

        [HttpGet]
        public IActionResult IrA(int parametro)
        {
            return RedirectToAction("Ingresar");
        }


        public IActionResult Ingresar()
        {
            Int32 codigoError = 0;

            try
            {
                if (_session.Sistema.TipoEmpresa == (Int32)EnumTiposEmpresas.Representada || _session.Sistema.TipoEmpresa == (Int32)EnumTiposEmpresas.EmpresaMultisector)
                {
                    if (_session.Usuario?.IdAlmaWeb == null || _session.Usuario?.IdAlmaWeb == 0)
                    {
                        codigoError = 1;
                        throw new Exception("Para acceder al panel, necesita iniciar session");
                    }
                    else
                    {
                        if (_session.Sistema.TipoEmpresa == 256)
                        {
                            if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                            {
                                codigoError = 2;
                                throw new Exception("Por favor seleccione la representada con la que quiere operar");
                            }
                        }
                        else
                        {
                            if (_session.Sistema?.SectorId == null || _session.Sistema?.SectorId == 0)
                            {
                                codigoError = 3;
                                throw new Exception("Por favor seleccione el sector de la empresa con la que quiere operar");
                            }
                        }


                    }

                }
                else
                {
                    if (_session.Usuario?.IdAlmaWeb == null || _session.Usuario?.IdAlmaWeb == 0)
                    {
                        codigoError = 1;
                        throw new Exception("Para acceder al panel, necesita iniciar session");
                    }
                }


                ViewData["FullData"] = _session.ModoFull_Cliente_Productos;

                return View(_session.Usuario);
            }
            catch (Exception ex)
            {
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
            }

        }


        /// <summary>
        /// Este metodo es un ejemplo de lo que no hay que hacer.
        /// Por simplificar una vista arme un gigante.. (desacoplar en 3 vistas)
        /// 19_08_2020 se desacoplo - hoy este metodo esta obsoleto.
        /// </summary>
        /// <returns></returns>
        [ObsoleteAttribute("Este metodo era muy complicado, se perdia el objetivo de la arquitectura", false)]
        public IActionResult CompletarDatos(UsuarioWeb_Configuracion datos, Int16 tipoOperacion)
        {
            UsuarioWeb_Configuracion entity = datos;

            if (tipoOperacion < 2)
            {
                //Aca se tiene que verificar si tiene o no datos el usuario.
                entity = _repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);
            }


            if (entity == null || entity?.Documento == 0)
            {

                if (TempData.ContainsKey("ValDatos"))
                {
                    //Maneter la url de ida.
                    TempData.Keep("ValDatos");
                }


                entity = new UsuarioWeb_Configuracion();
                tipoOperacion = (Int16)EnumTipoOperacion.SinDatos;
            }
            else
            {
                if (tipoOperacion == 0)
                {
                    tipoOperacion = (Int16)EnumTipoOperacion.Visualizar;
                }
            }

            //Sobre este datos se muestra uno u otro formulario en la vista.
            ViewBag.tipoOperacion = tipoOperacion;

            switch (tipoOperacion)
            {

                case (Int16)EnumTipoOperacion.SinDatos:
                    {

                        if (datos?.Documento > 0)
                        {
                            LibreriaBase.WebServices.LibreriaAfip libreriaAfip = new LibreriaBase.WebServices.LibreriaAfip();

                            datos.Sexo = 1;
                            var primero = libreriaAfip.getDatosWebService(datos);
                            if (primero == null || primero?.Cuit <= 0)
                            {
                                datos.Sexo = 2;
                                var seg = libreriaAfip.getDatosWebService(datos);
                            }
                            else
                            {
                                datos = primero;
                            }

                            String datosxml = datos.GetXML();
                            Boolean guardo = _repositorioCliente.Guardar_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb, datosxml);

                            if (TempData.ContainsKey("ValDatos"))
                            {
                                //Int16 to = (Int16)EnumTipoOperacion.Visualizar;

                                //RouteValueDictionary diccionario = new RouteValueDictionary();
                                //diccionario.Add("datos", datos);
                                //diccionario.Add("tipoOperacion", to);

                                //return RedirectToAction("CompletarDatos","Panel", diccionario);
                                ViewBag.tipoOperacion = (Int16)EnumTipoOperacion.Visualizar;
                                return View(datos);

                            }
                            else
                            {
                                return RedirectToAction("Ingresar");
                            }

                        }
                        else
                        {
                            return View(entity);
                        }
                    }
                case (Int16)EnumTipoOperacion.Visualizar:
                    {
                        if (entity == null || entity?.Documento == 0)
                        {
                            LibreriaBase.WebServices.LibreriaAfip libreriaAfip = new LibreriaBase.WebServices.LibreriaAfip();

                            datos.Sexo = 1;
                            var primero = libreriaAfip.getDatosWebService(datos);
                            if (primero == null || primero?.Cuit <= 0)
                            {
                                datos.Sexo = 2;
                                var seg = libreriaAfip.getDatosWebService(datos);
                                entity = seg;
                            }
                            else
                            {
                                entity = primero;
                            }
                        }

                        return View(entity);
                    }
                case (Int16)EnumTipoOperacion.Ediciones:
                    {
                        String datosxml = datos.GetXML();
                        Boolean guardo = _repositorioCliente.Guardar_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb, datosxml);

                        Boolean volverUrl = false;
                        String urlIr = "";

                        if (TempData.ContainsKey("ValDatos"))
                        {
                            //Maneter la url de ida.
                            urlIr = TempData["ValDatos"].ToString();
                            TempData.Remove("ValDatos");
                            volverUrl = true;
                        }

                        if (volverUrl == false)
                        {
                            NotificacionesViewModel model = new NotificacionesViewModel();

                            if (guardo == true)
                            {
                                model.Id = 1;
                                model.Mensaje = "Los datos se guardaron en la base de datos, gracias.";

                            }
                            else
                            {
                                model.Id = -1;
                                model.Mensaje = "No se pudieron guardar los datos";
                            }

                            var routeValues = new RouteValueDictionary
                            {
                                { "Id", model.Id },
                                { "Mensaje", model.Mensaje }
                            };

                            String url = Url.Action("Notificaciones", "Home", routeValues);

                            return Redirect(url);
                        }
                        else
                        {
                            //Temporal................
                            String[] data = urlIr.Split('\\');

                            if (data?.Count() == 2)
                            {
                                return RedirectToAction(data[1], data[0]);
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
                            }

                        }
                    }
                default:
                    {
                        return View(entity);
                    }
            }
        }


        [HttpGet]
        public IActionResult TuOpinion()
        {

            return View();
        }


        public IActionResult EditarDatos()
        {
            //Puede tener en session
            //HttpContext.Session.GetJson<UsuarioWeb_Configuracion>("BuscarDatos");

            var dato = HttpContext.Session.Get("BuscarDatos");

            ViewBag.urlAtras = HttpContext.Request.UrlAtras();

            if (dato == null)
            {
                UsuarioWeb_Configuracion entity = _repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);

                if (entity != null)
                {
                    return View(entity);
                }
                else
                {
                    TempData["ErrorRepresentada"] = "No disponemos de sus datos ingrese su numero de Dni";

                    return RedirectToAction("BuscarDatos");
                }

            }
            else
            {
                TempData["ErrorRepresentada"] = "Estos datos no se guardarón, puede editarlos y guardarlos (botón el la parte inferior) o volver atras y se cancela la operación.";

                UsuarioWeb_Configuracion entity = HttpContext.Session.GetJson<UsuarioWeb_Configuracion>("BuscarDatos");
                HttpContext.Session.Remove("BuscarDatos");

                return View(entity);
            }

        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult EditarDatos(UsuarioWeb_Configuracion entity)
        {
            String datosxml = entity.GetXML();
            Boolean guardo = _repositorioCliente.Guardar_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb, datosxml);

            TempData["ErrorRepresentada"] = "Sus datos se guardaron el la base de datos.";


            if (_session.Usuario?.Rol == (Int32)EnumRol.Vendedor || _session.Usuario?.Rol == (Int32)EnumRol.Administrador)
            {
                return RedirectToAction("Ingresar", "Panel");
            }
            else
            {
                return RedirectToAction("Principal", "Home");
            }

        }

        public IActionResult BuscarDatos()
        {
            UsuarioWeb_Configuracion entity = new UsuarioWeb_Configuracion();
            return View(entity);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult BuscarDatos(UsuarioWeb_Configuracion entity)
        {
            if (entity != null || entity?.Documento > 0)
            {
                LibreriaBase.WebServices.LibreriaAfip libreriaAfip = new LibreriaBase.WebServices.LibreriaAfip();

                entity.Sexo = 1;
                var primero = libreriaAfip.getDatosWebService(entity);
                if (primero == null || primero?.Cuit <= 0)
                {
                    entity.Sexo = 2;
                    var seg = libreriaAfip.getDatosWebService(entity);
                    entity = seg;
                }
                else
                {
                    entity = primero;
                }

                HttpContext.Session.SetJson("BuscarDatos", entity);

                return RedirectToAction("EditarDatos");
            }
            else
            {
                return View();
            }

        }


        public IActionResult OpcionesUsuario()
        {

            UsuarioWeb_Configuracion confUsuario = _repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);
            
            String tipoUsuario = confUsuario.TipoUsuario.ToString();

            if (confUsuario?.Configuraciones?.Count() > 0)
            {

                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confUsuario.GenerarEsquemaInicial();

                foreach (var item in lista)
                {
                    var entity = confUsuario.Configuraciones.FirstOrDefault(c => c.Codigo == item.Codigo);

                    if (entity == null || entity.Codigo == 0)
                    {
                        confUsuario.Configuraciones.Add(item);
                    }
                    else
                    {
                        if(entity.ExtraDos.IsNullOrEmpty())
                        {
                            entity.ExtraDos = item.ExtraDos;
                        }
                    }
                }

                //return View(confUsuario.Configuraciones);
                return View(confUsuario.Configuraciones.Where(c=>c.ExtraDos == tipoUsuario));
            }
            else
            {
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confUsuario.GenerarEsquemaInicial();

                //return View(lista);

                return View(lista.Where(c => c.ExtraDos == tipoUsuario));
            }
        }

        public IActionResult EditarOpcionesUsuario(DatoConfiguracion dato)
        {
            if(dato?.Codigo == 2)
            {
                RepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                repositorioEmpresa.DatosSistema = _session.Sistema;
                ViewData["listaPrecios"] = repositorioEmpresa.ListaPrecios();
            }

            return View(dato);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult EditarOpUsuario(DatoConfiguracion dato)
        {
            UsuarioWeb_Configuracion confUsuario = _repositorioCliente.Obtener_ConfiguracionXml_UsuarioWeb((Int32)_session.Usuario.IdAlmaWeb);

            var item = confUsuario.Configuraciones.FirstOrDefault(x => x.Codigo == dato.Codigo);

            if (item != null)
            {
               
                item.Valor = dato.Valor;
                item.Extra = dato.Extra;
                item.ExtraDos = dato.ExtraDos;
            }
            else
            {
                if (confUsuario.Configuraciones == null)
                {
                    confUsuario.Configuraciones = new List<DatoConfiguracion>();
                }

                confUsuario.Configuraciones.Add(dato);
            }
            confUsuario.WebUserID = _session.Usuario.IdAlmaWeb ?? 0;
            _repositorioCliente.Actualizar_ConfiguracionXml_UsuarioWeb(confUsuario);

            return RedirectToAction("OpcionesUsuario");
        }


        public IActionResult Root(DatosUsuario datos)
        {
            ViewData["Datos"] = datos;

            return View();
        }



        public async Task<IActionResult> Inyectar_SeleccionarSector()
        {
            String htmlComponent = await TransformarComponente_String("Sector",null);

            return Content(htmlComponent);
        }

        [HttpPost]
        public IActionResult SeleccionarSector(IFormCollection ifromcollection)
        {

            if(ifromcollection.ContainsKey("SectorSeleccionada.IdSector"))
            {
                _session.Sistema.SectorId = Convert.ToInt16( ifromcollection["SectorSeleccionada.IdSector"]);
               
            }
            else
            {
                _session.Usuario.SucursalID = null;
            }

            _session.GuardarSession(HttpContext);

            return RedirectToAction("Principal","Home");
        }
    }
}