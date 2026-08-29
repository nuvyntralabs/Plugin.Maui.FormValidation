namespace Plugin.Maui.FormValidation;

/// <summary>
/// Visual-state names applied by <see cref="Validation"/>.
/// </summary>
public static class ValidationStates
{
    /// <summary>Visual state group name.</summary>
    public const string Group = "ValidationStates";

    /// <summary>The field currently has no error.</summary>
    public const string Valid = "Valid";

    /// <summary>The field currently has an error.</summary>
    public const string Invalid = "Invalid";
}

/// <summary>
/// Attached properties that wire a MAUI control to a fluent validator.
/// </summary>
/// <example>
/// <code language="xml">
/// &lt;Entry Text="{Binding Email}" v:Validation.For="Email" /&gt;
/// &lt;Label v:Validation.MessageFor="Email" /&gt;
/// </code>
/// </example>
public static class Validation
{
    /// <summary>Property name on the binding context / validator, for example <c>Email</c>.</summary>
    public static readonly BindableProperty ForProperty = BindableProperty.CreateAttached(
        "For",
        typeof(string),
        typeof(Validation),
        defaultValue: null,
        propertyChanged: OnForChanged);

    /// <summary>Optional explicit <see cref="IValidationContext"/> or <see cref="IValidator"/>.</summary>
    public static readonly BindableProperty ContextProperty = BindableProperty.CreateAttached(
        "Context",
        typeof(object),
        typeof(Validation),
        defaultValue: null,
        propertyChanged: OnControllerPropertyChanged);

    /// <summary>When this control re-validates.</summary>
    public static readonly BindableProperty TriggerProperty = BindableProperty.CreateAttached(
        "Trigger",
        typeof(ValidationTrigger),
        typeof(Validation),
        ValidationTrigger.Default);

    /// <summary>Show an error label under stack-layout parents.</summary>
    public static readonly BindableProperty ShowMessageProperty = BindableProperty.CreateAttached(
        "ShowMessage",
        typeof(bool?),
        typeof(Validation),
        defaultValue: null);

    /// <summary>Tint the control when invalid.</summary>
    public static readonly BindableProperty ApplyVisualStateProperty = BindableProperty.CreateAttached(
        "ApplyVisualState",
        typeof(bool?),
        typeof(Validation),
        defaultValue: null);

    /// <summary>On a <see cref="Label"/>, display the error for this property.</summary>
    public static readonly BindableProperty MessageForProperty = BindableProperty.CreateAttached(
        "MessageFor",
        typeof(string),
        typeof(Validation),
        defaultValue: null,
        propertyChanged: OnMessageForChanged);

    /// <summary>Read-only: the control currently has a validation error.</summary>
    public static readonly BindableProperty HasErrorProperty = BindableProperty.CreateAttached(
        "HasError",
        typeof(bool),
        typeof(Validation),
        false);

    /// <summary>Read-only: first error message for the bound property.</summary>
    public static readonly BindableProperty ErrorProperty = BindableProperty.CreateAttached(
        "Error",
        typeof(string),
        typeof(Validation),
        defaultValue: null);

    /// <summary>Gets <see cref="ForProperty"/>.</summary>
    public static string? GetFor(BindableObject view) => (string?)view.GetValue(ForProperty);

    /// <summary>Sets <see cref="ForProperty"/>.</summary>
    public static void SetFor(BindableObject view, string? value) => view.SetValue(ForProperty, value);

    /// <summary>Gets <see cref="ContextProperty"/>.</summary>
    public static object? GetContext(BindableObject view) => view.GetValue(ContextProperty);

    /// <summary>Sets <see cref="ContextProperty"/>.</summary>
    public static void SetContext(BindableObject view, object? value) => view.SetValue(ContextProperty, value);

    /// <summary>Gets <see cref="TriggerProperty"/>.</summary>
    public static ValidationTrigger GetTrigger(BindableObject view) => (ValidationTrigger)view.GetValue(TriggerProperty);

    /// <summary>Sets <see cref="TriggerProperty"/>.</summary>
    public static void SetTrigger(BindableObject view, ValidationTrigger value) => view.SetValue(TriggerProperty, value);

    /// <summary>Gets <see cref="ShowMessageProperty"/>.</summary>
    public static bool? GetShowMessage(BindableObject view) => (bool?)view.GetValue(ShowMessageProperty);

    /// <summary>Sets <see cref="ShowMessageProperty"/>.</summary>
    public static void SetShowMessage(BindableObject view, bool? value) => view.SetValue(ShowMessageProperty, value);

    /// <summary>Gets <see cref="ApplyVisualStateProperty"/>.</summary>
    public static bool? GetApplyVisualState(BindableObject view) => (bool?)view.GetValue(ApplyVisualStateProperty);

    /// <summary>Sets <see cref="ApplyVisualStateProperty"/>.</summary>
    public static void SetApplyVisualState(BindableObject view, bool? value) => view.SetValue(ApplyVisualStateProperty, value);

    /// <summary>Gets <see cref="MessageForProperty"/>.</summary>
    public static string? GetMessageFor(BindableObject view) => (string?)view.GetValue(MessageForProperty);

    /// <summary>Sets <see cref="MessageForProperty"/>.</summary>
    public static void SetMessageFor(BindableObject view, string? value) => view.SetValue(MessageForProperty, value);

    /// <summary>Gets <see cref="HasErrorProperty"/>.</summary>
    public static bool GetHasError(BindableObject view) => (bool)view.GetValue(HasErrorProperty);

    /// <summary>Sets <see cref="HasErrorProperty"/>.</summary>
    public static void SetHasError(BindableObject view, bool value) => view.SetValue(HasErrorProperty, value);

    /// <summary>Gets <see cref="ErrorProperty"/>.</summary>
    public static string? GetError(BindableObject view) => (string?)view.GetValue(ErrorProperty);

    /// <summary>Sets <see cref="ErrorProperty"/>.</summary>
    public static void SetError(BindableObject view, string? value) => view.SetValue(ErrorProperty, value);

    static void OnForChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is not VisualElement view)
        {
            return;
        }

        Internal.ValidationController.Detach(view);
        if (newValue is string name && !string.IsNullOrWhiteSpace(name))
        {
            Internal.ValidationController.Attach(view, name.Trim());
        }
    }

    static void OnControllerPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is VisualElement view)
        {
            Internal.ValidationController.Refresh(view);
        }
    }

    static void OnMessageForChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is not Label label)
        {
            return;
        }

        Internal.ValidationMessageController.Detach(label);
        if (newValue is string name && !string.IsNullOrWhiteSpace(name))
        {
            Internal.ValidationMessageController.Attach(label, name.Trim());
        }
    }
}
