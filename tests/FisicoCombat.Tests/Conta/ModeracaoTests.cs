using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Tests.Conta;

public class ModeracaoTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    private async Task<int> ComentarAsync(string texto, StatusComentario status = StatusComentario.Pendente)
    {
        var (_, autor) = await app.CriarClienteUsuarioAsync("autor@teste.local");
        return await Apoio.CriarComentarioAsync(app, autor.Id, texto, status);
    }

    private Task<StatusComentario?> StatusAsync(int id) =>
        app.ExecutarAsync(db => db.Comentarios.Where(c => c.Id == id).Select(c => (StatusComentario?)c.Status).SingleOrDefaultAsync());

    private static Task<HttpResponseMessage> AcaoAsync(HttpClient admin, string handler, int id, string status = "Pendente") =>
        Formularios.EnviarAsync(admin, "/Admin/Comentarios?status=Todos", [],
            $"/Admin/Comentarios?handler={handler}&id={id}&status={status}");

    [Fact]
    public async Task Usuario_comum_e_anonimo_nao_moderam()
    {
        var (comum, _) = await app.CriarClienteUsuarioAsync("comum@teste.local");

        var negado = await comum.GetAsync("/Admin/Comentarios");
        Assert.Equal(HttpStatusCode.Redirect, negado.StatusCode);
        Assert.StartsWith("/Conta/AcessoNegado", negado.Headers.Location!.PathAndQuery);

        var anonimo = await app.CriarCliente().GetAsync("/Admin/Comentarios");
        Assert.Equal(HttpStatusCode.Redirect, anonimo.StatusCode);
        Assert.StartsWith("/Conta/Entrar", anonimo.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Lista_pendentes_por_padrao_e_filtra_por_status()
    {
        await ComentarAsync("Filtro pendente <script>alert(1)</script>");
        await ComentarAsync("Filtro aprovado", StatusComentario.Aprovado);
        await ComentarAsync("Filtro rejeitado", StatusComentario.Rejeitado);
        var admin = await app.CriarClienteAdminAsync();

        var padrao = await (await admin.GetAsync("/Admin/Comentarios")).Content.ReadAsStringAsync();
        Assert.Contains("Filtro pendente &lt;script&gt;", padrao);
        Assert.DoesNotContain("<script>alert(1)", padrao);
        Assert.DoesNotContain("Filtro aprovado", padrao);
        Assert.DoesNotContain("Filtro rejeitado", padrao);
        Assert.Contains("/Postagens/", padrao);

        var aprovados = await (await admin.GetAsync("/Admin/Comentarios?status=Aprovado")).Content.ReadAsStringAsync();
        Assert.Contains("Filtro aprovado", aprovados);
        Assert.DoesNotContain("Filtro pendente", aprovados);

        var rejeitados = await (await admin.GetAsync("/Admin/Comentarios?status=Rejeitado")).Content.ReadAsStringAsync();
        Assert.Contains("Filtro rejeitado", rejeitados);
        Assert.DoesNotContain("Filtro aprovado", rejeitados);

        var todos = await (await admin.GetAsync("/Admin/Comentarios?status=Todos")).Content.ReadAsStringAsync();
        Assert.Contains("Filtro pendente", todos);
        Assert.Contains("Filtro aprovado", todos);
        Assert.Contains("Filtro rejeitado", todos);
    }

    [Fact]
    public async Task Aprovar_rejeitar_e_excluir_mantem_o_filtro()
    {
        var id = await ComentarAsync("Comentário a moderar");
        var admin = await app.CriarClienteAdminAsync();

        var aprovar = await AcaoAsync(admin, "Aprovar", id);
        Assert.Equal(HttpStatusCode.Redirect, aprovar.StatusCode);
        Assert.Equal("/Admin/Comentarios?status=Pendente", aprovar.Headers.Location!.OriginalString);
        Assert.Equal(StatusComentario.Aprovado, await StatusAsync(id));

        var rejeitar = await AcaoAsync(admin, "Rejeitar", id, "Aprovado");
        Assert.Equal("/Admin/Comentarios?status=Aprovado", rejeitar.Headers.Location!.OriginalString);
        Assert.Equal(StatusComentario.Rejeitado, await StatusAsync(id));

        var excluir = await AcaoAsync(admin, "Excluir", id, "Todos");
        Assert.Equal("/Admin/Comentarios?status=Todos", excluir.Headers.Location!.OriginalString);
        Assert.Null(await StatusAsync(id));
        Assert.Contains("Comentário excluído.", await Apoio.HtmlAsync(await admin.GetAsync(excluir.Headers.Location)));

        await ComentarAsync("Outro comentário, para a página ter formulário (token)");
        Assert.Equal(HttpStatusCode.NotFound, (await AcaoAsync(admin, "Aprovar", id)).StatusCode);
    }

    [Fact]
    public async Task Acoes_exigem_token_antifalsificacao()
    {
        var id = await ComentarAsync("Sem token");
        var admin = await app.CriarClienteAdminAsync();

        var resposta = await admin.PostAsync($"/Admin/Comentarios?handler=Aprovar&id={id}", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(StatusComentario.Pendente, await StatusAsync(id));
    }
}
