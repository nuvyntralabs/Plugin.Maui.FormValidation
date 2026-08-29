namespace Plugin.Maui.FormValidation;

/// <summary>
/// Non-generic validator that can evaluate any instance of <see cref="ModelType"/>.
/// </summary>
public interface IValidator
{
    /// <summary>CLR type the rules were built for.</summary>
    Type ModelType { get; }

    /// <summary>Property paths that have at least one rule.</summary>
    IReadOnlyCollection<string> PropertyNames { get; }

    /// <summary>Runs every property rule synchronously. Async / server rules are skipped.</summary>
    ValidationResult Validate(object instance);

    /// <summary>Runs every property rule, including async and server rules.</summary>
    Task<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default);

    /// <summary>Validates a single property synchronously.</summary>
    ValidationResult ValidateProperty(object instance, string propertyName);

    /// <summary>Validates a single property, including async and server rules.</summary>
    Task<ValidationResult> ValidatePropertyAsync(object instance, string propertyName, CancellationToken cancellationToken = default);

    /// <summary>Writes <paramref name="value"/> onto <paramref name="instance"/> when the property is known.</summary>
    bool TrySetProperty(object instance, string propertyName, object? value);
}

/// <summary>
/// Strongly typed validator for <typeparamref name="T"/>.
/// </summary>
public interface IValidator<T> : IValidator
    where T : class
{
    /// <summary>Runs every property rule synchronously against <paramref name="instance"/>.</summary>
    ValidationResult Validate(T instance);

    /// <summary>Runs every property rule, including async and server rules.</summary>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);

    /// <summary>Validates a single property synchronously.</summary>
    ValidationResult ValidateProperty(T instance, string propertyName);

    /// <summary>Validates a single property, including async and server rules.</summary>
    Task<ValidationResult> ValidatePropertyAsync(T instance, string propertyName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Fluent builder that is also the live validator once rules are added.
/// </summary>
public interface IValidatorBuilder<T> : IValidator<T>
    where T : class
{
    /// <summary>Starts a rule chain for a property.</summary>
    IRuleBuilder<T, TProperty> Rule<TProperty>(Expression<Func<T, TProperty>> accessor);

    /// <summary>
    /// Validates the instance passed to <see cref="Validator.For{T}(T)"/>.
    /// Throws if the builder was created with <see cref="Validator.For{T}()"/>.
    /// </summary>
    ValidationResult Validate();

    /// <summary>
    /// Async counterpart of <see cref="Validate()"/>.
    /// </summary>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns this builder as a frozen-looking <see cref="IValidator{T}"/> (same instance).</summary>
    IValidator<T> Build();
}

/// <summary>
/// Fluent rule chain for one property. <see cref="IValidatorBuilder{T}.Rule{TProperty}"/> starts the next property.
/// </summary>
public interface IRuleBuilder<T, TProperty> : IValidatorBuilder<T>
    where T : class
{
    /// <summary>Fails when the value is null, empty, or whitespace.</summary>
    IRuleBuilder<T, TProperty> Required(string? message = null);

    /// <summary>Requires a simple <c>local@host.tld</c> email. Empty values pass unless <see cref="Required"/> is also set.</summary>
    IRuleBuilder<T, TProperty> Email(string? message = null);

    /// <summary>Requires 7–15 digits with an optional leading <c>+</c>. Formatting characters are ignored.</summary>
    IRuleBuilder<T, TProperty> Phone(string? message = null);

    /// <summary>Requires an absolute <c>http</c> or <c>https</c> URI.</summary>
    IRuleBuilder<T, TProperty> Url(string? message = null);

    /// <summary>Requires a number, or a string that parses as a number.</summary>
    IRuleBuilder<T, TProperty> Numeric(string? message = null);

    /// <summary>Requires the string form of the value to match <paramref name="pattern"/>.</summary>
    IRuleBuilder<T, TProperty> Regex(string pattern, string? message = null);

    /// <inheritdoc cref="Regex(string, string?)"/>
    IRuleBuilder<T, TProperty> Regex(System.Text.RegularExpressions.Regex pattern, string? message = null);

    /// <summary>Numeric / comparable minimum. For string length use <see cref="MinLength"/>.</summary>
    IRuleBuilder<T, TProperty> Min(IComparable minimum, string? message = null);

    /// <summary>Numeric / comparable maximum. For string length use <see cref="MaxLength"/>.</summary>
    IRuleBuilder<T, TProperty> Max(IComparable maximum, string? message = null);

    /// <summary>Minimum string length (whitespace-trimmed).</summary>
    IRuleBuilder<T, TProperty> MinLength(int length, string? message = null);

    /// <summary>Maximum string length.</summary>
    IRuleBuilder<T, TProperty> MaxLength(int length, string? message = null);

    /// <summary>String length inclusive range.</summary>
    IRuleBuilder<T, TProperty> Length(int min, int max, string? message = null);

    /// <summary>Inclusive numeric / comparable range.</summary>
    IRuleBuilder<T, TProperty> InclusiveBetween(IComparable from, IComparable to, string? message = null);

    /// <summary>Value must equal <paramref name="value"/>.</summary>
    IRuleBuilder<T, TProperty> EqualTo(TProperty value, string? message = null);

    /// <summary>Value must equal another property (for example confirm password).</summary>
    IRuleBuilder<T, TProperty> EqualTo(Expression<Func<T, TProperty>> other, string? message = null);

    /// <summary>Custom synchronous predicate.</summary>
    IRuleBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, string? message = null);

    /// <inheritdoc cref="Must(Func{TProperty, bool}, string?)"/>
    IRuleBuilder<T, TProperty> Must(Func<T, TProperty, bool> predicate, string? message = null);

    /// <summary>Custom asynchronous predicate (availability checks, etc.).</summary>
    IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicate, string? message = null);

    /// <inheritdoc cref="MustAsync(Func{TProperty, CancellationToken, Task{bool}}, string?)"/>
    IRuleBuilder<T, TProperty> MustAsync(Func<T, TProperty, CancellationToken, Task<bool>> predicate, string? message = null);

    /// <summary>Server-side check. Return <see cref="ServerValidationResult.Ok"/> or <see cref="ServerValidationResult.Fail"/>.</summary>
    IRuleBuilder<T, TProperty> Server(Func<TProperty, CancellationToken, Task<ServerValidationResult>> validate, string? message = null);

    /// <inheritdoc cref="Server(Func{TProperty, CancellationToken, Task{ServerValidationResult}}, string?)"/>
    IRuleBuilder<T, TProperty> Server(Func<T, TProperty, CancellationToken, Task<ServerValidationResult>> validate, string? message = null);

    /// <summary>Server-side check. Return <c>null</c> when valid, or an error message.</summary>
    IRuleBuilder<T, TProperty> Server(Func<TProperty, CancellationToken, Task<string?>> validate);

    /// <summary>Run the rules on this property only when <paramref name="condition"/> is true.</summary>
    IRuleBuilder<T, TProperty> When(Func<T, bool> condition);

    /// <summary>Skip the rules on this property when <paramref name="condition"/> is true.</summary>
    IRuleBuilder<T, TProperty> Unless(Func<T, bool> condition);

    /// <summary>Replaces the error message of the last rule added.</summary>
    IRuleBuilder<T, TProperty> WithMessage(string message);

    /// <summary>Stop or continue after the first failure on this property. Default is <see cref="CascadeMode.Stop"/>.</summary>
    IRuleBuilder<T, TProperty> Cascade(CascadeMode mode);
}

/// <summary>How many failures to collect on a single property.</summary>
public enum CascadeMode
{
    /// <summary>Stop after the first failing rule (default, mobile-friendly).</summary>
    Stop = 0,

    /// <summary>Run every rule and collect all messages.</summary>
    Continue = 1
}

/// <summary>When attached <c>Validation.For</c> controls re-run rules.</summary>
public enum ValidationTrigger
{
    /// <summary>Use <see cref="FormValidationOptions.Trigger"/> (LostFocus unless configured).</summary>
    Default = 0,

    /// <summary>Validate when the control loses focus. Live-update after the first blur.</summary>
    LostFocus = 1,

    /// <summary>Validate as the value changes (debounced for async / server rules).</summary>
    PropertyChanged = 2,

    /// <summary>Only when <see cref="IValidationContext.ValidateAsync"/> runs.</summary>
    Explicit = 3
}

/// <summary>
/// A view-model or wrapper that owns a validator and raises UI-friendly change events.
/// </summary>
public interface IValidationContext
{
    /// <summary>Object the rules read.</summary>
    object Model { get; }

    /// <summary>Compiled / live validator.</summary>
    IValidator Validator { get; }

    /// <summary>Most recent full or property result.</summary>
    ValidationResult LastResult { get; }

    /// <summary>Raised after any property or form validation.</summary>
    event EventHandler<ValidationChangedEventArgs>? ValidationChanged;

    /// <summary>Validates the whole model and marks every property as touched.</summary>
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates one property and marks it as touched.</summary>
    Task<ValidationResult> ValidatePropertyAsync(string propertyName, CancellationToken cancellationToken = default);

    /// <summary>Clears errors and touched state.</summary>
    void ClearValidation();

    /// <summary>Marks a property so later changes re-validate it.</summary>
    void MarkTouched(string propertyName);

    /// <summary>Whether the property has been blurred or submitted.</summary>
    bool IsTouched(string propertyName);
}

/// <summary>Payload for <see cref="IValidationContext.ValidationChanged"/>.</summary>
public sealed class ValidationChangedEventArgs : EventArgs
{
    /// <summary>Creates the event args.</summary>
    public ValidationChangedEventArgs(string? propertyName, ValidationResult result)
    {
        PropertyName = propertyName;
        Result = result;
    }

    /// <summary>Property that changed, or <c>null</c> for a full-form run.</summary>
    public string? PropertyName { get; }

    /// <summary>Result of that run.</summary>
    public ValidationResult Result { get; }
}
