using System.ComponentModel.DataAnnotations;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FisicoCombat.Web.Pages.Conta;

/// <summary>Login único para administrador (RF01) e usuário comum (RF08).</summary>
public class EntrarModel(SignInManager<Usuario> signIn, UserManager<Usuario> usuarios) : PageModel
{
    [BindProperty]
    public EntradaModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class EntradaModel
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = "";

        [Display(Name = "Manter conectado neste dispositivo")]
        public bool Lembrar { get; set; }
    }

    public IActionResult OnGet() => signIn.IsSignedIn(User) ? LocalRedirect("/") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (signIn.IsSignedIn(User)) return LocalRedirect("/");
        if (!ModelState.IsValid) return Page();

        var usuario = await usuarios.FindByEmailAsync(Input.Email);
        if (usuario is not null)
        {
            var resultado = await signIn.PasswordSignInAsync(usuario.UserName!, Input.Senha, Input.Lembrar, lockoutOnFailure: true);
            if (resultado.Succeeded)
                return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError("", "Conta bloqueada temporariamente por excesso de tentativas. Tente novamente em 15 minutos.");
                return Page();
            }
        }

        // Mesma mensagem para e-mail inexistente e senha errada, para não revelar quem tem conta.
        ModelState.AddModelError("", "E-mail ou senha incorretos.");
        return Page();
    }
}
