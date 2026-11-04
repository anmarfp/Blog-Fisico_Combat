using System.Net;
using System.Text.RegularExpressions;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using static FisicoCombat.Tests.Admin.Auxiliar;

namespace FisicoCombat.Tests.Admin;

public class PainelTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Mostra_totais_mais_vistas_e_visualizacoes_por_modalidade()
    {
        var (_, usuario) = await app.CriarClienteUsuarioAsync();
        await app.ExecutarAsync(async db =>
        {
            var boxe = new Modalidade { Nome = "Boxe", Slug = "boxe" };
            var judo = new Modalidade { Nome = "Judô", Slug = "judo" };
            var semPostagens = new Modalidade { Nome = "Esgrima", Slug = "esgrima" };
            db.AddRange(boxe, judo, semPostagens);
            db.Professores.Add(new Professor { Nome = "Prof. Teste", Modalidades = [boxe] });

            // 12 postagens: "Post 01" (1 visualização) … "Post 12" (12 visualizações).
            for (var i = 1; i <= 12; i++)
                db.Postagens.Add(new Postagem
                {
                    Titulo = $"Post {i:00}",
                    Slug = $"post-{i:00}",
                    Visualizacoes = i,
                    PublicadaEm = DateTime.UtcNow,
                    Modalidade = i <= 4 ? boxe : i <= 6 ? judo : null,
                });
            await db.SaveChangesAsync();

            var primeira = db.Postagens.Local.First();
            db.Comentarios.AddRange(
                new Comentario { Postagem = primeira, UsuarioId = usuario.Id, Texto = "a", Status = StatusComentario.Pendente },
                new Comentario { Postagem = primeira, UsuarioId = usuario.Id, Texto = "b", Status = StatusComentario.Pendente },
                new Comentario { Postagem = primeira, UsuarioId = usuario.Id, Texto = "c", Status = StatusComentario.Aprovado });
            await db.SaveChangesAsync();
        });

        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await cliente.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var html = await HtmlAsync(resposta);

        Assert.Equal("12", Total(html, "postagens"));
        Assert.Equal("1", Total(html, "professores"));
        Assert.Equal("3", Total(html, "modalidades"));
        Assert.Equal("2", Total(html, "comentarios-pendentes"));
        Assert.Contains("href=\"/Admin/Comentarios\"", html);

        var maisVistas = Tabela(html, "mais-vistas");
        Assert.Equal(10, Regex.Matches(maisVistas, "<tr>").Count - 1); // menos o cabeçalho
        Assert.Contains("Post 12", maisVistas);
        Assert.Contains("Post 03", maisVistas);
        Assert.DoesNotContain("Post 02", maisVistas);
        Assert.DoesNotContain("Post 01", maisVistas);
        Assert.True(maisVistas.IndexOf("Post 12") < maisVistas.IndexOf("Post 11"));

        // Boxe: 1+2+3+4 = 10; Judô: 5+6 = 11; sem modalidade: 7+…+12 = 57; Esgrima: 0.
        var porModalidade = Regex.Replace(Tabela(html, "por-modalidade"), @"\s+", " ");
        Assert.Contains("<td>Judô</td> <td class=\"text-end\">11</td>", porModalidade);
        Assert.Contains("<td>Boxe</td> <td class=\"text-end\">10</td>", porModalidade);
        Assert.Contains("<td>Esgrima</td> <td class=\"text-end\">0</td>", porModalidade);
        Assert.Contains("<td>Sem modalidade</td> <td class=\"text-end\">57</td>", porModalidade);
    }

    private static string Total(string html, string nome) =>
        Regex.Match(html, $@"id=""total-{nome}"">(\d+)<").Groups[1].Value;

    private static string Tabela(string html, string id) =>
        Regex.Match(html, $@"<table[^>]*id=""{id}"">(.*?)</table>", RegexOptions.Singleline).Groups[1].Value;
}

public class AcessoAdminTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Admin/Modalidades")]
    [InlineData("/Admin/Modalidades/Criar")]
    [InlineData("/Admin/Modalidades/Editar/1")]
    [InlineData("/Admin/Modalidades/Excluir/1")]
    [InlineData("/Admin/Professores")]
    [InlineData("/Admin/Professores/Criar")]
    [InlineData("/Admin/Professores/Editar/1")]
    [InlineData("/Admin/Professores/Excluir/1")]
    [InlineData("/Admin/Postagens")]
    [InlineData("/Admin/Postagens/Criar")]
    [InlineData("/Admin/Postagens/Editar/1")]
    [InlineData("/Admin/Postagens/Excluir/1")]
    public async Task Usuario_comum_e_redirecionado_para_acesso_negado(string url)
    {
        var (cliente, _) = await app.CriarClienteUsuarioAsync();

        var get = await cliente.GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.StartsWith("/Conta/AcessoNegado", get.Headers.Location!.PathAndQuery);

        var post = await cliente.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.StartsWith("/Conta/AcessoNegado", post.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Visitante_e_redirecionado_para_login()
    {
        var resposta = await app.CriarCliente().GetAsync("/Admin/Postagens/Criar");
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.StartsWith("/Conta/Entrar", resposta.Headers.Location!.PathAndQuery);
    }
}
