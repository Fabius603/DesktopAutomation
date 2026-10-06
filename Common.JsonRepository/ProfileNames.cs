namespace Common.ApplicationData;

public static class ProfileNames
{
    public static string? Validate(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (name.Length > 64 || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-zA-Z0-9][a-zA-Z0-9_-]*$") ||
            System.Text.RegularExpressions.Regex.IsMatch(name, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new ArgumentException("Profile names must contain 1-64 ASCII letters, digits, underscores or hyphens.", nameof(name));
        return name;
    }
}
