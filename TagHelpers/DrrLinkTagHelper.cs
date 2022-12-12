using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WebDRR.TagHelpers
{
    [HtmlTargetElement("drr-link")]
    public class DrrLinkTagHelper : TagHelper
    {
        private readonly IHtmlGenerator _htmlGenerator;

        public DrrLinkTagHelper(IHtmlGenerator htmlGenerator)
        {
            _htmlGenerator = htmlGenerator;
        }

        public String url { get; set; }
        public String css { get; set; }

        public String text { get; set; }

        [ViewContext]
        public ViewContext ViewContext { set; get; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            try
            {
                String[] data = url.Split('\\');

                if (data?.Count() == 2)
                {
                    //Me da la posibilidad que la etiqueta se cierre sobre si misma.
                    output.TagMode = TagMode.StartTagAndEndTag;


                    var actionAnchor = _htmlGenerator.GenerateActionLink(
                        ViewContext,
                        linkText: text,
                        actionName: data[1],
                        controllerName: data[0],
                        fragment: null,
                        hostname: null,
                        htmlAttributes: null,
                        protocol: null,
                        routeValues: null
                        );

                    actionAnchor.AddCssClass(css);

                    var builder = new HtmlContentBuilder();
                    builder.AppendHtml(actionAnchor);

                    output.Content.SetHtmlContent(builder);
                }
                else if (data?.Count() == 1)
                {
                    //Me da la posibilidad que la etiqueta se cierre sobre si misma.
                    output.TagMode = TagMode.StartTagAndEndTag;

                    String[] dataDos = url.Split('/');
                    dataDos = dataDos.Where(val => !String.IsNullOrEmpty(val)).ToArray();

                    if (dataDos?.Count() == 2)
                    {
                        var actionAnchor = _htmlGenerator.GenerateActionLink(
                        ViewContext,
                        linkText: text,
                        actionName: dataDos[1],
                        controllerName: dataDos[0],
                        fragment: null,
                        hostname: null,
                        htmlAttributes: null,
                        protocol: null,
                        routeValues: null
                        );

                        actionAnchor.AddCssClass(css);

                        var builder = new HtmlContentBuilder();
                        builder.AppendHtml(actionAnchor);

                        output.Content.SetHtmlContent(builder);
                    }
                }
            }
            catch (Exception)
            {

            }

        }
    }
}
