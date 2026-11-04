using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Postagens;

/// <summary>Lista de postagens com filtros por tipo, modalidade e texto (RF09).</summary>
public class IndexModel(AppDbContext db) : PageModel
{
    public const int PorPagina = 12;

    public List<Postagem> Postagens { get; private set; } = [];
    public List<Modalidade> Modalidades { get; private set; } = [];

    /// <summary>"noticia", "midia" ou null (valores desconhecidos são ignorados).</summary>
    public string? Tipo { get; private set; }
    public Modalidade? ModalidadeAtiva { get; private set; }
    public string? Busca { get; private set; }

    public int Pagina { get; private set; }
    public int TotalPaginas { get; private set; }
    public int Total { get; private set; }

    public bool TemFiltros => Tipo is not null || ModalidadeAtiva is not null || Busca is not null;

    public async Task OnGetAsync(string? tipo, string? modalidade, string? q, int pagina = 1)
    {
        Tipo = tipo is "noticia" or "midia" ? tipo : null;
        Modalidades = (await db.Modalidades.AsNoTracking().ToListAsync())
            .OrderBy(m => m.Nome, StringComparer.CurrentCulture).ToList();
        ModalidadeAtiva = Modalidades.FirstOrDefault(m => m.Slug == modalidade);
        var termo = q?.Trim();
        Busca = string.IsNullOrEmpty(termo) ? null : termo.Length > 100 ? termo[..100] : termo;

        var consulta = db.Postagens.AsNoTracking();
        if (Tipo is not null)
        {
            var tipoPostagem = Tipo == "midia" ? TipoPostagem.Midia : TipoPostagem.Noticia;
            consulta = consulta.Where(p => p.Tipo == tipoPostagem);
        }
        if (ModalidadeAtiva is not null)
        {
            var modalidadeId = ModalidadeAtiva.Id;
            consulta = consulta.Where(p => p.ModalidadeId == modalidadeId);
        }
        if (Busca is not null)
        {
            // shortcut: o LIKE do SQLite só ignora maiúsculas/minúsculas em letras sem acento (Á ≠ á);
            // trocar por busca de texto completo (FTS5) se o volume de postagens ou as reclamações crescerem.
            var padrao = "%" + Busca.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            consulta = consulta.Where(p => EF.Functions.Like(p.Titulo, padrao, "\\")
                || (p.Conteudo != null && EF.Functions.Like(p.Conteudo, padrao, "\\")));
        }

        Total = await consulta.CountAsync();
        TotalPaginas = Math.Max(1, (Total + PorPagina - 1) / PorPagina);
        Pagina = Math.Clamp(pagina, 1, TotalPaginas);
        Postagens = await consulta.Include(p => p.Modalidade)
            .OrderByDescending(p => p.PublicadaEm).ThenByDescending(p => p.Id)
            .Skip((Pagina - 1) * PorPagina).Take(PorPagina)
            .ToListAsync();
    }

    /// <summary>Parâmetros de consulta dos filtros ativos, para os links de paginação.</summary>
    public Dictionary<string, string> Rota(int pagina)
    {
        var rota = new Dictionary<string, string>();
        if (Tipo is not null) rota["tipo"] = Tipo;
        if (ModalidadeAtiva is not null) rota["modalidade"] = ModalidadeAtiva.Slug;
        if (Busca is not null) rota["q"] = Busca;
        if (pagina > 1) rota["pagina"] = pagina.ToString();
        return rota;
    }
}
