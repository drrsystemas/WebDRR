
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace WebDRR.Clases
{
    public class ControllerDrrSystemas:Controller
    {
        /// <summary>
        /// Transforma un componente a String. Este metodos es util para trabajar con Ajax y JS.
        /// </summary>
        /// <param name="viewComponent">Nombre del componente</param>
        /// <param name="args">Modelo del componente</param>
        /// <returns>String</returns>
        public async Task<string> TransformarComponente_String(string viewComponent, object args)
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

        /// <summary>
        /// Transforma una vista a String. Este metodos es util para trabajar con Ajax y JS.
        /// </summary>
        /// <param name="viewName">Nombre de la vista</param>
        /// <param name="model">Modelo</param>
        /// <returns>String</returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected async Task<string> TransformarVista_String(string viewName = null, object model = null)
        {
            // Primero, intentamos localizar la vista...
            var actionContext = new ActionContext(
                    HttpContext, RouteData, ControllerContext.ActionDescriptor, ModelState);
            var viewEngine = HttpContext.RequestServices.GetService<ICompositeViewEngine>();

            var viewEngineResult = viewEngine.FindView(actionContext, viewName, isMainPage: false);

            if (!viewEngineResult.Success)
            {
                var searchedLocations = string.Join(", ", viewEngineResult.SearchedLocations);
                throw new InvalidOperationException(
                    $"Couldn't find view '{viewName}', " +
                    $"searched locations: [{searchedLocations}]");
            }

            // Hemos encontrado la vista, vamos a renderizarla...
            using (var sw = new StringWriter())
            {
                // Preparamos el contexto de la vista
                var viewData = new ViewDataDictionary(ViewData) { Model = model };
                var helperOptions = HttpContext.RequestServices
                        .GetService<IOptions<HtmlHelperOptions>>()
                        .Value;
                var viewContext = new ViewContext(
                        actionContext, viewEngineResult.View, viewData, TempData, sw, helperOptions);

                // Y voila!
                await viewEngineResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }


      


    }
}
