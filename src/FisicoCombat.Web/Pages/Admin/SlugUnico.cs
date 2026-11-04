using FisicoCombat.Web.Services;

namespace FisicoCombat.Web.Pages.Admin;

public static class SlugUnico
{
    /// <summary>Gera o slug do texto e, se ele já existir, acrescenta -2, -3… até achar um livre.</summary>
    public static async Task<string> GerarAsync(string texto, int tamanhoMaximo, Func<string, Task<bool>> existe)
    {
        var raiz = Slug.Gerar(texto, tamanhoMaximo);
        var slug = raiz;
        for (var n = 2; await existe(slug); n++) slug = $"{raiz}-{n}";
        return slug;
    }
}
