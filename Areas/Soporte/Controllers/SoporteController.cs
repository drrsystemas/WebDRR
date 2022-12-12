using DRR.Core.DBEmpresaEjemplo.Models;
using FluentValidation.Results;
using Google.Apis.Calendar.v3.Data;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LibreriaBase.Areas.Soporte;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using LibreriaBase.Validaciones;
using LibreriaCoreDRR.GoogleCalendar;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Soporte.Controllers
{
    [Area("Soporte")]
    [Route("[controller]/[action]")]
    public class SoporteController : Controller
    {
        #region Variables
        private readonly IRepositorioSoporte _repositorioSoporte;
        private SessionAcceso _session;
        RouteValueDictionary _error;
        #endregion


        #region Metodos Google Calendar

        private String AgregarEvento(SoporteCasoTarea tarea, SoporteCaso caso)
        {
            String info = "";

            try
            {

                //Momentaneo
                String calendario = "c_9boc266g496tie3nmhvpbb41ec@group.calendar.google.com";
                try
                {
                    calendario = tarea.TecnicoSoporte?.CalendarioGoogleId;

                    if (String.IsNullOrEmpty(calendario))
                    {
                        calendario = "c_9boc266g496tie3nmhvpbb41ec@group.calendar.google.com";
                    }
                }
                catch (Exception)
                {
                    calendario = "c_9boc266g496tie3nmhvpbb41ec@group.calendar.google.com";
                }


                //Es opcional porque necesito el identificador del calendatirio.

                LibreriaCoreDRR.GoogleCalendar.Parametros parametros = new LibreriaCoreDRR.GoogleCalendar.Parametros();


                #region lo nuevo
                if (caso?.Contacto == null || caso?.Contacto.ContactoId == 0)
                {
                    var contacto = _repositorioSoporte.GetContacto(caso.ContactoId ?? 0);
                    if (contacto != null)
                    {
                        caso.Contacto = contacto;
                    }
                }
                //Req del jefe.
                caso.IdTareaPrincipa = tarea.TareaId.ToString();
                parametros.TareaId = tarea.TareaId;

                String cliente = caso.Cliente.ClienteId + " " + caso.Cliente?.EntidadSuc?.Entidad?.RazonSocial;
                if (caso.Cliente.EntidadSuc.ZonaId != null)
                {
                    cliente = "[Z-" + caso.Cliente.EntidadSuc.ZonaId + "]-" + cliente;
                }

                String titulo = cliente + " " + tarea.DescripcionTarea;
                #endregion

                parametros.Titulo = titulo;
                //   parametros.Descripcion = caso.ToString();
                parametros.Descripcion = caso.getDatos(_session.Usuario.Nombre);
                parametros.FechaAgendado = tarea.FechaHoraAsigando ?? DateTime.Now;


                LibreriaCoreDRR.GoogleCalendar.GoogleCalendar googleCalendar = new LibreriaCoreDRR.GoogleCalendar.GoogleCalendar("DrrSystemas", calendario);

                googleCalendar.CrearEvento(parametros, out info);

                return info;

            }
            catch (Exception ex)
            {
                info = ex.ErrorException();
                return info;
            }
        }

        private String ModificarEvento(SoporteCasoTarea tarea, SoporteCaso caso)
        {
            String info = "";
            try
            {

                if (caso == null)
                {
                    caso = tarea.Caso;
                }


                //Momentaneo
                String calendario = "c_9boc266g496tie3nmhvpbb41ec@group.calendar.google.com";
                try
                {
                    calendario = tarea?.TecnicoSoporte?.CalendarioGoogleId;

                    if (String.IsNullOrEmpty(calendario))
                    {
                        calendario = "c_9boc266g496tie3nmhvpbb41ec@group.calendar.google.com";
                    }
                }
                catch (Exception)
                {
                    calendario = "c_9boc266g496tie3nmhvpbb41ec@group.calendar.google.com";
                }


                LibreriaCoreDRR.GoogleCalendar.GoogleCalendar googleCalendar = new LibreriaCoreDRR.GoogleCalendar.GoogleCalendar("DrrSystemas", calendario);

                //Si el evento es distinto de null lo modificamos.
                Event evento = googleCalendar.ObtenerEvento(tarea.TareaId, out info);

                if (evento != null)
                {
                    #region lo nuevo
                    if (caso?.Contacto == null || caso?.Contacto?.ContactoId == 0)
                    {
                        var contacto = _repositorioSoporte.GetContacto(caso.ContactoId ?? 0);
                        if (contacto != null)
                        {
                            caso.Contacto = contacto;
                        }
                    }
                    //Req del jefe.
                    caso.IdTareaPrincipa = tarea.TareaId.ToString();

                    String cliente = caso.Cliente.ClienteId + " " + caso.Cliente?.EntidadSuc?.Entidad?.RazonSocial;
                    if (caso.Cliente.EntidadSuc.ZonaId != null)
                    {
                        cliente = "[Z-" + caso.Cliente.EntidadSuc.ZonaId + "]-" + cliente;
                    }

                    String titulo = cliente + " " + tarea.DescripcionTarea;
                    #endregion



                    if (tarea.EtapaId == 30)//Finalizada
                    {
                        Int32 resultadoImporte = 0;
                        try
                        {
                            decimal dato = ((decimal)tarea.CantidadFacturado * (decimal)tarea.ImporteNeto) / 60;

                            resultadoImporte = (int)Math.Round(dato, 0);
                        }
                        catch (Exception)
                        {

                        }

                        evento.Summary = "X($" + resultadoImporte + ")-" + titulo;
                    }
                    else
                    {
                        evento.Summary = titulo;
                    }


                    //** Nuevo req de cambio de fecha **//
                    if (tarea.FechaHoraAsigando != null)
                    {
                        googleCalendar.AsignarHorarios((DateTime)tarea.
        FechaHoraAsigando, evento);
                    }

                    string[] datoReg = evento?.Description?.Split("Reg. por:");
                    string reg = "";
                    if (datoReg.Count() == 2)
                    {
                        reg += datoReg[1].ToString();
                    }
                    //la descripcion tb.
                    evento.Description = caso.getDatos(reg);


                    googleCalendar.ModificarEvento(evento, tarea.TareaId, out info);


                    return info;
                }
                else
                {
                    return AgregarEvento(tarea, caso);
                }





            }
            catch (Exception ex)
            {
                info = ex.ErrorException();

                return info;
            }
        }
        #endregion


        #region Constructor
        //El constructor inyecta el repositorio de Cliente.
        //En dicho clase esta toda la comunicacion con la base de datos
        public SoporteController(IRepositorioSoporte repositorioSoporte, IHttpContextAccessor httpContextAccessor)
        {
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            if (_session?.Usuario?.EntidadSucId > 0)
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


            }


        }
        #endregion



        public IActionResult Principal()
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {
                if (_error == null)
                {
                    return View();
                }
                else
                {
                    string _urlError = Url.Action("Notificaciones", "Home", _error);

                    return Redirect(_urlError);
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



        public IActionResult ProbandoHs()
        {
            DateTime fecha = DateTime.Now.FechaHs_Argentina();



            return View();
        }


        public IActionResult ListarSoporte(FiltroSoporte filtro = null)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {

                //Control de que si hay 1 caso en edicion y viene aca se borra.
                if (HttpContext.Session.GetJson<SoporteViewModel_crub>("NuevoSoporte") != null)
                {
                    HttpContext.Session.Remove("NuevoSoporte");
                }

                //Paso en viewData porque necesito usar en vistas parciales.
                ViewData["ListaZonas"] = _repositorioSoporte.ListarZonas();

                ViewData["ListaSupervisores"] = _repositorioSoporte.ListarSupervisores();

                ViewData["ListaTecnicos"] = _repositorioSoporte.ListarTecnicos().ToList();
                ViewData["ListaEtapas"] = _repositorioSoporte.ListarEtapas().ToList();
                ViewData["UrlRetorno"] = HttpContext.Request.Headers["Referer"].ToString();


                if (filtro?.ToString() == "Vacio")
                {
                    string keyvista = HttpContext.Request.Query["vista"];
                    byte vista = 1;
                    Boolean ok = Byte.TryParse(keyvista, out vista);
                    if (vista == 0)
                    {
                        vista = 1;
                    }
                    filtro.Vista = vista;

                    filtro.TecnicoSoporteId = _repositorioSoporte.Obtener_TecnicoId_RazonSocial_porEntidadSucursalId((int)_session.Usuario.EntidadSucId)?.Id;

                    filtro.ListaEstados = new List<int?>();
                    filtro.ListaEstados.Add(10);
                    filtro.ListaEstados.Add(20);

                    filtro.FiltrarFechasPor = (Int32)FiltroSoporte.EnumFiltroSoporte_Fecha.Fecha_Registro;


                    filtro.TodasLasFechas = false;//pedido de dani
                    DateTime fecha = new DateTime().FechaHs_Argentina();
                    filtro.FechaDesde = fecha.AddDays(-7);
                    filtro.FechaHasta = fecha;
                }



                HttpContext.Session.SetJson("FiltroSoporte", filtro);

                var listaSoporte = _repositorioSoporte.ListarSoporte(filtro);


                ViewBag.Filtro = filtro;

                return View(listaSoporte);
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



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult getFiltro(FiltroSoporte filtro)
        {
            IFormCollection collection = this.HttpContext.Request.Form;
            filtro.ListaEstados = new List<int?>();
            if (collection.ContainsKey("check10"))
            {
                filtro.ListaEstados.Add(10);
            }

            if (collection.ContainsKey("check20"))
            {
                filtro.ListaEstados.Add(20);
            }

            if (collection.ContainsKey("check30"))
            {
                filtro.ListaEstados.Add(30);
            }

            if (collection.ContainsKey("check40"))
            {
                filtro.ListaEstados.Add(40);
            }

            return RedirectToAction("ListarSoporte", filtro);
        }


        /// <summary>
        /// Los clientes se utilizan los clientes del Controler Cliente....
        /// </summary>
        /// <param name="codigo"></param>
        /// <returns></returns>
        /// <modificacion>28/01/2021</modificacion>
        public IActionResult ListadoClientes(String codigo)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {

                FiltroCliente filtro = new FiltroCliente();
                filtro.Codigo = codigo;
                filtro.EnumFiltro = (int)FiltroCliente.EnumFiltroCliente.Soporte;


                String url = HttpContext.Request.UrlAtras();

                if (url.Contains("ListarSoporte"))
                {
                    //Filtro de casos.
                    HttpContext.Session.SetString("ListadoClientes", "ListarSoporte");
                }



                return RedirectToAction("ListarClientes", "Cliente", filtro);
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



        /// <summary>
        /// Los clientes se utilizan los clientes del Controler Cliente....
        /// </summary>
        /// <param name="codigo"></param>
        /// <returns></returns>
        /// <modificacion>28/01/2021</modificacion>
        public IActionResult ListadorContactos(Int32 entidadSucId, String codigo)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {

                FiltroCliente filtro = new FiltroCliente();
                filtro.Codigo = codigo;
                filtro.EnumFiltro = (int)FiltroCliente.EnumFiltroCliente.Soporte;




                return RedirectToAction("ListarContactos", "Contacto", filtro);
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


        #region Caso

        public IActionResult NuevoSoporteCaso(SoporteViewModel_crub item)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {

                Int32 operacion = 1;

                if (!String.IsNullOrEmpty(HttpContext.Request.Query["op"]))
                {
                    operacion = Convert.ToInt32(HttpContext.Request.Query["op"]);
                }


                if (item?.SoporteCaso != null)
                {
                    if (item.Usuario == null)
                    {
                        item.Usuario = _session?.Usuario;

                    }
                }
                else
                {
                    var dato = HttpContext.Session.GetJson<SoporteViewModel_crub>("NuevoSoporte");

                    if (dato != null)
                    {
                        //HttpContext.Session.Remove("NuevoSoporte");
                        item = dato;

                        if (item.Usuario == null)
                        {
                            item.Usuario = _session?.Usuario;

                        }

                    }
                    else
                    {
                        if (operacion == 1)
                        {
                            if (item?.SoporteCaso == null)
                            {
                                item = new SoporteViewModel_crub();
                                item.TipoOperacion = operacion;

                                item.Cliente = new ViewCliente();
                                item.Usuario = _session?.Usuario;

                                DateTime fechaArgentina = new DateTime();

                                item.SoporteCaso = new SoporteCaso();
                                item.SoporteCaso.FechaDeteccionCaso = DateTime.Now.FechaHs_Argentina();
                                item.SoporteCaso.FechaHora = DateTime.Now.FechaHs_Argentina();
                                var entidadEtapa = _repositorioSoporte.GetEtapa();
                                if (entidadEtapa != null)
                                {
                                    item.SoporteCaso.EtapaId = entidadEtapa.EtapaId;
                                }
                                else
                                {
                                    item.SoporteCaso.EtapaId = 10;
                                }
                                item.SoporteCaso.AgendarFecha = DateTime.Now.FechaHs_Argentina();
                                item.SoporteCaso.Activo = true;
                                item.SoporteCaso.AlmaUserId = (int)item.Usuario.AlmaUserID;



                                item.SoporteTarea = new SoporteCasoTarea();
                                item.SoporteTarea.AlmaUserId = (int)item.Usuario.AlmaUserID;
                                item.SoporteTarea.EtapaId = item.SoporteCaso.EtapaId;
                                item.SoporteTarea.TipoOperacionId = 120; //120;//Valor fijo;
                                item.SoporteTarea.PrioridadId = 1;

                                item.SoporteTarea.SeFactura = true;

                                //**
                                var tecnicodef = _repositorioSoporte.GetTecnicoIdByEmpleadoId(item?.Usuario?.EntidadSucId ?? 0);
                                if (tecnicodef != null)
                                {
                                    item.SoporteTarea.TecnicoSoporteId = tecnicodef.TecnicoSoporteId;
                                }


                                item.IdLocateCliente = new IdLocate();
                                item.IdLocateContacto = new IdLocate();
                            }
                        }
                        else if (operacion == 2)
                        {
                            item = new SoporteViewModel_crub();
                            item.TipoOperacion = operacion;
                            Int32 casoId = Convert.ToInt32(HttpContext.Request.Query["id"]);

                            item.SoporteCaso = _repositorioSoporte.GetSoporteCaso(casoId);
                            item.IdLocateContacto = new IdLocate
                            {
                                Dato = item.SoporteCaso.ContactoNombre,
                                Identificador = item.SoporteCaso.ContactoId ?? 0
                            };

                            item.Cliente = new ViewCliente();
                            IRepositorioCliente repCliente = new RepositorioCliente();
                            repCliente.DatosSistema = _repositorioSoporte.DatosSistema;
                            item.Cliente = repCliente.GetCliente(item.SoporteCaso.ClienteId);
                            item.Usuario = _session?.Usuario;
                        }
                    }

                }



                ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();

                return View(item);
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



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult SeleccionarCliente(ViewCliente cliente)
        {

            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("ListadoClientes")))
            {
                HttpContext.Session.Remove("ListadoClientes");

                var filtro = HttpContext.Session.GetJson<FiltroSoporte>("FiltroSoporte");
                if (filtro == null)
                {
                    filtro = new FiltroSoporte();

                }

                filtro.ClienteId = cliente.ClienteID;
                filtro.DatoCliente = cliente.RazonSocial;

                return RedirectToAction("ListarSoporte", "Soporte", filtro);
            }
            else
            {

                SoporteViewModel_crub item = HttpContext.Session.GetJson<SoporteViewModel_crub>("NuevoSoporte");

                if (item != null)
                {
                    item.Cliente = cliente;

                    #region Parte del saldo
                    IRepositorioCliente repCliente = new RepositorioCliente();
                    repCliente.DatosSistema = _repositorioSoporte.DatosSistema;
                    if (item.Cliente.SaldoCtaCte == 0)
                    {
                        var query = repCliente.GetSaldo(item.Cliente.EntidadSucId, (Int32)_session?.Sistema.EmpresaId);

                        if (query?.Count() > 0)
                        {
                            item.Cliente.SaldoCtaCte = query.Sum(c => c.SaldoCtaCte + c.Adelanto);
                        }
                    }
                    #endregion


                    item.IdLocateCliente.Codigo = "";
                    item.IdLocateCliente.Dato = item.Cliente.RazonSocial;
                    item.IdLocateCliente.Identificador = item.Cliente.ClienteID;
                    item.IdLocateCliente.DatoDos = item.Cliente.Supervisor;//05/05/2022

                    HttpContext.Session.SetJson("NuevoSoporte", item);

                    IFormCollection frm = HttpContext.Request.Form;

                    if (frm.ContainsKey("AgregarContacto"))
                    {
                        String razonSocial = frm["RazonSocial"];
                        TempData["ClienteSeleccionado"] = razonSocial;

                        int entidadSucId = item.Cliente.EntidadSucId;

                        String url = frm["AgregarContacto"];

                        return Redirect(url);
                    }
                    else
                    {
                        return RedirectToAction("NuevoSoporteCaso", "Soporte", item);
                    }

                }
                else
                {

                    IFormCollection frm = HttpContext.Request.Form;

                    if (frm.ContainsKey("AgregarContacto"))
                    {
                        //int entidadSucId = item.Cliente.EntidadSucId;
                        String razonSocial = frm["RazonSocial"];
                        TempData["ClienteSeleccionado"] = razonSocial;


                        String url = frm["AgregarContacto"];

                        return Redirect(url);
                    }
                    else
                    {
                        //TIENE QUE IR UN MENSAJE DE ERROR
                        TempData["ErrorRepresentada"] = "Ocurrio un error al Agregar el contacto.";

                        return RedirectToAction("ListarContactos", "Contacto");
                    }

                }


            }

        }

        public IActionResult SeleccionarContacto(Int32 contactoId)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {

                SoporteViewModel_crub item = HttpContext.Session.GetJson<SoporteViewModel_crub>("NuevoSoporte");


                Contacto entidad = _repositorioSoporte.GetContacto(contactoId);

                if (item == null)
                {
                    item = new SoporteViewModel_crub();
                }

                item.IdLocateContacto.Dato = entidad.ApellidoNombre + "  -  " + entidad?.Telefono;
                item.IdLocateContacto.Identificador = entidad.ContactoId;

                //if (item?.IdLocateCliente?.Identificador <= 0)
                //{
                    item.IdLocateCliente.Dato = entidad.EntidadSuc.Entidad.RazonSocial;

                    item.IdLocateCliente.Identificador = entidad.EntidadSuc.Cliente.ClienteId;

                    //14-05-2021
                    IRepositorioCliente repCliente = new RepositorioCliente();
                    repCliente.DatosSistema = _repositorioSoporte.DatosSistema;
                    item.Cliente = repCliente.GetCliente(entidad.EntidadSuc.Cliente.ClienteId,true);

                    if (item.Cliente.SaldoCtaCte == 0)
                    {
                        var query = repCliente.GetSaldo(item.Cliente.EntidadSucId, (Int32)_session?.Sistema.EmpresaId);

                        if (query?.Count() > 0)
                        {
                            item.Cliente.SaldoCtaCte = query.Sum(c => c.Total);
                        }
                    }

                    item.IdLocateCliente.DatoDos = item?.Cliente?.Supervisor;

                //}

                HttpContext.Session.SetJson("NuevoSoporte", item);


                return RedirectToAction("NuevoSoporteCaso", "Soporte", item);

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

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult GuardarSoporteCaso(SoporteViewModel_crub item)
        {
            //Aca traigo todo los datos del formulario, como mezclo validacion jquery algunos datos no se enlazan directamente.
            IFormCollection collection = this.HttpContext.Request.Form;



            #region Buscar Cliente
            if (item.Enum_Funcionalidad == (int)SoporteViewModel_crub.EnumFuncionalidad.BuscarCliente)
            {
                //se guarda el item en session.
                HttpContext.Session.SetJson("NuevoSoporte", item);

                FiltroCliente filtro = new FiltroCliente();
                filtro.Codigo = item.IdLocateCliente.Codigo?.Trim();
                filtro.EnumFiltro = (int)FiltroCliente.EnumFiltroCliente.Soporte;

                filtro.TipoBusqueda = (int)FiltroCliente.EnumTipoBusqueda.RazonSocial_Direccion_Cuit_Id;
                filtro.IncluirSupervisor = true;


                return RedirectToAction("ListarClientes", "Cliente", filtro);
            }
            #endregion

            #region Bucar Contacto
            else if (item.Enum_Funcionalidad == (int)SoporteViewModel_crub.EnumFuncionalidad.BuscarContacto)
            {


                if (item?.TipoOperacion == (int)EnumTipoOperacion.Modificar)
                {
                    item.SoporteCaso.DescripcionProblema = collection["DescripcionProblema"];
                    item.SoporteCaso.EmailNotificacion = collection["EmailNotificacion"];
                }

                string datoTecnico = collection["TecnicoSoporteId"];

                if (!string.IsNullOrEmpty(datoTecnico))
                {
                    item.SoporteTarea.TecnicoSoporteId = Convert.ToInt32(datoTecnico);
                }

                HttpContext.Session.SetJson("NuevoSoporte", item);

                FiltroContacto filtro = new FiltroContacto();
                filtro.EnditadSucId = item.Cliente?.EntidadSucId;
                filtro.Dato = item.IdLocateContacto.Codigo;

                if (!String.IsNullOrEmpty(filtro.Dato))
                {
                    filtro.Dato = filtro.Dato.Trim();

                    String[] numero = filtro.Dato.Split("-");
                    if (numero?.Length > 1)
                    {
                        filtro.Enum_TipoBusqueda = (int)FiltroContacto.EnumTipoBusqueda.Telefono;
                    }
                    else
                    {
                        if (filtro.Dato.EsNumerico())
                        {
                            filtro.Enum_TipoBusqueda = (int)FiltroContacto.EnumTipoBusqueda.Telefono;
                        }
                        else
                        {
                            filtro.Enum_TipoBusqueda = (int)FiltroContacto.EnumTipoBusqueda.NyA;
                        }
                    }
                }
                return RedirectToAction("ListarContactos", "Contacto", filtro);
            }

            #endregion

            #region Agregar - Modificar
            else
            {

                Boolean hayImagen = false;

                #region Imagen en el formulario
                IFormFile img = null;
                Byte[] imagenByte = null;


                if (collection?.Files?.Count() > 0)
                {
                    img = collection?.Files[0];
                    if (img != null && img.Length > 0)
                    {
                        hayImagen = true;
                    }

                    if (img.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            img.CopyTo(ms);

                            Byte[] imgOrinigal = ms.ToArray();
                            double tamañokb = img.Length / 1024;
                            imagenByte = Utilidades.ByteArrayToByteArrayReducido(imgOrinigal);

                            //imagenByte = ms.ToArray();
                            //string s = Convert.ToBase64String(imagenByte);
                        }
                    }
                }
                #endregion



                #region AGREGAR
                if (item.TipoOperacion == (Int32)EnumTipoOperacion.Agregar)
                {
                    item.SoporteCaso.DescripcionProblema = collection["DescripcionProblema"];
                    item.SoporteCaso.EmailNotificacion = collection["EmailNotificacion"];
                    item.SoporteCaso.ClienteId = Convert.ToInt32(collection["txtClienteId"]);
                    item.SoporteCaso.ContactoId = Convert.ToInt32(collection["txtContactoId"]);
                    item.SoporteCaso.ContactoNombre = item.IdLocateContacto.Dato;
                    if (item.SoporteTarea == null)
                    {
                        item.SoporteTarea = new SoporteCasoTarea();
                    }

                    //*OJO hay que validad.*//
                    if (item.SoporteCaso.DescripcionProblema.Length > 100)
                    {
                        item.SoporteTarea.DescripcionTarea = item.SoporteCaso.DescripcionProblema.Substring(0, 100);
                    }
                    else
                    {
                        item.SoporteTarea.DescripcionTarea = item.SoporteCaso.DescripcionProblema;
                    }

                    //Estimado de la fecha de la tarea.
                    item.SoporteTarea.FechaHoraAsigando = item.SoporteCaso.AgendarFecha;
                    item.SoporteTarea.TecnicoSoporteId = Convert.ToInt32(collection["TecnicoSoporteId"]);


                    if (item.Usuario == null)
                    {
                        item.Usuario = _session?.Usuario;
                    }

                    HttpContext.Session.SetJson("NuevoSoporte", item);

                    Boolean valida = true;

                    SoporteCaso soporteCaso = item.SoporteCaso;
                    SoporteCasoTarea soporteCasoTarea = item.SoporteTarea;

                    if (soporteCaso.ClienteId <= 0)
                    {
                        ModelState.AddModelError("", "Seleccione el cliente");
                        valida = false;
                    }

                    if (soporteCaso?.ContactoId == null || soporteCaso?.ContactoId <= 0)
                    {
                        ModelState.AddModelError("", "Seleccione el un contacto, o agregelo a la base de datos.");

                        valida = false;
                    }

                    if (valida == true)
                    {

                        // Validaciones que se necesitan para que la entidad se pueda agregar.
                        #region validaciones

                        //Primero hay que completar los valores ---- 


                        //Ahora se validan.-
                        SoporteCasoValidacion sc_validacion = new SoporteCasoValidacion();
                        ValidationResult result = sc_validacion.Validate(soporteCaso);



                        #endregion

                        if (result.IsValid)
                        {
                            SoporteCasoTareaValidacion sct_validacion = new SoporteCasoTareaValidacion();
                            ValidationResult result2 = sct_validacion.Validate(soporteCasoTarea);


                            if (result2.IsValid)
                            {
                                String info = "";
                                //Cargo el codigo por defecto en la tarea.
                                var tecnico = _repositorioSoporte.GetTecnicoId(soporteCasoTarea.TecnicoSoporteId);
                                if (tecnico != null)
                                {
                                    //Codigo por defecto que utiliza el empleado.
                                    if (tecnico?.DeafultCodigoId != null)
                                    {
                                        soporteCasoTarea.CodigoId = tecnico.DeafultCodigoId;

                                        ProductoView itemProducto = _repositorioSoporte.GetPorductoTecnico((Int32)soporteCasoTarea.CodigoId);


                                        soporteCasoTarea.ImporteNeto = itemProducto.Precio;
                                    }

                                }

                                soporteCaso.SoporteCasoTarea.Add(soporteCasoTarea);


                                if (hayImagen == true)
                                {

                                    OperacionArchivo operacionArchivo = new OperacionArchivo();
                                    operacionArchivo.Archivo = imagenByte;

                                    info = _repositorioSoporte.AgregarSoporteCaso(soporteCaso, operacionArchivo);
                                }
                                else
                                {
                                    info = _repositorioSoporte.AgregarSoporteCaso(soporteCaso);
                                }


                                #region EVENTOS CALENDAR
                                //**Agrego en el calendario.
                                SoporteCaso caso = _repositorioSoporte.GetSoporteCaso(soporteCaso.CasoId);
                                SoporteCasoTarea tarea = _repositorioSoporte.GetTarea(soporteCasoTarea.TareaId);

                                AgregarEvento(tarea, caso);

                                #endregion

                                if (String.IsNullOrEmpty(info))
                                {
                                    HttpContext.Session.Remove("NuevoSoporte");

                                    String modoG = collection["modoGuardar"];

                                    if (modoG == "1")
                                    {
                                        return RedirectToAction("ListarSoporte", "Soporte");
                                    }
                                    else
                                    {
                                        Int32 idTarea = soporteCaso.SoporteCasoTarea.FirstOrDefault().TareaId;

                                        var routeValueDictionary = new RouteValueDictionary()
                                         {
                                              {"tareaId",idTarea}
                                         };

                                        return RedirectToAction("EditarTarea", "Soporte", routeValueDictionary);
                                    }


                                }
                                else
                                {
                                    ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                                    ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                                    ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();
                                    ViewBag.Erro = info;
                                    return View("NuevoSoporteCaso", item);
                                }
                            }
                            else
                            {
                                String errores = "";

                                foreach (var erro in result2.Errors)
                                {
                                    errores += erro.ErrorMessage + " - ";

                                }

                                ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                                ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                                ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();
                                ViewBag.Erro = errores;
                                return View("NuevoSoporteCaso", item);
                            }

                        }
                        else
                        {
                            String errores = "";

                            foreach (var erro in result.Errors)
                            {
                                errores += erro.ErrorMessage + " - ";

                            }

                            ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                            ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                            ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();
                            ViewBag.Erro = errores;
                            return View("NuevoSoporteCaso", item);
                        }
                    }
                    else
                    {

                        ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                        ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                        ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();

                        return View("NuevoSoporteCaso", item);

                    }
                }
                #endregion


                #region MODIFICAR
                else if (item.TipoOperacion == (Int32)EnumTipoOperacion.Modificar)
                {
                    item.SoporteCaso.DescripcionProblema = collection["DescripcionProblema"];
                    item.SoporteCaso.EmailNotificacion = collection["EmailNotificacion"];

                    item.SoporteCaso.ContactoId = Convert.ToInt32(collection["txtContactoId"]);
                    item.SoporteCaso.ContactoNombre = item.IdLocateContacto.Dato;


                    //if (item.Usuario == null)
                    //{
                    //    item.Usuario = _session?.Usuario;
                    //}

                    HttpContext.Session.SetJson("NuevoSoporte", item);

                    Boolean valida = true;

                    SoporteCaso soporteCaso = item.SoporteCaso;


                    if (soporteCaso.ClienteId <= 0)
                    {
                        ModelState.AddModelError("", "Seleccione el cliente");
                        valida = false;
                    }

                    if (soporteCaso?.ContactoId == null || soporteCaso?.ContactoId <= 0)
                    {
                        ModelState.AddModelError("", "Seleccione el un contacto, o agregelo a la base de datos.");

                        valida = false;
                    }

                    if (valida == true)
                    {

                        // Validaciones que se necesitan para que la entidad se pueda agregar.
                        #region validaciones

                        //Primero hay que completar los valores ---- 


                        //Ahora se validan.-
                        SoporteCasoValidacion sc_validacion = new SoporteCasoValidacion();
                        ValidationResult result = sc_validacion.Validate(soporteCaso);

                        #endregion

                        if (result.IsValid)
                        {

                            String info = "";


                            if (hayImagen == true)
                            {
                                OperacionArchivo operacionArchivo = new OperacionArchivo();
                                operacionArchivo.Archivo = imagenByte;

                                info = _repositorioSoporte.ModificarSoporteCaso(soporteCaso, operacionArchivo);

                            }
                            else
                            {
                                info = _repositorioSoporte.ModificarSoporteCaso(soporteCaso);
                            }




                            if (String.IsNullOrEmpty(info))
                            {
                                HttpContext.Session.Remove("NuevoSoporte");

                                return RedirectToAction("ListarSoporte", "Soporte");
                            }
                            else
                            {
                                ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                                ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                                ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();
                                ViewBag.Erro = info;
                                return View("NuevoSoporteCaso", item);
                            }


                        }
                        else
                        {
                            String errores = "";

                            foreach (var erro in result.Errors)
                            {
                                errores += erro.ErrorMessage + " - ";

                            }

                            ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                            ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                            ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();
                            ViewBag.Erro = errores;
                            return View("NuevoSoporteCaso", item);
                        }
                    }
                    else
                    {

                        ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                        ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                        ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();

                        return View("NuevoSoporteCaso", item);

                    }


                }
                #endregion


                else
                {
                    ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                    ViewBag.ListaTipoCaso = _repositorioSoporte.ListarSoporteTipoCaso();
                    ViewBag.UrlRetorno = HttpContext.Request.Headers["Referer"].ToString();

                    return View("NuevoSoporteCaso", item);
                }



            }

            #endregion



        }






        #endregion

        /// <summary>
        /// Agregar no tiene vista sino que utiliza la de editar-
        /// </summary>
        /// <param name="casoId"></param>
        /// <returns></returns>
        public IActionResult AgregarTarea(Int32 casoId)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {

                ViewBag.Url = HttpContext.Request.UrlAtras();

                SoporteCasoTarea tarea = new SoporteCasoTarea();
                tarea.CasoId = casoId;
                tarea.AlmaUserId = (int)(_session?.Usuario?.AlmaUserID);
                tarea.TipoOperacionId = 120;
                tarea.FechaHoraAsigando = DateTime.Now.FechaHs_Argentina();
                ViewBag.Operacion = (Int32)EnumTipoOperacion.Agregar;

                tarea.SeFactura = true;


                ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();

                ViewBag.ListaEtapas = _repositorioSoporte.ListarEtapas().ToList();
                return View("EditarTarea", tarea);
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






        #region EDITAR TAREA

        public IActionResult EditarTarea(int tareaId)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {
                ViewBag.Url = HttpContext.Request.UrlAtras();

                SoporteCasoTarea tarea = _repositorioSoporte.GetTarea(tareaId);
                tarea.DescripcionTarea = tarea.DescripcionTarea?.Trim();

                if (tarea.FechaHoraCompletado == null)
                {
                    tarea.FechaHoraCompletado = DateTime.Now.FechaHs_Argentina();
                    tarea.SeFactura = true;
                }

                ViewBag.Operacion = (Int32)EnumTipoOperacion.Modificar;



                ViewBag.ListaServicios = _repositorioSoporte.ListarServicios(tarea.TecnicoSoporteId ?? 0);


                ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                ViewBag.ListaEtapas = _repositorioSoporte.ListarEtapas().ToList();
                return View(tarea);
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

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult EditarTarea(SoporteCasoTarea tarea)
        {
            IFormCollection collection = this.HttpContext.Request.Form;
            Int32 tipoOperacion = Convert.ToInt32(collection["tipoOperacion"]);

            String importeNeto = collection["importeNeto"];
            Int32 minutos = Convert.ToInt32(collection["minutos"]);

            tarea.CantidadFacturado = minutos;

            if (!String.IsNullOrEmpty(importeNeto))
            {
                tarea.ImporteNeto = Convert.ToDecimal(importeNeto.Replace(".", ","));
            }
            else
            {
                tarea.ImporteNeto = 0;
            }

            SoporteCasoTareaValidacion sct_validacion = new SoporteCasoTareaValidacion();
            ValidationResult result = sct_validacion.Validate(tarea);

            if (result.IsValid)
            {
                if (tipoOperacion == (Int32)EnumTipoOperacion.Agregar)
                {
                    #region Imagen en el formulario
                    IFormFile img = null;
                    Byte[] imagenByte = null;


                    if (collection?.Files?.Count() > 0)
                    {
                        img = collection?.Files[0];
                        if (img != null && img.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                img.CopyTo(ms);

                                Byte[] imgOrinigal = ms.ToArray();
                                double tamañokb = img.Length / 1024;
                                imagenByte = Utilidades.ByteArrayToByteArrayReducido(imgOrinigal);
                            }
                        }
                    }
                    #endregion


                    //Control interno de que si tiene fecha de inicio y fecha de fin se marca como finalizada.
                    if (tarea.FechaHoraInicio != null && tarea.FechaHoraCompletado != null && tarea.EtapaId == 10)
                    {
                        tarea.EtapaId = 30;
                    }

                    if (imagenByte != null)
                    {
                        OperacionArchivo operacionArchivo = new OperacionArchivo();
                        operacionArchivo.Archivo = imagenByte;

                        _repositorioSoporte.AgregarSoporteCasoTarea(tarea, operacionArchivo);
                    }
                    else
                    {
                        _repositorioSoporte.AgregarSoporteCasoTarea(tarea);
                    }


                    return RedirectToAction("ListarSoporte", "Soporte");
                }
                else
                {
                    #region Imagen en el formulario
                    IFormFile img = null;
                    Byte[] imagenByte = null;


                    if (collection?.Files?.Count() > 0)
                    {
                        img = collection?.Files[0];
                        if (img != null && img.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                img.CopyTo(ms);

                                Byte[] imgOrinigal = ms.ToArray();
                                double tamañokb = img.Length / 1024;
                                imagenByte = Utilidades.ByteArrayToByteArrayReducido(imgOrinigal);
                            }
                        }
                    }
                    #endregion

                    //Control interno de que si tiene fecha de inicio y fecha de fin se marca como finalizada.
                    if (tarea.FechaHoraInicio != null && tarea.FechaHoraCompletado != null && tarea.EtapaId == 10)
                    {
                        tarea.EtapaId = 30;
                    }

                    if (imagenByte != null)
                    {
                        OperacionArchivo operacionArchivo = new OperacionArchivo();
                        operacionArchivo.Archivo = imagenByte;

                        _repositorioSoporte.ModificarSoporteCasoTarea(tarea, operacionArchivo);
                    }
                    else
                    {
                        _repositorioSoporte.ModificarSoporteCasoTarea(tarea);
                    }


                    #region EVENTOS CALENDAR
                    //**Agrego en el calendario.
                    SoporteCaso caso = _repositorioSoporte.GetSoporteCaso(tarea.CasoId);
                    SoporteCasoTarea tareaM = _repositorioSoporte.GetTarea(tarea.TareaId);

                    ModificarEvento(tareaM, caso);

                    #endregion




                    return RedirectToAction("ListarSoporte", "Soporte");
                }
            }
            else
            {
                String errores = "";

                foreach (var erro in result.Errors)
                {
                    errores += erro.ErrorMessage + " - ";

                }

                ViewBag.Operacion = (Int32)EnumTipoOperacion.Modificar;
                ViewBag.Erro = errores;
                ViewBag.ListaTecnicos = _repositorioSoporte.ListarTecnicos().ToList();
                ViewBag.ListaEtapas = _repositorioSoporte.ListarEtapas().ToList();
                return View("EditarTarea", tarea);
            }



        }

        #endregion --Editar Tarea



        public class ServicioTecnicoView
        {
            public Int32 TecnicoId { get; set; }
            public Int32 CodigoId { get; set; }
            public DateTime? FechaInicio { get; set; }
            public DateTime? FechaFin { get; set; }

            public Decimal Importe { get; set; }
            public Int32 Minutos { get; set; }

            public Byte TipoCambio { get; set; } = 0;

            public Decimal Total { get; set; }

            public String Error { get; set; }
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AjaxPostServicioTecnicoCalculo(ServicioTecnicoView entity)
        {

            List<SoporteTecnicoServicios> listaServiciosDelTecnico = _repositorioSoporte.GetServiciosPorTecnico(entity.TecnicoId);

            SoporteTecnicoServicios item = listaServiciosDelTecnico?.FirstOrDefault(c => c.CodigoId == entity.CodigoId);

            ServicioTecnicoView respuesta = new ServicioTecnicoView();


            if (entity.TipoCambio == 0)
            {
                entity.TipoCambio = 2;
            }


            respuesta.TipoCambio = entity.TipoCambio;
            respuesta.Importe = entity.Importe;
            respuesta.FechaInicio = entity.FechaInicio;
            respuesta.FechaFin = entity.FechaFin;
            respuesta.Minutos = entity.Minutos;
            respuesta.CodigoId = entity.CodigoId;

            respuesta.Total = entity.Total;
            if (item != null)
            {
                #region Cambia Servicios
                if (respuesta.TipoCambio <= 4) //Cambios entididad
                {

                    //Obtener la entidad correspondiente
                    if (respuesta.TipoCambio == 1)
                    {
                        var producto = _repositorioSoporte.GetPorductoTecnico(respuesta.CodigoId);
                        if (producto != null)
                        {
                            respuesta.Importe = producto.Precio;
                        }
                    }
                    else
                    {
                        if (respuesta.Importe == 0)
                        {
                            var producto = _repositorioSoporte.GetPorductoTecnico(respuesta.CodigoId);
                            if (producto != null)
                            {
                                respuesta.Importe = producto.Precio;
                            }
                        }
                    }



                    if (respuesta.FechaInicio != null)
                    {
                        if (respuesta.FechaFin == null)
                        {
                            respuesta.Minutos = (int)item.CantidadMinima;
                            respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                        }

                        if (respuesta.TipoCambio == 4)
                        {

                            if (respuesta.Minutos < item.CantidadMinima)
                            {
                                respuesta.Minutos = (int)item.CantidadMinima;
                                respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                            }
                            respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                            respuesta.Total = (decimal)((respuesta.Minutos * respuesta.Importe) / 60);
                        }
                        else
                        {
                            if (respuesta.FechaFin > respuesta.FechaInicio)
                            {
                                TimeSpan diferencia = (TimeSpan)(respuesta.FechaFin - respuesta.FechaInicio);
                                respuesta.Minutos = Convert.ToInt32(diferencia.TotalMinutes);

                                if (item.CantidadMinima == null)
                                {
                                    item.CantidadMinima = 0;
                                }

                                if (respuesta.Minutos < item.CantidadMinima)
                                {
                                    respuesta.Minutos = (int)item.CantidadMinima;
                                    respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                                }

                                respuesta.Total = (decimal)((respuesta.Minutos * respuesta.Importe) / 60);
                            }
                            else
                            {
                                respuesta.Error = "La fecha de Fin no puede ser menor que la fecha de inicio.";
                            }
                        }
                    }
                    else
                    {
                        if (item.CantidadMinima == null || item.CantidadMinima == 0)
                        {
                            item.CantidadMinima = 15;
                        }
                        respuesta.Minutos = (int)item.CantidadMinima;
                        respuesta.FechaInicio = respuesta.FechaFin.Value.AddMinutes(-respuesta.Minutos);

                        TimeSpan diferencia = (TimeSpan)(respuesta.FechaFin - respuesta.FechaInicio);
                        respuesta.Minutos = Convert.ToInt32(diferencia.TotalMinutes);

                        if (item.CantidadMinima == null)
                        {
                            item.CantidadMinima = 0;
                        }

                        if (respuesta.Minutos < item.CantidadMinima)
                        {
                            respuesta.Minutos = (int)item.CantidadMinima;
                            respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                        }

                        respuesta.Total = (decimal)((respuesta.Minutos * respuesta.Importe) / 60);
                    }
                }
                #endregion

                #region Se cambia el importe total
                else if (respuesta.TipoCambio == 5)
                {
                    //Obtener la entidad correspondiente
                    if (respuesta.Importe == 0)
                    {
                        var producto = _repositorioSoporte.GetPorductoTecnico(respuesta.CodigoId);
                        if (producto != null)
                        {
                            respuesta.Importe = producto.Precio;
                        }
                    }

                    if (respuesta.FechaInicio != null)
                    {
                        if (respuesta.FechaFin == null)
                        {
                            respuesta.Minutos = (int)item.CantidadMinima;
                            respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                        }

                        if (respuesta.FechaFin > respuesta.FechaInicio)
                        {
                            respuesta.Minutos = Convert.ToInt32((respuesta.Total * 60) / respuesta.Importe);
                            respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                        }
                        else
                        {
                            respuesta.Error = "La fecha de Fin no puede ser menor que la fecha de inicio.";
                        }

                    }
                    else
                    {
                        if (item.CantidadMinima == null || item.CantidadMinima == 0)
                        {
                            item.CantidadMinima = 15;
                        }

                        respuesta.Minutos = (int)item.CantidadMinima;
                        respuesta.FechaInicio = respuesta.FechaFin.Value.AddMinutes(-respuesta.Minutos);

                        TimeSpan diferencia = (TimeSpan)(respuesta.FechaFin - respuesta.FechaInicio);
                        respuesta.Minutos = Convert.ToInt32(diferencia.TotalMinutes);

                        if (item.CantidadMinima == null)
                        {
                            item.CantidadMinima = 0;
                        }

                        if (respuesta.Minutos < item.CantidadMinima)
                        {
                            respuesta.Minutos = (int)item.CantidadMinima;
                            respuesta.FechaFin = respuesta.FechaInicio.Value.AddMinutes(respuesta.Minutos);
                        }

                        respuesta.Total = (decimal)((respuesta.Minutos * respuesta.Importe) / 60);
                    }
                }
                #endregion

            }

            if (respuesta != null)
            {
                respuesta.Importe = Math.Round(respuesta.Importe, 2);
                respuesta.Total = Math.Round(respuesta.Total, 2);
            }

            return Json(respuesta);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AjaxPostServicioTecnico(Int32 tecnicoId)
        {

            List<SoporteTecnicoServicios> listaServiciosDelTecnico = _repositorioSoporte.GetServiciosPorTecnico(tecnicoId);

            if (listaServiciosDelTecnico?.Count() > 0)
            {
                return Json(listaServiciosDelTecnico.Select(c => new { c.CodigoId, c.DescripcionAdicional }));
            }
            else
            {
                return Json(null);
            }

        }






        public IActionResult GenerarPdfListaTareas(FiltroSoporte filtro)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {
                var listaSoporte = _repositorioSoporte.ListarSoporte(filtro);




                //Secrea el documento
                iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4);
                doc.SetMargins(10f, 10f, 10f, 10f);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);

                //Las cuestiones basicas.
                doc.AddAuthor("Listado Tareas");
                //doc.AddTitle("Cliente :" + cliente.RazonSocial);
                doc.Open();


                #region Datos de cabecera
                Paragraph para1 = new Paragraph();
                Phrase ph2 = new Phrase();
                Paragraph mm1 = new Paragraph();

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("Listado Tareas", FontFactory.GetFont("Arial", 20, 2)));
                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk(Environment.NewLine));

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
                PdfPCell c1 = new PdfPCell(new Phrase("Agendado", negrita));
                PdfPCell c2 = new PdfPCell(new Phrase("Tarea", negrita));
                PdfPCell c3 = new PdfPCell(new Phrase("Tecnico", negrita));
                PdfPCell c4 = new PdfPCell(new Phrase("Información", negrita));

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


                int indice = 1;
                foreach (var item in listaSoporte)
                {
                    String contacto = "[" + item.ContactoId + "] " + item.ContactoNombre;
                    String cliente = "[" + item.ClienteId + "] " + item.Cliente.EntidadSuc.Entidad.RazonSocial;


                    foreach (var scTarea in item.SoporteCasoTarea)
                    {
                        if (filtro?.TecnicoSoporteId > 0)
                        {
                            if (scTarea.TecnicoSoporteId != filtro?.TecnicoSoporteId)
                            {
                                continue;
                            }
                        }

                        String tarea = scTarea.TareaId + " " + cliente;

                        String tecnico = "[" + scTarea?.TecnicoSoporte?.TecnicoSoporteId + "] " + scTarea?.TecnicoSoporte?.DenominacionAdicionel;


                        Boolean fotoTarea = false;
                        String rutaImagen = "";
                        var op_archivo = scTarea?.OperacionArchivo?.FirstOrDefault();
                        if (op_archivo != null)
                        {
                            if (op_archivo.Archivo != null)
                            {
                                fotoTarea = true;
                                rutaImagen = op_archivo.Archivo.RutaImagenJpg();
                            }
                        }

                        String nombrePrioridad = "";
                        if (scTarea.PrioridadId <= 4)
                        {
                            nombrePrioridad = "Baja";
                        }
                        else if (scTarea.PrioridadId <= 9)
                        {
                            nombrePrioridad = "Media";
                        }
                        else
                        {
                            nombrePrioridad = "Alta";
                        }

                        String textoCompleto = "[" + item.DescripcionProblema + "] ";

                        if (scTarea.EtapaId == 30)
                        {
                            //Finalizada.....
                            textoCompleto += " Finalizado: " + scTarea.FechaHoraCompletado?.FechaCorta();
                            textoCompleto += " Estado: " + scTarea?.Etapa?.Descripcion;
                            Decimal importe = 0;
                            try
                            {
                                importe = Math.Round((scTarea?.ImporteNeto ?? 0) * (scTarea.CantidadFacturado ?? 0) / 60, 2);
                            }
                            catch (Exception)
                            {
                                importe = 0;
                            }


                            textoCompleto += " " + importe.FormatoMoneda();
                        }
                        else
                        {
                            //Registrado - otro.
                            textoCompleto += "Prioridad: " + nombrePrioridad;
                            textoCompleto += " Estado: " + scTarea?.Etapa?.Descripcion;
                        }

                        if (indice % 2 == 0)
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


                        //Se crea una nueva fila con los datos-
                        if (scTarea.FechaHoraAsigando != null)
                        {
                            c1.Phrase = new Phrase(scTarea.FechaHoraAsigando.ToString());
                        }
                        else
                        {
                            c1.Phrase = new Phrase("------");
                        }

                        c2.Phrase = new Phrase(tarea);
                        c3.Phrase = new Phrase(tecnico);
                        c4.Phrase = new Phrase(textoCompleto);



                        tabla.AddCell(c1);
                        tabla.AddCell(c2);
                        tabla.AddCell(c3);
                        tabla.AddCell(c4);



                        indice += 1;
                    }
                }

                #endregion
                doc.Add(tabla);

                writer.Close();
                doc.Close();
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, "application/pdf");
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

        public IActionResult GenerarPdfCaso(Int32 casoId)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {
                var listaSoporte = _repositorioSoporte.GetSoporteCaso(casoId);




                //Secrea el documento
                iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4);
                doc.SetMargins(10f, 10f, 10f, 10f);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);

                //Las cuestiones basicas.
                doc.AddAuthor("Listado Tareas");
                //doc.AddTitle("Cliente :" + cliente.RazonSocial);
                doc.Open();


                #region Datos de cabecera
                Paragraph para1 = new Paragraph();
                Phrase ph2 = new Phrase();
                Paragraph mm1 = new Paragraph();

                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk("Listado Tareas", FontFactory.GetFont("Arial", 20, 2)));
                ph2.Add(new Chunk(Environment.NewLine));
                ph2.Add(new Chunk(Environment.NewLine));

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
                PdfPCell c1 = new PdfPCell(new Phrase("Agendado", negrita));
                PdfPCell c2 = new PdfPCell(new Phrase("Tarea", negrita));
                PdfPCell c3 = new PdfPCell(new Phrase("Tecnico", negrita));
                PdfPCell c4 = new PdfPCell(new Phrase("Información", negrita));

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


                int indice = 1;
                //foreach (var item in listaSoporte)
                //{
                //    String contacto = "[" + item.ContactoId + "] " + item.ContactoNombre;
                //    String cliente = "[" + item.ClienteId + "] " + item.Cliente.EntidadSuc.Entidad.RazonSocial;


                //    foreach (var scTarea in item.SoporteCasoTarea)
                //    {
                //        if (filtro?.TecnicoSoporteId > 0)
                //        {
                //            if (scTarea.TecnicoSoporteId != filtro?.TecnicoSoporteId)
                //            {
                //                continue;
                //            }
                //        }

                //        String tarea = scTarea.TareaId + " " + cliente;

                //        String tecnico = "[" + scTarea?.TecnicoSoporte?.TecnicoSoporteId + "] " + scTarea?.TecnicoSoporte?.DenominacionAdicionel;


                //        Boolean fotoTarea = false;
                //        String rutaImagen = "";
                //        var op_archivo = scTarea?.OperacionArchivo?.FirstOrDefault();
                //        if (op_archivo != null)
                //        {
                //            if (op_archivo.Archivo != null)
                //            {
                //                fotoTarea = true;
                //                rutaImagen = op_archivo.Archivo.RutaImagenJpg();
                //            }
                //        }

                //        String nombrePrioridad = "";
                //        if (scTarea.PrioridadId <= 4)
                //        {
                //            nombrePrioridad = "Baja";
                //        }
                //        else if (scTarea.PrioridadId <= 9)
                //        {
                //            nombrePrioridad = "Media";
                //        }
                //        else
                //        {
                //            nombrePrioridad = "Alta";
                //        }

                //        String textoCompleto = "[" + item.DescripcionProblema + "] ";

                //        if (scTarea.EtapaId == 30)
                //        {
                //            //Finalizada.....
                //            textoCompleto += " Finalizado: " + scTarea.FechaHoraCompletado?.FechaCorta();
                //            textoCompleto += " Estado: " + scTarea?.Etapa?.Descripcion;
                //            Decimal importe = 0;
                //            try
                //            {
                //                importe = Math.Round((scTarea?.ImporteNeto ?? 0) * (scTarea.CantidadFacturado ?? 0) / 60, 2);
                //            }
                //            catch (Exception)
                //            {
                //                importe = 0;
                //            }


                //            textoCompleto += " " + importe.FormatoMoneda();
                //        }
                //        else
                //        {
                //            //Registrado - otro.
                //            textoCompleto += "Prioridad: " + nombrePrioridad;
                //            textoCompleto += " Estado: " + scTarea?.Etapa?.Descripcion;
                //        }

                //        if (indice % 2 == 0)
                //        {
                //            c1.BackgroundColor = new BaseColor(204, 204, 204);
                //            c2.BackgroundColor = new BaseColor(204, 204, 204);
                //            c3.BackgroundColor = new BaseColor(204, 204, 204);
                //            c4.BackgroundColor = new BaseColor(204, 204, 204);
                //        }
                //        else
                //        {
                //            c1.BackgroundColor = BaseColor.White;
                //            c2.BackgroundColor = BaseColor.White;
                //            c3.BackgroundColor = BaseColor.White;
                //            c4.BackgroundColor = BaseColor.White;
                //        }


                //        //Se crea una nueva fila con los datos-
                //        if (scTarea.FechaHoraAsigando != null)
                //        {
                //            c1.Phrase = new Phrase(scTarea.FechaHoraAsigando.ToString());
                //        }
                //        else
                //        {
                //            c1.Phrase = new Phrase("------");
                //        }

                //        c2.Phrase = new Phrase(tarea);
                //        c3.Phrase = new Phrase(tecnico);
                //        c4.Phrase = new Phrase(textoCompleto);



                //        tabla.AddCell(c1);
                //        tabla.AddCell(c2);
                //        tabla.AddCell(c3);
                //        tabla.AddCell(c4);



                //        indice += 1;
                //    }
                //}

                #endregion
                doc.Add(tabla);

                writer.Close();
                doc.Close();
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, "application/pdf");
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


        public IActionResult ListarEventos(FiltroSoporte filtro = null)
        {
            if (_session?.Usuario?.EntidadSucId > 0)
            {
                try
                {
                    string info = "";

                    //Obtengo el tecnico actual.
                    var tecnico = _repositorioSoporte.GetTecnico_byAlmaUserId((int)_session.Usuario.AlmaUserID, out info);

                    GoogleCalendar googleCalendar = new GoogleCalendar("DrrSystemas", tecnico.CalendarioGoogleId);


                    var listado = googleCalendar.ListarEventos(out info);

                    InfoEventos infoEvento = new InfoEventos();
                    infoEvento.ListadoEventos = listado;

                    if (filtro == null)
                    {
                        filtro = new FiltroSoporte();
                    }

                    ViewBag.Filtro = filtro;

                    return View(infoEvento);
                }
                catch (Exception ex)
                {
                    throw;
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

    }
}