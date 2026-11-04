using System.ComponentModel.DataAnnotations;

namespace FisicoCombat.Web.Models;

public enum StatusComentario
{
    Pendente = 0,
    Aprovado = 1,
    Rejeitado = 2,
}

/// <summary>Comentário de um usuário em uma postagem (RF10). Só aparece após aprovação.</summary>
public class Comentario
{
    public int Id { get; set; }

    public int PostagemId { get; set; }
    public Postagem? Postagem { get; set; }

    public string UsuarioId { get; set; } = "";
    public Usuario? Usuario { get; set; }

    [Required(ErrorMessage = "Escreva o comentário.")]
    [StringLength(1000, ErrorMessage = "O comentário pode ter até {1} caracteres.")]
    public string Texto { get; set; } = "";

    public DateTime CriadoEm { get; set; }

    public StatusComentario Status { get; set; } = StatusComentario.Pendente;
}
