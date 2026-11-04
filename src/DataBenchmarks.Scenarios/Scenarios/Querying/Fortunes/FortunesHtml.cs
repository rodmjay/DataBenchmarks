using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using DataBenchmarks.Core.Fixtures.Fortunes;

namespace DataBenchmarks.Scenarios.Querying.Fortunes;

/// Steps 3 to 5 of the operation, shared by every approach so only data access differs.
public static class FortunesHtml
{
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    public static string Render(List<Fortune> fortunes)
    {
        fortunes.Add(new Fortune { Id = 0, Message = "Additional fortune added at request time." });
        fortunes.Sort((a, b) => string.CompareOrdinal(a.Message, b.Message));

        var html = new StringBuilder(2048);
        html.Append("<!DOCTYPE html><html><head><title>Fortunes</title></head><body><table><tr><th>id</th><th>message</th></tr>");
        foreach (var fortune in fortunes)
        {
            html.Append("<tr><td>").Append(fortune.Id).Append("</td><td>")
                .Append(Encoder.Encode(fortune.Message)).Append("</td></tr>");
        }
        html.Append("</table></body></html>");
        return html.ToString();
    }
}
