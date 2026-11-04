using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FisicoCombat.Tests.Publico;

/// <summary>Página da postagem: exibição, contador, comentários (RF10), compartilhar (RF11) e Open Graph.</summary>
public class PostagemTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    private async Task<Postagem> CriarAsync(string titulo, Action<Postagem>? ajustar = null)
    {
        var postagem = new Postagem
        {
            Titulo = titulo, Slug = Slug.Gerar(titulo), Tipo = TipoPostagem.Noticia,
            Conteudo = "Texto da postagem.", PublicadaEm = DateTime.UtcNow,
        };
        ajustar?.Invoke(postagem);
        await app.ExecutarAsync(async db => { db.Postagens.Add(postagem); await db.SaveChangesAsync(); });
        return postagem;
    }

    private static string Caminho(Postagem p) => $"/Postagens/{p.Id}/{p.Slug}";

    private Task<int> ContarComentariosAsync(Postagem p) =>
        app.ExecutarAsync(db => db.Comentarios.CountAsync(c => c.PostagemId == p.Id));

    [Fact]
    public async Task Texto_e_codificado_e_cada_linha_vira_paragrafo()
    {
        var p = await CriarAsync("Texto com script", x =>
            x.Conteudo = "Primeira linha\n\n<script>alert('x')</script>\r\nTerceira linha");

        var resposta = await app.CriarCliente().GetAsync(Caminho(p));
        var html = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("<p>Primeira linha</p>", html);
        Assert.Contains("<p>&lt;script&gt;alert(", html);
        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("<p>Terceira linha</p>", html);
    }

    [Fact]
    public async Task Foto_tem_texto_alternativo_e_usa_o_titulo_quando_falta_descricao()
    {
        var comDescricao = await CriarAsync("Foto descrita", x =>
        {
            x.ImagemCaminho = "/uploads/descrita.jpg";
            x.ImagemDescricao = "Alunos no tatame";
        });
        var semDescricao = await CriarAsync("Foto sem descricao", x => x.ImagemCaminho = "/uploads/sem.jpg");
        var cliente = app.CriarCliente();

        var html1 = await (await cliente.GetAsync(Caminho(comDescricao))).Content.ReadAsStringAsync();
        var html2 = await (await cliente.GetAsync(Caminho(semDescricao))).Content.ReadAsStringAsync();

        Assert.Contains("src=\"/uploads/descrita.jpg\" alt=\"Alunos no tatame\"", html1);
        Assert.Contains("src=\"/uploads/sem.jpg\" alt=\"Foto sem descricao\"", html2);
    }

    [Fact]
    public async Task Video_usa_youtube_nocookie_com_title()
    {
        var p = await CriarAsync("Luta de exibicao", x => { x.Tipo = TipoPostagem.Midia; x.YoutubeId = "dQw4w9WgXcQ"; });

        var texto = await Html.TextoAsync(await app.CriarCliente().GetAsync(Caminho(p)));

        Assert.Contains("<iframe src=\"https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ\" title=\"Vídeo: Luta de exibicao\"", texto);
        Assert.Contains("allowfullscreen", texto);
    }

    [Fact]
    public async Task Cada_abertura_soma_1_visualizacao()
    {
        var p = await CriarAsync("Contador");
        var cliente = app.CriarCliente();

        await cliente.GetAsync(Caminho(p));
        await cliente.GetAsync($"/Postagens/{p.Id}");

        var visualizacoes = await app.ExecutarAsync(db => db.Postagens.Where(x => x.Id == p.Id).Select(x => x.Visualizacoes).SingleAsync());
        Assert.Equal(2, visualizacoes);
    }

    [Fact]
    public async Task Postagem_inexistente_retorna_404()
    {
        var cliente = app.CriarCliente();

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/Postagens/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/Postagens/999999/qualquer")).StatusCode);
    }

    [Fact]
    public async Task So_comentarios_aprovados_aparecem_do_mais_antigo_ao_mais_novo()
    {
        var p = await CriarAsync("Com comentarios");
        var (_, usuario) = await app.CriarClienteUsuarioAsync("leitor@teste.local");
        var agora = DateTime.UtcNow;
        await app.ExecutarAsync(async db =>
        {
            db.Comentarios.AddRange(
                new Comentario { PostagemId = p.Id, UsuarioId = usuario.Id, Texto = "Segundo aprovado", Status = StatusComentario.Aprovado, CriadoEm = agora },
                new Comentario { PostagemId = p.Id, UsuarioId = usuario.Id, Texto = "Primeiro aprovado", Status = StatusComentario.Aprovado, CriadoEm = agora.AddHours(-1) },
                new Comentario { PostagemId = p.Id, UsuarioId = usuario.Id, Texto = "Ainda pendente", Status = StatusComentario.Pendente, CriadoEm = agora },
                new Comentario { PostagemId = p.Id, UsuarioId = usuario.Id, Texto = "Foi rejeitado", Status = StatusComentario.Rejeitado, CriadoEm = agora });
            await db.SaveChangesAsync();
        });

        var texto = await Html.TextoAsync(await app.CriarCliente().GetAsync(Caminho(p)));

        Assert.True(texto.IndexOf("Primeiro aprovado") < texto.IndexOf("Segundo aprovado"));
        Assert.Contains("Primeiro aprovado", texto);
        Assert.Contains(usuario.NomeExibicao, texto);
        Assert.DoesNotContain("Ainda pendente", texto);
        Assert.DoesNotContain("Foi rejeitado", texto);
    }

    [Fact]
    public async Task Usuario_logado_envia_comentario_que_fica_pendente()
    {
        var p = await CriarAsync("Para comentar");
        var (cliente, usuario) = await app.CriarClienteUsuarioAsync("comentarista@teste.local");

        var resposta = await Formularios.EnviarAsync(cliente, Caminho(p),
            new() { ["Texto"] = "  Muito bom o treino!  " }, Caminho(p) + "?handler=Comentar");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal(Caminho(p) + "#comentarios", resposta.Headers.Location!.OriginalString);
        var comentario = await app.ExecutarAsync(db => db.Comentarios.SingleAsync(c => c.PostagemId == p.Id));
        Assert.Equal(StatusComentario.Pendente, comentario.Status);
        Assert.Equal(usuario.Id, comentario.UsuarioId);
        Assert.Equal("Muito bom o treino!", comentario.Texto);
        Assert.True(comentario.CriadoEm > DateTime.UtcNow.AddMinutes(-5));

        var texto = await Html.TextoAsync(await cliente.GetAsync(Caminho(p)));
        Assert.Contains("Comentário enviado! Ele aparecerá após a aprovação do administrador.", texto);
        Assert.DoesNotContain("Muito bom o treino!", texto);
    }

    [Fact]
    public async Task Comentario_vazio_ou_longo_demais_nao_e_gravado()
    {
        var p = await CriarAsync("Comentario invalido");
        var (cliente, _) = await app.CriarClienteUsuarioAsync("invalido@teste.local");

        var vazio = await Formularios.EnviarAsync(cliente, Caminho(p), new() { ["Texto"] = "   " }, Caminho(p) + "?handler=Comentar");
        var longo = await Formularios.EnviarAsync(cliente, Caminho(p), new() { ["Texto"] = new string('a', 1001) }, Caminho(p) + "?handler=Comentar");

        Assert.Equal(HttpStatusCode.OK, vazio.StatusCode);
        Assert.Contains("Escreva o comentário.", await Html.TextoAsync(vazio));
        Assert.Equal(HttpStatusCode.OK, longo.StatusCode);
        Assert.Contains("O comentário pode ter até 1000 caracteres.", await Html.TextoAsync(longo));
        Assert.Equal(0, await ContarComentariosAsync(p));
    }

    [Fact]
    public async Task Visitante_ve_link_para_entrar_e_nao_ve_formulario()
    {
        var p = await CriarAsync("Para visitante");

        var html = await (await app.CriarCliente().GetAsync(Caminho(p))).Content.ReadAsStringAsync();

        Assert.Contains($"href=\"/Conta/Entrar?ReturnUrl=%2FPostagens%2F{p.Id}%2F{p.Slug}\"", html);
        Assert.Contains("Entre na sua conta para comentar.", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("handler=Comentar", html);
    }

    [Fact]
    public async Task Envio_anonimo_pede_login_e_nao_grava()
    {
        var p = await CriarAsync("Envio anonimo");
        var cliente = app.CriarCliente();

        // Sem token antiforgery: recusado antes de chegar à página.
        var semToken = await cliente.PostAsync(Caminho(p) + "?handler=Comentar",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["Texto"] = "Anonimo" }));
        Assert.Equal(HttpStatusCode.BadRequest, semToken.StatusCode);

        // Com token válido de visitante: a página pede login.
        using var scope = app.Services.CreateScope();
        var contexto = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var tokens = scope.ServiceProvider.GetRequiredService<IAntiforgery>().GetAndStoreTokens(contexto);
        var requisicao = new HttpRequestMessage(HttpMethod.Post, Caminho(p) + "?handler=Comentar")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Texto"] = "Anonimo",
                [tokens.FormFieldName] = tokens.RequestToken!,
            }),
        };
        requisicao.Headers.Add("Cookie", contexto.Response.Headers.SetCookie.ToString().Split(';')[0]);
        var comToken = await cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Redirect, comToken.StatusCode);
        Assert.StartsWith("/Conta/Entrar", comToken.Headers.Location!.PathAndQuery);
        Assert.Equal(0, await ContarComentariosAsync(p));
    }

    [Fact]
    public async Task Links_de_compartilhar_usam_url_codificada()
    {
        var p = await CriarAsync("Treino & Sparring");
        var url = $"http%3A%2F%2Flocalhost%2FPostagens%2F{p.Id}%2F{p.Slug}";

        var html = await (await app.CriarCliente().GetAsync(Caminho(p))).Content.ReadAsStringAsync();

        Assert.Contains($"href=\"https://wa.me/?text=Treino%20%26%20Sparring%20{url}\"", html);
        Assert.Contains($"href=\"https://www.facebook.com/sharer/sharer.php?u={url}\"", html);
        Assert.Contains($"href=\"https://twitter.com/intent/tweet?url={url}&amp;text=Treino%20%26%20Sparring\"", html);
        Assert.Equal(3, html.Split("target=\"_blank\" rel=\"noopener\"").Length - 1);
        Assert.Contains("data-acao=\"copiar\" hidden>Copiar link</button>", html);
        Assert.Contains("data-acao=\"nativo\" hidden>Compartilhar</button>", html);
        Assert.Contains("aria-live=\"polite\"", html);
    }

    [Fact]
    public async Task Metatags_open_graph_com_foto()
    {
        var conteudo = string.Join(' ', Enumerable.Repeat("palavra", 60));
        var p = await CriarAsync("Postagem com foto", x => { x.ImagemCaminho = "/uploads/og.jpg"; x.Conteudo = conteudo; });

        var html = await (await app.CriarCliente().GetAsync(Caminho(p))).Content.ReadAsStringAsync();

        Assert.Equal("Postagem com foto", Html.Og(html, "title"));
        Assert.Equal($"http://localhost{Caminho(p)}", Html.Og(html, "url"));
        Assert.Equal("http://localhost/uploads/og.jpg", Html.Og(html, "image"));
        var descricao = Html.Og(html, "description")!;
        Assert.True(descricao.Length <= 160);
        Assert.StartsWith("palavra palavra", descricao);
        Assert.EndsWith("…", descricao);
    }

    [Fact]
    public async Task Metatag_og_image_usa_miniatura_do_youtube()
    {
        var p = await CriarAsync("Postagem com video", x => { x.Tipo = TipoPostagem.Midia; x.YoutubeId = "dQw4w9WgXcQ"; });

        var html = await (await app.CriarCliente().GetAsync(Caminho(p))).Content.ReadAsStringAsync();

        Assert.Equal("https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg", Html.Og(html, "image"));
        Assert.Equal("Texto da postagem.", Html.Og(html, "description"));
    }
}
