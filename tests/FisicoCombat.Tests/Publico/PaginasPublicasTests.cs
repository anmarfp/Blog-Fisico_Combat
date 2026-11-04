using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;

namespace FisicoCombat.Tests.Publico;

public class InicioTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Inicio_mostra_as_6_postagens_mais_recentes_e_as_modalidades()
    {
        await app.ExecutarAsync(async db =>
        {
            var boxe = new Modalidade { Nome = "Boxe", Slug = "boxe" };
            db.Modalidades.AddRange(boxe, new Modalidade { Nome = "Judo", Slug = "judo" });
            var inicio = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            for (var i = 1; i <= 7; i++)
                db.Postagens.Add(new Postagem
                {
                    Titulo = $"Post inicio {i}", Slug = $"post-inicio-{i}", Modalidade = boxe,
                    Tipo = i % 2 == 0 ? TipoPostagem.Midia : TipoPostagem.Noticia, PublicadaEm = inicio.AddDays(i),
                });
            await db.SaveChangesAsync();
        });

        var resposta = await app.CriarCliente().GetAsync("/");
        var html = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(new[] { "Post inicio 7", "Post inicio 6", "Post inicio 5", "Post inicio 4", "Post inicio 3", "Post inicio 2" },
            Html.TitulosCartoes(html));
        Assert.Contains("href=\"/Modalidades/boxe\"", html);
        Assert.Contains("href=\"/Modalidades/judo\"", html);
        Assert.Equal(1, html.Split("<h1").Length - 1);
    }
}

public class ModalidadesEProfessoresTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Pagina_da_modalidade_mostra_descricao_professores_e_postagens()
    {
        await app.ExecutarAsync(async db =>
        {
            var karate = new Modalidade { Nome = "Karate", Slug = "karate", Descricao = "Arte marcial japonesa.\nTurmas infantis." };
            var outra = new Modalidade { Nome = "Capoeira", Slug = "capoeira" };
            db.Professores.Add(new Professor { Nome = "Joao Silva", Modalidades = [karate] });
            db.Postagens.AddRange(
                new Postagem { Titulo = "Kata do mes", Slug = "kata-do-mes", Modalidade = karate, Tipo = TipoPostagem.Midia, PublicadaEm = DateTime.UtcNow },
                new Postagem { Titulo = "Roda de capoeira", Slug = "roda-de-capoeira", Modalidade = outra, PublicadaEm = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });
        var cliente = app.CriarCliente();

        var lista = await (await cliente.GetAsync("/Modalidades")).Content.ReadAsStringAsync();
        var resposta = await cliente.GetAsync("/Modalidades/karate");
        var html = await resposta.Content.ReadAsStringAsync();

        Assert.Contains("href=\"/Modalidades/karate\"", lista);
        Assert.Contains("<p>Arte marcial japonesa.</p>", lista);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("<h1>Karate</h1>", html);
        Assert.Contains("<p>Turmas infantis.</p>", html);
        Assert.Contains("Joao Silva", html);
        Assert.Equal(new[] { "Kata do mes" }, Html.TitulosCartoes(html));
        Assert.Contains("href=\"/Postagens?tipo=midia&amp;modalidade=karate\"", html);
    }

    [Fact]
    public async Task Modalidade_inexistente_retorna_404()
    {
        var resposta = await app.CriarCliente().GetAsync("/Modalidades/nao-existe");
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Professores_mostra_foto_ou_iniciais_nome_biografia_e_modalidades()
    {
        await app.ExecutarAsync(async db =>
        {
            var judo = new Modalidade { Nome = "Judo infantil", Slug = Slug.Gerar("Judo infantil") };
            db.Professores.AddRange(
                new Professor { Nome = "Maria Souza", FotoCaminho = "/uploads/maria.jpg", Biografia = "Faixa preta <b>3o dan</b>.", Modalidades = [judo] },
                new Professor { Nome = "Pedro Lima" });
            await db.SaveChangesAsync();
        });

        var resposta = await app.CriarCliente().GetAsync("/Professores");
        var html = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("src=\"/uploads/maria.jpg\" alt=\"Maria Souza\"", html);
        Assert.Contains("<p>Faixa preta &lt;b&gt;3o dan&lt;/b&gt;.</p>", html);
        Assert.Contains("href=\"/Modalidades/judo-infantil\">Judo infantil</a>", html);
        Assert.Contains("aria-hidden=\"true\">PL</div>", html);
        Assert.Contains("<h2 class=\"h4\">Pedro Lima</h2>", html);
    }
}
