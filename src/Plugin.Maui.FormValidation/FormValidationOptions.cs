namespace Plugin.Maui.FormValidation;

/// <summary>
/// Defaults for attached-property validation and rule cascade.
/// </summary>
public sealed class FormValidationOptions
{
    /// <summary>Built-in defaults used when <c>UseMauiFormValidation</c> was not called.</summary>
    public static FormValidationOptions Default { get; } = new();

    static FormValidationOptions? current;

    /// <summary>Active options. Falls back to <see cref="Default"/>.</summary>
    public static FormValidationOptions Current
    {
        get => current ?? Default;
        set => current = value;
    }

    /// <summary>When <c>Validation.For</c> re-runs rules. Default is <see cref="ValidationTrigger.LostFocus"/>.</summary>
    public ValidationTrigger Trigger { get; set; } = ValidationTrigger.LostFocus;

    /// <summary>Insert or update an error label under the control when the parent is a stack layout.</summary>
    public bool ShowMessage { get; set; } = true;

    /// <summary>Tint the control and go to the <c>Invalid</c> visual state.</summary>
    public bool ApplyVisualState { get; set; } = true;

    /// <summary>Wait before running async / server rules while the user types.</summary>
    public TimeSpan AsyncDebounce { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Default cascade for new property rule sets.</summary>
    public CascadeMode CascadeMode { get; set; } = CascadeMode.Stop;

    /// <summary>Optional ISO region hint reserved for future phone formatting (validation itself is region-agnostic).</summary>
    public string? DefaultPhoneRegion { get; set; }

    /// <summary>Background used when a control is invalid and no <c>Invalid</c> visual state is defined.</summary>
    public string InvalidBackgroundHex { get; set; } = "#33E53935";

    /// <summary>Text color for generated error labels.</summary>
    public string ErrorTextColorHex { get; set; } = "#E53935";
}
