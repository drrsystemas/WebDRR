using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebDRR.Pages
{
    public class LocalizacionClientesModel : PageModel
    {

        public String Titulo { get; set; }
        public String Txt { get; set; }

        public void OnGet(String texto)
        {
            Txt = texto;
            Titulo = "Localizacion Clientes";
        }
    }
}
