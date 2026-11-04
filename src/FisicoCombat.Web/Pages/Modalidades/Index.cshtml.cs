using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Modalidades;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Modalidade> Modalidades { get; private set; } = [];

    public async Task OnGetAsync() =>
        Modalidades = (await db.Modalidades.AsNoTracking().ToListAsync())
            .OrderBy(m => m.Nome, StringComparer.CurrentCulture).ToList();
}
