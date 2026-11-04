using System.Net;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FisicoCombat.Tests.Conta;

/// <summary>Atalhos dos testes de conta: login real pelo formulário e criação de dados.</summary>
public static class Apoio
{
    public const string SenhaPadrao = "SenhaUsuario123";

    /// <summary>Envia o formulário de login (sem cabeçalho de teste: usa o cookie real do Identity).</summary>
    public static Task<HttpResponseMessage> EntrarAsync(HttpClient cliente, string email, string senha, string url = "/Conta/Entrar") =>
        Formularios.EnviarAsync(cliente, url, new() { ["Input.Email"] = email, ["Input.Senha"] = senha });

    /// <summary>Cliente com cookie de login obtido pelo formulário.</summary>
    public static async Task<HttpClient> ClienteLogadoAsync(FabricaApp app, string email, string senha = SenhaPadrao)
    {
        var cliente = app.CriarCliente();
        var resposta = await EntrarAsync(cliente, email, senha);
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        return cliente;
    }

    public static async Task<Usuario> CriarUsuarioAsync(FabricaApp app, string email, string senha = SenhaPadrao)
    {
        using var scope = app.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = new Usuario { UserName = email, Email = email, NomeExibicao = "Fulano", ConsentimentoEm = DateTime.UtcNow };
        var r = await usuarios.CreateAsync(usuario, senha);
        Assert.True(r.Succeeded, string.Join("; ", r.Errors.Select(e => e.Description)));
        return usuario;
    }

    /// <summary>Cria uma postagem e um comentário do usuário; devolve o Id do comentário.</summary>
    public static async Task<int> CriarComentarioAsync(FabricaApp app, string usuarioId, string texto,
        StatusComentario status = StatusComentario.Pendente)
    {
        var comentario = new Comentario
        {
            UsuarioId = usuarioId,
            Texto = texto,
            Status = status,
            CriadoEm = DateTime.UtcNow,
            Postagem = new Postagem { Titulo = "Postagem de teste", Slug = "postagem-de-teste-" + Guid.NewGuid().ToString("N")[..8], PublicadaEm = DateTime.UtcNow },
        };
        await app.ExecutarAsync(async db =>
        {
            db.Comentarios.Add(comentario);
            await db.SaveChangesAsync();
        });
        return comentario.Id;
    }

    /// <summary>HTML da resposta com entidades decodificadas (o Razor codifica acentos em textos dinâmicos).</summary>
    public static async Task<string> HtmlAsync(HttpResponseMessage resposta) =>
        WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());

    /// <summary>true se o cliente está logado (Minha conta abre em vez de redirecionar ao login).</summary>
    public static async Task<bool> LogadoAsync(HttpClient cliente) =>
        (await cliente.GetAsync("/Conta/Gerenciar")).StatusCode == HttpStatusCode.OK;
}
