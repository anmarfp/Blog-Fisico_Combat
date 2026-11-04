using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Modalidades;

public class CriarModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Modalidade Modalidade { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var nome = Modalidade.Nome.Trim();
        if (await db.Modalidades.AnyAsync(m => m.Nome.ToLower() == nome.ToLower()))
        {
            ModelState.AddModelError("Modalidade.Nome", "Já existe uma modalidade com esse nome.");
            return Page();
        }

        var nova = new Modalidade
        {
            Nome = nome,
            Descricao = Modalidade.Descricao?.Trim(),
            Slug = await SlugUnico.GerarAsync(nome, 80, s => db.Modalidades.AnyAsync(m => m.Slug == s)),
        };
        db.Modalidades.Add(nova);
        await db.SaveChangesAsync();

        TempData["Mensagem"] = $"Modalidade \"{nova.Nome}\" criada.";
        return RedirectToPage("Index");
    }
}
