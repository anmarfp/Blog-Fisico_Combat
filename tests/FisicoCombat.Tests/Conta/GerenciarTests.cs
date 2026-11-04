using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace FisicoCombat.Tests.Conta;

public class SairTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Sair_so_por_POST()
    {
        await Apoio.CriarUsuarioAsync(app, "sair@teste.local");
        var cliente = await Apoio.ClienteLogadoAsync(app, "sair@teste.local");

        var get = await cliente.GetAsync("/Conta/Sair");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.True(await Apoio.LogadoAsync(cliente));

        var post = await Formularios.EnviarAsync(cliente, "/Conta/Sair", []);
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("/", post.Headers.Location!.OriginalString);
        Assert.False(await Apoio.LogadoAsync(cliente));
    }
}

public class GerenciarTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    internal static Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string handler, Dictionary<string, string> campos) =>
        Formularios.EnviarAsync(cliente, "/Conta/Gerenciar", campos, "/Conta/Gerenciar?handler=" + handler);

    private static readonly DateTime MarcoUtc = new(2026, 3, 10, 15, 30, 0, DateTimeKind.Utc);
    private const string MarcoBrasilia = "10/03/2026 às 12:30";

    [Fact]
    public async Task Minha_conta_exige_login()
    {
        var resposta = await app.CriarCliente().GetAsync("/Conta/Gerenciar");
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.StartsWith("/Conta/Entrar", resposta.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Mostra_os_dados_e_muda_o_nome()
    {
        var usuario = await Apoio.CriarUsuarioAsync(app, "nome@teste.local");
        var cliente = await Apoio.ClienteLogadoAsync(app, "nome@teste.local");

        var html = await Apoio.HtmlAsync(await cliente.GetAsync("/Conta/Gerenciar"));
        Assert.Contains("nome@teste.local", html);
        Assert.Contains("Política de Privacidade aceita em", html);

        var resposta = await EnviarAsync(cliente, "Nome", new() { ["Nome.NomeExibicao"] = "  Nome Novo  " });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("Nome Novo", await app.ExecutarAsync(db => db.Users.Where(u => u.Id == usuario.Id).Select(u => u.NomeExibicao).SingleAsync()));
    }

    [Fact]
    public async Task Mostra_a_data_do_consentimento()
    {
        var usuario = await Apoio.CriarUsuarioAsync(app, "consentimento@teste.local");
        await app.ExecutarAsync(db => db.Users.Where(u => u.Id == usuario.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ConsentimentoEm, (DateTime?)MarcoUtc)));
        var cliente = await Apoio.ClienteLogadoAsync(app, "consentimento@teste.local");

        var html = await Apoio.HtmlAsync(await cliente.GetAsync("/Conta/Gerenciar"));

        Assert.Contains(MarcoBrasilia, html);
        Assert.DoesNotContain("não registrado", html);
    }

    [Fact]
    public async Task Mostra_os_proprios_comentarios_do_mais_novo_ao_mais_antigo()
    {
        var usuario = await Apoio.CriarUsuarioAsync(app, "meus-comentarios@teste.local");
        var outro = await Apoio.CriarUsuarioAsync(app, "outro-comentarista@teste.local");
        var antigo = await Apoio.CriarComentarioAsync(app, usuario.Id, "Meu antigo <b>negrito</b>", StatusComentario.Aprovado);
        var rejeitado = await Apoio.CriarComentarioAsync(app, usuario.Id, "Meu rejeitado", StatusComentario.Rejeitado);
        await Apoio.CriarComentarioAsync(app, usuario.Id, "Meu recente");
        await Apoio.CriarComentarioAsync(app, outro.Id, "Comentário de outra pessoa");
        await app.ExecutarAsync(async db =>
        {
            await db.Comentarios.Where(c => c.Id == antigo).ExecuteUpdateAsync(s => s.SetProperty(c => c.CriadoEm, MarcoUtc));
            await db.Comentarios.Where(c => c.Id == rejeitado).ExecuteUpdateAsync(s => s.SetProperty(c => c.CriadoEm, MarcoUtc.AddDays(1)));
        });
        var postagemId = await app.ExecutarAsync(db => db.Comentarios.Where(c => c.Id == antigo).Select(c => c.PostagemId).SingleAsync());
        var cliente = await Apoio.ClienteLogadoAsync(app, "meus-comentarios@teste.local");

        var bruto = await (await cliente.GetAsync("/Conta/Gerenciar")).Content.ReadAsStringAsync();
        var html = WebUtility.HtmlDecode(bruto);

        Assert.Contains("Meu antigo &lt;b&gt;negrito&lt;/b&gt;", bruto);
        Assert.DoesNotContain("<b>negrito", bruto);
        Assert.DoesNotContain("Comentário de outra pessoa", html);
        Assert.Matches($@"href=""/Postagens/{postagemId}/postagem-de-teste-[0-9a-f]{{8}}"">Postagem de teste</a>", html);
        Assert.Contains(MarcoBrasilia, html);
        var secao = html.IndexOf("Seus comentários", StringComparison.Ordinal);
        var recente = html.IndexOf("Meu recente", StringComparison.Ordinal);
        var meio = html.IndexOf("Meu rejeitado", StringComparison.Ordinal);
        var velho = html.IndexOf("Meu antigo", StringComparison.Ordinal);
        Assert.True(secao > 0 && secao < recente && recente < meio && meio < velho, "Ordem: do mais novo ao mais antigo.");
        // O status de cada comentário aparece logo antes do texto dele.
        Assert.Contains(">Pendente<", html[secao..recente]);
        Assert.Contains(">Rejeitado<", html[recente..meio]);
        Assert.Contains(">Aprovado<", html[meio..velho]);
    }

    [Fact]
    public async Task Troca_a_senha_e_entra_com_a_nova()
    {
        await Apoio.CriarUsuarioAsync(app, "senha@teste.local");
        var cliente = await Apoio.ClienteLogadoAsync(app, "senha@teste.local");

        var resposta = await EnviarAsync(cliente, "Senha", new()
        {
            ["Senha.SenhaAtual"] = Apoio.SenhaPadrao,
            ["Senha.NovaSenha"] = "SenhaTrocada456",
            ["Senha.ConfirmarSenha"] = "SenhaTrocada456",
        });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.True(await Apoio.LogadoAsync(cliente), "A sessão atual continua válida depois da troca.");
        var antiga = await Apoio.EntrarAsync(app.CriarCliente(), "senha@teste.local", Apoio.SenhaPadrao);
        Assert.Contains("E-mail ou senha incorretos.", await Apoio.HtmlAsync(antiga));
        await Apoio.ClienteLogadoAsync(app, "senha@teste.local", "SenhaTrocada456");
    }

    [Fact]
    public async Task Troca_de_senha_com_senha_atual_errada_e_recusada()
    {
        await Apoio.CriarUsuarioAsync(app, "senha-errada@teste.local");
        var cliente = await Apoio.ClienteLogadoAsync(app, "senha-errada@teste.local");

        var resposta = await EnviarAsync(cliente, "Senha", new()
        {
            ["Senha.SenhaAtual"] = "NaoEhEssa123",
            ["Senha.NovaSenha"] = "SenhaTrocada456",
            ["Senha.ConfirmarSenha"] = "SenhaTrocada456",
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Senha incorreta.", await Apoio.HtmlAsync(resposta));
    }

    [Fact]
    public async Task Excluir_conta_apaga_usuario_e_comentarios()
    {
        var usuario = await Apoio.CriarUsuarioAsync(app, "excluir@teste.local");
        var outro = await Apoio.CriarUsuarioAsync(app, "fica@teste.local");
        await Apoio.CriarComentarioAsync(app, usuario.Id, "Comentário que deve sumir");
        var deOutro = await Apoio.CriarComentarioAsync(app, outro.Id, "Comentário de outra pessoa, que fica");
        var cliente = await Apoio.ClienteLogadoAsync(app, "excluir@teste.local");

        var errada = await EnviarAsync(cliente, "Excluir", new() { ["Exclusao.SenhaAtual"] = "NaoEhEssa123" });
        Assert.Equal(HttpStatusCode.OK, errada.StatusCode);
        Assert.True(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Id == usuario.Id)));

        var resposta = await EnviarAsync(cliente, "Excluir", new() { ["Exclusao.SenhaAtual"] = Apoio.SenhaPadrao });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString);
        Assert.False(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Id == usuario.Id)));
        Assert.False(await app.ExecutarAsync(db => db.Comentarios.AnyAsync(c => c.UsuarioId == usuario.Id)));
        Assert.True(await app.ExecutarAsync(db => db.Comentarios.AnyAsync(c => c.Id == deOutro)));
        Assert.True(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Id == outro.Id)));
        Assert.False(await Apoio.LogadoAsync(cliente));
        Assert.Contains("Sua conta e seus comentários foram excluídos.", await Apoio.HtmlAsync(await cliente.GetAsync("/")));
    }

    [Fact]
    public async Task Ultimo_administrador_nao_exclui_a_propria_conta()
    {
        var cliente = await Apoio.ClienteLogadoAsync(app, FabricaApp.AdminEmail, FabricaApp.AdminSenha);

        var resposta = await EnviarAsync(cliente, "Excluir", new() { ["Exclusao.SenhaAtual"] = FabricaApp.AdminSenha });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("único administrador", await Apoio.HtmlAsync(resposta));
        Assert.True(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Email == FabricaApp.AdminEmail)));
    }

    [Theory]
    [InlineData("Excluir")]
    [InlineData("Senha")]
    public async Task Cinco_senhas_erradas_bloqueiam_a_conta_e_encerram_a_sessao(string handler)
    {
        var email = $"bloqueio-{handler.ToLowerInvariant()}@teste.local";
        var usuario = await Apoio.CriarUsuarioAsync(app, email);
        var cliente = await Apoio.ClienteLogadoAsync(app, email);
        var campos = handler == "Excluir"
            ? new Dictionary<string, string> { ["Exclusao.SenhaAtual"] = "NaoEhEssa123" }
            : new Dictionary<string, string>
            {
                ["Senha.SenhaAtual"] = "NaoEhEssa123",
                ["Senha.NovaSenha"] = "SenhaTrocada456",
                ["Senha.ConfirmarSenha"] = "SenhaTrocada456",
            };

        for (var i = 1; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await EnviarAsync(cliente, handler, new(campos))).StatusCode);
        var quinta = await EnviarAsync(cliente, handler, new(campos));

        Assert.Equal(HttpStatusCode.Redirect, quinta.StatusCode);
        Assert.Equal("/Conta/Entrar", quinta.Headers.Location!.OriginalString);
        Assert.Contains("sua conta foi bloqueada", await Apoio.HtmlAsync(await cliente.GetAsync("/Conta/Entrar")));
        Assert.False(await Apoio.LogadoAsync(cliente));
        using (var scope = app.Services.CreateScope())
        {
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Assert.True(await usuarios.IsLockedOutAsync((await usuarios.FindByIdAsync(usuario.Id))!));
        }
        Assert.True(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Id == usuario.Id)), "A conta não é excluída.");
        var entrar = await Apoio.EntrarAsync(app.CriarCliente(), email, Apoio.SenhaPadrao);
        Assert.Contains("Conta bloqueada temporariamente", await Apoio.HtmlAsync(entrar));
    }
}

/// <summary>Fábrica própria: o segundo administrador não pode interferir no teste do "único administrador".</summary>
public class DoisAdministradoresTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Com_dois_administradores_um_pode_excluir_a_propria_conta()
    {
        var segundo = await Apoio.CriarUsuarioAsync(app, "admin2@teste.local");
        using (var scope = app.Services.CreateScope())
        {
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var r = await usuarios.AddToRoleAsync((await usuarios.FindByIdAsync(segundo.Id))!, Papeis.Administrador);
            Assert.True(r.Succeeded);
        }
        var cliente = await Apoio.ClienteLogadoAsync(app, "admin2@teste.local");

        var resposta = await GerenciarTests.EnviarAsync(cliente, "Excluir", new() { ["Exclusao.SenhaAtual"] = Apoio.SenhaPadrao });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString);
        Assert.False(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Id == segundo.Id)));
        Assert.True(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Email == FabricaApp.AdminEmail)));
    }
}

public class PrivacidadeTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Politica_abre_sem_login_e_cobre_a_LGPD()
    {
        var resposta = await app.CriarCliente().GetAsync("/Privacidade");
        var html = await Apoio.HtmlAsync(resposta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        foreach (var trecho in new[] { "LGPD", "art. 7º", "art. 18", "antifalsificação", "contador anônimo", "autorização de uso de imagem",
                     "Última atualização: 04/11/2026", "youtube-nocookie.com", "https://policies.google.com/privacy" })
            Assert.Contains(trecho, html);
    }

    /// <summary>Nomes de cookie que a política documenta (o de antifalsificação tem sufixo variável).</summary>
    private static readonly string[] Documentados = ["fisicocombat.auth", ".AspNetCore.Antiforgery.", ".AspNetCore.Mvc.CookieTempDataProvider"];

    [Fact]
    public async Task Todo_cookie_gravado_no_cadastro_e_na_saida_esta_na_politica()
    {
        var politica = await Apoio.HtmlAsync(await app.CriarCliente().GetAsync("/Privacidade"));
        var cliente = app.CriarCliente();
        var nomes = new HashSet<string>();
        async Task<HttpResponseMessage> AnotarAsync(Task<HttpResponseMessage> requisicao)
        {
            var resposta = await requisicao;
            if (resposta.Headers.TryGetValues(HeaderNames.SetCookie, out var valores))
                foreach (var cookie in SetCookieHeaderValue.ParseList(valores.ToList()))
                    // Cookie já expirado é só ordem de apagar (ex.: Identity.External na saída); não guarda nada.
                    if (cookie.Expires is not { } expira || expira > DateTimeOffset.UtcNow)
                        nomes.Add(cookie.Name.ToString());
            return resposta;
        }
        async Task<HttpResponseMessage> EnviarAsync(string url, Dictionary<string, string> campos)
        {
            var html = await (await AnotarAsync(cliente.GetAsync(url))).Content.ReadAsStringAsync();
            campos["__RequestVerificationToken"] = Formularios.Token(html);
            return await AnotarAsync(cliente.PostAsync(url, new FormUrlEncodedContent(campos)));
        }

        var cadastro = await EnviarAsync("/Conta/Cadastrar", new()
        {
            ["Input.NomeExibicao"] = "Cookie Teste",
            ["Input.Email"] = "cookies@teste.local",
            ["Input.Senha"] = "SenhaNova123",
            ["Input.ConfirmarSenha"] = "SenhaNova123",
            ["Input.AceitoPrivacidade"] = "true",
        });
        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);
        await AnotarAsync(cliente.GetAsync("/"));
        var saida = await EnviarAsync("/Conta/Sair", []);
        Assert.Equal(HttpStatusCode.Redirect, saida.StatusCode);
        await AnotarAsync(cliente.GetAsync("/"));

        Assert.Contains("fisicocombat.auth", nomes);
        Assert.Contains(".AspNetCore.Mvc.CookieTempDataProvider", nomes);
        foreach (var nome in nomes)
        {
            var documentado = nome.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal) ? ".AspNetCore.Antiforgery." : nome;
            Assert.True(Documentados.Contains(documentado), $"Cookie não documentado na Política de Privacidade: {nome}");
            Assert.Contains($"<code>{documentado}</code>", politica);
        }
    }
}
