using System.Net;
using FisicoCombat.Tests.Infra;

namespace FisicoCombat.Tests.Admin;

public static class Auxiliar
{
    /// <summary>HTML da resposta com entidades decodificadas (o Razor codifica acentos como &amp;#xE1;).</summary>
    public static async Task<string> HtmlAsync(HttpResponseMessage resposta) =>
        WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());

    public static string[] ArquivosEnviados(FabricaApp app) =>
        Directory.Exists(app.PastaUploads) ? Directory.GetFiles(app.PastaUploads) : [];

    public static string CaminhoFisico(FabricaApp app, string caminhoPublico) =>
        Path.Combine(app.PastaUploads, Path.GetFileName(caminhoPublico));

    /// <summary>Acrescenta valores de um campo de lista (ex.: checkboxes) como Nome[0], Nome[1]…</summary>
    public static Dictionary<string, string> Com(this Dictionary<string, string> campos, string nome, params int[] valores)
    {
        for (var i = 0; i < valores.Length; i++) campos[$"{nome}[{i}]"] = valores[i].ToString();
        return campos;
    }

    public static void AssertRedirecionaPara(HttpResponseMessage resposta, string caminho)
    {
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal(caminho, resposta.Headers.Location!.OriginalString);
    }
}
