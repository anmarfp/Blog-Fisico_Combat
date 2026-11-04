using System.ComponentModel.DataAnnotations;

namespace FisicoCombat.Web.Models;

/// <summary>Professor do CT (RF02, RF03).</summary>
public class Professor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do professor.")]
    [StringLength(120, ErrorMessage = "O nome pode ter até {1} caracteres.")]
    public string Nome { get; set; } = "";

    [StringLength(2000, ErrorMessage = "A biografia pode ter até {1} caracteres.")]
    public string? Biografia { get; set; }

    /// <summary>Caminho público da foto (ex.: /uploads/abc.jpg), quando houver.</summary>
    [StringLength(300)]
    public string? FotoCaminho { get; set; }

    public List<Modalidade> Modalidades { get; set; } = [];
}
