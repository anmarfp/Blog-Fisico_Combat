using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Postagens;

public class IndexModel(AppDbContext db) : PageModel
{
    private static readonly TimeZoneInfo HorarioRio = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public List<Postagem> Postagens { get; private set; } = [];

    public async Task OnGetAsync() =>
        Postagens = await db.Postagens.Include(p => p.Modalidade).OrderByDescending(p => p.PublicadaEm).ToListAsync();

    /// <summary>Data gravada em UTC exibida no horário do Rio de Janeiro (dd/MM/aaaa HH:mm).</summary>
    public static string DataLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), HorarioRio).ToString("dd/MM/yyyy HH:mm");
}
