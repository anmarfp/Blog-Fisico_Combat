using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Modalidades;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Modalidade> Modalidades { get; private set; } = [];

    public async Task OnGetAsync() =>
        Modalidades = await db.Modalidades.OrderBy(m => m.Nome).ToListAsync();
}
