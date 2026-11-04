using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Postagens;

public class CriarModel(AppDbContext db, ArmazenamentoImagens imagens, UserManager<Usuario> usuarios)
    : FormularioPostagemModel(db, imagens)
{
    public Task OnGetAsync() => CarregarModalidadesAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var postagem = new Postagem { PublicadaEm = DateTime.UtcNow, AutorId = usuarios.GetUserId(User) };
        if (!await AplicarAsync(postagem))
        {
            await CarregarModalidadesAsync();
            return Page();
        }

        postagem.Slug = await SlugUnico.GerarAsync(postagem.Titulo, 150, s => Db.Postagens.AnyAsync(p => p.Slug == s));
        Db.Postagens.Add(postagem);
        await Db.SaveChangesAsync();

        TempData["Mensagem"] = $"Postagem \"{postagem.Titulo}\" publicada.";
        return RedirectToPage("Index");
    }
}
