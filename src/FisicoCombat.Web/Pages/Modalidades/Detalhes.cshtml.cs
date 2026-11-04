using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Modalidades;

public class DetalhesModel(AppDbContext db) : PageModel
{
    public Modalidade Modalidade { get; private set; } = null!;
    public List<Professor> Professores { get; private set; } = [];
    public List<Postagem> Recentes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var modalidade = await db.Modalidades.AsNoTracking().Include(m => m.Professores)
            .FirstOrDefaultAsync(m => m.Slug == slug);
        if (modalidade is null) return NotFound();

        Modalidade = modalidade;
        Professores = modalidade.Professores.OrderBy(p => p.Nome, StringComparer.CurrentCulture).ToList();
        Recentes = await db.Postagens.AsNoTracking().Include(p => p.Modalidade)
            .Where(p => p.ModalidadeId == modalidade.Id)
            .OrderByDescending(p => p.PublicadaEm).ThenByDescending(p => p.Id)
            .Take(6).ToListAsync();
        return Page();
    }
}
