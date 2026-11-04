using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Tests.Fundacao;

public class AppTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public async Task Pagina_inicial_abre_em_portugues()
    {
        var resposta = await app.CriarCliente().GetAsync("/");
        var html = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("lang=\"pt-BR\"", html);
        Assert.Contains("Físico Combat", html);
        Assert.Equal("nosniff", resposta.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Inicializacao_cria_administrador_configurado()
    {
        var ehAdmin = await app.ExecutarAsync(async db =>
            await (from u in db.Users
                   join ur in db.UserRoles on u.Id equals ur.UserId
                   join r in db.Roles on ur.RoleId equals r.Id
                   where u.Email == FabricaApp.AdminEmail && r.Name == Papeis.Administrador
                   select u).AnyAsync());
        Assert.True(ehAdmin);
    }

    [Fact]
    public async Task Area_admin_exige_login()
    {
        var resposta = await app.CriarCliente().GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.StartsWith("/Conta/Entrar", resposta.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Area_admin_recusa_usuario_comum()
    {
        var (cliente, _) = await app.CriarClienteUsuarioAsync();
        var resposta = await cliente.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.StartsWith("/Conta/AcessoNegado", resposta.Headers.Location!.PathAndQuery);
    }
}
