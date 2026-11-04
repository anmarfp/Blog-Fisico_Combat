using System.Globalization;
using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Padrao") ?? "Data Source=fisicocombat.db"));

builder.Services
    .AddIdentity<Usuario, IdentityRole>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.SignIn.RequireConfirmedAccount = false;
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddErrorDescriber<ErrosIdentityPtBr>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Conta/Entrar";
    o.LogoutPath = "/Conta/Sair";
    o.AccessDeniedPath = "/Conta/AcessoNegado";
    o.Cookie.Name = "fisicocombat.auth";
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
});

// Conta excluída ou senha trocada encerra as sessões de outros aparelhos em até 5 minutos (padrão: 30).
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Papeis.Administrador, p => p.RequireRole(Papeis.Administrador));

builder.Services.AddRazorPages(o =>
    {
        o.Conventions.AuthorizeFolder("/Admin", Papeis.Administrador);
        o.Conventions.AuthorizeFolder("/Conta/Gerenciar");
    })
    .AddMvcOptions(o => MensagensValidacao.Configurar(o.ModelBindingMessageProvider));

builder.Services.AddSingleton<ArmazenamentoImagens>();

var app = builder.Build();

await InicializadorBanco.InicializarAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.XContentTypeOptions = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

var ptBr = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = [ptBr],
    SupportedUICultures = [ptBr],
});

// Fotos enviadas em tempo de execução (fora do manifesto de MapStaticAssets).
var uploads = app.Services.GetRequiredService<ArmazenamentoImagens>().Pasta;
Directory.CreateDirectory(uploads);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploads),
    RequestPath = "/uploads",
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;
