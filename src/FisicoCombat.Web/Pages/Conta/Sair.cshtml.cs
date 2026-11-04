using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FisicoCombat.Web.Pages.Conta;

/// <summary>Sair só por POST (com antifalsificação); o GET apenas mostra o botão.</summary>
public class SairModel(SignInManager<Usuario> signIn) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await signIn.SignOutAsync();
        TempData["Mensagem"] = "Você saiu da sua conta.";
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }
}
