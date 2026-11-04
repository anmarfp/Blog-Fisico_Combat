using FisicoCombat.Web.Data;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Professores;

public class EditarModel(AppDbContext db, ArmazenamentoImagens imagens) : FormularioProfessorModel(db, imagens)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var professor = await Db.Professores.Include(p => p.Modalidades).FirstOrDefaultAsync(p => p.Id == id);
        if (professor is null) return NotFound();

        Professor = professor;
        ModalidadeIds = professor.Modalidades.Select(m => m.Id).ToArray();
        FotoAtual = professor.FotoCaminho;
        await CarregarModalidadesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var professor = await Db.Professores.Include(p => p.Modalidades).FirstOrDefaultAsync(p => p.Id == id);
        if (professor is null) return NotFound();

        var fotoAntiga = FotoAtual = professor.FotoCaminho;
        if (!await AplicarAsync(professor))
        {
            await CarregarModalidadesAsync();
            return Page();
        }

        await Db.SaveChangesAsync();
        if (fotoAntiga != professor.FotoCaminho) Imagens.Excluir(fotoAntiga);

        TempData["Mensagem"] = $"Professor(a) {professor.Nome} atualizado(a).";
        return RedirectToPage("Index");
    }
}
