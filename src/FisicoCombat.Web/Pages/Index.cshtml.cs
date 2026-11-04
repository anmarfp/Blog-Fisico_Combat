using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Postagem> Recentes { get; private set; } = [];
    public List<Modalidade> Modalidades { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Recentes = await db.Postagens.AsNoTracking().Include(p => p.Modalidade)
            .OrderByDescending(p => p.PublicadaEm).ThenByDescending(p => p.Id)
            .Take(6).ToListAsync();
        Modalidades = (await db.Modalidades.AsNoTracking().ToListAsync())
            .OrderBy(m => m.Nome, StringComparer.CurrentCulture).ToList();
    }
}
