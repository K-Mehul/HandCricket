using System.Text.RegularExpressions;

public static class AuthValidator
{
    public static bool ValidateEmail(string email)
    {
        return Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public static bool ValidatePassword(string password)
    {
        return password.Length >= 6;
    }

    public static bool ValidateUsername(string username)
    {
        return username.Length >= 3;
    }
}
