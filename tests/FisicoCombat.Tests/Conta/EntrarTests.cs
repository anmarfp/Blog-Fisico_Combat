using System.Net;
using FisicoCombat.Tests.Infra;

namespace FisicoCombat.Tests.Conta;

public class EntrarTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Login_real_do_administrador_libera_o_painel()
    {
        var cliente = app.CriarCliente();
        var resposta = await Apoio.EntrarAsync(cliente, FabricaApp.AdminEmail, FabricaApp.AdminSenha);

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Admin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Admin/Comentarios")).StatusCode);
    }

    [Fact]
    public async Task Senha_errada_ou_email_inexistente_mostram_a_mesma_mensagem()
    {
        await Apoio.CriarUsuarioAsync(app, "senha-errada@teste.local");

        var senhaErrada = await Apoio.EntrarAsync(app.CriarCliente(), "senha-errada@teste.local", "OutraSenha999");
        var semConta = await Apoio.EntrarAsync(app.CriarCliente(), "ninguem@teste.local", "OutraSenha999");

        Assert.Equal(HttpStatusCode.OK, senhaErrada.StatusCode);
        Assert.Contains("E-mail ou senha incorretos.", await Apoio.HtmlAsync(senhaErrada));
        Assert.Equal(HttpStatusCode.OK, semConta.StatusCode);
        Assert.Contains("E-mail ou senha incorretos.", await Apoio.HtmlAsync(semConta));
    }

    [Fact]
    public async Task ReturnUrl_externo_e_ignorado()
    {
        var resposta = await Apoio.EntrarAsync(app.CriarCliente(), FabricaApp.AdminEmail, FabricaApp.AdminSenha,
            "/Conta/Entrar?ReturnUrl=" + Uri.EscapeDataString("https://evil.com/roubo"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ReturnUrl_local_e_respeitado()
    {
        var resposta = await Apoio.EntrarAsync(app.CriarCliente(), FabricaApp.AdminEmail, FabricaApp.AdminSenha,
            "/Conta/Entrar?ReturnUrl=" + Uri.EscapeDataString("/Admin/Comentarios?status=Todos"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/Admin/Comentarios?status=Todos", resposta.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Quem_ja_esta_logado_volta_ao_inicio()
    {
        var cliente = await Apoio.ClienteLogadoAsync(app, FabricaApp.AdminEmail, FabricaApp.AdminSenha);

        foreach (var url in new[] { "/Conta/Entrar", "/Conta/Cadastrar" })
        {
            var resposta = await cliente.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
            Assert.Equal("/", resposta.Headers.Location!.OriginalString);
        }
    }

    [Fact]
    public async Task Acesso_negado_explica_e_oferece_outra_conta()
    {
        var (cliente, _) = await app.CriarClienteUsuarioAsync("negado@teste.local");

        var resposta = await cliente.GetAsync("/Conta/AcessoNegado?ReturnUrl=%2FAdmin");
        var html = await Apoio.HtmlAsync(resposta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("não tem permissão", html);
        Assert.Contains("Entrar com outra conta", html);
    }
}

/// <summary>Classe e fixture próprias: o bloqueio não pode vazar para outros testes.</summary>
public class BloqueioTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Cinco_senhas_erradas_bloqueiam_a_conta()
    {
        const string email = "bloqueio@teste.local";
        await Apoio.CriarUsuarioAsync(app, email);
        var cliente = app.CriarCliente();

        for (var i = 1; i <= 4; i++)
        {
            var html = await Apoio.HtmlAsync(await Apoio.EntrarAsync(cliente, email, "SenhaErrada999"));
            Assert.Contains("E-mail ou senha incorretos.", html);
        }
        var quinta = await Apoio.HtmlAsync(await Apoio.EntrarAsync(cliente, email, "SenhaErrada999"));
        Assert.Contains("Conta bloqueada temporariamente por excesso de tentativas. Tente novamente em 15 minutos.", quinta);

        // Nem a senha certa entra durante o bloqueio.
        var certa = await Apoio.EntrarAsync(cliente, email, Apoio.SenhaPadrao);
        Assert.Equal(HttpStatusCode.OK, certa.StatusCode);
        Assert.Contains("Conta bloqueada temporariamente", await Apoio.HtmlAsync(certa));
        Assert.False(await Apoio.LogadoAsync(cliente));
    }
}
