using Microsoft.AspNetCore.Identity;

namespace FisicoCombat.Web.Models;

/// <summary>Conta de acesso: administrador (RF01) ou usuário comum (RF08).</summary>
public class Usuario : IdentityUser
{
    public string NomeExibicao { get; set; } = "";

    /// <summary>Momento em que o usuário aceitou a Política de Privacidade (LGPD).</summary>
    public DateTime? ConsentimentoEm { get; set; }

    public List<Comentario> Comentarios { get; set; } = [];
}

public static class Papeis
{
    public const string Administrador = "Administrador";
}
