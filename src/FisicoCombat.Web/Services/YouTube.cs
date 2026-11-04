using System.Text.RegularExpressions;

namespace FisicoCombat.Web.Services;

public static partial class YouTube
{
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex IdValido();

    /// <summary>
    /// Extrai o ID do vídeo de links do YouTube (watch?v=, youtu.be/, /embed/, /shorts/, /live/).
    /// Devolve null para qualquer outro endereço, para não incorporar conteúdo de terceiros.
    /// </summary>
    public static string? ExtrairId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return null;

        var host = uri.Host.ToLowerInvariant();
        var partes = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? id = null;

        if (host is "youtu.be")
            id = partes.FirstOrDefault();
        else if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "music.youtube.com")
        {
            if (partes is ["watch"])
                id = System.Web.HttpUtility.ParseQueryString(uri.Query)["v"];
            else if (partes is ["embed" or "shorts" or "live", var segundo, ..])
                id = segundo;
        }

        return id is not null && IdValido().IsMatch(id) ? id : null;
    }

    public static string UrlIncorporacao(string id) => $"https://www.youtube-nocookie.com/embed/{id}";
}
