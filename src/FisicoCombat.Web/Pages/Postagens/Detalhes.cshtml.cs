using System.ComponentModel.DataAnnotations;
using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Postagens;

/// <summary>Exibe uma postagem, seus comentários aprovados (RF10) e os botões de compartilhar (RF11).</summary>
public class DetalhesModel(AppDbContext db, UserManager<Usuario> usuarios) : PageModel
{
    public Postagem Postagem { get; private set; } = null!;
    public List<Comentario> Comentarios { get; private set; } = [];

    /// <summary>Endereço completo da postagem, usado nos links de compartilhar e no Open Graph.</summary>
    public string UrlAbsoluta { get; private set; } = "";
    public string Resumo { get; private set; } = "";
    public string? ImagemCompartilhamento { get; private set; }

    [BindProperty]
    [Required(ErrorMessage = "Escreva o comentário.")]
    [StringLength(1000, ErrorMessage = "O comentário pode ter até {1} caracteres.")]
    [Display(Name = "Seu comentário")]
    public string? Texto { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Contador anônimo: soma 1 direto no banco a cada abertura, sem cookie nem IP.
        var alteradas = await db.Postagens.Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visualizacoes, p => p.Visualizacoes + 1));
        if (alteradas == 0) return NotFound();

        await CarregarAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostComentarAsync(int id)
    {
        // Conta excluída com cookie ainda válido também precisa entrar de novo (evita erro de chave estrangeira).
        var autor = User.Identity?.IsAuthenticated == true ? await usuarios.GetUserAsync(User) : null;
        if (autor is null) return Challenge();

        if (!ModelState.IsValid)
            return await CarregarAsync(id) ? Page() : NotFound();

        var slug = await db.Postagens.Where(p => p.Id == id).Select(p => p.Slug).FirstOrDefaultAsync();
        if (slug is null) return NotFound();

        db.Comentarios.Add(new Comentario
        {
            PostagemId = id,
            UsuarioId = autor.Id,
            Texto = Texto!.Trim(),
            CriadoEm = DateTime.UtcNow,
            Status = StatusComentario.Pendente,
        });
        await db.SaveChangesAsync();

        TempData["Mensagem"] = "Comentário enviado! Ele aparecerá após a aprovação do administrador.";
        return RedirectToPage("/Postagens/Detalhes", null, new { id, slug }, "comentarios");
    }

    private async Task<bool> CarregarAsync(int id)
    {
        var postagem = await db.Postagens.AsNoTracking().Include(p => p.Modalidade)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (postagem is null) return false;

        Postagem = postagem;
        Comentarios = await db.Comentarios.AsNoTracking().Include(c => c.Usuario)
            .Where(c => c.PostagemId == id && c.Status == StatusComentario.Aprovado)
            .OrderBy(c => c.CriadoEm).ThenBy(c => c.Id)
            .ToListAsync();

        UrlAbsoluta = Url.Page("/Postagens/Detalhes", null, new { id, slug = postagem.Slug }, Request.Scheme)!;

        var texto = string.Join(' ', (postagem.Conteudo ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        Resumo = texto.Length == 0 ? postagem.Titulo
            : texto.Length <= 160 ? texto
            : texto[..159].TrimEnd() + "…";

        ImagemCompartilhamento = !string.IsNullOrEmpty(postagem.ImagemCaminho)
            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{postagem.ImagemCaminho}"
            : !string.IsNullOrEmpty(postagem.YoutubeId)
                ? $"https://img.youtube.com/vi/{postagem.YoutubeId}/hqdefault.jpg"
                : null;
        return true;
    }
}
