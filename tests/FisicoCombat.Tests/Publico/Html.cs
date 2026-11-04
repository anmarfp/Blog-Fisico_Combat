using System.Net;
using System.Text.RegularExpressions;

namespace FisicoCombat.Tests.Publico;

internal static partial class Html
{
    [GeneratedRegex(@"class=""stretched-link""[^>]*>\s*([^<]+?)\s*</a>")]
    private static partial Regex TituloCartao();

    /// <summary>Títulos dos cartões de postagem, na ordem em que aparecem na página.</summary>
    public static List<string> TitulosCartoes(string html) =>
        TituloCartao().Matches(html).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();

    /// <summary>Texto dos links marcados com aria-current="page" (deve haver só um, o da página atual).</summary>
    public static List<string> PaginasAtuais(string html) =>
        Regex.Matches(html, @"<a [^>]*aria-current=""page""[^>]*>([^<]*)</a>").Select(m => m.Groups[1].Value).ToList();

    /// <summary>Valor de uma metatag Open Graph, já decodificado.</summary>
    public static string? Og(string html, string propriedade) =>
        Regex.Match(html, $@"<meta property=""og:{propriedade}"" content=""([^""]*)""") is { Success: true } m
            ? WebUtility.HtmlDecode(m.Groups[1].Value)
            : null;

    public static async Task<string> TextoAsync(HttpResponseMessage resposta) =>
        WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
}
