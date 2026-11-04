using System.ComponentModel.DataAnnotations;

namespace FisicoCombat.Web.Models;

public enum TipoPostagem
{
    [Display(Name = "Notícia")] Noticia = 0,
    [Display(Name = "Mídia")] Midia = 1,
}

/// <summary>Notícia (RF07) ou mídia (RF06) publicada pelo administrador.</summary>
public class Postagem
{
    public int Id { get; set; }

    public TipoPostagem Tipo { get; set; }

    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(150, ErrorMessage = "O título pode ter até {1} caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";

    [StringLength(170)]
    public string Slug { get; set; } = "";

    /// <summary>Texto puro; quebras de linha viram parágrafos na exibição.</summary>
    [StringLength(20000, ErrorMessage = "O texto pode ter até {1} caracteres.")]
    [Display(Name = "Texto")]
    public string? Conteudo { get; set; }

    /// <summary>Caminho público da imagem enviada (ex.: /uploads/abc.jpg).</summary>
    [StringLength(300)]
    public string? ImagemCaminho { get; set; }

    /// <summary>Descrição da imagem para leitores de tela.</summary>
    [StringLength(250, ErrorMessage = "A descrição da imagem pode ter até {1} caracteres.")]
    [Display(Name = "Descrição da imagem")]
    public string? ImagemDescricao { get; set; }

    /// <summary>ID de 11 caracteres de um vídeo do YouTube.</summary>
    [StringLength(11)]
    public string? YoutubeId { get; set; }

    [Display(Name = "Modalidade")]
    public int? ModalidadeId { get; set; }
    public Modalidade? Modalidade { get; set; }

    public DateTime PublicadaEm { get; set; }

    /// <summary>Contador anônimo de acessos (objetivo 5 da AE III).</summary>
    public int Visualizacoes { get; set; }

    public string? AutorId { get; set; }
    public Usuario? Autor { get; set; }

    public List<Comentario> Comentarios { get; set; } = [];
}
