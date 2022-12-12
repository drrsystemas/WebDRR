using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WebDRR.TagHelpers
{
    [HtmlTargetElement("td", Attributes = "wrap")]
    public class TableCellTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context,
        TagHelperOutput output)
        {
            output.PreContent.SetHtmlContent("<b><i>");
            output.PostContent.SetHtmlContent("</i></b>");
        }

        //La salida de esto seria: <td wrap><b><i>London</i></b></td> -- negrita y cursiva.
    }
}
