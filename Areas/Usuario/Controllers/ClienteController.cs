using DRR.Core.DBAlmaNET.Models;
using DRR.Core.DBEmpresaEjemplo.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Usuario;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace WebDRR.Areas.Usuario.Controllers
{
    [Area("Usuario")]
    [Route("[controller]/[action]")]
    public class ClienteController : Controller
    {
        #region Variables
        private SessionAcceso _session;
        IRepositorioCliente _repositorioCliente;
        private IMemoryCache _memoryCache;
        private MemoryCacheEntryOptions _cacheEntryOptions;
        #endregion


        #region Constructor
        public ClienteController(IRepositorioCliente repositorioCliente, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
        {
            //Se obtiene el 
            _repositorioCliente = repositorioCliente;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioCliente.DatosSistema = _session?.Sistema;


            _repositorioCliente.ElementosPorPagina = 24;

            _memoryCache = memoryCache;
            _cacheEntryOptions = new MemoryCacheEntryOptions();
            _cacheEntryOptions.AbsoluteExpiration = DateTimeOffset.Now.AddMonths(4);
            _cacheEntryOptions.Priority = CacheItemPriority.Normal;

        }
        #endregion




        public IActionResult ListarClientes(FiltroCliente filtro)
        {

            if (filtro == null)
            {
                filtro = new FiltroCliente();

            }

            String urlAtras = HttpContext.Request.UrlAtras();

            filtro.SectorId = _session.Sistema.SectorId;
            filtro.Representada = _session.Sistema.TipoEmpresa == 256 ? true : false;

            if (_session.Usuario.Rol == (Int32)EnumRol.Vendedor)
            {
                if(_session.Usuario.XmlConfiguracion.VendedorVisualizaTodosLosCliente == true)
                {
                    filtro.VendedorId = null;
                }
                else
                {
                    filtro.VendedorId = _session.Usuario.Cliente_Vendedor_Id;
                }
                

            }

            ViewModelCliente item = new ViewModelCliente();

            if (filtro.PaginaActual == 0)
            {
                filtro.PaginaActual = 1;
            }


            if (filtro?.EnumFiltro == (Int32)FiltroCliente.EnumFiltroCliente.Soporte_AgregarContacto)
            {
                TempData["ErrorRepresentada"] = "Seleccione el cliente para el cual se va agregar el Contacto.";
            }



            ViewData["Session"] = _session;

            //PaisProvCiudad
            ViewModelPaisProvinciaCiudad viewModelPaisProvinciaCiudad = new ViewModelPaisProvinciaCiudad();
            RepositorioPais repositorioPais = new RepositorioPais();
            repositorioPais.DatosSistema = _session.Sistema;
            viewModelPaisProvinciaCiudad.ListaPaises = repositorioPais.GetPais(null);
            ViewBag.ViewModelPaisProvinciaCiudad = viewModelPaisProvinciaCiudad;

            //07/12/2021

            var itemConf = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.VerSoloClientesDelVendedor);

            if (itemConf?.Valor.MostrarEntero() == 1)
            {
                filtro.SoloClientesConVendedorId = true;
            }


            var itemBusquedaOR = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Buscar_Clientes_Usando_Or_Logico);

            if (itemBusquedaOR?.Valor.MostrarEntero() == 1)
            {
                filtro.BuscarUsando_Or_Logico = true;
            }



            if (_session.Usuario.Rol == (Int32)EnumRol.Vendedor || _session.Usuario.Rol == (Int32)EnumRol.Administrador)
            {
                #region Empresa - Multiempresa
                String info = "";

                Dictionary<Int32, List<ViewCliente>> query = null;
                if (filtro.Representada == false)
                {
                    if(_session.ModoFull_Cliente_Productos == true)
                    {
                        SessionFull_Clientes_Productos datosFull = SessionFull_Clientes_Productos.RecuperarSession(HttpContext);
                        if(datosFull!=null && datosFull.Clientes?.Count()>0)
                        {
                            filtro.FullData = true;
                            query = _repositorioCliente.ClientesVendedorV2(filtro, out info, data:datosFull.Clientes);
                        }
                        else
                        {
                            query = _repositorioCliente.ClientesVendedorV2(filtro, out info);
                        }
                    }
                    else
                    {
                        query = _repositorioCliente.ClientesVendedorV2(filtro, out info);
                    }
                   
                }
                else
                {
                    query = _repositorioCliente.ClientesRepresentadaV2(filtro, out info);
                }


                #region Zonas
                var listaZonas = _repositorioCliente.ListarZonas(out info);

                List<SelectListItem> selectList = new List<SelectListItem>();
                foreach (var elementos in listaZonas)
                {
                    SelectListItem select = new SelectListItem();
                    select.Value = elementos.Id.ToString();
                    select.Text = "[" + elementos.Id + "] " + elementos.Nombre + " - N°: " + elementos.UrlRetorno;
                    selectList.Add(select);
                }

                ViewData["ListaZonas"] = selectList;
                #endregion



                #region Clasificacion
                var listaClasificacion = _repositorioCliente.ListarClasificaciones();

                List<SelectListItem> selectListClasificaciones = new List<SelectListItem>();
                foreach (var elementos in listaClasificacion)
                {
                    SelectListItem select = new SelectListItem();
                    select.Value = elementos.ClasificacionId.ToString();
                    select.Text = elementos.Descripcion;
                    selectListClasificaciones.Add(select);
                }

                ViewData["ListaClasificacionClientes"] = selectListClasificaciones;
                #endregion

                int cantidad = 0;

                try
                {
                    cantidad = query?.First().Key ?? 0;

                    item.Lista = query?.First().Value ?? null;
                }
                catch (Exception ex)
                {
                    cantidad = 0;
                    item.Lista = new List<ViewCliente>();
                }



                if (String.IsNullOrEmpty(filtro.UrLRetorno))
                {
                    filtro.UrLRetorno = urlAtras;
                }


                item.Filtro = filtro;

                if (filtro.BusquedaCliente == true)
                {
                    if (cantidad == 1)
                    {
                        var carrito = SessionCarrito.getCarrito(HttpContext.Session);

                        ViewCliente cliente = item.Lista[0];

                        if (cliente.SaldoCtaCte == 0)
                        {
                            var querySaldo = _repositorioCliente.GetSaldo(cliente.EntidadSucId, (Int32)_session?.Sistema.EmpresaId);

                            if (querySaldo?.Count() > 0)
                            {
                                cliente.SaldoCtaCte = querySaldo.Sum(c => c.SaldoCtaCte);
                            }
                        }


                        carrito?.setCliente(cliente);

                        String redireccionar = "/Carrito/Index";

                        return Redirect(redireccionar);
                    }
                }


                item.Paginacion = new Paginacion
                {
                    PaginaActual = filtro.PaginaActual,
                    ElementosPorPagina = _repositorioCliente.ElementosPorPagina,
                    Elementos = cantidad
                };

                return View(item);

                #endregion
            }
            else
            {
                TempData["ErrorRepresentada"] = "El rol que tiene no le da permisos para operar en esta pantalla";

                String redireccionar = Url.Action("Principal", "Home");

                return Redirect(redireccionar);
            }
        }


        public IActionResult FiltroCiudad(int ciudadId)
        {
            FiltroCliente filtro = new FiltroCliente();
            filtro.CiudadId = ciudadId;
            return RedirectToAction("ListarClientes", filtro);
        }

        public IActionResult VerCliente(int clienteId, String urlAtras)
        {
            ViewCliente cliente = new ViewCliente();

            cliente = _repositorioCliente.GetCliente(clienteId);

            if (!String.IsNullOrEmpty(urlAtras))
            {
                ViewBag.urlAtras = urlAtras; //HttpContext.Request.UrlAtras();
            }
            else
            {
                ViewBag.urlAtras = Url.Action("ListarClientes", "Cliente");  //HttpContext.Request.UrlAtras();
            }


            var puedeEditar = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (int)ConfPortal.EnumConfPortal.Vendedor_EditarDatos_Cliente);

            if (puedeEditar?.Valor.MostrarEntero() == 1)
            {
                ViewBag.permisosEditar = true;
            }
            else
            {
                ViewBag.permisosEditar = false;
            }


            return View(cliente);

        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AgregarCliente_Carrito(ViewCliente view, String urlAtras = "")
        {
            IFormCollection collection = this.HttpContext.Request.Form;
            if (urlAtras == "")
            {
                if (collection.ContainsKey("urlAtras"))
                {
                    urlAtras = collection["urlAtras"];
                }
            }

            var carrito = SessionCarrito.getCarrito(HttpContext.Session);

            if (view.SaldoCtaCte == 0)
            {
                Int16 sector = _session.Sistema.SectorId ?? 0;

                var query = _repositorioCliente.GetSaldo(view.EntidadSucId, (Int32)_session?.Sistema.EmpresaId, sector);

                if (query?.Count() > 0)
                {
                    //Cambio: de Total a SaldoCtaCte
                    view.SaldoCtaCte = query.Sum(c => c.SaldoCtaCte + c.Adelanto);
                }
            }

            view.ListaPrecID_Nombre = _repositorioCliente.ObtenerNombre_ListaPrecio(view.ListaPrecID);

            //Cuando se selecciona 1 cliente traigo los impuesto del mismo.
            var impuestoCliente = _repositorioCliente.Obtener_Impuestos(view.EntidadSucId);
            Int16 iibb = 900;

            if (impuestoCliente?.Count() > 0)
            {
                EntidadSucursalImpuesto impuestoIIBB = impuestoCliente.FirstOrDefault(c => c.ImpuestoId == iibb);

                if (impuestoIIBB != null)
                {

                    if (impuestoIIBB.EximidoHastaFecha != null)
                    {

                        if (impuestoIIBB.EximidoHastaFecha < DateTime.Now)
                        {
                            var impDatos = _repositorioCliente.getImpuestoAlmaNet(impuestoIIBB.ImpuestoId);

                            view.Impuesto = new LibreriaBase.Clases.Impuesto();
                            view.Impuesto.ImpuestoID = impDatos.ImpuestoId;
                            view.Impuesto.Nombre = impDatos.DescripcionImpuesto;
                            view.Impuesto.PorcentajeAlicuota = impDatos.PocentajeDeducir ?? 0;
                            view.Impuesto.ImporteMinimo = impDatos.MontoMinimo ?? 0;
                        }

                    }
                    else
                    {
                        var impDatos = _repositorioCliente.getImpuestoAlmaNet(impuestoIIBB.ImpuestoId);

                        view.Impuesto = new LibreriaBase.Clases.Impuesto();
                        view.Impuesto.ImpuestoID = impDatos.ImpuestoId;
                        view.Impuesto.Nombre = impDatos.DescripcionImpuesto;
                        view.Impuesto.PorcentajeAlicuota = impDatos.PocentajeDeducir ?? 0;
                        view.Impuesto.ImporteMinimo = impDatos.MontoMinimo ?? 0;
                    }

                }

            }


            ///OBSOLETO----



            carrito?.setCliente(view);

            string host = @"https://" + HttpContext.Request.Host.Value + "/";

            if (String.IsNullOrEmpty(urlAtras) || host == urlAtras)
            {
                String redireccionar = Url.Action("Productos", "Producto");//  "/Carrito/Index";

                return Redirect(redireccionar);
            }
            else
            {
                if(urlAtras.Contains("ListarClientes"))
                {
                    return RedirectToAction("Principal", "Home");
                }
                else
                {
                    return Redirect(urlAtras);
                }
               
            }


        }



        [HttpGet]
        //[ValidateAntiForgeryToken]
        public IActionResult EstadoCta(int id, string rz)
        {
            ViewCliente cliente = new ViewCliente();
            cliente.EntidadSucId = id;
            cliente.RazonSocial = rz;

            cliente.LimiteCredito = _repositorioCliente.GetLimiteCredito_Cliente(id);

            Int16 sector = _session.Sistema.SectorId ?? 0;

            List<ViewEstadoCuenta> lista = _repositorioCliente.GetSaldo((Int32)cliente?.EntidadSucId, (Int32)_session.Sistema.EmpresaId, sector);

            if (lista?.Count() > 0)
            {
                if (_session?.Usuario?.Rol == (Int32)EnumRol.ClienteFidelizado)
                {
                    var confCtaCteFidelizados = _session?.Configuracion?.ConfiguracionesPortal?.FirstOrDefault(c => c.Codigo == (Int32)ConfPortal.EnumConfPortal.CuentaCorriente_ClienteFidelizado_TodoElMovimiento);

                    if (confCtaCteFidelizados.Valor.MostrarEntero() == 0)
                    {
                        lista = lista?.Where(c => c.ComprobanteID != 35).ToList();
                    }

                }
            }





            ViewData["LimiteCredito"] = cliente?.LimiteCredito;
            ViewData["EntidadSucId"] = cliente?.EntidadSucId;
            ViewData["RazonSocial"] = cliente?.RazonSocial;
            ViewData["urlRetorno"] = HttpContext.Request.UrlAtras();

            return View("MiEstadoDeCuenta", lista);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult MiEstadoDeCuenta(ViewCliente cliente = null)
        {
            try
            {
                ViewData["urlRetorno"] = HttpContext.Request.Headers["Referer"].ToString();




                if (cliente?.EntidadSucId == 0)
                {
                    List<ViewEstadoCuenta> lista = _repositorioCliente.GetSaldo((Int32)_session.Usuario.EntidadSucId, (Int32)_session.Sistema.EmpresaId);
                    ViewData["EntidadSucId"] = (Int32)_session.Usuario.EntidadSucId;
                    return View(lista);
                }
                else
                {
                    //Obsoleto.

                    List<ViewEstadoCuenta> lista = _repositorioCliente.GetSaldo((Int32)cliente?.EntidadSucId, (Int32)_session.Sistema.EmpresaId);

                    ViewData["EntidadSucId"] = (Int32)cliente?.EntidadSucId;
                    ViewData["RazonSocial"] = cliente?.RazonSocial;

                    return View(lista);
                }

            }
            catch (Exception)
            {
                return View(null);
            }
        }

        public IActionResult GenerarPdfComprobante(int registroOperacion)
        {

            using (MemoryStream stream = new System.IO.MemoryStream())
            {
                //This code is responsible for initialize the PDF document object.
                Document pdfDoc = new Document(PageSize.A4, 1f, 1f, 1f, 1f);


                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();


                // int numVenta =Convert.ToInt32(formulario["RegistroOperacionID"]);

                string info = "";
                var entidad = _repositorioCliente.GetComprobante_Archivo(registroOperacion, 1, out info);


                if (entidad != null)
                {
                    //This code is responsible for to add the Image file to the PDF document object.
                    iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(entidad.Archivo);
                    img.ScalePercent(75f);
                    pdfDoc.Add(img);
                    pdfDoc.Close();
                }



                //This code is responsible for download the PDF file.
                //return File(stream.ToArray(), "application/pdf", "Comprobante.pdf");

                return File(stream.ToArray(), "application/pdf");
            }

        }

        public IActionResult GenerarPdfEstadoDeCuenta(Int32 entidadSucId, String cliente)
        {

            List<ViewEstadoCuenta> lista;

            lista = _repositorioCliente.GetSaldo(entidadSucId, (Int32)_session.Sistema.EmpresaId);


            //Secrea el documento
            Document doc = new Document(PageSize.A4);
            doc.SetMargins(20f, 20f, 20f, 20f);
            System.IO.MemoryStream ms = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(doc, ms);

            //Las cuestiones basicas.
            doc.AddAuthor("Estado de Cuenta");
            //doc.AddTitle("Cliente :" + cliente.RazonSocial);
            doc.Open();


            #region Datos de cabecera
            Paragraph para1 = new Paragraph();
            Phrase ph2 = new Phrase();
            Paragraph mm1 = new Paragraph();

            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk("Estado de Cuenta", FontFactory.GetFont("Arial", 20, 2)));
            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk("Cliente: " + cliente, FontFactory.GetFont("Arial", 16, 2)));
            ph2.Add(new Chunk(Environment.NewLine));
            ph2.Add(new Chunk(Environment.NewLine));
            mm1.Add(ph2);
            para1.Add(mm1);
            doc.Add(para1);

            #endregion



            BaseFont helvetica = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1250, true);

            iTextSharp.text.Font negrita = new iTextSharp.text.Font(helvetica, 9f, iTextSharp.text.Font.BOLD, new BaseColor(0, 0, 0));


            #region Esquema basico de la tabla

            PdfPTable tabla = new PdfPTable(new float[] { 10f, 25f, 25f, 15f, 15f, 10f }) { WidthPercentage = 100f };
            PdfPCell c1 = new PdfPCell(new Phrase("Operación", negrita));
            PdfPCell c2 = new PdfPCell(new Phrase("Fecha", negrita));
            PdfPCell c3 = new PdfPCell(new Phrase("Comprobante", negrita));
            PdfPCell c4 = new PdfPCell(new Phrase("Total", negrita));
            PdfPCell c5 = new PdfPCell(new Phrase("Saldo", negrita));
            PdfPCell c6 = new PdfPCell(new Phrase("Adelanto", negrita));

            c1.Border = 0;
            c2.Border = 0;
            c3.Border = 0;
            c4.Border = 0;
            c5.Border = 0;
            c6.Border = 0;

            c4.HorizontalAlignment = Element.ALIGN_RIGHT;
            c5.HorizontalAlignment = Element.ALIGN_RIGHT;
            c6.HorizontalAlignment = Element.ALIGN_RIGHT;

            tabla.AddCell(c1);
            tabla.AddCell(c2);
            tabla.AddCell(c3);
            tabla.AddCell(c4);
            tabla.AddCell(c5);
            tabla.AddCell(c6);
            #endregion

            #region se carga los datos de la tabla

            DatoConfiguracion datoItem =  _session.Configuracion.ConfiguracionesPortal.FirstOrDefault(c => c.Codigo == 12);
            Boolean presiosFinales = true;
            if (datoItem!= null)
            {
                if(datoItem.Valor !=0)
                {
                    presiosFinales = false;
                }
            }
                        
            Int32 i = 0;
            for (i = 0; i < lista.Count(); i++)
            {
                if (i % 2 == 0)
                {
                    c1.BackgroundColor = new BaseColor(204, 204, 204);
                    c2.BackgroundColor = new BaseColor(204, 204, 204);
                    c3.BackgroundColor = new BaseColor(204, 204, 204);
                    c4.BackgroundColor = new BaseColor(204, 204, 204);
                    c5.BackgroundColor = new BaseColor(204, 204, 204);
                    c6.BackgroundColor = new BaseColor(204, 204, 204);
                }
                else
                {
                    c1.BackgroundColor = BaseColor.White;
                    c2.BackgroundColor = BaseColor.White;
                    c3.BackgroundColor = BaseColor.White;
                    c4.BackgroundColor = BaseColor.White;
                    c5.BackgroundColor = new BaseColor(204, 204, 204);
                    c6.BackgroundColor = new BaseColor(204, 204, 204);
                }

                //Se obtiene el item del carrito
                var item = lista.ElementAt(i);

                //Se crea una nueva fila con los datos-
                c1.Phrase = new Phrase(item.TipoOperacion);

                c2.Phrase = new Phrase(item.FechaComprobante.ToString());

                c3.Phrase = new Phrase(item.Comprobante);
                c4.Phrase = new Phrase(item.Total.FormatoMoneda());
                c5.Phrase = new Phrase(item.SaldoCtaCte.FormatoMoneda());
                c6.Phrase = new Phrase(item.Adelanto.FormatoMoneda());


                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(c3);
                tabla.AddCell(c4);
                tabla.AddCell(c5);
                tabla.AddCell(c6);
            }




            c1.Colspan = 4;
            c1.Phrase = new Phrase("");
            c1.HorizontalAlignment = Element.ALIGN_LEFT;
            c1.Phrase = new Phrase("SALDO TOTAL: ");

            tabla.AddCell(c1);

            c2.Colspan = 5;
            c2.Phrase = new Phrase("");
            c2.HorizontalAlignment = Element.ALIGN_RIGHT;
            c2.Phrase = new Phrase(lista.Sum(c => c.SaldoCtaCte + c.Adelanto).FormatoMoneda());

            tabla.AddCell(c2);
            #endregion
            doc.Add(tabla);

            writer.Close();
            doc.Close();
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms, "application/pdf");

        }



        public IActionResult MapaCliente(Int32 clienteId)
        {
            ViewCliente cliente = new ViewCliente();

            cliente = _repositorioCliente.GetCliente(clienteId);
            ViewBag.urlAtras = HttpContext.Request.UrlAtras();
            return View(cliente);
        }



        public IActionResult Mapa(String data)
        {

            //if (_session.Usuario.Rol == (Int32)EnumRol.Vendedor || _session.Usuario.Rol == (Int32)EnumRol.Administrador)
            //{
            //    #region Empresa - Multiempresa
            //    String info = "";

            //    Dictionary<Int32, List<ViewCliente>> query = null;
            //    if (filtro.Representada == false)
            //    {
            //        query = _repositorioCliente.ClientesVendedorV2(filtro, out info);
            //    }
            //    else
            //    {
            //        query = _repositorioCliente.ClientesRepresentadaV2(filtro, out info);
            //    }

            //    var listaZonas = _repositorioCliente.ListarZonas(out info);

            //    List<SelectListItem> selectList = new List<SelectListItem>();
            //    foreach (var elementos in listaZonas)
            //    {
            //        SelectListItem select = new SelectListItem();
            //        select.Value = elementos.Id.ToString();
            //        select.Text = "[" + elementos.Id + "] " + elementos.Nombre + " - N°: " + elementos.UrlRetorno;
            //        selectList.Add(select);
            //    }

            //    ViewData["ListaZonas"] = selectList;


            //    int cantidad = 0;

            //    try
            //    {
            //        cantidad = query?.First().Key ?? 0;

            //        item.Lista = query?.First().Value ?? null;
            //    }
            //    catch (Exception ex)
            //    {
            //        cantidad = 0;
            //        item.Lista = new List<ViewCliente>();
            //    }



            //    if (String.IsNullOrEmpty(filtro.UrLRetorno))
            //    {
            //        filtro.UrLRetorno = urlAtras;
            //    }


            //    item.Filtro = filtro;

            //    if (filtro.BusquedaCliente == true)
            //    {
            //        if (cantidad == 1)
            //        {
            //            var carrito = SessionCarrito.getCarrito(HttpContext.Session);

            //            ViewCliente cliente = item.Lista[0];

            //            if (cliente.SaldoCtaCte == 0)
            //            {
            //                var querySaldo = _repositorioCliente.GetSaldo(cliente.EntidadSucId, (Int32)_session?.Sistema.EmpresaId);

            //                if (querySaldo?.Count() > 0)
            //                {
            //                    cliente.SaldoCtaCte = querySaldo.Sum(c => c.SaldoCtaCte);
            //                }
            //            }


            //            carrito?.setCliente(cliente);

            //            String redireccionar = "/Carrito/Index";

            //            return Redirect(redireccionar);
            //        }
            //    }


            //    item.Paginacion = new Paginacion
            //    {
            //        PaginaActual = filtro.PaginaActual,
            //        ElementosPorPagina = _repositorioCliente.ElementosPorPagina,
            //        Elementos = cantidad
            //    };

            //    return View(item);

            //    //}
            //    //else
            //    //{
            //    //    return Redirect(urlAtras);
            //    //}
            //    #endregion
            //}


            return View();
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GuardarMapaCliente(Int32 clienteId, decimal lat, decimal log)
        {
            Boolean guardo = _repositorioCliente.ActualizarLat_long(clienteId, lat, log);

            if (guardo == true)
            {
                if ((bool)_session?.Usuario.NumeroReparto.IsNullOrValue(0))
                {
                    return RedirectToAction("VerCliente", "Cliente", new { clienteId = clienteId, urlAtras = "" });
                }
                else
                {
                    return RedirectToAction("DetalleReparto", "Reparto");
                }
            }
            else
            {
                return RedirectToAction("ListarClientes");
            }
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GuardarDatos_EdicionCliente(Int32 idCliente, String direccion, String telefono, String correo, String datosRetiro)
        {

            Boolean guardo = _repositorioCliente.GuardarDatos_EdicionCliente(idCliente, direccion, telefono, correo, datosRetiro);

            if (guardo == true)
            {
                return RedirectToAction("ListarClientes");

            }
            else
            {
                return RedirectToAction("VerCliente", "Cliente", new { clienteId = idCliente, urlAtras = "" });

            }
        }





        public IActionResult ComoLlegar(Int32 clienteId)
        {
            ViewCliente cliente = new ViewCliente();

            cliente = _repositorioCliente.GetCliente(clienteId);

            ViewBag.urlAtras = HttpContext.Request.UrlAtras();

            IRepositorioEmpresa repositorioEmpresa = new RepositorioEmpresa();
            repositorioEmpresa.DatosSistema = _session.Sistema;
            ViewBag.ListaSucursales = repositorioEmpresa.Lista_ViewSucursales();

            return View(cliente);
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GetProvincias(Int16 paisId)
        {
            RepositorioPais repositorio = new RepositorioPais();
            repositorio.DatosSistema = _session.Sistema;

            List<PaisProvinciaEstado> listaProv = repositorio.GetProvincia(paisId, null);

            String json = JsonConvert.SerializeObject(listaProv);

            return new JsonResult(json);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GetCiudades(Int32 provinciaId)
        {
            RepositorioPais repositorio = new RepositorioPais();
            repositorio.DatosSistema = _session.Sistema;

            List<PaisProvinciaEstadoCiudad> listaCiud = repositorio.GetCiudad(provinciaId, null);

            String json = JsonConvert.SerializeObject(listaCiud);

            return new JsonResult(json);
        }




        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult GetEstadosCuenta(string jsonListaClientes)
        {
            ViewModelCliente view = new ViewModelCliente();
            view.Parametro = "VerEstadoCuenta";
            //view.Lista = JsonConvert.DeserializeObject<List<ViewCliente>>(jsonListaClientes);

            view.Lista = jsonListaClientes.ToObsect<List<ViewCliente>>();


            foreach (var cliente in view.Lista)
            {
                Int16 sector = _session.Sistema.SectorId ?? 0;

                var query = _repositorioCliente.GetSaldo(cliente.EntidadSucId, (Int32)_session?.Sistema.EmpresaId, sector);

                if (query?.Count() > 0)
                {
                    //Cambio: de Total a SaldoCtaCte
                    cliente.SaldoCtaCte = query.Sum(c => c.SaldoCtaCte + c.Adelanto);

                }
            }

            view.Filtro = new FiltroCliente();

            view.Filtro.SectorId = _session.Sistema.SectorId;
            view.Filtro.Representada = _session.Sistema.TipoEmpresa == 256 ? true : false;

            if (_session.Usuario.Rol == (Int32)EnumRol.Vendedor)
            {
                view.Filtro.VendedorId = _session.Usuario.Cliente_Vendedor_Id;

            }
            return PartialView("_tablaClientesPorVendedor", view);
        }
    }
}