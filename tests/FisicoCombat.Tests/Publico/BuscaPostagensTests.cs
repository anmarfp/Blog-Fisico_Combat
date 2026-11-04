using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Tests.Publico;

/// <summary>Lista de postagens com filtros (RF09), busca e paginação.</summary>
public class BuscaPostagensTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    // 19 postagens; a ordem de publicação é a ordem de criação abaixo (a última é a mais recente).
    private Task PrepararAsync() => app.ExecutarAsync(async db =>
    {
        if (await db.Modalidades.AnyAsync()) return;

        var muay = new Modalidade { Nome = "Muay Thai", Slug = "muay-thai" };
        var jiu = new Modalidade { Nome = "Jiu-Jitsu", Slug = "jiu-jitsu" };
        var inicio = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var hora = 0;
        void Nova(string titulo, TipoPostagem tipo, Modalidade? modalidade, string? conteudo = null) =>
            db.Postagens.Add(new Postagem
            {
                Titulo = titulo, Slug = Slug.Gerar(titulo), Tipo = tipo, Modalidade = modalidade,
                Conteudo = conteudo, PublicadaEm = inicio.AddHours(hora++),
            });

        for (var i = 1; i <= 14; i++) Nova($"Treino muay {i:00}", TipoPostagem.Noticia, muay);
        Nova("Video muay 1", TipoPostagem.Midia, muay);
        Nova("Video muay 2", TipoPostagem.Midia, muay);
        Nova("Campeonato jiu", TipoPostagem.Noticia, jiu);
        Nova("Fotos jiu", TipoPostagem.Midia, jiu, "Graduacao de faixa preta");
        Nova("Aviso geral", TipoPostagem.Noticia, null, "Recesso de FERIADO na sexta.\n100% de presenca_total");
        await db.SaveChangesAsync();
    });

    private async Task<(HttpResponseMessage Resposta, string Html, List<string> Titulos)> AbrirAsync(string consulta)
    {
        await PrepararAsync();
        var resposta = await app.CriarCliente().GetAsync("/Postagens" + consulta);
        var html = await resposta.Content.ReadAsStringAsync();
        return (resposta, html, Html.TitulosCartoes(html));
    }

    [Fact]
    public async Task Sem_filtros_lista_12_por_pagina_do_mais_recente()
    {
        var (resposta, html, titulos) = await AbrirAsync("");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(12, titulos.Count);
        Assert.Equal(new[] { "Aviso geral", "Fotos jiu", "Campeonato jiu", "Video muay 2", "Video muay 1", "Treino muay 14" }, titulos[..6]);
        Assert.Contains("19 postagens encontradas", html);
        Assert.Contains("<nav aria-label=\"Paginação\"", html);
        Assert.Equal(new[] { "1" }, Html.PaginasAtuais(html));
        Assert.Contains("href=\"/Postagens?pagina=2\"", html);
    }

    [Fact]
    public async Task Segunda_pagina_traz_o_restante()
    {
        var (_, html, titulos) = await AbrirAsync("?pagina=2");

        Assert.Equal(7, titulos.Count);
        Assert.Equal("Treino muay 07", titulos[0]);
        Assert.Equal("Treino muay 01", titulos[^1]);
        Assert.Equal(new[] { "2" }, Html.PaginasAtuais(html));
    }

    [Theory]
    [InlineData("?tipo=midia", "Fotos jiu|Video muay 2|Video muay 1")]
    [InlineData("?modalidade=jiu-jitsu", "Fotos jiu|Campeonato jiu")]
    [InlineData("?tipo=midia&modalidade=muay-thai", "Video muay 2|Video muay 1")]
    [InlineData("?tipo=noticia&modalidade=jiu-jitsu", "Campeonato jiu")]
    [InlineData("?q=feriado", "Aviso geral")]
    [InlineData("?q=CAMPEONATO", "Campeonato jiu")]
    [InlineData("?q=video", "Video muay 2|Video muay 1")]
    [InlineData("?q=faixa&tipo=midia&modalidade=jiu-jitsu", "Fotos jiu")]
    [InlineData("?q=100%25", "Aviso geral")]
    [InlineData("?q=presenca_total", "Aviso geral")]
    public async Task Filtros_e_busca_restringem_a_lista(string consulta, string esperados)
    {
        var (resposta, _, titulos) = await AbrirAsync(consulta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(esperados.Split('|'), titulos);
    }

    [Fact]
    public async Task Filtro_por_tipo_pagina_mantendo_o_filtro()
    {
        var (_, html, titulos) = await AbrirAsync("?tipo=noticia");

        Assert.Equal(12, titulos.Count);
        Assert.Contains("16 postagens encontradas", html);
        Assert.Contains("href=\"/Postagens?tipo=noticia&amp;pagina=2\"", html);
        Assert.Contains("<option value=\"noticia\" selected=\"selected\">", html);
        Assert.DoesNotContain("<option value=\"midia\" selected", html);
    }

    [Fact]
    public async Task Rf09_mostra_filtros_ativos_selecionados()
    {
        var (_, html, _) = await AbrirAsync("?tipo=midia&modalidade=muay-thai");
        var texto = System.Net.WebUtility.HtmlDecode(html);

        Assert.Contains("<option value=\"midia\" selected=\"selected\">", html);
        Assert.Contains("<option value=\"muay-thai\" selected=\"selected\">", html);
        Assert.Contains("Filtros ativos:", texto);
        Assert.Contains("Tipo: Mídias", texto);
        Assert.Contains("Modalidade: Muay Thai", texto);
    }

    [Theory]
    [InlineData("?q=inexistente")]
    [InlineData("?q=Treino%25muay")] // % e _ são texto, não curingas do LIKE
    [InlineData("?q=Video_muay")]
    [InlineData("?q=faixa&tipo=noticia")]
    public async Task Busca_sem_resultado_mostra_aviso(string consulta)
    {
        var (resposta, html, titulos) = await AbrirAsync(consulta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Empty(titulos);
        Assert.Contains("Nenhuma postagem corresponde aos filtros escolhidos.", System.Net.WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("aria-label=\"Paginação\"", html);
    }

    [Fact]
    public async Task Valores_desconhecidos_sao_ignorados()
    {
        var (resposta, html, titulos) = await AbrirAsync("?tipo=xyz&modalidade=nao-existe&pagina=abc&q=");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(12, titulos.Count);
        Assert.Contains("19 postagens encontradas", html);
        Assert.DoesNotContain("Filtros ativos:", html);
    }

    [Fact]
    public async Task Pagina_alem_do_fim_mostra_a_ultima()
    {
        var (_, _, titulos) = await AbrirAsync("?pagina=99");

        Assert.Equal(7, titulos.Count);
    }
}
