using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Comentarios;

/// <summary>Moderação de comentários (RF10): pendentes por padrão, filtro por status.</summary>
public class IndexModel(AppDbContext db) : PageModel
{
    public const string Todos = "Todos";
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>Filtro vindo da URL: Pendente (padrão), Aprovado, Rejeitado ou Todos.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public List<Comentario> Comentarios { get; private set; } = [];

    /// <summary>Status filtrado; null quando o filtro é "Todos". Valores desconhecidos viram Pendente.</summary>
    public StatusComentario? Filtro =>
        string.Equals(Status, Todos, StringComparison.OrdinalIgnoreCase) ? null
        : Enum.TryParse(Status, ignoreCase: true, out StatusComentario s) && Enum.IsDefined(s) ? s
        : StatusComentario.Pendente;

    public string FiltroAtual => Filtro?.ToString() ?? Todos;

    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Brasilia);

    public async Task OnGetAsync()
    {
        var consulta = db.Comentarios.AsNoTracking().Include(c => c.Usuario).Include(c => c.Postagem).AsQueryable();
        if (Filtro is { } status) consulta = consulta.Where(c => c.Status == status);
        Comentarios = await consulta.OrderByDescending(c => c.CriadoEm).ThenByDescending(c => c.Id).ToListAsync();
    }

    public Task<IActionResult> OnPostAprovarAsync(int id) => MudarStatusAsync(id, StatusComentario.Aprovado, "Comentário aprovado e publicado.");

    public Task<IActionResult> OnPostRejeitarAsync(int id) => MudarStatusAsync(id, StatusComentario.Rejeitado, "Comentário rejeitado.");

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (await db.Comentarios.Where(c => c.Id == id).ExecuteDeleteAsync() == 0) return NotFound();
        TempData["Mensagem"] = "Comentário excluído.";
        return RedirectToPage(new { status = FiltroAtual });
    }

    private async Task<IActionResult> MudarStatusAsync(int id, StatusComentario novo, string mensagem)
    {
        var alterados = await db.Comentarios.Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, novo));
        if (alterados == 0) return NotFound();
        TempData["Mensagem"] = mensagem;
        return RedirectToPage(new { status = FiltroAtual });
    }
}
