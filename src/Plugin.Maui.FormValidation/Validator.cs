namespace Plugin.Maui.FormValidation;

/// <summary>
/// Entry point for fluent, mobile-first form validation.
/// </summary>
/// <example>
/// <code>
/// Validator
///     .For(model)
///     .Rule(x => x.Email)
///     .Required()
///     .Email();
/// </code>
/// </example>
public static class Validator
{
    /// <summary>
    /// Starts a rule builder bound to <paramref name="instance"/>.
    /// <see cref="IValidatorBuilder{T}.Validate()"/> uses this instance.
    /// </summary>
    public static IValidatorBuilder<T> For<T>(T instance)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        return new Internal.ValidatorBuilder<T>(instance);
    }

    /// <summary>
    /// Starts a reusable rule builder. Pass the model to <c>Validate(instance)</c>.
    /// </summary>
    public static IValidatorBuilder<T> For<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
        where T : class
        => new Internal.ValidatorBuilder<T>(instance: null);
}
