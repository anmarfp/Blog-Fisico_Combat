using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FisicoCombat.Web.Pages.Conta;

public class AcessoNegadoModel : PageModel
{
    /// <summary>Página que o usuário tentou abrir (enviada pelo cookie de autenticação).</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>Destino depois de sair: o login, voltando à página recusada se ela for local.</summary>
    public string EntrarComOutraConta =>
        Url.Page("/Conta/Entrar", new { returnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null })!;

    public void OnGet()
    {
    }
}
