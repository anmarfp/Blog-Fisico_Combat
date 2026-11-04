using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FisicoCombat.Web.Pages.Admin.Professores;

public class ExcluirModel(AppDbContext db, ArmazenamentoImagens imagens) : PageModel
{
    public Professor Professor { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var professor = await db.Professores.FindAsync(id);
        if (professor is null) return NotFound();
        Professor = professor;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var professor = await db.Professores.FindAsync(id);
        if (professor is null) return NotFound();

        db.Professores.Remove(professor);
        await db.SaveChangesAsync();
        imagens.Excluir(professor.FotoCaminho);

        TempData["Mensagem"] = $"Professor(a) {professor.Nome} excluído(a).";
        return RedirectToPage("Index");
    }
}
