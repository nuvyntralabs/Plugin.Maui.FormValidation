namespace Plugin.Maui.FormValidation;

/// <summary>
/// Outcome of a validate call.
/// </summary>
public sealed class ValidationResult
{
    static readonly ValidationError[] EmptyErrors = [];

    /// <summary>A successful result with no errors.</summary>
    public static ValidationResult Success { get; } = new(EmptyErrors);

    /// <summary>Creates a result from the given errors.</summary>
    public ValidationResult(IReadOnlyList<ValidationError> errors)
    {
        Errors = errors ?? EmptyErrors;
    }

    /// <summary>Creates a failed result.</summary>
    public static ValidationResult Failure(params ValidationError[] errors)
        => new(errors ?? EmptyErrors);

    /// <summary>Creates a failed result.</summary>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
        => new((errors ?? []).ToArray());

    /// <summary>True when <see cref="Errors"/> is empty.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>All errors from this run.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>Errors for one property (ordinal ignore-case).</summary>
    public IReadOnlyList<ValidationError> ErrorsFor(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return Errors.Where(error => string.Equals(error.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    /// <summary>First error message for a property, or <c>null</c>.</summary>
    public string? FirstError(string propertyName)
        => ErrorsFor(propertyName).FirstOrDefault()?.Message;
}

/// <summary>
/// One failed rule.
/// </summary>
public sealed class ValidationError
{
    /// <summary>Creates an error.</summary>
    public ValidationError(string propertyName, string message, string? code = null, object? attemptedValue = null)
    {
        PropertyName = propertyName;
        Message = message;
        Code = code;
        AttemptedValue = attemptedValue;
    }

    /// <summary>Property path, for example <c>Email</c>.</summary>
    public string PropertyName { get; }

    /// <summary>Human-readable message.</summary>
    public string Message { get; }

    /// <summary>Stable rule code: <c>required</c>, <c>email</c>, <c>server</c>, …</summary>
    public string? Code { get; }

    /// <summary>Value that failed.</summary>
    public object? AttemptedValue { get; }
}
