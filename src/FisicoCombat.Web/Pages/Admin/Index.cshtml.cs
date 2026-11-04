using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin;

public class IndexModel(AppDbContext db) : PageModel
{
    public int TotalPostagens { get; private set; }
    public int TotalProfessores { get; private set; }
    public int TotalModalidades { get; private set; }
    public int ComentariosPendentes { get; private set; }
    public List<Postagem> MaisVistas { get; private set; } = [];
    public List<(string Modalidade, int Visualizacoes)> VisualizacoesPorModalidade { get; private set; } = [];

    public async Task OnGetAsync()
    {
        TotalPostagens = await db.Postagens.CountAsync();
        TotalProfessores = await db.Professores.CountAsync();
        TotalModalidades = await db.Modalidades.CountAsync();
        ComentariosPendentes = await db.Comentarios.CountAsync(c => c.Status == StatusComentario.Pendente);

        MaisVistas = await db.Postagens.Include(p => p.Modalidade)
            .OrderByDescending(p => p.Visualizacoes).ThenByDescending(p => p.PublicadaEm)
            .Take(10).ToListAsync();

        var porModalidade = await db.Modalidades
            .Select(m => new { m.Nome, Total = m.Postagens.Sum(p => p.Visualizacoes) })
            .ToListAsync();
        var semModalidade = await db.Postagens.Where(p => p.ModalidadeId == null).SumAsync(p => p.Visualizacoes);
        VisualizacoesPorModalidade = porModalidade
            .OrderByDescending(m => m.Total).ThenBy(m => m.Nome)
            .Select(m => (m.Nome, m.Total))
            .Append(("Sem modalidade", semModalidade))
            .ToList();
    }
}
