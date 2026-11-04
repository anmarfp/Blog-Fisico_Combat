using System.ComponentModel.DataAnnotations;
using FisicoCombat.Web.Data;
using FisicoCombat.Web.Models;
using FisicoCombat.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Pages.Admin.Postagens;

/// <summary>Campos e regras comuns às páginas Criar e Editar postagem (formulário em _Formulario.cshtml).</summary>
public abstract class FormularioPostagemModel(AppDbContext db, ArmazenamentoImagens imagens) : PageModel
{
    protected readonly AppDbContext Db = db;
    protected readonly ArmazenamentoImagens Imagens = imagens;

    [BindProperty]
    public Postagem Postagem { get; set; } = new();

    [BindProperty]
    [Display(Name = "Link do vídeo no YouTube")]
    public string? LinkYoutube { get; set; }

    [BindProperty]
    public IFormFile? Foto { get; set; }

    [BindProperty]
    [Display(Name = "Confirmo que tenho autorização de uso de imagem das pessoas que aparecem na foto.")]
    public bool AutorizacaoImagem { get; set; }

    [BindProperty]
    public bool RemoverFoto { get; set; }

    public string? FotoAtual { get; protected set; }
    public List<SelectListItem> OpcoesModalidade { get; private set; } = [];

    protected async Task CarregarModalidadesAsync() =>
        OpcoesModalidade = await Db.Modalidades.OrderBy(m => m.Nome)
            .Select(m => new SelectListItem(m.Nome, m.Id.ToString())).ToListAsync();

    /// <summary>
    /// Valida o formulário e o copia para <paramref name="destino"/>, salvando a foto nova se houver.
    /// Devolve false (sem gravar nada) se algo for inválido.
    /// </summary>
    protected async Task<bool> AplicarAsync(Postagem destino)
    {
        if (!Enum.IsDefined(Postagem.Tipo))
            ModelState.AddModelError("Postagem.Tipo", "Escolha o tipo da postagem.");
        if (Postagem.Tipo == TipoPostagem.Noticia && string.IsNullOrWhiteSpace(Postagem.Conteudo))
            ModelState.AddModelError("Postagem.Conteudo", "Escreva o texto da notícia.");
        if (Postagem.ModalidadeId is int modalidadeId && !await Db.Modalidades.AnyAsync(m => m.Id == modalidadeId))
            ModelState.AddModelError("Postagem.ModalidadeId", "Escolha uma modalidade da lista.");

        var linkInformado = !string.IsNullOrWhiteSpace(LinkYoutube);
        var youtubeId = YouTube.ExtrairId(LinkYoutube);
        if (linkInformado && youtubeId is null)
            ModelState.AddModelError(nameof(LinkYoutube), "Informe um link válido do YouTube.");

        var teraFoto = Foto is not null || (destino.ImagemCaminho is not null && !RemoverFoto);
        if (Foto is not null && !AutorizacaoImagem)
            ModelState.AddModelError(nameof(AutorizacaoImagem), "Confirme a autorização de uso de imagem para enviar a foto.");
        if (teraFoto && string.IsNullOrWhiteSpace(Postagem.ImagemDescricao))
            ModelState.AddModelError("Postagem.ImagemDescricao", "Descreva a foto (texto alternativo para leitores de tela).");
        if (Postagem.Tipo == TipoPostagem.Midia && !teraFoto && !linkInformado)
            ModelState.AddModelError(string.Empty, "Uma mídia precisa de uma foto ou de um link do YouTube.");

        if (!ModelState.IsValid) return false;

        if (Foto is not null)
        {
            var upload = await Imagens.SalvarAsync(Foto, HttpContext.RequestAborted);
            if (!upload.Sucesso)
            {
                ModelState.AddModelError(nameof(Foto), upload.Erro!);
                return false;
            }
            destino.ImagemCaminho = upload.Caminho;
        }
        else if (RemoverFoto)
        {
            destino.ImagemCaminho = null;
        }

        destino.Tipo = Postagem.Tipo;
        destino.Titulo = Postagem.Titulo.Trim();
        destino.Conteudo = Postagem.Conteudo?.Trim();
        destino.ModalidadeId = Postagem.ModalidadeId;
        destino.YoutubeId = youtubeId;
        destino.ImagemDescricao = destino.ImagemCaminho is null ? null : Postagem.ImagemDescricao!.Trim();
        return true;
    }
}
