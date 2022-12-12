using DRR.Core.DBEmpresaEjemplo.Models;
using LibreriaBase.Areas.Soporte;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Soporte.Controllers
{
    [Area("Soporte")]
    [Route("[controller]/[action]")]
    public class ContactoController : Controller
    {

        #region Variables
        private readonly IRepositorioSoporte _repositorioSoporte;
        private SessionAcceso _session;
        RouteValueDictionary _error;
        #endregion


        #region Constructor
        //El constructor inyecta el repositorio de Cliente.
        //En dicho clase esta toda la comunicacion con la base de datos
        public ContactoController(IRepositorioSoporte repositorioSoporte, IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            if (_session?.Usuario?.AlmaUserID > 0)
            {
                _repositorioSoporte = repositorioSoporte;

                _repositorioSoporte.DatosSistema = _session?.Sistema;

                if (_session.Sistema.TipoEmpresa == 256 || _session.Sistema.TipoEmpresa == 512)
                {
                    _repositorioSoporte.ElementosPorPagina = 48;
                }
                else
                {
                    _repositorioSoporte.ElementosPorPagina = 24;
                }
            }
            else
            {
                //String urlTexto = "";
                //String urlRetorno = "";
                //String icono = "";


                //Metodo de error hay que armar.
                _error = new RouteValueDictionary
                {
                    { "Id", 4 },
                    { "Mensaje", "El usuario tiene que estar vinculado con un usario del sistema de gestión, para poder tener acceso al módulo de soporte"}
                };

                RedirectToAction("Notificaciones", "Home", _error);
            }


        }
        #endregion


        // GET: ContactoController
        public ActionResult ListarContactos(FiltroContacto filtro)
        {
            if (_session?.Usuario?.AlmaUserID > 0)
            {

                string urlRetorno = HttpContext.Request.Headers["Referer"].ToString();
                ViewBag.UrlRetorno = urlRetorno;

                if (urlRetorno.Contains("/Soporte/Principal"))
                {
                    ViewData["ocultarSeleccionar"] = true;
                }

                ViewBag.EntidadSucId = filtro.EnditadSucId;

                var query = _repositorioSoporte.ListarContactos(filtro);

                return View(query);
            }
            else
            {
                //Metodo de error hay que armar.
                var routeValues = new RouteValueDictionary
                {
                    { "Id", 04 },
                    { "Mensaje", "No tiene permisos para ingresar a esta pantalla - el intento de ingreso queda registrado" },
                };

                String url = Url.Action("Notificaciones", "Home", routeValues);

                return Redirect(url);
            }
        }




        private List<ContactoTipo> getListaContactoTipo()
        {
            List<ContactoTipo> listaContactoTipo = new List<ContactoTipo>();
            listaContactoTipo.Add(new ContactoTipo { ContactoTipoId = 0, Descripcion = "" });
            var query = _repositorioSoporte.ListarContactoTipos();
            listaContactoTipo.AddRange(query);
            return listaContactoTipo;
        }







        // GET: ContactoController/Create
        public ActionResult CrubContacto(Int32 entidadSucId, String urlAtras = "")
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {

                if (entidadSucId == 0)
                {
                    //Seleccionar 1ero  el cliente-
                    FiltroCliente filtro = new FiltroCliente();
                    filtro.EnumFiltro = (int)FiltroCliente.EnumFiltroCliente.Soporte_AgregarContacto;

                    return RedirectToAction("ListarClientes", "Cliente", filtro);
                }
                else
                {
                    if (!String.IsNullOrEmpty(urlAtras))
                    {
                        ViewBag.UrlRetorno = urlAtras;
                    }


                    String clienteSeleccionado = null;

                    if (TempData.ContainsKey("ClienteSeleccionado"))
                    {
                        clienteSeleccionado = TempData["ClienteSeleccionado"].ToString();
                        TempData.Remove("ClienteSeleccionado");

                        ViewBag.Cliente = clienteSeleccionado;
                    }

                    ViewBag.TipoOperacion = 1;

                    Contacto nuevoContacto = new Contacto();
                    nuevoContacto.EntidadSucId = entidadSucId;
                    nuevoContacto.Telefono = "549-";

                    ViewBag.ListarContactoTipo = getListaContactoTipo();

                    return View(nuevoContacto);
                }
            }
            else
            {
                //Metodo de error hay que armar.
                var routeValues = new RouteValueDictionary
                {
                    { "Id", 04 },
                    { "Mensaje", "No tiene permisos para ingresar a esta pantalla - el intento de ingreso queda registrado" },
                };

                String url = Url.Action("Notificaciones", "Home", routeValues);

                return Redirect(url);
            }
        }


        // POST: ContactoController/Create
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult CrubContacto(IFormCollection collection)
        {
            Contacto entidad = new Contacto();

            try
            {
                Boolean esValido = true;
                int tipoOperacion = Convert.ToInt32(collection["tipoOperacion"]);

                entidad.EntidadSucId = Convert.ToInt32(collection["EntidadSucId"]);
                string nya = collection["ApellidoNombre"];
                if (!String.IsNullOrEmpty(nya))
                {
                    if (nya.Length <= 50)
                    {
                        entidad.ApellidoNombre = nya;
                    }
                    else
                    {

                        ModelState.AddModelError("ApellidoNombre", "La cantidad maxima de caractes en 50");
                        esValido = false;
                    }
                }
                else
                {
                    ModelState.AddModelError("ApellidoNombre", "Ingrese el Apellido y Nombre del contacto.");
                    esValido = false;
                }


                byte tipo = 0;
                Boolean okTipo = Byte.TryParse(collection["ContactoTipoId"], out tipo);
                if (okTipo)
                {
                    if (tipo > 0)
                    {
                        entidad.ContactoTipoId = tipo;
                    }
                }


                entidad.Telefono = collection["Telefono"];

                if (String.IsNullOrEmpty(entidad.Telefono))
                {
                    ModelState.AddModelError("Telefono", "Ingrese el numero de celular del contacto.");
                    esValido = false;
                }
                else
                {
                    if (tipoOperacion == 1)
                    {
                        Boolean verificarNumero = _repositorioSoporte.ExisteTelefono(entidad.EntidadSucId, entidad.Telefono);

                        if (verificarNumero == true)
                        {
                            ModelState.AddModelError("Telefono", "El numero del contacto ya figura en este Cliente");
                            esValido = false;
                        }
                    }


                }



                entidad.Observaciones = collection["Observaciones"];



                if (esValido == true)
                {
                   

                    if (tipoOperacion == 1)
                    {
                        entidad.FechaAlta = entidad.FechaAlta.FechaHs_Argentina();
                        entidad.ContactoId = _repositorioSoporte.GenerarIdentificador_Contacto();
                        _repositorioSoporte.AgregarContacto(entidad);



                        FiltroContacto filtro = new FiltroContacto();
                        filtro.EnditadSucId = entidad.EntidadSucId;

                        return RedirectToAction("ListarContactos", "Contacto", filtro);
                    }
                    else if (tipoOperacion == 2)
                    {
                        String info = "";

                        entidad.ContactoId = Convert.ToInt32(collection["ContactoId"]);
                        entidad.FechaAlta = Convert.ToDateTime(collection["FechaAlta"]);

                        int mod = _repositorioSoporte.ModificarContacto(entidad, out info);

                        FiltroContacto filtro = new FiltroContacto();
                        filtro.EnditadSucId = entidad.EntidadSucId;

                        return RedirectToAction("ListarContactos", "Contacto", filtro);
                    }
                    else
                    {

                        return RedirectToAction("ListarContactos", "Contacto", null);
                    }

                }
                else
                {
                    //Los problemas de acomplamiento.
                    ViewBag.ListarContactoTipo = getListaContactoTipo();
                    return View(entidad);
                }
            }
            catch
            {
                ViewBag.ListarContactoTipo = getListaContactoTipo();
                return View(entidad);
            }
        }



        public IActionResult EditarContacto(Int32 contactoId)
        {
            if (contactoId > 0)
            {
                Contacto entidad = _repositorioSoporte.GetContacto(contactoId);

                string cliente = entidad?.EntidadSuc?.Entidad?.RazonSocial;
                if (!String.IsNullOrEmpty(cliente))
                {
                    ViewBag.Cliente = cliente;
                }
                else
                {
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = _session.Sistema;

                    int idCliente = repositorioCliente.GetClienteVendedor((int)EnumRol.Cliente, entidad.EntidadSucId);

                    if (idCliente > 0)
                    {
                        ViewBag.Cliente = repositorioCliente.GetCliente(idCliente)?.RazonSocial;
                    }
                }

                ViewBag.ListarContactoTipo = getListaContactoTipo();
                ViewBag.TipoOperacion = 2;
                return View("CrubContacto", entidad);

            }
            else
            {
                return RedirectToAction("ListarContactos", "Contacto", null);
            }

        }
    }
}
