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

        Db.Postagens.Add(postagem);
        // O índice único protege contra dois envios simultâneos com o mesmo título (ex.: clique duplo):
        // se o slug for tomado entre a verificação e a gravação, gera o próximo e tenta de novo.
        for (var tentativa = 1; ; tentativa++)
        {
            postagem.Slug = await SlugUnico.GerarAsync(postagem.Titulo, 150, s => Db.Postagens.AnyAsync(p => p.Slug == s));
            try
            {
                await Db.SaveChangesAsync();
                break;
            }
            catch (DbUpdateException)
            {
                if (tentativa < 3 && await Db.Postagens.AnyAsync(p => p.Slug == postagem.Slug)) continue;
                // Não deixa a foto recém-enviada órfã no disco.
                Imagens.Excluir(postagem.ImagemCaminho);
                throw;
            }
        }

        TempData["Mensagem"] = $"Postagem \"{postagem.Titulo}\" publicada.";
        return RedirectToPage("Index");
    }
}
