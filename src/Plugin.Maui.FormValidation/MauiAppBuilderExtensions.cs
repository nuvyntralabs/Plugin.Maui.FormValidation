namespace Plugin.Maui.FormValidation;

/// <summary>
/// MAUI host registration for form validation defaults.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="FormValidationOptions"/> used by <c>Validation.For</c> attached properties.
    /// Calling this is optional; built-in defaults apply otherwise.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseMauiFormValidation(options =>
    /// {
    ///     options.Trigger = ValidationTrigger.LostFocus;
    ///     options.ShowMessage = true;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseMauiFormValidation(this MauiAppBuilder builder, Action<FormValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new FormValidationOptions();
        configure?.Invoke(options);
        FormValidationOptions.Current = options;
        builder.Services.AddSingleton(options);
        return builder;
    }
}
