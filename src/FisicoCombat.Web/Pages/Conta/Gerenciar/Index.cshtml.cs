using System.ComponentModel.DataAnnotations;
using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Conta.Gerenciar;

/// <summary>"Minha conta": ver dados e comentários, mudar nome, trocar senha e excluir a conta (LGPD, art. 18).</summary>
public class IndexModel(UserManager<Usuario> usuarios, SignInManager<Usuario> signIn, AppDbContext db) : PageModel
{
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public Usuario Usuario { get; private set; } = null!;

    /// <summary>Comentários do próprio usuário, do mais novo ao mais antigo (direito de acesso, LGPD).</summary>
    public List<Comentario> Comentarios { get; private set; } = [];

    public DateTime? ConsentimentoLocal =>
        Usuario.ConsentimentoEm is { } d ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(d, DateTimeKind.Utc), Brasilia) : null;

    [BindProperty]
    public NomeModel Nome { get; set; } = new();

    [BindProperty]
    public SenhaModel Senha { get; set; } = new();

    [BindProperty]
    public ExclusaoModel Exclusao { get; set; } = new();

    public class NomeModel
    {
        [Required(ErrorMessage = "Informe seu nome.")]
        [StringLength(60, ErrorMessage = "O nome pode ter até {1} caracteres.")]
        [Display(Name = "Nome de exibição")]
        public string NomeExibicao { get; set; } = "";
    }

    public class SenhaModel
    {
        [Required(ErrorMessage = "Informe a senha atual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string SenhaAtual { get; set; } = "";

        [Required(ErrorMessage = "Informe a nova senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; } = "";

        [Required(ErrorMessage = "Confirme a nova senha.")]
        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirme a nova senha")]
        public string ConfirmarSenha { get; set; } = "";
    }

    public class ExclusaoModel
    {
        [Required(ErrorMessage = "Informe sua senha para confirmar.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string SenhaAtual { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CarregarAsync()) return Sair();
        return Page();
    }

    public async Task<IActionResult> OnPostNomeAsync()
    {
        if (!await CarregarAsync(nameof(Nome))) return Sair();
        if (!ModelState.IsValid) return Page();

        var anterior = Usuario.NomeExibicao;
        Usuario.NomeExibicao = Nome.NomeExibicao.Trim();
        var resultado = await usuarios.UpdateAsync(Usuario);
        if (!Sucesso(resultado, $"{nameof(Nome)}.{nameof(NomeModel.NomeExibicao)}"))
        {
            Usuario.NomeExibicao = anterior; // "Seus dados" mostra só o que está salvo.
            return Page();
        }

        TempData["Mensagem"] = "Nome atualizado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSenhaAsync()
    {
        if (!await CarregarAsync(nameof(Senha))) return Sair();
        if (!ModelState.IsValid) return Page();

        var resultado = await usuarios.ChangePasswordAsync(Usuario, Senha.SenhaAtual, Senha.NovaSenha);
        if (resultado.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
            && await SenhaErradaBloqueouAsync())
            return await EncerrarPorBloqueioAsync();
        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
                ModelState.AddModelError(
                    erro.Code == nameof(IdentityErrorDescriber.PasswordMismatch) ? "Senha.SenhaAtual" : "Senha.NovaSenha",
                    erro.Description);
            return Page();
        }

        // A troca de senha muda o carimbo de segurança; renova o cookie desta sessão.
        await signIn.RefreshSignInAsync(Usuario);
        TempData["Mensagem"] = "Senha alterada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluirAsync()
    {
        if (!await CarregarAsync(nameof(Exclusao))) return Sair();
        if (!ModelState.IsValid) return Page();

        if (!await usuarios.CheckPasswordAsync(Usuario, Exclusao.SenhaAtual))
        {
            if (await SenhaErradaBloqueouAsync()) return await EncerrarPorBloqueioAsync();
            ModelState.AddModelError("Exclusao.SenhaAtual", "Senha incorreta.");
            return Page();
        }

        // UserManager usa este mesmo AppDbContext; a transação (BEGIN IMMEDIATE no SQLite) impede
        // que dois administradores se excluam ao mesmo tempo e o blog fique sem nenhum.
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (await usuarios.IsInRoleAsync(Usuario, Papeis.Administrador)
            && (await usuarios.GetUsersInRoleAsync(Papeis.Administrador)).Count == 1)
        {
            ModelState.AddModelError("Exclusao.SenhaAtual",
                "Você é o único administrador do blog. Peça a quem mantém o blog para configurar outro administrador antes de excluir esta conta.");
            return Page();
        }

        // Os comentários do usuário são apagados em cascata (Data/AppDbContext.cs).
        var resultado = await usuarios.DeleteAsync(Usuario);
        if (!Sucesso(resultado, "Exclusao.SenhaAtual")) return Page();
        await transacao.CommitAsync();

        await signIn.SignOutAsync();
        TempData["Mensagem"] = "Sua conta e seus comentários foram excluídos.";
        return LocalRedirect("/");
    }

    /// <summary>
    /// Carrega o usuário logado. Cada formulário só valida os próprios campos: as entradas
    /// dos outros dois formulários (não enviadas) saem do ModelState.
    /// </summary>
    private async Task<bool> CarregarAsync(string? formulario = null)
    {
        var usuario = await usuarios.GetUserAsync(User);
        if (usuario is null) return false;
        Usuario = usuario;

        if (formulario is not null)
            foreach (var chave in ModelState.Keys.Where(k => !k.StartsWith(formulario + ".")).ToList())
                ModelState.Remove(chave);
        if (formulario != nameof(Nome))
            Nome.NomeExibicao = usuario.NomeExibicao;

        Comentarios = await db.Comentarios.AsNoTracking()
            .Where(c => c.UsuarioId == usuario.Id)
            .Include(c => c.Postagem)
            .OrderByDescending(c => c.CriadoEm).ThenByDescending(c => c.Id)
            .ToListAsync();
        return true;
    }

    /// <summary>Conta a senha errada como tentativa falha (mesmo limite do login) e diz se a conta ficou bloqueada.</summary>
    private async Task<bool> SenhaErradaBloqueouAsync()
    {
        await usuarios.AccessFailedAsync(Usuario);
        return await usuarios.IsLockedOutAsync(Usuario);
    }

    private async Task<IActionResult> EncerrarPorBloqueioAsync()
    {
        await signIn.SignOutAsync();
        TempData["Mensagem"] = "Senha incorreta muitas vezes. Por segurança, sua conta foi bloqueada por 15 minutos e você saiu dela.";
        return RedirectToPage("/Conta/Entrar");
    }

    private bool Sucesso(IdentityResult resultado, string campo)
    {
        foreach (var erro in resultado.Errors)
            ModelState.AddModelError(campo, erro.Description);
        return resultado.Succeeded;
    }

    /// <summary>Cookie de uma conta que não existe mais: encerra a sessão e volta ao login.</summary>
    private IActionResult Sair() => SignOut(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/Conta/Entrar" },
        IdentityConstants.ApplicationScheme);
}
