using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FisicoCombat.Web.Pages.Admin.Professores;

public class CriarModel(AppDbContext db, ArmazenamentoImagens imagens) : FormularioProfessorModel(db, imagens)
{
    public Task OnGetAsync() => CarregarModalidadesAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var professor = new Professor();
        if (!await AplicarAsync(professor))
        {
            await CarregarModalidadesAsync();
            return Page();
        }

        Db.Professores.Add(professor);
        await Db.SaveChangesAsync();

        TempData["Mensagem"] = $"Professor(a) {professor.Nome} cadastrado(a).";
        return RedirectToPage("Index");
    }
}
