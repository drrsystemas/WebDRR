using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebDRR.Controllers
{
    [Area("Logistica")]
    [Route("[controller]/[action]")]
    public class ClienteController : Controller
    {
        #region Variables
        private readonly IRepositorioCliente _repositorioCliente;
        private SessionAcceso _session;
        #endregion


        #region Constructor
        //El constructor inyecta el repositorio de Cliente.
        //En dicho clase esta toda la comunicacion con la base de datos
        public ClienteController(IRepositorioCliente repositorioCliente, IHttpContextAccessor httpContextAccessor)
        {
            _repositorioCliente = repositorioCliente;
            _session = httpContextAccessor.HttpContext.Session.GetJson<SessionAcceso>("SessionAcceso");
            _repositorioCliente.DatosSistema = _session.Sistema;
        }
        #endregion


        public IActionResult BuscarCliente(Int32 tipoB, String dato)
        {
            if (tipoB > 0 && !String.IsNullOrEmpty(dato))
            {

                var query = _repositorioCliente.BuscarCliente(tipoB, dato);

                if (_session.Sistema.EmpresaId == 29)
                {
                    if (query != null && query.Count() > 0)
                    {
                        query = _repositorioCliente.GenerarClave(query);
                    }

                }

                return View(query);
            }
            else
            {
                return View();
            }

        }
    }
}