using System.Net;
using System.Text;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.EntityFrameworkCore;
using static FisicoCombat.Tests.Admin.Auxiliar;

namespace FisicoCombat.Tests.Admin;

public class ProfessoresTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    private Task<int> CriarModalidadeAsync(string nome) => app.ExecutarAsync(async db =>
    {
        var m = new Modalidade { Nome = nome, Slug = Web.Services.Slug.Gerar(nome) };
        db.Modalidades.Add(m);
        await db.SaveChangesAsync();
        return m.Id;
    });

    private Task<Professor?> BuscarAsync(string nome) =>
        app.ExecutarAsync(db => db.Professores.Include(p => p.Modalidades).SingleOrDefaultAsync(p => p.Nome == nome));

    [Fact]
    public async Task Cria_lista_edita_e_exclui_professor_com_modalidades()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var judo = await CriarModalidadeAsync("Judô");
        var karate = await CriarModalidadeAsync("Caratê");
        var luta = await CriarModalidadeAsync("Luta Livre");

        var html = await HtmlAsync(await cliente.GetAsync("/Admin/Professores/Criar"));
        Assert.Contains("Judô", html);

        var criado = await Formularios.EnviarAsync(cliente, "/Admin/Professores/Criar", new Dictionary<string, string>
        {
            ["Professor.Nome"] = "Ana Souza",
            ["Professor.Biografia"] = "Faixa preta.",
        }.Com("ModalidadeIds", judo, karate));
        AssertRedirecionaPara(criado, "/Admin/Professores");

        var professor = await BuscarAsync("Ana Souza");
        Assert.NotNull(professor);
        Assert.Equal("Faixa preta.", professor.Biografia);
        Assert.Equal([judo, karate], professor.Modalidades.Select(m => m.Id).Order());
        Assert.Null(professor.FotoCaminho);

        var lista = await HtmlAsync(await cliente.GetAsync("/Admin/Professores"));
        Assert.Contains("Ana Souza", lista);
        Assert.Contains("Caratê, Judô", lista);

        var editado = await Formularios.EnviarAsync(cliente, $"/Admin/Professores/Editar/{professor.Id}", new Dictionary<string, string>
        {
            ["Professor.Nome"] = "Ana Souza Lima",
            ["Professor.Biografia"] = "",
        }.Com("ModalidadeIds", luta));
        AssertRedirecionaPara(editado, "/Admin/Professores");
        professor = await BuscarAsync("Ana Souza Lima");
        Assert.NotNull(professor);
        Assert.Null(professor.Biografia);
        Assert.Equal([luta], professor.Modalidades.Select(m => m.Id));

        var excluido = await Formularios.EnviarAsync(cliente, $"/Admin/Professores/Excluir/{professor.Id}", new());
        AssertRedirecionaPara(excluido, "/Admin/Professores");
        Assert.Null(await BuscarAsync("Ana Souza Lima"));
        // As modalidades continuam existindo.
        Assert.Equal(3, await app.ExecutarAsync(db => db.Modalidades.CountAsync(m => new[] { judo, karate, luta }.Contains(m.Id))));
    }

    [Fact]
    public async Task Foto_e_salva_trocada_removida_e_apagada_ao_excluir()
    {
        var cliente = await app.CriarClienteAdminAsync();

        var criado = await Formularios.EnviarMultipartAsync(cliente, "/Admin/Professores/Criar",
            new() { ["Professor.Nome"] = "Bruno Costa" }, "Foto", Formularios.PngFalso(), "bruno.png", "image/png");
        AssertRedirecionaPara(criado, "/Admin/Professores");
        var professor = (await BuscarAsync("Bruno Costa"))!;
        Assert.NotNull(professor.FotoCaminho);
        var primeira = CaminhoFisico(app, professor.FotoCaminho);
        Assert.True(File.Exists(primeira));

        var lista = await HtmlAsync(await cliente.GetAsync("/Admin/Professores"));
        Assert.Contains($"src=\"{professor.FotoCaminho}\" alt=\"Bruno Costa\"", lista);

        // Trocar a foto apaga o arquivo antigo.
        var trocado = await Formularios.EnviarMultipartAsync(cliente, $"/Admin/Professores/Editar/{professor.Id}",
            new() { ["Professor.Nome"] = "Bruno Costa" }, "Foto", Formularios.PngFalso(), "nova.png", "image/png");
        AssertRedirecionaPara(trocado, "/Admin/Professores");
        professor = (await BuscarAsync("Bruno Costa"))!;
        var segunda = CaminhoFisico(app, professor.FotoCaminho!);
        Assert.NotEqual(primeira, segunda);
        Assert.False(File.Exists(primeira));
        Assert.True(File.Exists(segunda));

        // Remover a foto apaga o arquivo.
        var removido = await Formularios.EnviarAsync(cliente, $"/Admin/Professores/Editar/{professor.Id}", new Dictionary<string, string>
        {
            ["Professor.Nome"] = "Bruno Costa",
            ["RemoverFoto"] = "true",
        });
        AssertRedirecionaPara(removido, "/Admin/Professores");
        professor = (await BuscarAsync("Bruno Costa"))!;
        Assert.Null(professor.FotoCaminho);
        Assert.False(File.Exists(segunda));

        // Excluir o professor apaga a foto.
        await Formularios.EnviarMultipartAsync(cliente, $"/Admin/Professores/Editar/{professor.Id}",
            new() { ["Professor.Nome"] = "Bruno Costa" }, "Foto", Formularios.PngFalso(), "outra.png", "image/png");
        professor = (await BuscarAsync("Bruno Costa"))!;
        var terceira = CaminhoFisico(app, professor.FotoCaminho!);
        Assert.True(File.Exists(terceira));
        await Formularios.EnviarAsync(cliente, $"/Admin/Professores/Excluir/{professor.Id}", new());
        Assert.False(File.Exists(terceira));
    }

    [Fact]
    public async Task Arquivo_que_nao_e_imagem_e_recusado_e_nada_e_salvo()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var antes = ArquivosEnviados(app).Length;

        var resposta = await Formularios.EnviarMultipartAsync(cliente, "/Admin/Professores/Criar",
            new() { ["Professor.Nome"] = "Carla Dias" }, "Foto", Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'/>"),
            "foto.png", "image/png");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Envie uma imagem JPG, PNG ou WebP.", await HtmlAsync(resposta));
        Assert.Null(await BuscarAsync("Carla Dias"));
        Assert.Equal(antes, ArquivosEnviados(app).Length);
    }

    [Fact]
    public async Task Nome_obrigatorio()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await Formularios.EnviarAsync(cliente, "/Admin/Professores/Criar", new() { ["Professor.Nome"] = "" });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Informe o nome do professor.", await HtmlAsync(resposta));
    }
}
