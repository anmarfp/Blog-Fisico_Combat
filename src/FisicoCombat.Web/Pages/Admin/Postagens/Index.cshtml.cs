using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Postagens;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Postagem> Postagens { get; private set; } = [];

    public async Task OnGetAsync() =>
        Postagens = await db.Postagens.Include(p => p.Modalidade).OrderByDescending(p => p.PublicadaEm).ToListAsync();

    /// <summary>Data gravada em UTC exibida no horário do Rio de Janeiro (dd/MM/aaaa HH:mm).</summary>
    public static string DataLocal(DateTime utc) =>
        HoraLocal.DeUtc(utc).ToString("dd/MM/yyyy HH:mm");
}
