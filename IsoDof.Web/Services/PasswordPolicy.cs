namespace IsoDof.Web.Services;

/// <summary>
/// Uygulama genelinde tek bir şifre kuralı: en az 8 karakter, en az bir harf ve bir rakam.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public const string Description = "Şifre en az 8 karakter olmalı ve en az bir harf ile bir rakam içermelidir.";

    public static bool IsValid(string? password, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength
            || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            error = Description;
            return false;
        }
        return true;
    }
}
