using System.Text.RegularExpressions;

namespace Josapar.Api.Features.Auth;

/// <summary>
/// Replica as 5 regras exigidas pela tela "Criar Senha" do app Flutter
/// (create_password_screen.dart) para que a validação não fique só no client.
/// </summary>
public static partial class PasswordPolicy
{
    private static readonly (string Label, Func<string, bool> IsMet)[] Rules =
    [
        ("Mínimo 8 caracteres", p => p.Length >= 8),
        ("Pelo menos uma letra maiúscula", p => UpperRegex().IsMatch(p)),
        ("Pelo menos uma letra minúscula", p => LowerRegex().IsMatch(p)),
        ("Pelo menos um número", p => DigitRegex().IsMatch(p)),
        ("Um caractere especial (@, #, $, etc.)", p => SpecialCharRegex().IsMatch(p)),
    ];

    public static IReadOnlyList<string> GetUnmetRequirements(string password) =>
        Rules.Where(rule => !rule.IsMet(password)).Select(rule => rule.Label).ToList();

    public static bool IsValid(string password) => GetUnmetRequirements(password).Count == 0;

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UpperRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowerRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex("""[!@#$%^&*(),.?":{}|<>_\-+=\[\]/~`]""")]
    private static partial Regex SpecialCharRegex();
}
