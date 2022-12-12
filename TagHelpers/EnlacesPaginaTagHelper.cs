using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Clases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WebDRR.TagHelpers
{
    [HtmlTargetElement("div", Attributes = "page-model")]
    public class EnlacesPaginaTagHelper : TagHelper
    {
        private IUrlHelperFactory urlHelperFactory;


        public EnlacesPaginaTagHelper(IUrlHelperFactory helperFactory)
        {
            urlHelperFactory = helperFactory;
        }


        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public Paginacion PageModel { get; set; }

        public string PageAction { get; set; }



        [HtmlAttributeName(DictionaryAttributePrefix = "page-url-")]
        public Dictionary<string, object> PageUrlValues { get; set; }
            = new Dictionary<string, object>();

        public bool PageClassesEnabled { get; set; } = false;
        public string PageClass { get; set; }
        public string PageClassNormal { get; set; }
        public string PageClassSelected { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            IUrlHelper urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
            TagBuilder result = new TagBuilder("div");

            Boolean btnSiguiente = false;

            int paginaActual = PageModel.PaginaActual;
            int totalPagina = PageModel.CantidadPaginas;

            //a) evaluo la pagina actual. (se evalua si se muestra el boton de atras

            #region Agrego el Atras en caso de que la pagina sea mayor que 1
            if (paginaActual > 1)
            {
                TagBuilder tag = new TagBuilder("a");

                PageUrlValues["PaginaActual"] = (paginaActual - 1);

                tag.Attributes["href"] = urlHelper.Action(PageAction, PageUrlValues);

                if (PageClassesEnabled)
                {
                    tag.AddCssClass(PageClass);

                    tag.AddCssClass(1 == (paginaActual - 1)
                        ? PageClassSelected : PageClassNormal);
                }

                tag.InnerHtml.Append("<");
                result.InnerHtml.AppendHtml(tag);
            }
            #endregion


            //b) 
            int restoPaginas = totalPagina - paginaActual;
            if (restoPaginas > 6)
            {
                btnSiguiente = true;
            }


            Int32 catidadPaginas = 7;

            if (restoPaginas < catidadPaginas)
            {
                //Para que muestre el ultimo.
                catidadPaginas = restoPaginas + 1;
            }

            int hasta = paginaActual + catidadPaginas;


            FiltroProducto filtro = new FiltroProducto();

            for (int i = paginaActual; i < hasta; i++)
            {
                TagBuilder tag = new TagBuilder("a");

                PageUrlValues["PaginaActual"] = i;

                tag.Attributes["href"] = urlHelper.Action(PageAction, PageUrlValues);

                if (PageClassesEnabled)
                {
                    tag.AddCssClass(PageClass);

                    tag.AddCssClass(i == PageModel.PaginaActual
                        ? PageClassSelected : PageClassNormal);
                }

                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }


            if (btnSiguiente == true)
            {
                Int32 item = paginaActual + catidadPaginas;

                TagBuilder tag = new TagBuilder("a");

                PageUrlValues["PaginaActual"] = item;

                tag.Attributes["href"] = urlHelper.Action(PageAction, PageUrlValues);

                if (PageClassesEnabled)
                {
                    tag.AddCssClass(PageClass);

                    tag.AddCssClass(1 == item
                        ? PageClassSelected : PageClassNormal);
                }

                tag.InnerHtml.Append(">");
                result.InnerHtml.AppendHtml(tag);
            }

            output.Content.AppendHtml(result.InnerHtml);
        }
    }
}
