using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Professores;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Professor> Professores { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Professores = (await db.Professores.AsNoTracking().Include(p => p.Modalidades).ToListAsync())
            .OrderBy(p => p.Nome, StringComparer.CurrentCulture).ToList();
        foreach (var p in Professores)
            p.Modalidades = p.Modalidades.OrderBy(m => m.Nome, StringComparer.CurrentCulture).ToList();
    }

    /// <summary>"Ana Paula Souza" -> "AS"; usado quando o professor não tem foto.</summary>
    public static string Iniciais(string nome)
    {
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => (partes[0][..1] + partes[^1][..1]).ToUpperInvariant(),
        };
    }
}
