using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FisicoCombat.Tests.Conta;

public class CadastroTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    private static Dictionary<string, string> Campos(string email, string confirmar = "SenhaNova123", bool aceito = true)
    {
        var campos = new Dictionary<string, string>
        {
            ["Input.NomeExibicao"] = "Maria Aluna",
            ["Input.Email"] = email,
            ["Input.Senha"] = "SenhaNova123",
            ["Input.ConfirmarSenha"] = confirmar,
        };
        if (aceito) campos["Input.AceitoPrivacidade"] = "true";
        return campos;
    }

    private Task<bool> ExisteAsync(string email) => app.ExecutarAsync(db => db.Users.AnyAsync(u => u.Email == email));

    [Fact]
    public async Task Cadastro_cria_usuario_comum_com_consentimento_e_ja_entra()
    {
        var cliente = app.CriarCliente();
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var resposta = await Formularios.EnviarAsync(cliente, "/Conta/Cadastrar", Campos("maria@teste.local"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString);
        var usuario = await app.ExecutarAsync(db => db.Users.SingleAsync(u => u.Email == "maria@teste.local"));
        Assert.Equal("Maria Aluna", usuario.NomeExibicao);
        Assert.NotNull(usuario.ConsentimentoEm);
        Assert.True(usuario.ConsentimentoEm >= antes);

        using var scope = app.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Assert.False(await usuarios.IsInRoleAsync(usuario, Papeis.Administrador));

        Assert.True(await Apoio.LogadoAsync(cliente));
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/Admin")).StatusCode);
    }

    [Fact]
    public async Task Sem_aceite_da_politica_nada_e_criado()
    {
        var resposta = await Formularios.EnviarAsync(app.CriarCliente(), "/Conta/Cadastrar", Campos("sem-aceite@teste.local", aceito: false));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Você precisa aceitar a Política de Privacidade para criar a conta.", await Apoio.HtmlAsync(resposta));
        Assert.False(await ExisteAsync("sem-aceite@teste.local"));
    }

    [Fact]
    public async Task Senhas_diferentes_sao_recusadas()
    {
        var resposta = await Formularios.EnviarAsync(app.CriarCliente(), "/Conta/Cadastrar", Campos("diferentes@teste.local", confirmar: "OutraSenha123"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("As senhas não conferem.", await Apoio.HtmlAsync(resposta));
        Assert.False(await ExisteAsync("diferentes@teste.local"));
    }

    [Fact]
    public async Task Email_ja_cadastrado_e_recusado()
    {
        var resposta = await Formularios.EnviarAsync(app.CriarCliente(), "/Conta/Cadastrar", Campos(FabricaApp.AdminEmail));
        var html = await Apoio.HtmlAsync(resposta);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var mensagem = $"O e-mail {FabricaApp.AdminEmail} já está cadastrado.";
        Assert.Equal(2, html.Split(mensagem).Length); // aparece uma vez só
        Assert.Equal(1, await app.ExecutarAsync(db => db.Users.CountAsync(u => u.Email == FabricaApp.AdminEmail)));
    }

    [Fact]
    public async Task ReturnUrl_externo_no_cadastro_e_ignorado()
    {
        var resposta = await Formularios.EnviarAsync(app.CriarCliente(),
            "/Conta/Cadastrar?ReturnUrl=" + Uri.EscapeDataString("//evil.com"), Campos("retorno@teste.local"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location!.OriginalString);
    }
}
