using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Modalidades;

public class ExcluirModel(AppDbContext db) : PageModel
{
    public Modalidade Modalidade { get; private set; } = new();
    public int QuantidadePostagens { get; private set; }
    public int QuantidadeProfessores { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var modalidade = await db.Modalidades.FindAsync(id);
        if (modalidade is null) return NotFound();
        Modalidade = modalidade;
        QuantidadePostagens = await db.Postagens.CountAsync(p => p.ModalidadeId == id);
        QuantidadeProfessores = await db.Professores.CountAsync(p => p.Modalidades.Any(m => m.Id == id));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var modalidade = await db.Modalidades.FindAsync(id);
        if (modalidade is null) return NotFound();

        db.Modalidades.Remove(modalidade);
        await db.SaveChangesAsync();

        TempData["Mensagem"] = $"Modalidade \"{modalidade.Nome}\" excluída.";
        return RedirectToPage("Index");
    }
}
