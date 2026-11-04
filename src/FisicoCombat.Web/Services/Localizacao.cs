using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace FisicoCombat.Web.Services;

/// <summary>Mensagens do ASP.NET Core Identity em português.</summary>
public class ErrosIdentityPtBr : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"O e-mail {email} já está cadastrado." };
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"O e-mail {userName} já está cadastrado." };
    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "E-mail inválido." };
    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "E-mail inválido." };
    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"A senha deve ter pelo menos {length} caracteres." };
    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "A senha deve ter pelo menos um número." };
    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "A senha deve ter pelo menos uma letra minúscula." };
    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "A senha deve ter pelo menos uma letra maiúscula." };
    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "A senha deve ter pelo menos um símbolo." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"A senha deve ter pelo menos {uniqueChars} caracteres diferentes." };
    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Senha incorreta." };
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Ocorreu um erro. Tente novamente." };
}

/// <summary>Mensagens de validação de formulário em português.</summary>
public static class MensagensValidacao
{
    public static void Configurar(DefaultModelBindingMessageProvider p)
    {
        p.SetValueMustNotBeNullAccessor(_ => "Este campo é obrigatório.");
        p.SetMissingBindRequiredValueAccessor(campo => $"O campo {campo} é obrigatório.");
        p.SetMissingKeyOrValueAccessor(() => "Este campo é obrigatório.");
        p.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"O valor '{valor}' não é válido para {campo}.");
        p.SetUnknownValueIsInvalidAccessor(campo => $"O valor informado não é válido para {campo}.");
        p.SetValueIsInvalidAccessor(valor => $"O valor '{valor}' não é válido.");
        p.SetValueMustBeANumberAccessor(campo => $"O campo {campo} deve ser um número.");
        p.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"O valor '{valor}' não é válido.");
        p.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor informado não é válido.");
        p.SetNonPropertyValueMustBeANumberAccessor(() => "O campo deve ser um número.");
        p.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo da requisição é obrigatório.");
    }
}
