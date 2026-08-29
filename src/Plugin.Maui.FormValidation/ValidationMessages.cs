namespace Plugin.Maui.FormValidation;

/// <summary>
/// Default English messages. Override per rule with a message argument or <c>WithMessage</c>.
/// </summary>
public static class ValidationMessages
{
    /// <summary>Required field.</summary>
    public static string Required(string property) => $"{property} is required.";

    /// <summary>Email format.</summary>
    public static string Email(string property) => $"{property} must be a valid email.";

    /// <summary>Phone format.</summary>
    public static string Phone(string property) => $"{property} must be a valid phone number.";

    /// <summary>URL format.</summary>
    public static string Url(string property) => $"{property} must be a valid URL.";

    /// <summary>Numeric value.</summary>
    public static string Numeric(string property) => $"{property} must be a number.";

    /// <summary>Regex mismatch.</summary>
    public static string Regex(string property) => $"{property} is not in the expected format.";

    /// <summary>Below minimum.</summary>
    public static string Min(string property, object minimum) => $"{property} must be at least {minimum}.";

    /// <summary>Above maximum.</summary>
    public static string Max(string property, object maximum) => $"{property} must be at most {maximum}.";

    /// <summary>Too short.</summary>
    public static string MinLength(string property, int length) => $"{property} must be at least {length} characters.";

    /// <summary>Too long.</summary>
    public static string MaxLength(string property, int length) => $"{property} must be at most {length} characters.";

    /// <summary>Length range.</summary>
    public static string Length(string property, int min, int max) => $"{property} must be between {min} and {max} characters.";

    /// <summary>Inclusive range.</summary>
    public static string InclusiveBetween(string property, object from, object to) => $"{property} must be between {from} and {to}.";

    /// <summary>Equality.</summary>
    public static string EqualTo(string property) => $"{property} does not match.";

    /// <summary>Custom predicate.</summary>
    public static string Must(string property) => $"{property} is invalid.";

    /// <summary>Server rejection.</summary>
    public static string Server(string property) => $"{property} was rejected by the server.";
}
