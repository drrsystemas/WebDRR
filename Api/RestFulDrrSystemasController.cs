using Castle.Core.Internal;
using DRR.Core.DBAlmaNET.Models;
using DRR.Core.DBEmpresaEjemplo.Models;
using LibreriaBase.Areas.Carrito;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Cobro;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using LibreriaBase.WebServices;
using LibreriaCoreDRR.GoogleSheets;
using LibreriaCoreDRR.Web.Reparto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using NuGet.Common;
using ServiceAfip;
using System.Dynamic;
using static LibreriaBase.Areas.Carrito.Precios;
using Pedido = LibreriaCoreDRR.GoogleSheets.Pedido;
using Presentacion_ListaPrecio = LibreriaBase.Areas.Carrito.Presentacion_ListaPrecio;
using ViewEstadoCuenta = LibreriaBase.Areas.Usuario.ViewEstadoCuenta;

namespace WebDRR.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestFulDrrSystemasController : ControllerBase
    {
        #region Variables
        IWebHostEnvironment _environment;
        #endregion


        #region Metodos

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


        private Boolean verificarToken(Int32 empresaId, string token)
        {
            String dato = empresaId + "-" + "0";
            dato = dato.Encriptar();

            if (token == dato)
            {
                return true;
            }
            else
            {
                return false;
            }
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

                Int32 id =Convert.ToInt32( dato[0].Remove(0, 2));

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

        #endregion


        #region Constructor
        public RestFulDrrSystemasController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        #endregion


        #region NUBE 


        #region INGRESO - LOGIN

        [HttpPost]
        [Route("ingresoapp")]
        public String IngresoApp([FromBody] dynamic datos)
        {
            //- usuario y clave.
            try
            {
                Int32 idEmpresa = (Int32)datos.EmpresaId;

                SessionAcceso session = conectar(idEmpresa, 0, _environment);

                DatosUsuario datosUsuario = new DatosUsuario();
                datosUsuario.IdEmpresa = session.Sistema.EmpresaId;
                datosUsuario.Correo = datos.Correo;
                datosUsuario.Clave = datos.Clave;

                IRepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = session.Sistema;

                var entity = repositorioCliente.ObtenerUsuarioWeb_V2(datosUsuario);


                dynamic respuesta = new ExpandoObject();
                respuesta.VendedorId = entity.Cliente_Vendedor_Id;
                respuesta.Token = Utilidades.GetToken(idEmpresa);

                String json = JsonConvert.SerializeObject(respuesta);

                return json;
            }
            catch (Exception)
            {
                return "Error";
            }

        }

        #endregion


        #region SECTORES


        [HttpGet]
        [Route("listarsectores/{token}/{empresaId}")]
        public List<Generica> ListarSectores(String token, Int32 empresaId)
        {
            try
            {
                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);


                    IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                    repositorioEmpresa.DatosSistema = session.Sistema;

                    var query = repositorioEmpresa.ListaSectores();

                    if (query != null)
                    {
                        List<Generica> lista = new List<Generica>();
                        foreach (var item in query)
                        {
                            lista.Add(new Generica
                            {
                                Id = item.SectorId,
                                Nombre = item.Descripcion
                            });
                        }

                        return lista;
                    }

                    return null;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion


        #region CLIENTES

        /// <summary>
        /// Con este metodo puede obtener una listado en un principio de los cliente o un cliente.
        /// </summary>
        /// <param name="token">Autorizacion para poder operar</param>
        /// <param name="empresaId">identificador de la empresa</param>
        /// <param name="vendedorId">filtra los clientes para ese vendedor</param>
        /// <param name="dato">0 para todos los cliente, n para un producto en particular</param>
        /// <returns></returns>
        //[HttpGet("{token}/{empresaId}/{vendedorId}/{dato}")]
        [HttpGet]
        [Route("listarclientes/{token}/{empresaId}/{vendedorId}/{dato}")]
        public List<ViewCliente> ListarClientes(String token, Int32 empresaId, Int32 vendedorId, String dato)
        {
            try
            {

                if (dato == "0")
                {
                    dato = "";
                }

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {

                    SessionAcceso session = conectar(empresaId, 0, _environment);


                    Dictionary<Int32, List<ViewCliente>> query = null;
                    RepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = session.Sistema;
                    repositorioCliente.ElementosPorPagina = 5000;

                    LibreriaBase.Areas.Usuario.FiltroCliente filtro = new LibreriaBase.Areas.Usuario.FiltroCliente();
                    filtro.SectorId = session.Sistema.SectorId;
                    filtro.Representada = session.Sistema.TipoEmpresa == 256 ? true : false;
                    filtro.PaginaActual = 1;

                    if (vendedorId > 0)
                    {
                        filtro.VendedorId = vendedorId;
                    }

                    filtro.Todos = true;

                    if (!String.IsNullOrEmpty(dato))
                    {
                        filtro.Codigo = dato;
                    }


                    String info = "";

                    if (filtro.Representada == false)
                    {
                        query = repositorioCliente.ClientesVendedorV2(filtro, out info);
                    }
                    else
                    {
                        query = repositorioCliente.ClientesRepresentadaV2(filtro, out info);
                    }

                    return query?.First().Value;
                }
                else
                {
                    List<ViewCliente> lista = new List<ViewCliente>();
                    lista.Add(new ViewCliente { Observacion = "Los datos de verificacion no son correctas." });

                    return lista;
                }
            }
            catch (Exception ex)
            {
                List<ViewCliente> lista = new List<ViewCliente>();
                lista.Add(new ViewCliente { Observacion = ex.Message });

                return lista;

            }
        }


        [HttpPost]
        [Route("listarclientes")]
        public List<ViewCliente> ListarClientes(dynamic data)
        {
            try
            {
                #region Configuraciones

                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String dato = (String)data.Dato;
                DateTime? obtenerModificadosDesde = (DateTime?)data.ObtenerModificadosDesde;

                #endregion


                if (dato == "0")
                {
                    dato = "";
                }

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {

                    SessionAcceso session = conectar(empresaId, 0, _environment);


                    Dictionary<Int32, List<ViewCliente>> query = null;
                    RepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = session.Sistema;
                    repositorioCliente.ElementosPorPagina = 5000;

                    LibreriaBase.Areas.Usuario.FiltroCliente filtro = new LibreriaBase.Areas.Usuario.FiltroCliente();
                    filtro.SectorId = session.Sistema.SectorId;
                    filtro.Representada = session.Sistema.TipoEmpresa == 256 ? true : false;
                    filtro.PaginaActual = 1;

                    if (vendedorId > 0)
                    {
                        filtro.VendedorId = vendedorId;
                    }

                    filtro.Todos = true;

                    if (!String.IsNullOrEmpty(dato))
                    {
                        filtro.Codigo = dato;
                    }


                    String info = "";

                    if (filtro.Representada == false)
                    {
                        query = repositorioCliente.ClientesVendedorV2(filtro, out info);
                    }
                    else
                    {
                        query = repositorioCliente.ClientesRepresentadaV2(filtro, out info);
                    }

                    return query?.First().Value;
                }
                else
                {
                    List<ViewCliente> lista = new List<ViewCliente>();
                    lista.Add(new ViewCliente { Observacion = "Los datos de verificacion no son correctas." });

                    return lista;
                }
            }
            catch (Exception ex)
            {
                List<ViewCliente> lista = new List<ViewCliente>();
                lista.Add(new ViewCliente { Observacion = ex.Message });

                return lista;

            }
        }




        [HttpPost]
        [Route("saldocliente")]
        public Decimal? SaldoCliente([FromBody] dynamic data)
        {
            try
            {
                Decimal? saldo = 0;

                Int32 clienteId = (Int32)data.Cliente;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica==true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();

                    repositorioCliente.DatosSistema = session.Sistema;
                    var resutado = repositorioCliente.GetSaldo(clienteId);

                    saldo = resutado.Item1;

                }
                else
                {
                    saldo = null;
                }

                return saldo;
            }
            catch (Exception ex)
            {
                return null;
            }
        }




        [HttpPost]
        [Route("estadocuentacliente")]
        public List<ViewEstadoCuenta> EstadoCuentaCliente([FromBody] dynamic data)
        {
            try
            {
                List<ViewEstadoCuenta> lista = null;

                Int32 EntidadSucId = (Int32)data.EntidadSucursalId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();

                    repositorioCliente.DatosSistema = session.Sistema;

                    //Obtener entidad sucursal.
                    lista = repositorioCliente.GetSaldo(EntidadSucId, empresaId);
                }

                return lista;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpPost]
        [Route("estadocuentatodoslosclientes")]
        public List<ViewEstadoCuenta> EstadoCuentaTodosLosClientes([FromBody] dynamic data)
        {
            try
            {
                List<ViewEstadoCuenta> lista = null;

                Int32 EntidadSucId = (Int32)data.EntidadSucursalId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;

                DateTime? obtenerModificadosDesde = (DateTime?)data.ObtenerModificadosDesde;


                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();

                    repositorioCliente.DatosSistema = session.Sistema;

                    //Obtener entidad sucursal.
                    lista = repositorioCliente.GetSaldo_Todos();
                }

                return lista;
            }
            catch (Exception ex)
            {
                return null;
            }
        }





        [HttpPost]
        [Route("ultimocobrocliente")]
        public ViewEstadoCuenta UltimoCobroCliente([FromBody] dynamic data)
        {
            try
            {
                ViewEstadoCuenta cobro = null;

                Int32 EntidadSucId = (Int32)data.EntidadSucursalId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;
                Int32 clienteId = (Int32)data.ClienteId;

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();

                    repositorioCliente.DatosSistema = session.Sistema;

                    //Obtener entidad sucursal.
                    cobro = repositorioCliente.GetUltimoCobro(clienteId);
                }

                return cobro;
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        [HttpPost]
        [Route("todoslosultimoscobrosclientes")]
        public List<ViewEstadoCuenta> TodosLosUltimosCobrosClientes([FromBody] dynamic data)
        {
            try
            {
                List<ViewEstadoCuenta> cobros = null;

                DateTime fechaDesde = (DateTime)data.FechaDesde;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;
                Int32 clienteId = (Int32)data.ClienteId;

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();

                    repositorioCliente.DatosSistema = session.Sistema;

                    //Obtener entidad sucursal.
                    cobros = repositorioCliente.GetUltimosCobrosTodosLosClientes(fechaDesde);
                }

                return cobros;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        [Route("getciudades")]
        public List<EntityCiudad> GetCiudades([FromBody] dynamic data)
        {
            try
            {
                List<EntityCiudad> lista = null;

                Int32 EntidadSucId = (Int32)data.EntidadSucursalId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = session.Sistema;

                    //Obtener entidad sucursal.
                    lista = repositorioCliente.GetCiudades();
                }

                return lista;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpPost]
        [Route("actualizar_datoentrega_correo_tel")]
        public Boolean Actualizar_DatoEntrega_Correo_Tel([FromBody] dynamic data)
        {
            try
            {
                Boolean actualiza = false;

                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;
                Int32 clienteid = (Int32)data.ClienteId;
                String datosEntrega = (String)data.DatosEntrega;
                String correoCliente = (String)data.CorreoCliente;
                String telefono = (String)data.Telefono;


                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = session.Sistema;

                    actualiza = repositorioCliente.Actualizar_DatosEntresa_Correo_Telefono(clienteid, datosEntrega,correoCliente,telefono);
                }

                return actualiza;
            }
            catch (Exception ex)
            {
                return false;
            }
        }



        [HttpPost]
        [Route("getdatosclientesafip")]
        public Dictionary<String, personaReturn> GetDatosClientesAfip([FromBody] dynamic data)
        {
            try
            {

                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;
                Boolean esCuit = (Boolean)data.EsCuit;
                Int64 numero = (Int64)data.Numero;
                Char sexo = (Char)data.Sexo;



                Boolean verifica = verificarToken(empresaId, token);
                Dictionary<String, personaReturn> respuesta = new Dictionary<string, personaReturn>();

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);
                    IRepositorioCliente repositorioCliente = new RepositorioCliente();
                    repositorioCliente.DatosSistema = session.Sistema;



                    LibreriaAfip libreriaAfip = new LibreriaAfip();

                    Int64 cuit = 0;

                    if(esCuit == true)
                    {
                        cuit = numero;
                    }
                    else
                    {
                        cuit =libreriaAfip.GenerarCuit_Dni(numero, sexo);

                    }


                    //Primer control que el cuit ya exista en el sistema.
                    var listaCcliente = repositorioCliente.BuscarCliente(2, cuit.ToString());

                    if(listaCcliente.IsNullOrEmpty())
                    {
                        //El cliente no existe.
                        respuesta = libreriaAfip.GetPersona(cuit);
                    }
                    else
                    {
                        //El cliente existe.
                        var cliente = listaCcliente.FirstOrDefault();
                        ClienteVendedor clienteVendedor = repositorioCliente.GetClienteVendedor_byCliente(cliente.ClienteId);


                        if(clienteVendedor.VendedorId == vendedorId)
                        {
                            //El cliente es del vendedor
                            respuesta.Add("El cliente existe, y esta asignado al el vendedor actual", null);
                        }
                        else
                        {
                            //El cliente esta asignado a otro vendedor, para poder usar el cliente el mismo tiene que tener vendedor en null.
                            respuesta.Add("El cliente existe, esta asignado a otro vendedor, para poder usar el cliente el mismo tiene que tener vendedor en null", null);
                        }
                    }

                    
                }

                return respuesta;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        #endregion


        #region PRODUCTOS

        [HttpGet]
        [Route("getproducto/{token}/{empresaId}/{idProducto}")]
        public DRR.Core.DBEmpresaEjemplo.Models.Producto GetProducto(String token, Int32 empresaId, int idProducto)
        {
            try
            {
                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {

                    SessionAcceso session = conectar(empresaId, 0, _environment);

                    RepositorioProducto repositorioProducto = new RepositorioProducto();

                    repositorioProducto.DatosSistema = session.Sistema;

                    return repositorioProducto.GetProducto(idProducto);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }



        /// <summary>
        /// Con este metodo puede obtener una listado en un principio de los productos o un producto.
        /// </summary>
        /// <param name="token">Autorizacion para poder operar</param>
        /// <param name="empresaId">identificador de la empresa</param>
        /// <param name="sectorId">0 para todos los productos, n para un producto en particular</param>
        /// <param name="dato">0 para todos los productos, n para un producto en particular</param>
        /// <returns></returns>
        //[HttpGet("{token}/{empresaId}/{sectorId}/{dato}")]
        [HttpGet]
        [Route("listarproductos/{token}/{empresaId}/{sectorId}/{dato}")]
        public IEnumerable<ProductoMinimo> ListarProductos(String token, Int32 empresaId, Int16 sectorId, String dato)
        {

            try
            {

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);

                    IRepositorioProducto repositorioProducto = new RepositorioProducto();
                    repositorioProducto.DatosSistema = session.Sistema;

                    var filtro = new LibreriaBase.Areas.Carrito.Clases.FiltroProducto();
                    filtro.SectorId = session.Sistema?.SectorId;
                    filtro.TipoEmpresa = session.Sistema?.TipoEmpresa;
                    filtro.ListaPrecID = session.getListaPrecio(this.HttpContext);
                    filtro.ListaPrecioOferta = session.getListaPrecioOferta();
                    filtro.VerProductosSinStock = session.getMostrarProductosStockCero();
                    filtro.TipoEmpresa = session?.Sistema?.TipoEmpresa ?? 1;

                    if (!String.IsNullOrEmpty(dato) && dato != "0")
                    {
                        filtro.Dato = dato;
                        filtro.VerTodos = true;
                    }
                    else
                    {
                        filtro.VerTodos = true;
                    }


                    Dictionary<Int32, List<ProductoMinimo>> consulta = new Dictionary<int, List<ProductoMinimo>>();
                    consulta = repositorioProducto.ListaProductosV3(filtro);

                    return consulta?.FirstOrDefault().Value;

                }
                else
                {
                    List<ProductoMinimo> lista = new List<ProductoMinimo>();
                    lista.Add(new ProductoMinimo { NombreCompleto = "Los datos de verificacion no son correctas." });

                    return lista;
                }



            }
            catch (Exception ex)
            {
                List<ProductoMinimo> lista = new List<ProductoMinimo>();
                lista.Add(new ProductoMinimo { NombreCompleto = ex.Message });

                return lista;
            }


        }


        [HttpGet]
        [Route("lista_producto_presentacion_y_precio/{token}/{empresaId}/{sectorId}/{dato}")]
        public IEnumerable<Presentacion_ListaPrecio> Lista_Producto_Presentacion_y_Precio(String token, Int32 empresaId, Int16 sectorId, String dato)
        {

            try
            {

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);

                    IRepositorioProducto repositorioProducto = new RepositorioProducto();
                    repositorioProducto.DatosSistema = session.Sistema;

                    //var filtro = new FiltroProducto();
                    //filtro.SectorId = session.Sistema?.SectorId;
                    //filtro.TipoEmpresa = session.Sistema?.TipoEmpresa;
                    //filtro.ListaPrecID = session.getListaPrecio(this.HttpContext);
                    //filtro.ListaPrecioOferta = session.getListaPrecioOferta();
                    //filtro.VerProductosSinStock = session.getMostrarProductosStockCero();
                    //filtro.TipoEmpresa = session?.Sistema?.TipoEmpresa ?? 1;

                    //if (!String.IsNullOrEmpty(dato) && dato != "0")
                    //{
                    //    filtro.Dato = dato;
                    //    filtro.VerTodos = true;
                    //}
                    //else
                    //{
                    //    filtro.VerTodos = true;
                    //}

                    List<Presentacion_ListaPrecio> lista = repositorioProducto.Lista_Producto_Presentacion_y_Precio(null);

                    return lista;

                }
                else
                {
                    return null;
                }



            }
            catch (Exception ex)
            {
                return null;
            }
        }



        [HttpGet]
        [Route("lista_costoproducto/{token}/{empresaId}/{sectorId}/{dato}")]
        public IEnumerable<LibreriaBase.Areas.Carrito.CostoProducto> Lista_CostoProducto(String token, Int32 empresaId, Int16 sectorId, String dato)
        {

            try
            {

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);

                    IRepositorioProducto repositorioProducto = new RepositorioProducto();
                    repositorioProducto.DatosSistema = session.Sistema;

                    //var filtro = new FiltroProducto();
                    //filtro.SectorId = session.Sistema?.SectorId;
                    //filtro.TipoEmpresa = session.Sistema?.TipoEmpresa;
                    //filtro.ListaPrecID = session.getListaPrecio(this.HttpContext);
                    //filtro.ListaPrecioOferta = session.getListaPrecioOferta();
                    //filtro.VerProductosSinStock = session.getMostrarProductosStockCero();
                    //filtro.TipoEmpresa = session?.Sistema?.TipoEmpresa ?? 1;

                    //if (!String.IsNullOrEmpty(dato) && dato != "0")
                    //{
                    //    filtro.Dato = dato;
                    //    filtro.VerTodos = true;
                    //}
                    //else
                    //{
                    //    filtro.VerTodos = true;
                    //}

                    List<LibreriaBase.Areas.Carrito.CostoProducto> lista = repositorioProducto.Lista_CostoProducto(null);

                    return lista;

                }
                else
                {
                    return null;
                }



            }
            catch (Exception ex)
            {
                return null;
            }
        }





        [HttpGet]
        [Route("listarfamilias/{token}/{empresaId}")]
        public IEnumerable<FamiliaMinimo> ListarFamilias(String token, Int32 empresaId)
        {

            try
            {

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);

                    IRepositorioProducto repositorioProducto = new RepositorioProducto();
                    repositorioProducto.DatosSistema = session.Sistema;

                    var request = repositorioProducto.ListaFamiliasQueTienenProductosWeb(null);

                    return request;
                }
                else
                {
                    return null;
                }



            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpGet]
        [Route("listarmarcas/{token}/{empresaId}")]
        public IEnumerable<MarcaMinimo> ListarMarcas(String token, Int32 empresaId)
        {

            try
            {

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);

                    IRepositorioProducto repositorioProducto = new RepositorioProducto();
                    repositorioProducto.DatosSistema = session.Sistema;

                    var request = repositorioProducto.ListaMarcasQueTienenProductosWeb(null);

                    return request;

                }
                else
                {
                    return null;
                }



            }
            catch (Exception ex)
            {
                return null;
            }
        }



        [HttpGet]
        [Route("getlistasprecios/{token}/{empresaId}")]
        public List<Generica> GetListasPrecios(String token, Int32 empresaId)
        {
            try
            {
                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    SessionAcceso session = conectar(empresaId, 0, _environment);


                    IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
                    repositorioEmpresa.DatosSistema = session.Sistema;

                    var query = repositorioEmpresa.ListaPrecios();

                    if (query != null)
                    {
                        List<Generica> lista = new List<Generica>();
                        foreach (var item in query)
                        {
                            lista.Add(new Generica
                            {
                                Id = item.ListaPrecId,
                                Nombre = item.Descripcion
                            });
                        }

                        return lista;
                    }

                    return null;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion


        #region PEDIDOS

        // GET api/<ApiPedidoController>/5
        //[HttpGet("{token}/{empresaId}/{vendedorId}/{rango}")]
        [HttpGet]
        [Route("listarpedidos/{token}/{empresaId}/{vendedorId}/{rango}")]
        public List<PedidoView> ListarPedidos(String token, Int32 empresaId, Int32 vendedorId, byte rango)
        {
            Boolean verifica = verificarToken(empresaId, token);

            if (verifica == true)
            {
                SessionAcceso session = conectar(empresaId, 0, _environment);

                Dictionary<Int32, List<PedidoView>> query = null;

                RepositorioPedido repositorioPedido = new RepositorioPedido();
                repositorioPedido.DatosSistema = session.Sistema;
                repositorioPedido.ElementosPorPagina = 5000;


                RepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = session.Sistema;
                DRR.Core.DBAlmaNET.Models.Impuesto ingB = repositorioCliente.getImpuestoAlmaNet(900);

                FiltroPedido filtro = new FiltroPedido();
                filtro.TipoEmpresa = session.Sistema.TipoEmpresa;
                filtro.PaginaActual = 1;

                if (rango > 0)
                {
                    filtro.FechaDesde = DateTime.Now.AddDays(-rango);
                }

                if (vendedorId > 0)
                {
                    filtro.VendedorId = vendedorId;
                }

                query = repositorioPedido.ListarPedidos(filtro, ingB);

                return query?.First().Value;
            }
            else
            {
                List<PedidoView> error = new List<PedidoView>();
                error.Add(new PedidoView { NombreCliente = "Error al validar los datos" });
                return error;
            }

        }



        // POST api/<ApiPedidoController>
        [HttpPost]
        [Route("guardarpedidoapp")]
        public Generica GuardarPedidoApp([FromBody] dynamic data)
        {

            #region Capturo el pedido
            Int32 pedidoId = (Int32)data.PedidoId;
            DateTime fecha = (DateTime)data.Fecha;
            Decimal TotalCarrito = (Decimal)data.Total;
            String Cliente = (String)data.Cliente;
            Boolean? Enviado = (Boolean?)data.EnviadoServidor;
            Int32 pedidoWebId = (Int32)data.PedidoWebId_Servidor;
            String Carrito = (String)data.Carrito;

            #endregion

            #region Capturamos los datos del carrito.
            dynamic dataCarrito = JsonConvert.DeserializeObject<ExpandoObject>(Carrito);

            Int16? sectorId = (Int16)dataCarrito.SectorId;

            if(sectorId==0)
            {
                sectorId = null;
            }

            Int32 idCliente = (Int32)dataCarrito.Cliente.ClienteID;
            String Observacion = (String)dataCarrito.Observacion;



            Subtotales subtotales = JsonConvert.DeserializeObject<Subtotales>(JsonConvert.SerializeObject(dataCarrito.Subtotales));

            var listaItem = dataCarrito.ListaItems;
            List<Carrito.ItemCarrito> itemsCarrito = new List<Carrito.ItemCarrito>();

            if (listaItem?.Count > 0)
            {
                foreach (var item in listaItem)
                {
                    Carrito.ItemCarrito elemento = JsonConvert.DeserializeObject<Carrito.ItemCarrito>(JsonConvert.SerializeObject(item));

                    itemsCarrito.Add(elemento);
                }
            }
            #endregion

            #region Configuraciones

            String Configuracion = (String)data.Configuracion;
            dynamic dataConfiguracion = JsonConvert.DeserializeObject<ExpandoObject>(Configuracion);



            Int32 vendedorId = (Int32)dataConfiguracion.VendedorId;
            Int32 empresaId = (Int32)dataConfiguracion.EmpresaId;
            String token = (String)dataConfiguracion.Token;
            String correo = (String)dataConfiguracion.Correo;
            String clave = (String)dataConfiguracion.Clave;
            #endregion

            Boolean verifica = verificarToken(empresaId, token);

            Generica request = new Generica();

            if (verifica == true)
            {
                SessionAcceso session = conectar(empresaId, 0, _environment);
                session.Sistema.SectorId = sectorId;
                IRepositorioPedido repositorioPedido = new RepositorioPedido();
                repositorioPedido.DatosSistema = session.Sistema;

                IRepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = session.Sistema;


                var datosUsuario = repositorioCliente.ObtenerUsuarioWeb_V2(new DatosUsuario
                {
                    Correo = correo,
                    Clave = clave,
                    IdEmpresa = empresaId
                });

                if (datosUsuario != null && !datosUsuario.IdAlmaWeb.IsNullOrValue(0))
                {


                    Carrito CarritoMovil = new Carrito();
                    CarritoMovil.FechaPedido = fecha;
                    CarritoMovil.VendedorId = vendedorId;
                    CarritoMovil.EmpresaId = empresaId;
                    CarritoMovil.Cliente = repositorioCliente.GetCliente(idCliente, false);
                    CarritoMovil.Observacion = Observacion;
                    CarritoMovil.SectorId = sectorId;

                    foreach (var item in itemsCarrito)
                    {
                        CarritoMovil.AgregarItem(item.Producto, item.Cantidad);
                    }

                    ParametroPedido parametro = new ParametroPedido(CarritoMovil, datosUsuario.XmlConfiguracion);
                    parametro.ModalidadRegistroPedido = 1;
                    parametro.Android = true;
                    parametro.ExtraData = new Generica();
                    parametro.ExtraData.Id = pedidoId;
                    parametro.ExtraData.Auxiliar = $"Celular - PedidoId: {pedidoId} - Vendedor: {vendedorId} - Fecha: {fecha.FechaCorta()}";


                    Boolean ok = repositorioPedido.AgregarPedido(parametro);
                    request.Id = CarritoMovil.PedidoId ?? 0;

                }


            }
            else
            {
                request.Id = 0;
                request.Nombre = "Error";
            }






            return request;
        }

        #endregion


        #region COBROS

        // POST api/<ApiPedidoController>
        [HttpPost]
        [Route("guardarcobroapp")]
        public Generica GuardarCobroApp([FromBody] dynamic data)
        {

            #region Capturo el cobro

            Int32 cobroId = (Int32)data.CobroId;
            String vendedor = (String)data.Vendedor;
            DateTime fecha = (DateTime)data.Fecha;

            Decimal total = (Decimal)data.Total;
            String cliente = (String)data.Cliente;

            Boolean? enviadoServidor = (Boolean?)data.EnviadoServidor;
            Int32 cobroWebId_Servidor = (Int32)data.CobroWebId_Servidor;

            String cobroViewModel = (String)data.CobroViewModel;
            String configuracion = (String)data.Configuracion;
            #endregion

            #region CobroViewModel
            dynamic dataCobroViewModel = JsonConvert.DeserializeObject<CobroViewModel>(cobroViewModel);

            #endregion

            #region Configuraciones

            dynamic dataConfiguracion = JsonConvert.DeserializeObject<ExpandoObject>(configuracion);


            Int32 vendedorId = (Int32)dataConfiguracion.VendedorId;
            Int32 empresaId = (Int32)dataConfiguracion.EmpresaId;
            String token = (String)dataConfiguracion.Token;
            String correo = (String)dataConfiguracion.Correo;
            String clave = (String)dataConfiguracion.Clave;

            #endregion

            Boolean verifica = verificarToken(empresaId, token);

            Generica request = new Generica();

            if (verifica == true)
            {
                SessionAcceso session = conectar(empresaId, 0, _environment);



                IRepositorioCobro repositorioCobro = new RepositorioCobro();
                repositorioCobro.DatosSistema = session.Sistema;

                if (session.Usuario.CobradorId.IsNullOrValue(0))
                {
                    session.Usuario.CobradorId = repositorioCobro.GetCobradorId_by_VendedorId(vendedorId);
                }

                IRepositorioCliente repositorioCliente = new RepositorioCliente();
                repositorioCliente.DatosSistema = session.Sistema;


                var datosUsuario = repositorioCliente.ObtenerUsuarioWeb_V2(new DatosUsuario
                {
                    Correo = correo,
                    Clave = clave,
                    IdEmpresa = empresaId
                });

                if (datosUsuario != null && !datosUsuario.IdAlmaWeb.IsNullOrValue(0))
                {


                    OperacionCobroWeb operacionCobro = dataCobroViewModel.CobroWeb;
                    operacionCobro.CobradorId = session.Usuario.CobradorId;
                    operacionCobro.TotalCobro = total;
                    //controlar la data.
                    operacionCobro.FechaComprobante = fecha;

                   var resp = repositorioCobro.AgregarCobro(operacionCobro);
                    request.Id = (int)(resp.Item2?.Id);
                }

            }
            else
            {
                request.Id = 0;
                request.Nombre = "Error";
            }

            return request;
        }

        #endregion


        #region REPARTO

        [HttpPost]
        [Route("getreparto")]
        public EntityTransporteCarga GetReparto([FromBody] dynamic data)
        {
            try
            {
                EntityTransporteCarga entityTransporteCarga = null; 


                Int32 numeroReparto = (Int32)data.NumeroReparto;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;
                String clave = (String)data.Clave;

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {

                    entityTransporteCarga = new EntityTransporteCarga();

                    SessionAcceso session = conectar(empresaId, 0, _environment);

                    IRepositorioPedido repositorioPedido = new RepositorioPedido();

                    repositorioPedido.DatosSistema = session.Sistema;

                    var carga = repositorioPedido.GetReparto(numeroReparto);

                    if(carga!=null)
                    {

                        entityTransporteCarga.MovId = carga.MovId;
                        entityTransporteCarga.MovTipo = carga.MovTipo;
                        entityTransporteCarga.FechaAlta = carga.FechaAlta;
                        entityTransporteCarga.ConductorId = carga.ConductorId;
                        entityTransporteCarga.Descripcion = carga.Descripcion;
                        entityTransporteCarga.ImpVentas = carga.ImpVentas;
                        entityTransporteCarga.TipoOperacionId = carga.TipoOperacionId;
                        entityTransporteCarga.SectorId = carga.SectorId;
                        entityTransporteCarga.EstadoId = carga.EstadoId;
                        entityTransporteCarga.VendedorId = carga.VendedorId;



                        //Obtener entidad sucursal.
                        var query = repositorioPedido.DetalleReparto(numeroReparto).ToList();

                        if (!query.IsNullOrEmpty())
                        {
                            foreach (var item in query)
                            {
                                if (item != null)
                                {
                                    EntityTransporteCarga.EntityReparto viewReparto = new EntityTransporteCarga.EntityReparto();
                                    viewReparto.MovId = item.MovId;
                                    viewReparto.RepartoPedidoId = item.RepartoPedidoId;
                                    viewReparto.TipoOperacionIdpedido = item.TipoOperacionIdpedido;
                                    viewReparto.PedidoVentaId = item.PedidoVentaId;
                                    viewReparto.TipoOperacionIdventa =item.TipoOperacionIdventa;
                                    viewReparto.VentaId = item.VentaId;
                                    viewReparto.Detalle = item.Detalle;

                                    if (item.OperacionVenta!=null)
                                    {
                                        viewReparto.ClienteId = item.OperacionVenta.ClienteId;
                                        try
                                        {
                                            viewReparto.Cliente = item.OperacionVenta.Cliente.EntidadSuc.Entidad.RazonSocial;

                                        }
                                        catch
                                        {
                                            viewReparto.Cliente ="-";
                                        }

                                        viewReparto.Total = item.OperacionVenta.TotalVenta;
                                    }

                                    var queryDetalle = repositorioPedido.DetalleVenta_ModoRemito((int)viewReparto.VentaId);

                                    if (!queryDetalle.IsNullOrEmpty())
                                    {

                                        foreach (var itemDetalle in queryDetalle)
                                        {
                                            EntityTransporteCarga.EntityReparto.ViewDetalleVenta viewDetalle = new EntityTransporteCarga.EntityReparto.ViewDetalleVenta();
                                            viewDetalle.Item = itemDetalle.Item;
                                            viewDetalle.Producto = itemDetalle.Producto;
                                            viewDetalle.Cantidad = itemDetalle.Cantidad;
                                            viewDetalle.PrecioBruto = itemDetalle.PrecioBruto;
                                            viewDetalle.Bonificacion = itemDetalle.Bonificacion;
                                            viewDetalle.Subtotal = itemDetalle.Subtotal;

                                            viewReparto.DetalleVentas.Add(viewDetalle);
                                        }
                                    }

                                    entityTransporteCarga.Repartos.Add(viewReparto);
                                }


                            }
                        }
                    }

                }

                return entityTransporteCarga;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        #endregion


        #endregion


        #region GoogleSheets


        /// <summary>
        /// Sirve para ingresar a la planilla de GoogleSheets
        /// </summary>
        /// <param name="data">
        /// Para poder realizar el login se necesita:
        /// 1) EmpresaID
        /// 2) SheetsID
        /// 3) Correo
        /// 4) Clave
        /// </param>
        /// <returns>
        /// Se devuelve en caso afirmativo:
        /// 1) VendedorID
        /// 2) Nombre
        /// 3) Token
        /// </returns>
        [HttpPost]
        [Route("ingresoappgooglesheets")]
        public String IngresoAppGoogleSheets([FromBody] dynamic data)
        {
            //- usuario y clave.
            try
            {
                dynamic respuesta = new ExpandoObject();


                Int32 empresaId = (Int32)data.EmpresaId;
                //String token = (String)data.Token;
                String sheetsId = (String)data.SheetsId;


                GoogleSheets googleSheets = new GoogleSheets();
                googleSheets.SheetId = sheetsId;
                googleSheets.Service = googleSheets.Authorize_AcountService();

                Configuracion configuracion = googleSheets.Leer_Configuracion(googleSheets.Service, "configuracion!A2:C2").FirstOrDefault();

                if(configuracion!=null)
                {

                    if(configuracion.SheetsId == sheetsId && configuracion.EmpresaId == empresaId)
                    {
                        String correo = data.Correo;
                        String clave = data.Clave;

                        List<LibreriaCoreDRR.GoogleSheets.Usuario> usuarios = googleSheets.Leer_Usuario(googleSheets.Service, "usuario!A2:H1000").ToList();


                        LibreriaCoreDRR.GoogleSheets.Usuario dato = usuarios.FirstOrDefault(c => c.Correo.ToLower() == correo && c.Clave.ToLower() == clave);

                        if(dato!=null)
                        {
                            respuesta.VendedorId = dato.VendedorId;
                            respuesta.Nombre = dato.Nombre;
                            respuesta.Token = configuracion.Token;
                        }
                    }

                }


                String json = JsonConvert.SerializeObject(respuesta);

                return json;
            }
            catch (Exception)
            {
                return "Error";
            }

        }


        /// <summary>
        /// Me devuelve los sectores de la empresa.
        /// </summary>
        /// <param name="data">
        /// Para poder realizar la consulta se necesita:
        /// 1) EmpresaID
        /// 2) SheetsID
        /// 3) Token
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [Route("listarsectoresgooglesheets")]
        public List<LibreriaCoreDRR.GoogleSheets.Sector> ListarSectoresGoogleSheets([FromBody] dynamic data)
        {
            try
            {
                #region Configuraciones


                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;

                String sheetsId = (String)data.SheetsId;
                String rango = (String)data.RangoSheets;
                #endregion

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    List<LibreriaCoreDRR.GoogleSheets.Sector> sectores = googleSheets.Leer_Sector(googleSheets.Service, "sector!A2:B25").ToList();

                    return sectores;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


        /// <summary>
        /// Me devuelve las sucursales de la empresa.
        /// </summary>
        /// <param name="data">
        /// Para poder realizar la consulta se necesita:
        /// 1) EmpresaID
        /// 2) SheetsID
        /// 3) Token
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [Route("listarsucursalesgooglesheets")]
        public List<LibreriaCoreDRR.GoogleSheets.Sucursal> ListarSucursalesGoogleSheets([FromBody] dynamic data)
        {
            try
            {
                #region Configuraciones


                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;

                String sheetsId = (String)data.SheetsId;
                String rango = (String)data.RangoSheets;
                #endregion

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    List<LibreriaCoreDRR.GoogleSheets.Sucursal> sucursales = googleSheets.Leer_Sucursal(googleSheets.Service, "sucursal!A2:B25").ToList();

                    return sucursales;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }



        [HttpGet]
        [Route("getlistarsectoresgooglesheets/{sheetsId}/{empresaId}/{token}")]
        public List<LibreriaCoreDRR.GoogleSheets.Sector> getlistarsectoresgooglesheets(String sheetsId, Int32 empresaId, String token)
        {
            try
            {

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    List<LibreriaCoreDRR.GoogleSheets.Sector> sectores = googleSheets.Leer_Sector(googleSheets.Service, "sector!A2:B10").ToList();

                    return sectores;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }



        [HttpPost]
        [Route("getlistarrubrosgooglesheets")]
        public List<LibreriaCoreDRR.GoogleSheets.Familia> getlistarRubrosGooglesheets([FromBody] dynamic data)
        {
            try
            {

                #region Configuraciones

                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String sheetsId = (String)data.SheetsId;
                String rango = (String)data.RangoSheets;

                #endregion

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    List<LibreriaCoreDRR.GoogleSheets.Familia> rubros = googleSheets.Leer_Rubro(googleSheets.Service, "rubro!A2:C2500").ToList();

                    return rubros;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


        [HttpPost]
        [Route("getlistarmarcasgooglesheets")]
        public List<LibreriaCoreDRR.GoogleSheets.Marca> getListarMarcasGooglesheets([FromBody] dynamic data)
        {
            try
            {
                #region Configuraciones

                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String sheetsId = (String)data.SheetsId;
                String rango = (String)data.RangoSheets;

                #endregion

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    List<LibreriaCoreDRR.GoogleSheets.Marca> marcas = googleSheets.Leer_Marca(googleSheets.Service, "marca!A2:B2500").ToList();

                    return marcas;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }




        [HttpPost]
        [Route("listarclientesgooglesheets")]
        public List<LibreriaCoreDRR.GoogleSheets.Cliente> ListarClientesGoogleSheets([FromBody] dynamic data)
        {
            try
            {

                #region Configuraciones

                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String sheetsId = (String)data.SheetsId;
                String rango = (String)data.RangoSheets;

                DateTime? obtenerModificadosDesde = (DateTime?)data.ObtenerModificadosDesde;

                #endregion

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {

                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    if(String.IsNullOrEmpty( rango))
                    {
                        rango = "cliente!A2:AM50000";
                    }

                    List<LibreriaCoreDRR.GoogleSheets.Cliente> clientes = googleSheets.Leer_Cliente(googleSheets.Service, rango).ToList();

                    if(obtenerModificadosDesde != null)
                    {
                        if(!clientes.IsNullOrEmpty())
                        {
                            clientes = clientes.Where(c => c.FechaModificacion?.Date >= obtenerModificadosDesde?.Date).ToList();
                        }
                    }

                    return clientes;

                }

                return null;

            }
            catch (Exception ex)
            {
                return null;
            }

        }


        [HttpPost]
        [Route("listarProductosGoogleSheets")]
        public List<LibreriaCoreDRR.GoogleSheets.Producto> ListarProductosGoogleSheets([FromBody] dynamic data)
        {

            try
            {

                #region Configuraciones

                Int32 vendedorId = (Int32)data.VendedorId;
                Int32 empresaId = (Int32)data.EmpresaId;
                String token = (String)data.Token;
                String correo = (String)data.Correo;

                String sheetsId = (String)data.SheetsId;
                String rango = (String)data.RangoSheets;

                DateTime? obtenerModificadosDesde = (DateTime?)data.ObtenerModificadosDesde;

                #endregion


                Boolean verifica = verificarToken(empresaId, token);
                List<LibreriaCoreDRR.GoogleSheets.Producto> lista = new List<LibreriaCoreDRR.GoogleSheets.Producto>();

                if (verifica == true)
                {

                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();


                    if(String.IsNullOrEmpty(rango))
                    {
                        rango = "producto!A2:BD100000";
                    }

                    List<LibreriaCoreDRR.GoogleSheets.Producto> productos = googleSheets.Leer_Producto(googleSheets.Service, rango).ToList();

                    if (obtenerModificadosDesde != null)
                    {
                        if (!productos.IsNullOrEmpty())
                        {


                            productos = productos.Where(c => c.FechaActualizacion?.Date >= obtenerModificadosDesde?.Date).ToList();
                        }
                    }

                    return productos;

                }
                else
                {

                    lista.Add(new LibreriaCoreDRR.GoogleSheets.Producto { NombreCompleto = "Los datos de verificacion no son correctas." });

                }



                return lista;
            }
            catch (Exception ex)
            {
                List<LibreriaCoreDRR.GoogleSheets.Producto> lista = new List<LibreriaCoreDRR.GoogleSheets.Producto>();
                lista.Add(new LibreriaCoreDRR.GoogleSheets.Producto { NombreCompleto = ex.Message });

                return lista;
            }


        }


        [HttpPost]
        [Route("guardarpedidoappgooglesheets")]
        public Generica GuardarPedidoAppGoogleSheets([FromBody] dynamic data)
        {

            #region Capturo el pedido
            Int32 pedidoId = (Int32)data.PedidoId;
            DateTime fecha = (DateTime)data.Fecha;
            Decimal TotalCarrito = (Decimal)data.Total;
            String Cliente = (String)data.Cliente;
            Boolean? Enviado = (Boolean?)data.EnviadoServidor;
            Int32 pedidoWebId = (Int32)data.PedidoWebId_Servidor;
            String Carrito = (String)data.Carrito;

            #endregion


            #region Configuraciones

            String Configuracion = (String)data.Configuracion;
            dynamic dataConfiguracion = JsonConvert.DeserializeObject<ExpandoObject>(Configuracion);

            Int32 vendedorId = (Int32)dataConfiguracion.VendedorId;
            Int32 empresaId = (Int32)dataConfiguracion.EmpresaId;
            String token = (String)dataConfiguracion.Token;
            String correo = (String)dataConfiguracion.Correo;
            String clave = (String)dataConfiguracion.Clave;

            String sheetsId = (String)dataConfiguracion.SheetsId;
            String rango = (String)dataConfiguracion.RangoSheets;
            #endregion

            Boolean verifica = verificarToken(empresaId, token);

            Generica request = new Generica();

            if (verifica == true)
            {
                LibreriaCoreDRR.GoogleSheets.Pedido nuevoPedido = new Pedido();
                nuevoPedido.PedidoId = pedidoId;
                nuevoPedido.Fecha = fecha;
                nuevoPedido.Total = TotalCarrito;
                nuevoPedido.Cliente = Cliente;
                nuevoPedido.Vendedor = "";
                nuevoPedido.Carrito = Carrito;
                nuevoPedido.Configuracion = Configuracion;

                GoogleSheets googleSheets = new GoogleSheets();
                googleSheets.SheetId = sheetsId;
                googleSheets.Service = googleSheets.Authorize_AcountService();


                List<Pedido> lista = new List<Pedido>();
                lista.Add(nuevoPedido);

                var datatos = googleSheets.Generar_Datos_Pedido(lista);

                googleSheets.Agregar(datatos, googleSheets.SheetId, "pedido!A2", googleSheets.Service);

                request.Id = 1;
                request.Nombre = "ok";

            }
            else
            {
                request.Id = 0;
                request.Nombre = "Error";
            }






            return request;
        }




        [HttpGet]
        [Route("listarfamiliasgooglesheets/{sheetsId}/{empresaId}/{token}")]
        public IEnumerable<Familia> ListarFamiliasGoogleSheets(String sheetsId, Int32 empresaId, String token)
        {

            try
            {



                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    
                      String  rango = "rubro!A2:C1000";
                    

                    List<LibreriaCoreDRR.GoogleSheets.Familia> rubros = googleSheets.Leer_Rubro(googleSheets.Service, rango).ToList();

                    return rubros;
                }
                else
                {
                    return null;
                }



            }
            catch (Exception ex)
            {
                return null;
            }
        }


        [HttpGet]
        [Route("listarmarcasgooglesheets/{sheetsId}/{empresaId}/{token}")]
        public IEnumerable<Marca> ListarMarcasGoogleSheets(String sheetsId, Int32 empresaId, String token)
        {

            try
            {

                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    string rango = "marca!A2:B1000";
                    

                    List<LibreriaCoreDRR.GoogleSheets.Marca> marcas = googleSheets.Leer_Marca(googleSheets.Service, rango).ToList();

                    return marcas;

                }
                else
                {
                    return null;
                }



            }
            catch (Exception ex)
            {
                return null;
            }
        }



        // POST api/<ApiPedidoController>
        [HttpPost]
        [Route("guardarcobrogooglesheets")]
        public Generica GuardarCobroGoogleSheets([FromBody] dynamic data)
        {

            #region Capturo el cobro

            Int32 cobroId = (Int32)data.CobroId;
            String vendedor = (String)data.Vendedor;
            DateTime fecha = (DateTime)data.Fecha;

            Decimal total = (Decimal)data.Total;
            String cliente = (String)data.Cliente;

            Boolean? enviadoServidor = (Boolean?)data.EnviadoServidor;
            Int32 cobroWebId_Servidor = (Int32)data.CobroWebId_Servidor;

            String cobroViewModel = (String)data.CobroViewModel;
            String configuracion = (String)data.Configuracion;
            Int32? numeroReparto = (Int32?)data.NumeroRearto;
            #endregion

            #region CobroViewModel
            dynamic dataCobroViewModel = JsonConvert.DeserializeObject<CobroViewModel>(cobroViewModel);

            #endregion

            #region Configuraciones

            dynamic dataConfiguracion = JsonConvert.DeserializeObject<ExpandoObject>(configuracion);


            Int32 vendedorId = (Int32)dataConfiguracion.VendedorId;
            Int32 empresaId = (Int32)dataConfiguracion.EmpresaId;
            String token = (String)dataConfiguracion.Token;
            String correo = (String)dataConfiguracion.Correo;
            String clave = (String)dataConfiguracion.Clave;

            String sheetsId = (String)dataConfiguracion.SheetsId;
            String rango = (String)dataConfiguracion.RangoSheets;

            #endregion

            Boolean verifica = verificarToken(empresaId, token);

            Generica request = new Generica();

            if (verifica == true)
            {
                Cobro nuevoCobro = new Cobro();
                nuevoCobro.CobroId=cobroId;
                nuevoCobro.Vendedor=vendedor;
                nuevoCobro.Fecha=fecha;
                nuevoCobro.Total=total;
                nuevoCobro.Cliente=cliente;
                nuevoCobro.EnviadoServidor=enviadoServidor;
                nuevoCobro.CobroWebId_Servidor=cobroWebId_Servidor;
                nuevoCobro.CobroViewModel=cobroViewModel;
                nuevoCobro.Configuracion=configuracion;
                nuevoCobro.NumeroReparto=numeroReparto;

                GoogleSheets googleSheets = new GoogleSheets();
                googleSheets.SheetId = sheetsId;
                googleSheets.Service = googleSheets.Authorize_AcountService();

                List<Cobro> lista = new List<Cobro>();
                lista.Add(nuevoCobro);

                var datatos = googleSheets.Generar_Datos_Cobro(lista);

                googleSheets.Agregar(datatos, googleSheets.SheetId, "cobro!A2", googleSheets.Service);

                request.Id = 1;
                request.Nombre = "ok";

            }
            else
            {
                request.Id = 0;
                request.Nombre = "Error";
            }

            return request;
        }





        [HttpGet]
        [Route("getlistaspreciosgooglesheets/{sheetsId}/{empresaId}/{token}")]
        public List<ListaPrecio> GetListasPreciosGoogleSheets(String sheetsId, Int32 empresaId, String token)
        {
            try
            {
                Boolean verifica = verificarToken(empresaId, token);

                if (verifica == true)
                {
                    GoogleSheets googleSheets = new GoogleSheets();
                    googleSheets.SheetId = sheetsId;
                    googleSheets.Service = googleSheets.Authorize_AcountService();

                    string rango = "listaprecio!A2:B20";


                    List<LibreriaCoreDRR.GoogleSheets.ListaPrecio> listaPrecios = googleSheets.Leer_ListasPrecios(googleSheets.Service, rango).ToList();

                    return listaPrecios;

                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }




        [HttpPost]
        [Route("guardarcolectorappgooglesheets")]
        public Generica GuardarColectorAppGoogleSheets([FromBody] dynamic data)
        {

            //String configuracion = (String)data.Configuracion;
            //String colector = (String)data.Colector;

            #region Configuraciones

            //dynamic dataConfiguracion = JsonConvert.DeserializeObject<ExpandoObject>(configuracion);

            Int32 vendedorId = (Int32)data.Configuracion.VendedorId;
            Int32 empresaId = (Int32)data.Configuracion.EmpresaId;
            String token = (String)data.Configuracion.Token;
            String correo = (String)data.Configuracion.Correo;
            String clave = (String)data.Configuracion.Clave;

            String sheetsId = (String)data.Configuracion.SheetsId;
            String rango = (String)data.Configuracion.RangoSheets;
            #endregion

            Boolean verifica = verificarToken(empresaId, token);

            Generica request = new Generica();

            if (verifica == true)
            {
                LibreriaCoreDRR.GoogleSheets.Colector nuevoColector = new Colector();
                nuevoColector = JsonConvert.DeserializeObject<Colector>(data.Colector.ToString());


                GoogleSheets googleSheets = new GoogleSheets();
                googleSheets.SheetId = sheetsId;
                googleSheets.Service = googleSheets.Authorize_AcountService();


                List<Colector> lista = new List<Colector>();
                lista.Add(nuevoColector);

                var datatos = googleSheets.Generar_Datos_Colector(lista);

                googleSheets.Agregar(datatos, googleSheets.SheetId, "colector!A2", googleSheets.Service);

                request.Id = 1;
                request.Nombre = "ok";

            }
            else
            {
                request.Id = 0;
                request.Nombre = "Error";
            }






            return request;
        }


        #endregion


    }
}
