using LibreriaBase.Areas.Logistica.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Logistica.Controllers
{
    [Area("Logistica")]
    [Route("[controller]/[action]")]
    public class LogisticaController : Controller
    {

        #region Variables
        private readonly IRepositorioTransporte _repositorioTransporte;
        private SessionAcceso _session;
        IWebHostEnvironment _environment;
        #endregion


        #region Constructor
        public LogisticaController(IRepositorioTransporte repositorioTransporte, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _repositorioTransporte = repositorioTransporte;

            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            if (_session != null)
            {
                _repositorioTransporte.DatosSistema = _session.Sistema;

            }
            else
            {
                LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();


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

                _environment = environment;

                _session.Sistema.EmpresaId = 29;

                string ruta, documento;
                documento = obtenerJsonEmpresas(out ruta);
                var empresaDatos = documento?.ToObsect<List<DatosEmpresa>>()?.FirstOrDefault(c => c.IdEmpresa == 29);

                _session.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa_V2(empresaDatos.Nombre_BaseDatos);

                httpContextAccessor.HttpContext.Session.SetJson("SessionAcceso", _session);
            }



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





        #region Acciones Simples -- Ingreso a Vistas de accion
        public IActionResult NumeroGuia(string msj, int tipo = 0)
        {

            //----
            #region IngresaDirecto

            #endregion



            if (!string.IsNullOrEmpty(msj))
            {
                ViewBag.ErrorMessage = msj;
            }

            return View(tipo);
        }
        #endregion


        #region Buscar Guia

        [HttpGet]
        public IActionResult DatosGuia(Int32? numero)
        {
            ViewBag.Message = "Datos de Seguimiento";
            string msj = "";
            try
            {
                if (numero != null)
                {
                    DatosGuiaModel.Guia dato = _repositorioTransporte.BuscarGuia((int)numero);

                    if (dato != null)
                    {
                        return View(dato);
                    }
                    else
                    {
                        msj = "No se encontro el N° de guia ";
                    }
                }
                else
                {
                    msj = "Ingrese el N° de guia";
                }
            }
            catch (Exception ex)
            {
                msj = "No se encontro el N° de guia";
            }


            return RedirectToAction("NumeroGuia", new { msj });
        }



        /// <summary>
        /// Un hash que se ingresa por vinculo cuando se envia el correo este controlador permite el acceso a los datos de ese numero de guia, que fue filtrada mediante el hash
        /// </summary>
        /// <param name="guia">hash - ver clase Encriptar_Desencriptar_texto</param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Guia(String guia)
        {
            ViewBag.Message = "Datos de Seguimiento";


            string msj = "";
            try
            {

                Encriptar_Desencriptar_Guias cifrado = new Encriptar_Desencriptar_Guias();

                Int32 reqguia = cifrado.Desencriptar(guia);

                if (guia != null)
                {
                    var ddd = guia.Split('-');
                    int partesCadena = ddd.Count();

                    if (guia.Split('-').Count() != 3)
                    {
                        throw new Exception("El formato del código no es correcto");
                    }

                    if (guia.Count() != 11)
                    {
                        throw new Exception("El formato del código no es correcto");
                    }



                    DatosGuiaModel.Guia dato = _repositorioTransporte.BuscarGuia(reqguia);

                    if (dato == null)
                    {
                        throw new Exception("No se encontro la guía");
                    }

                    Boolean validaFecha = cifrado.VerificarFecha(guia, dato.FechaGuia);

                    if (validaFecha == false)
                    {
                        throw new Exception("Error de validación en la fecha");
                    }


                    if (dato.EstadoCargaID >= 400)
                    {
                        if (dato.EstadoCargaID == 400)
                        {
                            int cantidadElementos = dato.ListaMovimientos.Count();
                            var fechaLlega = dato.ListaMovimientos[cantidadElementos - 1].FechaLlega;

                            if (fechaLlega == null)
                            {
                                throw new Exception("Esta guía ya no puede ser consultada, su estado es: " + dato.Estado);
                            }
                            else
                            {
                                TimeSpan tiempo = DateTime.Now - (DateTime)fechaLlega;
                                int dias = tiempo.Days;

                                if (dias > 3)
                                {
                                    throw new Exception("Esta guía ya no puede ser consultada, su estado es: " + dato.Estado);
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Esta guía ya no puede ser consultada, su estado es: " + dato.Estado);
                        }
                    }



                    if (dato != null)
                    {
                        return View(dato);
                    }
                    else
                    {
                        msj = "No se encontro el código";
                    }

                }
                else
                {
                    msj = "Ingrese el código";
                }


            }
            catch (Exception ex)
            {
                if (String.IsNullOrEmpty(ex.Message))
                {
                    msj = "No se encontro el código";
                }
                else
                {
                    msj = ex.Message;
                }

            }


            return RedirectToAction("NumeroGuia", new { msj });
        }

        #endregion


        #region Guia por Cliente
        /// <summary>
        /// Listado de todas las guias de 1 cliente
        /// </summary>
        /// <param name="numero">guia.</param>
        /// <returns></returns>
        //[HttpPost]
        public IActionResult GuiasPorCliente(Boolean filtrada)
        {
            try
            {
                if (filtrada == false)
                {
                    //Buscamos en la BD, las guis del usuaio.
                    var query = _repositorioTransporte.ListarGuias_Sql((Int32)_session.Sistema.EmpresaId, (Int32)_session.Usuario.IdAlmaWeb);

                    Model_GuiaEstado model_GuiaEstado = new Model_GuiaEstado();

                    if (query != null && query.Count() > 0)
                    {
                        model_GuiaEstado.MisDatos.NombreUsuario = query.FirstOrDefault().Cliente;
                    }
                    else
                    {
                        model_GuiaEstado.MisDatos.NombreUsuario = "";
                    }


                    model_GuiaEstado.ListadoGuias = query;
                    model_GuiaEstado.ListaEstados = _repositorioTransporte.ListarEstadosCargas();

                    //Guardo en Sesion el listado por cliente de guias.
                    HttpContext.Session.SetJson("model_GuiaEstado", model_GuiaEstado);


                    //Se va a llamar a la vista de Guias por Clientes, pasandole el parametro de los datos.
                    return View(model_GuiaEstado);
                }
                else
                {
                    Model_GuiaEstado model_GuiaEstado = HttpContext.Session.GetJson<Model_GuiaEstado>("model_GuiaEstado");

                    if (!String.IsNullOrEmpty(model_GuiaEstado.MisDatos.Estado))
                    {
                        model_GuiaEstado.ListadoGuias = model_GuiaEstado.ListadoGuias.Where(c => c.Estado == model_GuiaEstado.MisDatos.Estado).ToList();
                    }


                    if (model_GuiaEstado.ListadoGuias != null && model_GuiaEstado.ListadoGuias.Count() > 0)
                    {
                        if (model_GuiaEstado.MisDatos.OrdenFecha == "1")
                        {
                            model_GuiaEstado.ListadoGuias = model_GuiaEstado.ListadoGuias.OrderBy(c => c.FechaGuia).ToList();
                        }
                        else
                        {
                            model_GuiaEstado.ListadoGuias = model_GuiaEstado.ListadoGuias.OrderByDescending(c => c.FechaGuia).ToList();
                        }
                    }

                    return View(model_GuiaEstado);
                }



            }
            catch (Exception ex)
            {
                //if(_usuarioModel!=null && _usuarioModel.Usuario.Rol == 1)
                //{
                //    return View(null);
                //}
                //else
                //{
                //Se pasa algun error volvemos al login, con un msj que habria que depurar un poco mas.

                // return RedirectToAction("Acceso", "Acceso");

                return RedirectToAction("NumeroGuia", "Logistica");
                //}



            }
        }

        #endregion


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GuiasPorCliente_filtros(String ordenFecha)
        {
            Model_GuiaEstado model = HttpContext.Session.GetJson<Model_GuiaEstado>("model_GuiaEstado");

            if (model != null)
            {
                model.MisDatos.OrdenFecha = ordenFecha;
                //Guardo en Sesion el listado por cliente de guias.
                HttpContext.Session.SetJson("model_GuiaEstado", model);

                return RedirectToAction("GuiasPorCliente", "Logistica", new { filtrada = true });
            }
            else
            {
                //Se pasa algun error volvemos al login, con un msj que habria que depurar un poco mas.
                return RedirectToAction("Acceso", "Acceso");
            }

        }


    }
}