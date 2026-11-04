using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.EntityFrameworkCore;
using static FisicoCombat.Tests.Admin.Auxiliar;

namespace FisicoCombat.Tests.Admin;

public class ModalidadesTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    private Task<Modalidade?> BuscarAsync(string nome) =>
        app.ExecutarAsync(db => db.Modalidades.SingleOrDefaultAsync(m => m.Nome == nome));

    private async Task<HttpResponseMessage> CriarAsync(HttpClient cliente, string nome, string descricao = "") =>
        await Formularios.EnviarAsync(cliente, "/Admin/Modalidades/Criar", new()
        {
            ["Modalidade.Nome"] = nome,
            ["Modalidade.Descricao"] = descricao,
        });

    [Fact]
    public async Task Cria_lista_edita_e_exclui_modalidade()
    {
        var cliente = await app.CriarClienteAdminAsync();

        var criada = await CriarAsync(cliente, "Jiu-Jitsu Brasileiro", "Arte suave.");
        AssertRedirecionaPara(criada, "/Admin/Modalidades");
        var modalidade = await BuscarAsync("Jiu-Jitsu Brasileiro");
        Assert.NotNull(modalidade);
        Assert.Equal("jiu-jitsu-brasileiro", modalidade.Slug);
        Assert.Equal("Arte suave.", modalidade.Descricao);

        var lista = await HtmlAsync(await cliente.GetAsync("/Admin/Modalidades"));
        Assert.Contains("Jiu-Jitsu Brasileiro", lista);

        var editada = await Formularios.EnviarAsync(cliente, $"/Admin/Modalidades/Editar/{modalidade.Id}", new()
        {
            ["Modalidade.Nome"] = "Jiu-Jitsu",
            ["Modalidade.Descricao"] = "Nova descrição.",
        });
        AssertRedirecionaPara(editada, "/Admin/Modalidades");
        var depois = await app.ExecutarAsync(db => db.Modalidades.SingleAsync(m => m.Id == modalidade.Id));
        Assert.Equal("Jiu-Jitsu", depois.Nome);
        Assert.Equal("jiu-jitsu", depois.Slug);
        Assert.Equal("Nova descrição.", depois.Descricao);

        var confirmacao = await HtmlAsync(await cliente.GetAsync($"/Admin/Modalidades/Excluir/{modalidade.Id}"));
        Assert.Contains("não serão excluídas", confirmacao);

        var excluida = await Formularios.EnviarAsync(cliente, $"/Admin/Modalidades/Excluir/{modalidade.Id}", new());
        AssertRedirecionaPara(excluida, "/Admin/Modalidades");
        Assert.False(await app.ExecutarAsync(db => db.Modalidades.AnyAsync(m => m.Id == modalidade.Id)));
    }

    [Fact]
    public async Task Slug_em_colisao_recebe_sufixo()
    {
        var cliente = await app.CriarClienteAdminAsync();

        await CriarAsync(cliente, "Muay Thai");
        await CriarAsync(cliente, "Muay-Thai");
        await CriarAsync(cliente, "Muay Thai!");

        Assert.Equal("muay-thai", (await BuscarAsync("Muay Thai"))!.Slug);
        Assert.Equal("muay-thai-2", (await BuscarAsync("Muay-Thai"))!.Slug);
        Assert.Equal("muay-thai-3", (await BuscarAsync("Muay Thai!"))!.Slug);
    }

    [Fact]
    public async Task Nome_duplicado_e_recusado_com_mensagem()
    {
        var cliente = await app.CriarClienteAdminAsync();
        await CriarAsync(cliente, "Boxe");

        var repetida = await CriarAsync(cliente, " boxe ");
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        Assert.Contains("Já existe uma modalidade com esse nome.", await HtmlAsync(repetida));
        Assert.Equal(1, await app.ExecutarAsync(db => db.Modalidades.CountAsync(m => m.Nome.ToLower() == "boxe")));

        // Editar outra modalidade para o mesmo nome também é recusado.
        await CriarAsync(cliente, "Kickboxing");
        var outra = await BuscarAsync("Kickboxing");
        var editada = await Formularios.EnviarAsync(cliente, $"/Admin/Modalidades/Editar/{outra!.Id}", new()
        {
            ["Modalidade.Nome"] = "Boxe",
        });
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Contains("Já existe uma modalidade com esse nome.", await HtmlAsync(editada));
    }

    [Fact]
    public async Task Nome_obrigatorio()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await CriarAsync(cliente, "");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Informe o nome da modalidade.", await HtmlAsync(resposta));
    }

    [Fact]
    public async Task Excluir_modalidade_mantem_as_postagens_sem_modalidade()
    {
        var cliente = await app.CriarClienteAdminAsync();
        await CriarAsync(cliente, "Capoeira");
        var modalidade = await BuscarAsync("Capoeira");
        var postagemId = await app.ExecutarAsync(async db =>
        {
            var p = new Postagem { Titulo = "Roda de capoeira", Slug = "roda-de-capoeira", Conteudo = "Texto", ModalidadeId = modalidade!.Id, PublicadaEm = DateTime.UtcNow };
            db.Postagens.Add(p);
            await db.SaveChangesAsync();
            return p.Id;
        });

        var confirmacao = await HtmlAsync(await cliente.GetAsync($"/Admin/Modalidades/Excluir/{modalidade!.Id}"));
        Assert.Contains("As postagens dessa modalidade (1) <strong>não serão excluídas</strong>", confirmacao);

        await Formularios.EnviarAsync(cliente, $"/Admin/Modalidades/Excluir/{modalidade.Id}", new());

        var postagem = await app.ExecutarAsync(db => db.Postagens.SingleAsync(p => p.Id == postagemId));
        Assert.Null(postagem.ModalidadeId);
    }

    [Fact]
    public async Task Modalidade_inexistente_retorna_404()
    {
        var cliente = await app.CriarClienteAdminAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/Admin/Modalidades/Editar/9999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/Admin/Modalidades/Excluir/9999")).StatusCode);
    }
}
