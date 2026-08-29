namespace Plugin.Maui.FormValidation.Internal;

internal static partial class ValidationPatterns
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    internal static partial Regex Email();

    [GeneratedRegex(@"^\+?[0-9]{7,15}$", RegexOptions.CultureInvariant)]
    internal static partial Regex Phone();
}
