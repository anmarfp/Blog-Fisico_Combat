using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Postagens;

public class ExcluirModel(AppDbContext db, ArmazenamentoImagens imagens) : PageModel
{
    public Postagem Postagem { get; private set; } = new();
    public int QuantidadeComentarios { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var postagem = await db.Postagens.FindAsync(id);
        if (postagem is null) return NotFound();
        Postagem = postagem;
        QuantidadeComentarios = await db.Comentarios.CountAsync(c => c.PostagemId == id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var postagem = await db.Postagens.FindAsync(id);
        if (postagem is null) return NotFound();

        db.Postagens.Remove(postagem);
        await db.SaveChangesAsync();
        imagens.Excluir(postagem.ImagemCaminho);

        TempData["Mensagem"] = $"Postagem \"{postagem.Titulo}\" excluída.";
        return RedirectToPage("Index");
    }
}
