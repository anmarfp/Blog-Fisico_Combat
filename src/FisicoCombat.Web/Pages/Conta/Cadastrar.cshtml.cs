using System.ComponentModel.DataAnnotations;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FisicoCombat.Web.Pages.Conta;

/// <summary>Cadastro de usuário comum (RF08) com consentimento da LGPD.</summary>
public class CadastrarModel(SignInManager<Usuario> signIn, UserManager<Usuario> usuarios) : PageModel
{
    [BindProperty]
    public CadastroModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class CadastroModel
    {
        [Required(ErrorMessage = "Informe seu nome.")]
        [StringLength(60, ErrorMessage = "O nome pode ter até {1} caracteres.")]
        [Display(Name = "Nome de exibição")]
        public string NomeExibicao { get; set; } = "";

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(256, ErrorMessage = "O e-mail pode ter até {1} caracteres.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Informe a senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = "";

        [Required(ErrorMessage = "Confirme a senha.")]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirme a senha")]
        public string ConfirmarSenha { get; set; } = "";

        public bool AceitoPrivacidade { get; set; }
    }

    public IActionResult OnGet() => signIn.IsSignedIn(User) ? LocalRedirect("/") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (signIn.IsSignedIn(User)) return LocalRedirect("/");

        // [Required] não serve para bool: false também é um valor.
        if (!Input.AceitoPrivacidade)
            ModelState.AddModelError("Input.AceitoPrivacidade", "Você precisa aceitar a Política de Privacidade para criar a conta.");
        if (!ModelState.IsValid) return Page();

        var usuario = new Usuario
        {
            UserName = Input.Email,
            Email = Input.Email,
            NomeExibicao = Input.NomeExibicao.Trim(),
            ConsentimentoEm = DateTime.UtcNow,
        };
        var resultado = await usuarios.CreateAsync(usuario, Input.Senha);
        if (!resultado.Succeeded)
        {
            // E-mail repetido gera dois erros (usuário e e-mail) com o mesmo texto.
            foreach (var erro in resultado.Errors.Select(e => e.Description).Distinct())
                ModelState.AddModelError("", erro);
            return Page();
        }

        await signIn.SignInAsync(usuario, isPersistent: false);
        TempData["Mensagem"] = $"Conta criada. Boas-vindas, {usuario.NomeExibicao}!";
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }
}
