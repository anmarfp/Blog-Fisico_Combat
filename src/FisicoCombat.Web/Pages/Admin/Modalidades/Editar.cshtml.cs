using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Modalidades;

public class EditarModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Modalidade Modalidade { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var modalidade = await db.Modalidades.FindAsync(id);
        if (modalidade is null) return NotFound();
        Modalidade = modalidade;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var modalidade = await db.Modalidades.FindAsync(id);
        if (modalidade is null) return NotFound();
        if (!ModelState.IsValid) return Page();

        var nome = Modalidade.Nome.Trim();
        if (await CriarModel.NomeEmUsoAsync(db, nome, idAtual: id))
        {
            ModelState.AddModelError("Modalidade.Nome", "Já existe uma modalidade com esse nome.");
            return Page();
        }

        if (nome != modalidade.Nome)
            modalidade.Slug = await SlugUnico.GerarAsync(nome, 80, s => db.Modalidades.AnyAsync(m => m.Id != id && m.Slug == s));
        modalidade.Nome = nome;
        modalidade.Descricao = Modalidade.Descricao?.Trim();
        await db.SaveChangesAsync();

        TempData["Mensagem"] = $"Modalidade \"{modalidade.Nome}\" atualizada.";
        return RedirectToPage("Index");
    }
}
