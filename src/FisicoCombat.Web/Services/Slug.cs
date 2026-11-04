using System.Globalization;
using System.Text;

namespace FisicoCombat.Web.Services;

public static class Slug
{
    /// <summary>"Jiu-Jitsu Brasileiro" -> "jiu-jitsu-brasileiro"; "Notícia: Ação!" -> "noticia-acao".</summary>
    public static string Gerar(string texto, int tamanhoMaximo = 80)
    {
        var semAcento = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(c);

        var slug = new StringBuilder();
        foreach (var c in semAcento.ToString().ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9') slug.Append(c);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        }

        var resultado = slug.ToString().Trim('-');
        if (resultado.Length > tamanhoMaximo) resultado = resultado[..tamanhoMaximo].TrimEnd('-');
        return resultado.Length == 0 ? "item" : resultado;
    }
}
