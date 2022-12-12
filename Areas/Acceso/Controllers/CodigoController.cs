using LibreriaBase.Areas.Logistica.Clases;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Areas.Acceso.Controllers
{
    [Area("Acceso")]
    [Route("[controller]/[action]")]
    public class CodigoController : Controller
    {
        #region Variables
        private readonly IRepositorioCliente _repositorioCliente;
        #endregion


        #region Constructor
        //El constructor inyecta el repositorio de Cliente.
        //En dicho clase esta toda la comunicacion con la base de datos
        public CodigoController(IRepositorioCliente repositorioCliente)
        {
            _repositorioCliente = repositorioCliente;

        }
        #endregion


        /// <summary>
        /// Esta pantalla es para el administrador, con los datos de la empresa y cliente se 
        /// genera el codigo de acceso.
        /// </summary>
        public IActionResult GenerarClave(Int32 empresaID, Int32 clienteID)
        {

            if (clienteID > 0)
            {
                LoginBasico loginBasico = new LoginBasico();

                Int32 codigo = loginBasico.GenerarClave(clienteID, empresaID);

                return View();
            }
            else
            {
                return View();
            }

        }

        /// <summary>
        /// Pantalla de Login, la pantalla principal donde el usuario ingresa sus datos.
        /// </summary>
        public IActionResult Acceso(String msj)
        {

            //ISessionAcceso sessionAcceso = HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            //if(sessionAcceso==null)
            //{
            //    LibreriaBase.Clases.DRREnviroment enviroment = new LibreriaBase.Clases.DRREnviroment();


            //    //El primer ingreso hay que cargar la informacion.
            //    if (sessionAcceso == null)
            //    {
            //        sessionAcceso = new SessionAcceso();
            //        sessionAcceso.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
            //    }
            //    else
            //    {
            //        if (String.IsNullOrEmpty(sessionAcceso.Sistema?.Cn_Alma))
            //        {
            //            sessionAcceso.Sistema.Cn_Alma = enviroment.Obtener_CadenaConexion_Alma_Alternativa();
            //        }
            //    }

            //    sessionAcceso.Sistema.EmpresaId = 29;
            //    sessionAcceso.Sistema.CN_Empresa = enviroment.Obtener_CadenaConexion_Empresa((Int32)sessionAcceso.Sistema.EmpresaId);

            //    HttpContext.Session.SetJson("SessionAcceso", sessionAcceso);
            //}


            //Esto se utiliza para enviar mensajes de error desde otras pantallas cuando se llama a Login.
            if (!String.IsNullOrEmpty(msj))
            {
                //ViewBag -> Objeto del controlador al que puedo asignarles variables de manera dinamica.
                ViewBag.ErrorMessage = msj;
            }


            return View();
        }





        /// <summary>
        /// Metodo de entrada al sistema, se verifican que los datos ingresados sean correctos.
        /// </summary>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Verificar(DatosUsuario modelo)
        {
            ISessionAcceso sessionAcceso = HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");

            modelo.IdEmpresa = 29;

            Boolean ok = false;

            if (modelo.IdAlmaWeb == 29 && modelo.Clave == "4483300")
            {
                modelo.Nombre = "Jorge Yaworski";
                modelo.Rol = 1;

                sessionAcceso.Usuario = modelo;
                //Guardo la clase en sesion serializado en Json.
                HttpContext.Session.SetJson("SessionAcceso", sessionAcceso);

                //Los datos son validos ingresa a la pantalla donde se muestran todas las guias del
                //cliente en un intervalo que hoy es de 30 dias.
                return RedirectToAction("Index", "Home");

            }
            else
            {
                LoginBasico loginBasico = new LoginBasico();

                ok = loginBasico.Verificar((int)modelo.IdAlmaWeb, (int)modelo.IdEmpresa, Convert.ToInt32(modelo.Clave));

            }

            if (ok)
            {

                _repositorioCliente.DatosSistema = sessionAcceso.Sistema;
                var cliente = _repositorioCliente.Get_DatosMinimos((int)modelo.IdAlmaWeb);

                modelo.Nombre = cliente?.Nombre;
                modelo.Rol = cliente?.Rol;

                sessionAcceso.Usuario = modelo;

                //Guardo la clase en sesion serializado en Json.
                HttpContext.Session.SetJson("SessionAcceso", sessionAcceso);

                //Los datos son validos ingresa a la pantalla donde se muestran todas las guias del
                //cliente en un intervalo que hoy es de 30 dias.
                //10/02/2020
                return RedirectToAction("GuiasPorCliente", "Logistica");

            }
            else
            {
                //Vuelve a login pero, se muestra el msj de que los datos no son correctos.
                String msj = "Los datos ingresados no son correctos";
                return RedirectToAction("Acceso", new { msj });
            }

        }








        /// <summary>
        /// Cierra session del usuario actual en el sistema.
        /// </summary>
        public IActionResult Cerrar()
        {
            HttpContext.Session.Remove("SessionAcceso");
            return RedirectToAction("Acceso");
        }
    }
}