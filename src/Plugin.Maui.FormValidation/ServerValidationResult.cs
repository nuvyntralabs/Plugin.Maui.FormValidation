namespace Plugin.Maui.FormValidation;

/// <summary>
/// Result of a <c>.Server(...)</c> callback.
/// </summary>
public sealed class ServerValidationResult
{
    /// <summary>Creates a successful server result.</summary>
    public static ServerValidationResult Ok() => new() { IsValid = true };

    /// <summary>Creates a failed server result.</summary>
    public static ServerValidationResult Fail(string message, string? code = "server")
        => new() { IsValid = false, ErrorMessage = message, ErrorCode = code };

    /// <summary>Whether the server accepted the value.</summary>
    public bool IsValid { get; init; }

    /// <summary>Message shown when <see cref="IsValid"/> is false.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Optional machine-readable code.</summary>
    public string? ErrorCode { get; init; }
}
