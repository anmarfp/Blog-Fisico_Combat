using System.ComponentModel.DataAnnotations;

namespace FisicoCombat.Web.Models;

/// <summary>Arte marcial oferecida pelo CT (RF04, RF05).</summary>
public class Modalidade
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da modalidade.")]
    [StringLength(80, ErrorMessage = "O nome pode ter até {1} caracteres.")]
    public string Nome { get; set; } = "";

    /// <summary>Identificador usado na URL, gerado a partir do nome.</summary>
    [StringLength(100)]
    public string Slug { get; set; } = "";

    [StringLength(2000, ErrorMessage = "A descrição pode ter até {1} caracteres.")]
    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    public List<Professor> Professores { get; set; } = [];
    public List<Postagem> Postagens { get; set; } = [];
}
