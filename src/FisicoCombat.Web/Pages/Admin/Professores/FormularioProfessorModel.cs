using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Professores;

/// <summary>Campos e regras comuns às páginas Criar e Editar professor (formulário em _Formulario.cshtml).</summary>
public abstract class FormularioProfessorModel(AppDbContext db, ArmazenamentoImagens imagens) : PageModel
{
    protected readonly AppDbContext Db = db;
    protected readonly ArmazenamentoImagens Imagens = imagens;

    [BindProperty]
    public Professor Professor { get; set; } = new();

    [BindProperty]
    public int[] ModalidadeIds { get; set; } = [];

    [BindProperty]
    public IFormFile? Foto { get; set; }

    [BindProperty]
    public bool RemoverFoto { get; set; }

    public string? FotoAtual { get; protected set; }
    public List<Modalidade> TodasModalidades { get; private set; } = [];

    protected async Task CarregarModalidadesAsync() =>
        TodasModalidades = await Db.Modalidades.OrderBy(m => m.Nome).ToListAsync();

    /// <summary>
    /// Copia o formulário para <paramref name="destino"/>, salvando a foto nova se houver.
    /// Devolve false (sem gravar nada) se algum campo ou a foto for inválido.
    /// </summary>
    protected async Task<bool> AplicarAsync(Professor destino)
    {
        if (!ModelState.IsValid) return false;

        if (Foto is not null)
        {
            var upload = await Imagens.SalvarAsync(Foto, HttpContext.RequestAborted);
            if (!upload.Sucesso)
            {
                ModelState.AddModelError(nameof(Foto), upload.Erro!);
                return false;
            }
            destino.FotoCaminho = upload.Caminho;
        }
        else if (RemoverFoto)
        {
            destino.FotoCaminho = null;
        }

        destino.Nome = Professor.Nome.Trim();
        destino.Biografia = Professor.Biografia?.Trim();
        var selecionadas = await Db.Modalidades.Where(m => ModalidadeIds.Contains(m.Id)).ToListAsync();
        destino.Modalidades.Clear();
        destino.Modalidades.AddRange(selecionadas);
        return true;
    }
}
