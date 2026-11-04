using FisicoCombat.Web.Data;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FisicoCombat.Web.Pages.Admin.Postagens;

/// <summary>Edita a postagem mantendo slug, data de publicação e autor, para não quebrar links já compartilhados.</summary>
public class EditarModel(AppDbContext db, ArmazenamentoImagens imagens) : FormularioPostagemModel(db, imagens)
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var postagem = await Db.Postagens.FindAsync(id);
        if (postagem is null) return NotFound();

        Postagem = postagem;
        LinkYoutube = postagem.YoutubeId is null ? null : $"https://www.youtube.com/watch?v={postagem.YoutubeId}";
        FotoAtual = postagem.ImagemCaminho;
        await CarregarModalidadesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var postagem = await Db.Postagens.FindAsync(id);
        if (postagem is null) return NotFound();

        var fotoAntiga = FotoAtual = postagem.ImagemCaminho;
        if (!await AplicarAsync(postagem))
        {
            await CarregarModalidadesAsync();
            return Page();
        }

        await Db.SaveChangesAsync();
        if (fotoAntiga != postagem.ImagemCaminho) Imagens.Excluir(fotoAntiga);

        TempData["Mensagem"] = $"Postagem \"{postagem.Titulo}\" atualizada.";
        return RedirectToPage("Index");
    }
}
