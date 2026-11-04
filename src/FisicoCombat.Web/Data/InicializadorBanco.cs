using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Data;

/// <summary>
/// Aplica as migrações, cria o papel de administrador e, se configurado, o
/// administrador inicial. As credenciais vêm de configuração externa
/// (variáveis de ambiente ou user-secrets), nunca do repositório.
/// </summary>
public static class InicializadorBanco
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(InicializadorBanco));

        GarantirPastaDoBanco(db.Database.GetConnectionString());
        await db.Database.MigrateAsync();

        var papeis = sp.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await papeis.RoleExistsAsync(Papeis.Administrador))
            await papeis.CreateAsync(new IdentityRole(Papeis.Administrador));

        var config = sp.GetRequiredService<IConfiguration>().GetSection("AdminInicial");
        var email = config["Email"];
        var senha = config["Senha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogInformation("AdminInicial não configurado; nenhum administrador criado.");
            return;
        }

        var usuarios = sp.GetRequiredService<UserManager<Usuario>>();
        var admin = await usuarios.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new Usuario
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NomeExibicao = config["Nome"] ?? "Administrador",
                ConsentimentoEm = DateTime.UtcNow,
            };
            var criado = await usuarios.CreateAsync(admin, senha);
            if (!criado.Succeeded)
                throw new InvalidOperationException("Falha ao criar o administrador inicial: " +
                    string.Join("; ", criado.Errors.Select(e => e.Description)));
        }

        if (!await usuarios.IsInRoleAsync(admin, Papeis.Administrador))
            await usuarios.AddToRoleAsync(admin, Papeis.Administrador);
    }

    /// <summary>
    /// O SQLite cria o arquivo do banco, mas não a pasta. Em produção o banco fica numa
    /// subpasta (ex.: App_Data), que ainda não existe na primeira execução.
    /// </summary>
    internal static void GarantirPastaDoBanco(string? connectionString)
    {
        var arquivo = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrEmpty(arquivo) || arquivo == ":memory:") return;
        var pasta = Path.GetDirectoryName(Path.GetFullPath(arquivo));
        if (!string.IsNullOrEmpty(pasta)) Directory.CreateDirectory(pasta);
    }
}
