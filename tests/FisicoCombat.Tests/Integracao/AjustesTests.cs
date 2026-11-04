using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FisicoCombat.Tests.Integracao;

/// <summary>Ajustes de integração apontados pelas revisões das três áreas.</summary>
public class AjustesTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    [Fact]
    public void Hora_local_converte_utc_para_brasilia()
    {
        var local = HoraLocal.DeUtc(new DateTime(2026, 3, 10, 15, 30, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 3, 10, 12, 30, 0), local);
    }

    [Fact]
    public async Task Nome_de_modalidade_com_acento_e_caixa_diferente_e_duplicado()
    {
        await app.ExecutarAsync(db => { db.Modalidades.Add(new Modalidade { Nome = "JUDÔ", Slug = "judo" }); return db.SaveChangesAsync(); });
        var admin = await app.CriarClienteAdminAsync();

        var resposta = await Formularios.EnviarAsync(admin, "/Admin/Modalidades/Criar",
            new() { ["Modalidade.Nome"] = "judô" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Já existe uma modalidade com esse nome.", WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync()));
        Assert.Equal(1, await app.ExecutarAsync(db => db.Modalidades.CountAsync(m => m.Slug.StartsWith("judo"))));
    }

    [Fact]
    public async Task Banco_recusa_duas_postagens_com_o_mesmo_slug()
    {
        await app.ExecutarAsync(db =>
        {
            db.Postagens.Add(new Postagem { Titulo = "A", Slug = "mesmo-slug", PublicadaEm = DateTime.UtcNow });
            return db.SaveChangesAsync();
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => app.ExecutarAsync(db =>
        {
            db.Postagens.Add(new Postagem { Titulo = "B", Slug = "mesmo-slug", PublicadaEm = DateTime.UtcNow });
            return db.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Comentario_de_conta_ja_excluida_pede_login_e_nao_grava()
    {
        var postagem = await app.ExecutarAsync(async db =>
        {
            var p = new Postagem { Titulo = "Aula aberta", Slug = "aula-aberta-ajustes", Conteudo = "Texto", PublicadaEm = DateTime.UtcNow };
            db.Postagens.Add(p);
            await db.SaveChangesAsync();
            return p;
        });
        var (cliente, usuario) = await app.CriarClienteUsuarioAsync("excluido@teste.local");
        var url = $"/Postagens/{postagem.Id}/{postagem.Slug}";
        var html = await (await cliente.GetAsync(url)).Content.ReadAsStringAsync();
        var token = Formularios.Token(html);

        await app.ExecutarAsync(async db => { db.Users.Remove(await db.Users.SingleAsync(u => u.Id == usuario.Id)); await db.SaveChangesAsync(); });

        // O cabeçalho de teste aponta para um usuário que não existe mais: a autenticação falha e o
        // pedido cai no login, exatamente como um cookie válido de uma conta já excluída.
        var resposta = await cliente.PostAsync(url + "?handler=Comentar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Texto"] = "Olá",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.NotEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(0, await app.ExecutarAsync(db => db.Comentarios.CountAsync(c => c.PostagemId == postagem.Id)));
    }

    [Fact]
    public async Task Senha_certa_em_minha_conta_zera_as_tentativas_erradas()
    {
        var (cliente, usuario) = await app.CriarClienteUsuarioAsync("tentativas@teste.local");
        using (var scope = app.Services.CreateScope())
        {
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var u = await usuarios.FindByIdAsync(usuario.Id);
            await usuarios.AccessFailedAsync(u!);
            await usuarios.AccessFailedAsync(u!);
        }

        var resposta = await Formularios.EnviarAsync(cliente, "/Conta/Gerenciar", new()
        {
            ["Senha.SenhaAtual"] = "SenhaUsuario123",
            ["Senha.NovaSenha"] = "NovaSenha12345",
            ["Senha.ConfirmarSenha"] = "NovaSenha12345",
        }, urlPost: "/Conta/Gerenciar?handler=Senha");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal(0, await app.ExecutarAsync(db => db.Users.Where(u => u.Id == usuario.Id).Select(u => u.AccessFailedCount).SingleAsync()));
    }

    [Fact]
    public async Task Cartao_da_postagem_mostra_resumo_do_texto_cortado_em_palavra()
    {
        var texto = string.Join(" ", Enumerable.Repeat("treino", 40));
        await app.ExecutarAsync(db =>
        {
            db.Postagens.Add(new Postagem { Titulo = "Resumo longo", Slug = "resumo-longo", Conteudo = texto, PublicadaEm = DateTime.UtcNow.AddYears(1) });
            return db.SaveChangesAsync();
        });

        var html = WebUtility.HtmlDecode(await (await app.CriarCliente().GetAsync("/")).Content.ReadAsStringAsync());

        Assert.Contains("treino treino…", html);
        Assert.DoesNotContain(texto, html);
    }
}
