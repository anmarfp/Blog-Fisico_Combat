using System.ComponentModel.DataAnnotations;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FisicoCombat.Web.Pages.Conta.Gerenciar;

/// <summary>"Minha conta": ver dados, mudar nome, trocar senha e excluir a conta (LGPD, art. 18).</summary>
public class IndexModel(UserManager<Usuario> usuarios, SignInManager<Usuario> signIn) : PageModel
{
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public Usuario Usuario { get; private set; } = null!;

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

        Usuario.NomeExibicao = Nome.NomeExibicao.Trim();
        var resultado = await usuarios.UpdateAsync(Usuario);
        if (!Sucesso(resultado, $"{nameof(Nome)}.{nameof(NomeModel.NomeExibicao)}")) return Page();

        TempData["Mensagem"] = "Nome atualizado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSenhaAsync()
    {
        if (!await CarregarAsync(nameof(Senha))) return Sair();
        if (!ModelState.IsValid) return Page();

        var resultado = await usuarios.ChangePasswordAsync(Usuario, Senha.SenhaAtual, Senha.NovaSenha);
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
            ModelState.AddModelError("Exclusao.SenhaAtual", "Senha incorreta.");
            return Page();
        }

        if (await usuarios.IsInRoleAsync(Usuario, Papeis.Administrador)
            && (await usuarios.GetUsersInRoleAsync(Papeis.Administrador)).Count == 1)
        {
            ModelState.AddModelError("Exclusao.SenhaAtual",
                "Você é o único administrador do blog. Cadastre outro administrador antes de excluir esta conta.");
            return Page();
        }

        // Os comentários do usuário são apagados em cascata (Data/AppDbContext.cs).
        var resultado = await usuarios.DeleteAsync(Usuario);
        if (!Sucesso(resultado, "Exclusao.SenhaAtual")) return Page();

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
        return true;
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
