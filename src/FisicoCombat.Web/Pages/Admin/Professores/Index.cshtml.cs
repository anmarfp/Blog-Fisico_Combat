using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Professores;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Professor> Professores { get; private set; } = [];

    public async Task OnGetAsync() =>
        Professores = await db.Professores.Include(p => p.Modalidades).OrderBy(p => p.Nome).ToListAsync();
}
