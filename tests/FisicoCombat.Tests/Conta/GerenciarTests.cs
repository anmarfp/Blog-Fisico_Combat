using System.Net;
using FisicoCombat.Tests.Infra;
using Microsoft.EntityFrameworkCore;

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
    private static Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string handler, Dictionary<string, string> campos) =>
        Formularios.EnviarAsync(cliente, "/Conta/Gerenciar", campos, "/Conta/Gerenciar?handler=" + handler);

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
        await Apoio.CriarComentarioAsync(app, usuario.Id, "Comentário que deve sumir");
        var cliente = await Apoio.ClienteLogadoAsync(app, "excluir@teste.local");

        var errada = await EnviarAsync(cliente, "Excluir", new() { ["Exclusao.SenhaAtual"] = "NaoEhEssa123" });
        Assert.Equal(HttpStatusCode.OK, errada.StatusCode);
        Assert.True(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Id == usuario.Id)));

        var resposta = await EnviarAsync(cliente, "Excluir", new() { ["Exclusao.SenhaAtual"] = Apoio.SenhaPadrao });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString);
        Assert.False(await app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Id == usuario.Id)));
        Assert.False(await app.ExecutarAsync(db => db.Comentarios.AnyAsync(c => c.UsuarioId == usuario.Id)));
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
}

public class PrivacidadeTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Politica_abre_sem_login_e_cobre_a_LGPD()
    {
        var resposta = await app.CriarCliente().GetAsync("/Privacidade");
        var html = await Apoio.HtmlAsync(resposta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        foreach (var trecho in new[] { "LGPD", "art. 7º", "art. 18", "antifalsificação", "contador anônimo", "autorização de uso de imagem", "Última atualização: 04/11/2026" })
            Assert.Contains(trecho, html);
    }
}
