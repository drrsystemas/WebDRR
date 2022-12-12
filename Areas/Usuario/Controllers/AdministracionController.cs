using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Newtonsoft.Json;
using System.Text;
using System.Text.Encodings.Web;

namespace WebDRR.Areas.Usuario.Controllers
{
    [Area("Usuario")]
    [Route("[controller]/[action]")]
    public class AdministracionController : Controller
    {

        /// <summary>
        /// Se tiene que directamente manejar con las varible de sessionAcceso --- ver si funciona e implementar.
        /// </summary>


        #region Variables
        private SessionAcceso _session;
        IRepositorioEmpresa _repositorioEmpresa;

        IWebHostEnvironment _environment;
        #endregion


        #region Constructor
        public AdministracionController(IRepositorioEmpresa repositorioEmpresa, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            //Se obtiene el 
            _repositorioEmpresa = repositorioEmpresa;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioEmpresa.DatosSistema = _session?.Sistema;

            _environment = environment;
        }
        #endregion


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

        private void actualizarJsonEmpresas(String datos)
        {
            string[] filePaths = Directory.GetFiles(Path.Combine(_environment.WebRootPath, "files\\"));
            string archivo = Path.GetFileName(filePaths[0]);
            string ruta = filePaths[0];

            //Primero hay que lleerlo.

            StreamWriter sw = new StreamWriter(ruta, false, Encoding.ASCII);
            sw.Write(datos);
            sw.Close();

        }


        public IActionResult Index()
        {
            return View();
        }


        /// <summary>
        /// En esta view se configuran el nivel de acceso del portal, si hay modulos publicos o privados.
        /// Con este metodo se configura el esquema de DMurner, el caso de el es una web cerrada.
        /// </summary>
        /// <returns></returns>
        public IActionResult NivelPermisos()
        {
            //Recuperar de base de datos....
            ConfiguracionAdminEmpresa op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);


            ViewData["EmpresaID"] = _session?.Sistema?.EmpresaId;


            if (op?.ConfiguracionesPortal?.Count() > 0)
            {
                //Verificacion si no hay elementos nuevos...


                ConfPortal confPortal = new ConfPortal();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confPortal.GenerarEsquemaInicial();

                foreach (var item in lista)
                {
                    var itemExiste = op.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == item.Codigo);

                    if (itemExiste == null)
                    {
                        op.ConfiguracionesPortal.Add(item);
                    }
                    else
                    {
                        itemExiste.Type = item.Type;
                        //Si modifico la descripcion o los tipos de actualiza.
                        itemExiste.Descripcion = item.Descripcion;
                    }
                }

                ViewData["lwi"] = confPortal.GetListaWebInicio();

                HttpContext.Session.SetJson("op", op);
                //TempData["op"] = op;

                return View(op.ConfiguracionesPortal?.OrderBy(c => c.Codigo));
            }
            else
            {
                if (op == null)
                {
                    op = new ConfiguracionAdminEmpresa();
                }

                ConfPortal confPortal = new ConfPortal();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confPortal.GenerarEsquemaInicial();

                op.ConfiguracionesPortal = new List<DatoConfiguracion>();
                op.ConfiguracionesPortal = lista;

                ViewData["lwi"] = confPortal.GetListaWebInicio();

                HttpContext.Session.SetJson("op", op);
                //TempData["op"] = op;

                return View(lista);
            }

        }


        public IActionResult Validacion(String msj, Int16 pantalla)
        {
            ViewBag.Error = msj;
            ViewBag.Pantalla = pantalla;

            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult VerificaValidacion(String dato, Int16 pantalla)
        {

            if (!(String.IsNullOrEmpty(dato)) && dato.ToLower() == "web123")
            {
                if (pantalla == 1)//
                {
                    return RedirectToAction("Index");
                }
                else if (pantalla == 2)
                {
                    return RedirectToAction("NivelPermisos");
                }
                else if (pantalla == 3)
                {
                    return RedirectToAction("ConfigurarViewDatosProductos");
                }
                else if (pantalla == 4)
                {
                    return RedirectToAction("RubroUbicacion");
                }
                else if (pantalla == 5)
                {
                    return RedirectToAction("UsuariosWeb", "UsuarioWeb");
                }
                else if (pantalla == 6)
                {
                    return RedirectToAction("Administracion", "Optimizacion");
                }
                else if (pantalla == 7)
                {

                    var datoUSuario = _session.Usuario;

                    return RedirectToAction("Root", "Panel", datoUSuario);
                }
                else
                {
                    return RedirectToAction("Validacion", new { msj = "Los datos ingresados no son correctos", pantalla });
                }

            }
            else
            {
                return RedirectToAction("Validacion", new { msj = "Los datos ingresados no son correctos", pantalla });

            }

        }


        public IActionResult ConfigurarMetodosPago()
        {
            //Recuperar de base de datos....
            ConfiguracionAdminEmpresa op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

            if (op?.ConfiguracionesPago?.Count() > 0)
            {

                ConfMetodosPago confEnvios = new ConfMetodosPago();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confEnvios.GenerarEsquemaInicial();

                foreach (var item in lista)
                {
                    var dato = op.ConfiguracionesPago.FirstOrDefault(c => c.Codigo == item.Codigo);

                    if (dato == null)
                    {
                        op.ConfiguracionesPago.Add(item);
                    }
                    else
                    {
                        dato.Type = item.Type;
                        //Si modifico la descripcion o los tipos de actualiza.
                        dato.Descripcion = item.Descripcion;
                        dato.Extra = item.Extra;

                    }
                }

                //Elimina si hay alguna que queda obsoleta.
                for (int i = 0; i < op.ConfiguracionesPago.Count; i++)
                {
                    var operacion = op.ConfiguracionesPago[i];

                    var existe = lista.Any(c => c.Codigo == operacion.Codigo);
                    if (existe == false)
                    {
                        op.ConfiguracionesPago.RemoveAt(i);
                        i--;
                    }

                }

                //TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);

                return View(op.ConfiguracionesPago);

            }
            else
            {
                if (op == null)
                {
                    op = new ConfiguracionAdminEmpresa();
                }

                ConfMetodosPago confMetodosPago = new ConfMetodosPago();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confMetodosPago.GenerarEsquemaInicial();

                op.ConfiguracionesPago = new List<DatoConfiguracion>();
                op.ConfiguracionesPago = lista;

                //TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);

                return View(lista);
            }

        }




        public IActionResult ModificarPago(Int32 codigoEnvio)
        {
            ConfiguracionAdminEmpresa op = null;

            op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");

            if (op == null)
            {
                op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

                //TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);
            }

            DatoConfiguracion conf = new DatoConfiguracion();
            conf = op.ConfiguracionesPago.FirstOrDefault(x => x.Codigo == codigoEnvio);

            return View(conf);
        }


        #region Envios
        public IActionResult ConfigurarEnvios()
        {

            //Recuperar de base de datos....
            ConfiguracionAdminEmpresa op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

            int cantidad = op?.ConfiguracionesEnvio?.Count() ?? 0;

            if (cantidad > 0)
            {
                ConfEnvio confEnvios = new ConfEnvio();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confEnvios.GenerarEsquemaInicial();

                foreach (var item in lista)
                {
                    Boolean existe = op.ConfiguracionesEnvio.Any(c => c.Codigo == item.Codigo);
                    if (existe == false)
                    {
                        op.ConfiguracionesEnvio.Add(item);
                    }
                    else
                    {
                        var dato = op.ConfiguracionesEnvio.FirstOrDefault(c => c.Codigo == item.Codigo);
                        if (dato.Type != item.Type)
                        {
                            dato.Type = item.Type;
                        }
                    }
                }

                //    TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);
                return View(op.ConfiguracionesEnvio);


            }
            else
            {
                if (op == null)
                {
                    op = new ConfiguracionAdminEmpresa();
                }


                ConfEnvio confEnvio = new ConfEnvio();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confEnvio.GenerarEsquemaInicial();
                op.ConfiguracionesEnvio = new List<DatoConfiguracion>();
                op.ConfiguracionesEnvio = lista;

                HttpContext.Session.SetJson("op", op);
                //  TempData["op"] = op;

                return View(lista);
            }


        }

        public IActionResult ModificarEnvio(Int32 codigoEnvio)
        {
            ConfiguracionAdminEmpresa op = null;

            op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");

            if (op == null)
            {
                op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);
                HttpContext.Session.SetJson("op", op);
            }

            DatoConfiguracion conf = new DatoConfiguracion();
            conf = op.ConfiguracionesEnvio.FirstOrDefault(x => x.Codigo == codigoEnvio);

            return View(conf);
        }
        #endregion


        public IActionResult Flete()
        {
            ConfiguracionAdminEmpresa op = null;

            op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

            //TempData["op"] = op;
            HttpContext.Session.SetJson("op", op);

            DatoConfiguracion conf = new DatoConfiguracion();

            conf = op.Flete;

            RepositorioProducto rep = new RepositorioProducto();
            rep.DatosSistema = _session.Sistema;

            var servicios = rep.Servicios_Dto();
            ViewData["servicios"] = servicios;


            return View(conf);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Flete(Int32 idFlete, String nombre)
        {
            if (idFlete > 0)
            {
                ConfiguracionAdminEmpresa op = null;
                op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");

                if (op != null)
                {


                    //String json = TempData["op"].ToString();
                    //op = JsonConvert.DeserializeObject<ConfiguracionAdminEmpresa>(json);

                    if (op.Flete == null)
                    {
                        op.Flete = new DatoConfiguracion();
                        op.Flete.Codigo = 1;
                        op.Flete.Type = "Flete";
                    }
                    op.Flete.Valor = idFlete;
                    op.Flete.Descripcion = nombre;

                    String xml = ConfiguracionAdminEmpresa.GetXml(op);

                    Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                    if (guardo == true)
                    {
                        //TempData["op"] = op;
                        HttpContext.Session.SetJson("op", op);
                    }
                }

                return RedirectToAction("ConfigurarMetodosPago");

            }


            return View(null);
        }


        public IActionResult ModificarPermiso(Int32 codigoPermiso)
        {
            Boolean visibleExtra = false;

            ConfiguracionAdminEmpresa op = null;
            op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");

            if (op == null)
            {
                op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

                // TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);
            }

            DatoConfiguracion conf = new DatoConfiguracion();
            conf = op.ConfiguracionesPortal.FirstOrDefault(x => x.Codigo == codigoPermiso);

            ConfPortal confPortal = new ConfPortal();
            var lita = confPortal.GetListaWebInicio();
            ViewData["lwi"] = lita;
            var elemento = lita.FirstOrDefault(c => c.Value == conf.Valor.MostrarEntero().ToString());
            ViewData["Selected"] = elemento?.Text;

            if (conf.Codigo == 7 || conf.Codigo == 8 || conf.Codigo == 49)
            {
                ViewData["listaPrecios"] = _repositorioEmpresa.ListaPrecios();
            }
            else if (conf.Codigo == 14)
            {
                RepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = _session.Sistema;
                ViewData["listaVendedores"] = repositorioCliente.GetVendedores();
            }

            else if (conf.Codigo == 17)
            {
                List<Generica> listaweb = new List<Generica>();
                listaweb.Add(new Generica { Id = 1, Nombre = "Carrito" });
                listaweb.Add(new Generica { Id = 2, Nombre = "Principal" });
                listaweb.Add(new Generica { Id = 3, Nombre = "Producto" });
                ViewData["listaWeb"] = listaweb;
            }
            else
            {
                if (conf.Codigo == 5 || conf.Codigo == 6)
                {
                    visibleExtra = true;
                }
            }



            ViewData["visibleExtra"] = visibleExtra;

            return View(conf);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Modificar(Int32 tipo, DatoConfiguracion datoConfiguracion)
        {
            //if(datoConfiguracion.Codigo == 0)
            //{
            //   if( Request.Form.ContainsKey("Codigo")==true)
            //    {
            //        datoConfiguracion.Codigo =Convert.ToInt32( Request.Form.ContainsKey("Codigo"));

            //        if (Request.Form.ContainsKey("tipo") == true)
            //        {
            //            tipo = Convert.ToInt32(Request.Form.ContainsKey("tipo"));
            //        }
            //    }
            //}
            
            

            ConfiguracionAdminEmpresa op = null;
            op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");

            if (tipo == (Int32)ConfiguracionAdminEmpresa.EnumConfiguracionAdminEmpresa.Envio)
            {
                if (op != null)
                {
                    //ConfiguracionAdminEmpresa op;

                    //String json = TempData["op"].ToString();
                    //op = JsonConvert.DeserializeObject<ConfiguracionAdminEmpresa>(json);

                    foreach (var item in op.ConfiguracionesEnvio)
                    {
                        if (item.Codigo == datoConfiguracion.Codigo)
                        {
                            item.Descripcion = datoConfiguracion.Descripcion;
                            item.Valor = datoConfiguracion.Valor;
                            item.Extra = datoConfiguracion.Extra;
                            item.ExtraDos = datoConfiguracion.ExtraDos;
                            break;
                        }
                    }

                    String xml = ConfiguracionAdminEmpresa.GetXml(op);

                    Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                    if (guardo == true)
                    {
                        //TempData["op"] = op;
                        HttpContext.Session.SetJson("op", op);
                    }
                }


                return RedirectToAction("ConfigurarEnvios");
            }
            else if ((tipo == (Int32)ConfiguracionAdminEmpresa.EnumConfiguracionAdminEmpresa.Pago))
            {


                if (op != null)
                {
                    //ConfiguracionAdminEmpresa op;

                    //String json = TempData["op"].ToString();
                    //op = JsonConvert.DeserializeObject<ConfiguracionAdminEmpresa>(json);

                    foreach (var item in op.ConfiguracionesPago)
                    {
                        if (item.Codigo == datoConfiguracion.Codigo)
                        {
                            item.Descripcion = datoConfiguracion.Descripcion;
                            item.Valor = datoConfiguracion.Valor;

                            break;
                        }
                    }

                    String xml = ConfiguracionAdminEmpresa.GetXml(op);

                    Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                    if (guardo == true)
                    {
                        //TempData["op"] = op;
                        HttpContext.Session.SetJson("op", op);
                    }
                }
                return RedirectToAction("ConfigurarMetodosPago");
            }
            else if ((tipo == (Int32)ConfiguracionAdminEmpresa.EnumConfiguracionAdminEmpresa.ViewDatosProductos))
            {
                if (op != null)
                {
                    //ConfiguracionAdminEmpresa op;

                    //String json = TempData["op"].ToString();
                    //op = JsonConvert.DeserializeObject<ConfiguracionAdminEmpresa>(json);

                    foreach (var item in op.ConfiguracionesViewDatosProductos)
                    {
                        if (item.Codigo == datoConfiguracion.Codigo)
                        {
                            item.Descripcion = datoConfiguracion.Descripcion;

                            if(datoConfiguracion.Codigo == 28)
                            {
                                if(TempData.ContainsKey("valoresClasificaciones"))
                                {
                                    item.Extra = TempData["valoresClasificaciones"].ToString();

                                    TempData.Remove("valoresClasificaciones");
                                }
                                else
                                {
                                    item.Extra = datoConfiguracion.Extra;
                                }
                                
                            }
                            else
                            {
                                item.Extra = datoConfiguracion.Extra;
                            }
                            
                            item.Valor = datoConfiguracion.Valor;
                            item.ExtraDos = datoConfiguracion.ExtraDos;

                            break;
                        }
                    }

                    String xml = ConfiguracionAdminEmpresa.GetXml(op);

                    Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                    if (guardo == true)
                    {
                        //TempData["op"] = op;
                        HttpContext.Session.SetJson("op", op);
                    }
                }

                return RedirectToAction("ConfigurarViewDatosProductos");
            }
            else if ((tipo == (Int32)ConfiguracionAdminEmpresa.EnumConfiguracionAdminEmpresa.Portal))
            {
                if (op != null)
                {
                    //ConfiguracionAdminEmpresa op;

                    //String json = TempData["op"].ToString();
                    //op = JsonConvert.DeserializeObject<ConfiguracionAdminEmpresa>(json);

                    foreach (var item in op.ConfiguracionesPortal)
                    {
                        if (item.Codigo == datoConfiguracion.Codigo)
                        {
                            item.Descripcion = datoConfiguracion.Descripcion;
                            item.Valor = datoConfiguracion.Valor;
                            item.Extra = datoConfiguracion.Extra;
                            item.ExtraDos = datoConfiguracion.ExtraDos;
                            if (item.Codigo == 12)
                            {
                                if (item.Valor == 0)
                                {
                                    item.Extra = " Precios Finales";
                                }
                                else
                                {
                                    item.Extra = " Precios Netos";
                                }

                            }
                            else if (item.Codigo == 13)
                            {
                                if (item.Valor == 0)
                                {
                                    item.Extra = " 1 Solo Registro";
                                }
                                else if (item.Valor == 1)
                                {
                                    item.Extra = "2 En caso de haber Productos en L.Oferta";
                                }
                                else if (item.Valor == 2)
                                {
                                    item.Extra = "2 En caso de haber Productos en distinto Sectores.";
                                }
                            }
                            else if (item.Codigo == 17)
                            {

                                if (item.Valor == 1)
                                {
                                    item.Extra = "Carrito";
                                }
                                else if (item.Valor == 2)
                                {
                                    item.Extra = "Principal";
                                }
                                else
                                {
                                    item.Extra = "Producto";
                                }


                            }


                            break;
                        }
                    }

                    String xml = ConfiguracionAdminEmpresa.GetXml(op);

                    Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                    if (guardo == true)
                    {
                        //TempData["op"] = op;
                        HttpContext.Session.SetJson("op", op);
                    }
                }

                return RedirectToAction("NivelPermisos");
            }


            else
            {
                //Error

                return View();
            }

        }



        #region Rubro Ubicacion

        public IActionResult RubroUbicacion()
        {

            //Recuperar de base de datos....
            ConfiguracionAdminEmpresa op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

            if (op.RubroUbicacion == null)
            {
                op.RubroUbicacion = new List<DatoConfiguracion>();
            }

            return View(op.RubroUbicacion);

        }



        public IActionResult Abm_RubroUbicacion(int familiaId, int tipoOperacion)
        {
            string view = "";
            DatoConfiguracion entity = new DatoConfiguracion();

            switch (tipoOperacion)
            {
                case 1:
                    {
                        view = "AgregarRubroUbicacion";

                        break;
                    }
                case 2:
                    {
                        view = "ModificarRubroUbicacion";

                        break;
                    }
            }


            return View(view, entity);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AgregarRubroUbicacion(DatoConfiguracion entidad)
        {
            if (entidad.Codigo == 0)
            {
                return View(entidad);
            }
            else
            {
                ConfiguracionAdminEmpresa op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

                op.RubroUbicacion.Add(entidad);

                String xml = ConfiguracionAdminEmpresa.GetXml(op);

                Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                return RedirectToAction("RubroUbicacion", "Administracion");
            }
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult ModificarRubroUbicacion(DatoConfiguracion entidad)
        {
            if (entidad.Codigo == 0)
            {
                return View(entidad);
            }
            else
            {
                ConfiguracionAdminEmpresa op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

                var seleccion = op.RubroUbicacion.FirstOrDefault(c => c.Codigo == entidad.Codigo);
                seleccion = entidad;

                String xml = ConfiguracionAdminEmpresa.GetXml(op);

                Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                return RedirectToAction("RubroUbicacion", "Administracion");
            }
        }
        #endregion



        public IActionResult ConfigurarViewDatosProductos()
        {
            //Recuperar de base de datos....
            ConfiguracionAdminEmpresa op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

            if (op?.ConfiguracionesViewDatosProductos?.Count() > 0)
            {


                ConfViewDatosProductos confDatosProductos = new ConfViewDatosProductos();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confDatosProductos.GenerarEsquemaInicial();

                foreach (var item in lista)
                {
                    var itemExiste = op.ConfiguracionesViewDatosProductos.FirstOrDefault(c => c.Codigo == item.Codigo);

                    if (itemExiste == null)
                    {
                        op.ConfiguracionesViewDatosProductos.Add(item);
                    }
                    else
                    {
                        itemExiste.Type = item.Type;
                        //Si modifico la descripcion o los tipos de actualiza.
                        itemExiste.Descripcion = item.Descripcion;
                    }
                }

                //TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);
                return View(op.ConfiguracionesViewDatosProductos);
            }
            else
            {
                if (op == null)
                {
                    op = new ConfiguracionAdminEmpresa();
                }

                ConfViewDatosProductos confDatosProductos = new ConfViewDatosProductos();
                List<DatoConfiguracion> lista = new List<DatoConfiguracion>();
                lista = confDatosProductos.GenerarEsquemaInicial();

                op.ConfiguracionesViewDatosProductos = new List<DatoConfiguracion>();
                op.ConfiguracionesViewDatosProductos = lista;

                //TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);

                return View(lista);
            }

        }


        public IActionResult ModificarViewDatosProductos(Int32 codigoViewDatoProducto)
        {
            ConfiguracionAdminEmpresa op = null;
            op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");
            if (op == null)
            {
                op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

                //TempData["op"] = op;
                HttpContext.Session.SetJson("op", op);
            }

            DatoConfiguracion conf = new DatoConfiguracion();
            conf = op.ConfiguracionesViewDatosProductos.FirstOrDefault(x => x.Codigo == codigoViewDatoProducto);

            if (conf.Codigo == 9)
            {
                if (!String.IsNullOrEmpty(conf?.Extra))
                {
                    String[] armando_esquema = conf.Extra.Split('|');

                    ViewBag.DesdeSemaforo = armando_esquema[0] ?? "";
                    ViewBag.HastaSemaforo = armando_esquema[1] ?? "";
                }

                if (!String.IsNullOrEmpty(conf?.ExtraDos))
                {
                    String[] armando_leyendas = conf.ExtraDos.Split('|');
                    conf.ExtraDos = "";

                    ViewBag.Rojo = armando_leyendas[0] ?? "";
                    ViewBag.Amarillo = armando_leyendas[1] ?? "";
                    ViewBag.Verde = armando_leyendas[2] ?? "";
                }

            }
            else if (conf.Codigo == 14)
            {
                ViewData["listaPrecios"] = _repositorioEmpresa.ListaPrecios();

            }
            else if (conf.Codigo == 28)
            {
                if(!String.IsNullOrEmpty(conf.Extra))
                {
                    if(conf.Extra.Contains('-')==false)
                    {
                        conf.Extra += "-";
                    }
                }
            }
            else if (conf.Codigo == 33)
            {
                return RedirectToAction("ConfigurarFiltroProducto", conf);
            }


            return View(conf);
        }


        #region -- Permiten configurar la opcion de Filtro de Producto para lo que se va a visualizar
        public IActionResult ConfigurarFiltroProducto(DatoConfiguracion configuacion)
        {
            FiltroProducto filtro = new FiltroProducto();

            ViewData["Configuracion"] = configuacion;
            ViewData["ListaRubros"] = ListaRubros();
            ViewData["ListaMarcas"] = ListaMarcas();

            return View(filtro);
        }

        [HttpPost]
        public IActionResult ConfigurarFiltroProducto(IFormCollection formCollection)
        {

            if (formCollection.Keys?.Count > 0)
            {
                String datosCon = formCollection["configuracion"];
                DatoConfiguracion datoConfiguracion = datosCon.ToObsect<DatoConfiguracion>();

                if (formCollection.ContainsKey("valorLlave"))
                {
                    datoConfiguracion.Valor = 1;
                }
                else
                {
                    datoConfiguracion.Valor = 0;
                }

                String marcaFamilia = formCollection["MarcaId"] + "|" + formCollection["FamiliaId"];
                datoConfiguracion.Extra = marcaFamilia;
                datoConfiguracion.ExtraDos = formCollection["Dato"];


                ConfiguracionAdminEmpresa op = null;
                op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");

                if (op != null)
                {
                    //ConfiguracionAdminEmpresa op;

                    //String json = TempData["op"].ToString();
                    //op = JsonConvert.DeserializeObject<ConfiguracionAdminEmpresa>(json);

                    foreach (var item in op.ConfiguracionesViewDatosProductos)
                    {
                        if (item.Codigo == datoConfiguracion.Codigo)
                        {
                            item.Descripcion = datoConfiguracion.Descripcion;
                            item.Extra = datoConfiguracion.Extra;
                            item.Valor = datoConfiguracion.Valor;
                            item.ExtraDos = datoConfiguracion.ExtraDos;

                            break;
                        }
                    }

                    String xml = ConfiguracionAdminEmpresa.GetXml(op);

                    Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                    if (guardo == true)
                    {
                        //TempData["op"] = op;
                        HttpContext.Session.SetJson("op", op);
                    }
                }
            }



            return RedirectToAction("ConfigurarViewDatosProductos");
        }




        private List<SelectListItem> ListaRubros()
        {
            try
            {
                List<SelectListItem> lista = new List<SelectListItem>();

                IRepositorioProducto repositorioProducto = new RepositorioProducto();
                repositorioProducto.DatosSistema = _session.Sistema;

                var rubros = repositorioProducto.ListaFamiliasQueTienenProductosWeb(null);

                if (rubros?.Count > 0)
                {
                    foreach (var item in rubros)
                    {
                        lista.Add(new SelectListItem { Text = item.Nombre, Value = item.FamiliaId.ToString() });
                    }
                }

                return lista;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private List<SelectListItem> ListaMarcas()
        {
            try
            {
                List<SelectListItem> lista = new List<SelectListItem>();

                IRepositorioProducto repositorioProducto = new RepositorioProducto();
                repositorioProducto.DatosSistema = _session.Sistema;

                var rubros = repositorioProducto.ListaMarcasQueTienenProductosWeb(null);

                if (rubros?.Count > 0)
                {
                    foreach (var item in rubros)
                    {
                        lista.Add(new SelectListItem { Text = item.Nombre, Value = item.MarcaId.ToString() });
                    }
                }


                return lista;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion




        public IActionResult EnvioGratis()
        {
            ConfiguracionAdminEmpresa op = null;

            op = _repositorioEmpresa.Obtener_ConfiguracionAdminEmpresa((Int32)_session.Sistema.EmpresaId);

            //TempData["op"] = op;
            HttpContext.Session.SetJson("op", op);

            DatoConfiguracion conf = new DatoConfiguracion();

            conf = op.EnvioGratis;

            return View(conf);
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult EnvioGratis(DatoConfiguracion data)
        {
            ConfiguracionAdminEmpresa op;
            op = HttpContext.Session.GetJson<ConfiguracionAdminEmpresa>("op");
            if (op != null)
            {


                String json = TempData["op"].ToString();
                op = JsonConvert.DeserializeObject<ConfiguracionAdminEmpresa>(json);

                if (op.EnvioGratis == null)
                {
                    op.EnvioGratis = new DatoConfiguracion();
                    op.EnvioGratis.Codigo = 1;
                    op.EnvioGratis.Type = "Envio Gratis";
                }
                op.EnvioGratis.Valor = data.Valor;

                if (data.Valor > 0)
                {
                    op.EnvioGratis.Descripcion = "Activo";
                }
                else
                {
                    op.EnvioGratis.Descripcion = "Inactivo";
                }


                String xml = ConfiguracionAdminEmpresa.GetXml(op);

                Boolean guardo = _repositorioEmpresa.Guardar_ConfiguracionAdminEmpresa(xml, (Int32)_session.Sistema.EmpresaId);

                if (guardo == true)
                {
                    //TempData["op"] = op;
                    HttpContext.Session.SetJson("op", op);
                }
            }

            return RedirectToAction("ConfigurarMetodosPago");

        }







        //public IActionResult Empresas()
        //{
        //    if(_session.Sistema.EmpresaId == 2)
        //    {
        //        LeerScript leerScript = new LeerScript();
        //        //leer el archivo.
        //        string confJson = leerScript.Leer("Configuracion.empresas.txt");

        //        List<DatosEmpresa> lista = confJson.ToObsect<List<DatosEmpresa>>();

        //        var listaTiposEmpresas = Utilidades.EnumTiposEmpresas_GenerarLista();
        //        ViewData["ListaTiposEmpresas"] = listaTiposEmpresas;

        //        return View(lista);
        //    }
        //    else
        //    {
        //        return RedirectToAction("Ingresar","Panel");
        //    }
        //}



        public IActionResult ConfigurarEmpresas()
        {
            if (_session.Sistema.EmpresaId == 2)
            {

                string ruta, documento;
                documento = obtenerJsonEmpresas(out ruta);

                ConfiguracionEmpresas configuracionEmpresas = new ConfiguracionEmpresas();

                List<DatosEmpresa> lista = documento.ToObsect<List<DatosEmpresa>>();

                var listaTiposEmpresas = Utilidades.EnumTiposEmpresas_GenerarLista();
                ViewData["ListaTiposEmpresas"] = listaTiposEmpresas;

                return View(lista);
            }
            else
            {
                return RedirectToAction("Ingresar", "Panel");
            }
        }





        //public IActionResult CrubEmpresa(DatosEmpresa entity, byte tipoOperacion)
        public IActionResult CrubEmpresa(String entityJson, byte tipoOperacion)
        {
            DatosEmpresa entity;

            if (tipoOperacion == 0)
            {
                tipoOperacion = (byte)EnumTipoOperacion.Agregar;
                entity = new DatosEmpresa();
            }
            else if (tipoOperacion == (byte)EnumTipoOperacion.Modificar)
            {
                entity = entityJson.ToObsect<DatosEmpresa>();
            }
            else
            {
                entity = new DatosEmpresa();
            }

            var listaTiposEmpresas = Utilidades.EnumTiposEmpresas_GenerarLista();
            ViewData["ListaTiposEmpresas"] = listaTiposEmpresas;
            ViewData["TipoOperacion"] = tipoOperacion;
            return View(entity);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult CrubEmpresa(DatosEmpresa entity, byte tipoOperacion)
        {
            string ruta, documento;
            documento = obtenerJsonEmpresas(out ruta);

            ConfiguracionEmpresas configuracionEmpresas = new ConfiguracionEmpresas();

            List<DatosEmpresa> lista = documento.ToObsect<List<DatosEmpresa>>();


            if (tipoOperacion == (byte)EnumTipoOperacion.Agregar)
            {
                lista.Add(entity);
            }
            else if (tipoOperacion == (byte)EnumTipoOperacion.Modificar)
            {
                //if(entity.IdEmpresa == 0)
                //{
                //    IFormCollection collection = this.HttpContext.Request.Form;
                //    Int32 idEmpresa = Convert.ToInt32(collection["IdEmpresa"]);
                //    entity.IdEmpresa = idEmpresa;
                //}



                var obj = lista.FirstOrDefault(c => c.IdEmpresa == entity.IdEmpresa);

                if (obj != null)
                {
                    obj.Nombre_BaseDatos = entity.Nombre_BaseDatos;
                    obj.Nombre_Empresa = entity.Nombre_Empresa;
                    obj.TipoEmpresa = entity.TipoEmpresa;
                    obj.Activa = entity.Activa;
                }
            }


            actualizarJsonEmpresas(lista.ToJson());


            return RedirectToAction("ConfigurarEmpresas", "Administracion");
        }

        [HttpPost]
        /// <summary>
        /// Llama al componente clasificacion-
        /// </summary>
        /// <param name="tipoOperacion">nos indica si agrega o elimina 1 Agrega 3 Elimina</param>
        /// <param name="id">Identificador de la Clasificacion</param>
        /// <returns></returns>
        public IActionResult CallViewComponentClasificacion(byte tipoOperacion, int id)
        {
            DatoConfiguracion dato = _session.Configuracion.ConfiguracionesViewDatosProductos.FirstOrDefault(c => c.Codigo == 28);

            Generica parametro = new Generica
            {
                Auxiliar = "15-7"
            };

            return ViewComponent("Clasificacion", parametro);

        }

        public async Task<IActionResult> CallViewComponentClasificacion_Dos(byte tipoOperacion, int id)
        {
            try
            {
                String valores = "";

                Boolean tempData = false;

                if(TempData.ContainsKey("valoresClasificaciones"))
                {
                    valores = TempData["valoresClasificaciones"].ToString();
                    tempData = true;
                }
                else
                {
                    DatoConfiguracion dato = _session.Configuracion.ConfiguracionesViewDatosProductos.FirstOrDefault(c => c.Codigo == 28);

                    if(dato!=null)
                    {
                        valores = dato.Extra;

                        if (!String.IsNullOrEmpty(valores))
                        {
                            if (valores.Contains('-') == false)
                            {
                                valores += "-";
                            }
                        }
                    }

                }

              
  
                if (tipoOperacion == (byte)EnumTipoOperacion.Agregar)//agregar
                {
                    valores += $"{id}-";
                }
                else if (tipoOperacion == (byte)EnumTipoOperacion.Eliminar)//quitar
                {
                    valores = valores.Replace($"{id}-", "");
                }


                if (tempData == true)
                {
                    TempData["valoresClasificaciones"] = valores;
                    TempData.Keep("valoresClasificaciones");
                }
                else
                {
                    TempData["valoresClasificaciones"] = valores;
                }

                Generica parametro = new Generica();
                parametro.Auxiliar = valores;

                var result = await RenderViewComponent("Clasificacion", parametro);


                dynamic datos = new System.Dynamic.ExpandoObject();
                datos.componente = result;
                datos.valores = valores;

                return Json(datos);
          

            }
            catch (Exception)
            {
                return Content("");
            }

        }




        public async Task<string> RenderViewComponent(string viewComponent, object args)
        {

            var sp = HttpContext.RequestServices;

            var helper = new DefaultViewComponentHelper(
                sp.GetRequiredService<IViewComponentDescriptorCollectionProvider>(),
                HtmlEncoder.Default,
                sp.GetRequiredService<IViewComponentSelector>(),
                sp.GetRequiredService<IViewComponentInvokerFactory>(),
                sp.GetRequiredService<IViewBufferScope>());

            using (var writer = new StringWriter())
            {
                var context = new ViewContext(ControllerContext, NullView.Instance, ViewData, TempData, writer,
                    new HtmlHelperOptions());
                helper.Contextualize(context);
                var result = await helper.InvokeAsync(viewComponent, args);
                result.WriteTo(writer, HtmlEncoder.Default);
                await writer.FlushAsync();
                return writer.ToString();
            }
        }


    }


    public class NullView : IView
    {
        public static readonly NullView Instance = new();

        public string Path => string.Empty;

        public Task RenderAsync(ViewContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.CompletedTask;
        }
    }
}