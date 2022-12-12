using DRR.Core.DBEmpresaEjemplo.Models;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.ColectorDatos;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using System.Dynamic;
using System.Runtime.InteropServices;
using WebDRR.Clases;

namespace WebDRR.Areas.ColectorDatos.Controllers
{
    [Area("ColectorDatos")]
    [Route("[controller]/[action]")]
    public class ColectorController : ControllerDrrSystemas
    {

        #region Variables
        private IRepositorioProducto _repositorioProducto;
        private SessionAcceso _session;
        private FiltroColector _filtroColector;
        #endregion


        #region Constructor
        public ColectorController(IRepositorioProducto repositorioProducto, LibreriaBase.Areas.Carrito.Clases.Carrito carritoServicio, IHttpContextAccessor httpContextAccessor)
        {
            _repositorioProducto = repositorioProducto;

            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            _repositorioProducto.DatosSistema = _session?.Sistema;

            _repositorioProducto.ElementosPorPagina = _session.ElementosPagina;


            _filtroColector = FiltroColector.RecuperarSession(httpContextAccessor.HttpContext);
            if(_filtroColector == null)
            {
                _filtroColector = new FiltroColector();
                _filtroColector.ModoVisor = false;

                _filtroColector.Todos = false;
                _filtroColector.FechaDesde = DateTime.Now.Inicio();
                _filtroColector.FechaHasta = DateTime.Now.Fin();
              
                _filtroColector.SucursalId = (short?)_session.Usuario.SucursalID;

                RepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                repositorioEmpresa.DatosSistema = _session.Sistema;

                var listaSuc = repositorioEmpresa.ListaSucursales(_session.Sistema.EmpresaId);

                if(listaSuc?.Count>0)
                {
                    _filtroColector.ListaSucursales = new List<Generica>();
                    _filtroColector.ListaSucursales.Add(new Generica { Id = 0, Nombre = "sin definir" });

                    foreach (var item in listaSuc)
                    {
                        _filtroColector.ListaSucursales.Add(new Generica { Id = item.SucursalId, Nombre = item.DescripcionSucursal });
                    }

                }

                if(_filtroColector.SucursalId!=null)
                {
                    var suc = repositorioEmpresa.GetSucursal((int)_filtroColector.SucursalId);
                    if (suc != null)
                    {
                        _filtroColector._Nombre_Sucursal = suc.DescripcionSucursal;
                    }
                }
                
                //_filtroColector.ListaDepositos = _repositorioProducto.ListarDepositos();

                _filtroColector.GuardarSession(httpContextAccessor.HttpContext);
            }
        }
        #endregion



        public ActionResult Index([Optional] Boolean refrescar)
        {
            String error = "";

            if(refrescar == true)
            {
                _filtroColector.SucursalId = (short?)_session.Usuario.SucursalID;

                RepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                repositorioEmpresa.DatosSistema = _session.Sistema;

                if (_filtroColector.SucursalId != null)
                {
                    var suc = repositorioEmpresa.GetSucursal((int)_filtroColector.SucursalId);
                    if (suc != null)
                    {
                        _filtroColector._Nombre_Sucursal = suc.DescripcionSucursal;
                    }
                }
            }
            

            var lista = _repositorioProducto.ListarColector(_filtroColector, out error);
            
            ViewData["FiltroColector"] = _filtroColector;

            return View(lista);

        }

        public IActionResult Filtro(FiltroColector filtro)
        {
            FiltroColector FiltroGuardado = FiltroColector.RecuperarSession(HttpContext);
            FiltroGuardado.TipoOperacion = filtro.TipoOperacion;
            FiltroGuardado.SucursalId = filtro.SucursalId;
            FiltroGuardado.Todos = filtro.Todos;
            FiltroGuardado.ModoVisor = filtro.ModoVisor;
            FiltroGuardado.FechaDesde = filtro.FechaDesde.Inicio();
            FiltroGuardado.FechaHasta = filtro.FechaHasta.Fin();

            FiltroGuardado.GuardarSession(HttpContext); 


            return RedirectToAction(nameof(Index));
        }


        public ActionResult Modificar(int id, int reg)
        {

            var colector = _repositorioProducto.Get_Colector(reg, id);

            DateTime fecha = DateTime.Now.FechaHs_Argentina();

            if(colector.Fecha?.Date != fecha.Date)
            {
                TempData["ErrorRepresentada"] = "No se puede modificar un registro de una fecha anterior";
                return RedirectToAction("Index");
            }


            return View(colector);
        }

        [HttpPost]
        public ActionResult Modificar(ViewColector vcolector)
        {
            String infoError = "";
            int cambios = _repositorioProducto.Modificar_Colector(vcolector.Registro,vcolector.CodigoId, (int)vcolector.Cantidad, null, null, out infoError);

            return RedirectToAction("Index");
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> BuscarProducto(String datoProducto)
        {
            try
            {
                if (!String.IsNullOrEmpty(datoProducto))
                {
                    FiltroColector filtroColector = FiltroColector.RecuperarSession(HttpContext);
                    filtroColector.Dato = datoProducto;

                    var productosBuscado = _repositorioProducto.Buscar_Colector(filtroColector);

                    if (productosBuscado != null)
                    {
                        if (productosBuscado.Count == 1)
                        {
                            var result = productosBuscado[0];

                            #region CodigoBarra
                            ProductoMinimo item = new ProductoMinimo();
                            item.ProductoId = result.CodigoId;
                            item.CodigoBarras = result.CodigoBarra;
                            item.NombreCompleto = result.Descripcion;


                            filtroColector.Dato = item.ProductoId.ToString();
                            filtroColector.TipoBusqueda = (byte)FiltroColector.EnumTipoBusquedaDato.CodigoId;

                            String error = "";


                            var prod = _repositorioProducto.ListarColector(filtroColector, out error);


                            if (prod == null || prod.Count() == 0)
                            {
                                //Tengo que agregar el registro.
                                ProductoListadoColector productoListadoColector = new ProductoListadoColector();
                                productoListadoColector.PresentacionId = 0;
                                productoListadoColector.CodigoId = item.ProductoId;
                                productoListadoColector.ListaPrecId = 0;
                                productoListadoColector.Cantidad = 1;
                                productoListadoColector.CodigoBarra = item.CodigoBarras;
                                productoListadoColector.Descripcion = item.NombreCompleto;
                                productoListadoColector.SucursalId = _filtroColector.SucursalId;
                                productoListadoColector.TipoOperacionId = _filtroColector.TipoOperacion == 0 ? null : _filtroColector.TipoOperacion;
                                productoListadoColector.FechaHora = DateTime.Now.FechaHs_Argentina();

                                _repositorioProducto.Agregar_Colector(productoListadoColector, out error);

                                dynamic respuesta = new ExpandoObject();
                                respuesta.registro = productoListadoColector.Registro;
                                respuesta.codigoBarra = productoListadoColector.CodigoBarra;
                                respuesta.idProducto = productoListadoColector.CodigoId;
                                respuesta.tipo = 3;
                                respuesta.cantidad = productoListadoColector.Cantidad;
                                respuesta.fecha = new DateTime().FechaHs_Argentina().Fecha_Hs();
                                respuesta.dato = "[" + productoListadoColector.CodigoBarra + "] - " + productoListadoColector.Descripcion;


                                return Json(respuesta);

                            }
                            else
                            {
                                var colector = prod?.FirstOrDefault();

                                int cant = (int)(colector?.Cantidad + 1);

                                _repositorioProducto.Modificar_Colector(colector.Registro, item.ProductoId, cant, null, null, out error);
                                //Actualizar.

                                dynamic respuesta = new ExpandoObject();
                                respuesta.registro = colector.Registro;
                                respuesta.codigoBarra = colector.CodigoBarra;

                                respuesta.idProducto = colector.CodigoId;
                                respuesta.tipo = 1;
                                respuesta.cantidad = cant;
                                respuesta.fecha = new DateTime().FechaHs_Argentina().Fecha_Hs();
                                respuesta.dato = "[" + colector.CodigoBarra + "] - " + colector.Descripcion;


                                return Json(respuesta);

                            }
                            #endregion
                        }
                        else
                        {
                            #region Mostrar Busqueda

                            string html = await TransformarVista_String(viewName: "_frmSeleccionarProducto", model: productosBuscado);

                            dynamic respuesta = new ExpandoObject();
                            respuesta.tipo = 10;
                            respuesta.html = html;

                            return Json(respuesta);
                            #endregion
                        }
                    }
                    else
                    {
                        return Json("");
                    }

                }
                else
                {
                    return Json("");
                }

            }
            catch (Exception ex)
            {
                String urlTexto = "";
                String url = "";
                String icono = "";

                //Metodo de error hay que armar.
                var routeValues = new RouteValueDictionary
                {
                    { "Id", ex.HResult  },
                    { "Mensaje", ex.Message }
                };

                String link = Url.Action("Notificaciones", "Home", routeValues);

                return Redirect(link);
            }
        }


        // POST: ColectorController/Create
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult SeleccionarSucursal(IFormCollection collection)
        {
            try
            {
                if (collection.ContainsKey("SucursalSeleccionada.IdSucursal"))
                {
                    Int16 idSucursal = Convert.ToInt16(collection["SucursalSeleccionada.IdSucursal"]);
                    _session.Usuario.XmlConfiguracion.SucursalID = idSucursal;
                    _session.Usuario.SucursalID = idSucursal;
                }
                else
                {
                    _session.Usuario.XmlConfiguracion.SucursalID = null;
                    _session.Usuario.SucursalID = null;
                }

                _session.GuardarSession(HttpContext);

                return RedirectToAction(nameof(Index), new { refrescar = true });
            }
            catch
            {
                _session.Usuario.XmlConfiguracion.SucursalID = null;
                _session.Usuario.SucursalID = null;
                _session.GuardarSession(HttpContext);

                return RedirectToAction(nameof(Index), new { refrescar = true });
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="idProducto"></param>
        /// <param name="tipo">0 Resta la cantidad - 1 Suma la cantidad - 2 Elimina el Registro</param>
        /// <param name="cantidad"></param>
        /// <returns></returns>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult SumarRestarColector(Int32 registro, Int32 idProducto, Int32 tipo, Int32 cantidad)
        {
            try
            {
                dynamic respuesta = new ExpandoObject();

                var colector = _repositorioProducto.Get_Colector(registro, idProducto);

                if (colector.Fecha?.Date != DateTime.Now.FechaHs_Argentina().Date)
                {
                    respuesta.error= "No se puede modificar un registro de una fecha anterior";
                }
                else
                {
                    String infoError = "";
                    int cambios = 0;

                    if (tipo == 0)
                    {
                        cantidad -= 1;
                        cambios = _repositorioProducto.Modificar_Colector(registro, idProducto, cantidad, null, null, out infoError);
                    }
                    else if (tipo == 1)
                    {
                        cantidad += 1;
                        cambios = _repositorioProducto.Modificar_Colector(registro, idProducto, cantidad, null, null, out infoError);

                    }
                    else if (tipo == 2)
                    {
                        cambios = _repositorioProducto.Eliminar_Colector(registro, idProducto, null, null, out infoError);
                    }


                    respuesta.idProducto = idProducto;
                    respuesta.tipo = tipo;
                    respuesta.cantidad = cantidad;
                }



                return Json(respuesta);


            }
            catch
            {
                return Json("");
            }
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult SeleccionarProducto(IFormCollection collection)
        {
            try
            {

                Int32 registro = 0;

                Int32 codigoId = Convert.ToInt32(collection["codigoId"]);
                string codigoBarras = collection["codigoBarra"].ToString();
                string nombre = collection["nombre"].ToString();


                FiltroColector filtroColector = FiltroColector.RecuperarSession(HttpContext);
                filtroColector.Dato = codigoId.ToString();
                filtroColector.TipoBusqueda =(byte) FiltroColector.EnumTipoBusquedaDato.CodigoId;

                String error = "";

                var prod = _repositorioProducto.ListarColector(filtroColector, out error);

                if (prod == null || prod.Count() == 0)
                {
                    //Tengo que agregar el registro.
                    ProductoListadoColector productoListadoColector = new ProductoListadoColector();
                    productoListadoColector.TipoOperacionId = filtroColector.TipoOperacion;
                    productoListadoColector.Filer = _session.Usuario.Correo;
                    productoListadoColector.PresentacionId = 0;
                    productoListadoColector.CodigoId = codigoId;
                    productoListadoColector.ListaPrecId = 0;
                    productoListadoColector.Cantidad = 1;
                    productoListadoColector.CodigoBarra = codigoBarras;
                    productoListadoColector.Descripcion = nombre;
                    productoListadoColector.SucursalId = _filtroColector.SucursalId;
                    productoListadoColector.TipoOperacionId = _filtroColector.TipoOperacion == 0 ? null : _filtroColector.TipoOperacion;
                    productoListadoColector.FechaHora = DateTime.Now.FechaHs_Argentina();


                   

                    _repositorioProducto.Agregar_Colector(productoListadoColector, out error);


                    dynamic respuesta = new ExpandoObject();
                    respuesta.registro = productoListadoColector.Registro;
                    respuesta.idProducto = productoListadoColector.CodigoId;
                    respuesta.codigoBarra = productoListadoColector.CodigoBarra;
                    respuesta.tipo = 3;
                    respuesta.cantidad = productoListadoColector.Cantidad;
                    respuesta.fecha = new DateTime().FechaHs_Argentina().Fecha_Hs();
                    respuesta.dato = "[" + productoListadoColector.CodigoId + "] - " + productoListadoColector.Descripcion;


                    return Json(respuesta);

                }
                else
                {
                    var colector = prod?.FirstOrDefault();

                    int cant = (int)(colector?.Cantidad + 1);

                    int reg = (int)(colector.Registro);

                    _repositorioProducto.Modificar_Colector(reg, colector.CodigoId, cant, null, null, out error);

                    dynamic respuesta = new ExpandoObject();
                    respuesta.registro = colector.Registro;
                    respuesta.idProducto = colector.CodigoId;
                    respuesta.codigoBarra = colector.CodigoBarra;
                    respuesta.tipo = 1;
                    respuesta.cantidad = cant;
                    respuesta.fecha = new DateTime().FechaHs_Argentina().Fecha_Hs();
                    respuesta.dato = "[" + colector.CodigoId + "] - " + colector.Descripcion;


                    return Json(respuesta);

                }

            }
            catch (Exception ex)
            {
                return Json("");
            }
        }
    }
}
