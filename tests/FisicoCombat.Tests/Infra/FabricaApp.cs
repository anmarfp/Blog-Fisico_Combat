using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FisicoCombat.Tests.Infra;

/// <summary>
/// Sobe o blog em memória com um banco SQLite próprio e uma pasta de uploads temporária.
/// Cada instância é isolada; use uma por classe de teste (IClassFixture) ou por teste.
/// </summary>
public class FabricaApp : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@teste.local";
    public const string AdminSenha = "SenhaAdmin123";

    private readonly SqliteConnection _conexao = new("Data Source=:memory:");
    public string PastaUploads { get; } = Path.Combine(Path.GetTempPath(), "fc-testes-" + Guid.NewGuid().ToString("N"));

    public FabricaApp() => _conexao.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testes");
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminInicial:Email"] = AdminEmail,
            ["AdminInicial:Senha"] = AdminSenha,
            ["AdminInicial:Nome"] = "Admin de Teste",
            ["Armazenamento:PastaUploads"] = PastaUploads,
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_conexao));

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, AutenticacaoTeste>(AutenticacaoTeste.Esquema, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                // Requisições com o cabeçalho de teste são autenticadas por ele; as demais usam o cookie normal.
                o.DefaultAuthenticateScheme = AutenticacaoTeste.Seletor;
                o.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            });
            services.AddAuthentication().AddPolicyScheme(AutenticacaoTeste.Seletor, null, o =>
                o.ForwardDefaultSelector = ctx => ctx.Request.Headers.ContainsKey(AutenticacaoTeste.Cabecalho)
                    ? AutenticacaoTeste.Esquema
                    : IdentityConstants.ApplicationScheme);
        });
    }

    /// <summary>Cliente que não segue redirecionamentos, para verificar 302 de login/autorização.</summary>
    public HttpClient CriarCliente() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Cliente autenticado como o administrador inicial.</summary>
    public async Task<HttpClient> CriarClienteAdminAsync()
    {
        var admin = await ExecutarAsync(db => db.Users.SingleAsync(u => u.Email == AdminEmail));
        return ClienteComo(admin.Id);
    }

    /// <summary>Cria um usuário comum e devolve um cliente autenticado como ele.</summary>
    public async Task<(HttpClient Cliente, Usuario Usuario)> CriarClienteUsuarioAsync(string email = "usuario@teste.local")
    {
        using var scope = Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null)
        {
            usuario = new Usuario { UserName = email, Email = email, NomeExibicao = "Usuário de Teste", ConsentimentoEm = DateTime.UtcNow };
            var r = await usuarios.CreateAsync(usuario, "SenhaUsuario123");
            if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
        }
        return (ClienteComo(usuario.Id), usuario);
    }

    private HttpClient ClienteComo(string usuarioId)
    {
        var cliente = CriarCliente();
        cliente.DefaultRequestHeaders.Add(AutenticacaoTeste.Cabecalho, usuarioId);
        return cliente;
    }

    /// <summary>Executa código com um AppDbContext do app em teste (para preparar ou conferir dados).</summary>
    public async Task<T> ExecutarAsync<T>(Func<AppDbContext, Task<T>> acao)
    {
        using var scope = Services.CreateScope();
        return await acao(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task ExecutarAsync(Func<AppDbContext, Task> acao) =>
        ExecutarAsync(async db => { await acao(db); return 0; });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _conexao.Dispose();
            if (Directory.Exists(PastaUploads)) Directory.Delete(PastaUploads, recursive: true);
        }
    }
}

/// <summary>Autentica a requisição como o usuário cujo Id vem no cabeçalho X-Teste-Usuario, com os papéis do banco.</summary>
public class AutenticacaoTeste(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    UserManager<Usuario> usuarios)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Esquema = "Teste";
    public const string Seletor = "TesteOuCookie";
    public const string Cabecalho = "X-Teste-Usuario";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = Request.Headers[Cabecalho].ToString();
        var usuario = await usuarios.FindByIdAsync(id);
        if (usuario is null) return AuthenticateResult.Fail("Usuário de teste inexistente.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.UserName ?? ""),
        };
        claims.AddRange((await usuarios.GetRolesAsync(usuario)).Select(p => new Claim(ClaimTypes.Role, p)));
        // Tipo "Identity.Application" para que SignInManager.IsSignedIn reconheça o usuário.
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Esquema));
    }
}

public static partial class Formularios
{
    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""")]
    private static partial Regex TokenRegex();

    /// <summary>Extrai o token antiforgery do HTML de um formulário.</summary>
    public static string Token(string html) =>
        TokenRegex().Match(html) is { Success: true } m
            ? m.Groups[1].Value
            : throw new InvalidOperationException("Token antiforgery não encontrado no HTML.");

    /// <summary>Faz GET na página do formulário e POST com os campos + token antiforgery.</summary>
    public static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string url, Dictionary<string, string> campos, string? urlPost = null)
    {
        var html = await (await cliente.GetAsync(url)).Content.ReadAsStringAsync();
        campos["__RequestVerificationToken"] = Token(html);
        return await cliente.PostAsync(urlPost ?? url, new FormUrlEncodedContent(campos));
    }

    /// <summary>Envia um formulário multipart (com arquivo) incluindo o token antiforgery da página.</summary>
    public static async Task<HttpResponseMessage> EnviarMultipartAsync(HttpClient cliente, string url, Dictionary<string, string> campos,
        string campoArquivo, byte[] arquivo, string nomeArquivo, string tipo)
    {
        var html = await (await cliente.GetAsync(url)).Content.ReadAsStringAsync();
        using var conteudo = new MultipartFormDataContent();
        foreach (var (k, v) in campos) conteudo.Add(new StringContent(v), k);
        conteudo.Add(new StringContent(Token(html)), "__RequestVerificationToken");
        var arq = new ByteArrayContent(arquivo);
        arq.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        conteudo.Add(arq, campoArquivo, nomeArquivo);
        return await cliente.PostAsync(url, conteudo);
    }

    /// <summary>Bytes mínimos com assinatura PNG válida (8 bytes + preenchimento).</summary>
    public static byte[] PngFalso() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52];
}
