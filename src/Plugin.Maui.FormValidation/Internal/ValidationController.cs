namespace Plugin.Maui.FormValidation.Internal;

internal static class ValidationController
{
    static readonly ConditionalWeakTable<VisualElement, FieldBinding> Bindings = [];

    public static void Attach(VisualElement view, string propertyName)
    {
        var binding = new FieldBinding(view, propertyName);
        Bindings.Add(view, binding);
        binding.Connect();
    }

    public static void Detach(VisualElement view)
    {
        if (Bindings.TryGetValue(view, out var binding))
        {
            binding.Dispose();
            Bindings.Remove(view);
        }
    }

    public static void Refresh(VisualElement view)
    {
        if (Bindings.TryGetValue(view, out var binding))
        {
            binding.Reconnect();
        }
    }
}

internal static class ValidationMessageController
{
    static readonly ConditionalWeakTable<Label, MessageBinding> Bindings = [];

    public static void Attach(Label label, string propertyName)
    {
        var binding = new MessageBinding(label, propertyName);
        Bindings.Add(label, binding);
        binding.Connect();
    }

    public static void Detach(Label label)
    {
        if (Bindings.TryGetValue(label, out var binding))
        {
            binding.Dispose();
            Bindings.Remove(label);
        }
    }
}

internal sealed class FieldBinding : IDisposable
{
    readonly VisualElement _view;
    readonly string _propertyName;
    IValidationContext? _context;
    Color? _originalBackground;
    bool _originalBackgroundCaptured;
    Label? _generatedLabel;
    CancellationTokenSource? _debounce;
    bool _disposed;

    public FieldBinding(VisualElement view, string propertyName)
    {
        _view = view;
        _propertyName = propertyName;
    }

    public void Connect()
    {
        _view.HandlerChanged += OnHandlerChanged;
        _view.BindingContextChanged += OnBindingContextChanged;
        _view.Unloaded += OnUnloaded;
        _view.Unfocused += OnUnfocused;
        SubscribeValueChanges(true);
        Reconnect();
    }

    public void Reconnect()
    {
        UnsubscribeContext();
        _context = ResolveContext(_view);
        if (_context is not null)
        {
            _context.ValidationChanged += OnValidationChanged;
            Apply(_context.LastResult.FirstError(_propertyName));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _debounce?.Cancel();
        _debounce?.Dispose();
        _view.HandlerChanged -= OnHandlerChanged;
        _view.BindingContextChanged -= OnBindingContextChanged;
        _view.Unloaded -= OnUnloaded;
        _view.Unfocused -= OnUnfocused;
        SubscribeValueChanges(false);
        UnsubscribeContext();
        RemoveGeneratedLabel();
    }

    void OnHandlerChanged(object? sender, EventArgs e) => Reconnect();

    void OnBindingContextChanged(object? sender, EventArgs e) => Reconnect();

    void OnUnloaded(object? sender, EventArgs e) => Dispose();

    void OnUnfocused(object? sender, FocusEventArgs e)
    {
        var trigger = ResolveTrigger();
        if (trigger is ValidationTrigger.LostFocus or ValidationTrigger.Default)
        {
            _ = ValidateAsync();
        }
    }

    void OnValueChanged(object? sender, EventArgs e)
    {
        var trigger = ResolveTrigger();
        if (trigger == ValidationTrigger.PropertyChanged)
        {
            _ = ValidateAsync();
            return;
        }

        if (trigger is ValidationTrigger.LostFocus or ValidationTrigger.Default
            && _context?.IsTouched(_propertyName) == true)
        {
            _ = ValidateAsync();
        }
    }

    void OnValidationChanged(object? sender, ValidationChangedEventArgs e)
    {
        if (e.PropertyName is null || string.Equals(e.PropertyName, _propertyName, StringComparison.OrdinalIgnoreCase))
        {
            Apply(e.Result.FirstError(_propertyName));
        }
    }

    async Task ValidateAsync()
    {
        var context = _context ?? ResolveContext(_view);
        if (context is null)
        {
            return;
        }

        _context = context;
        PushControlValue(context);
        context.MarkTouched(_propertyName);

        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        var delay = FormValidationOptions.Current.AsyncDebounce;
        if (ResolveTrigger() == ValidationTrigger.PropertyChanged && delay > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(delay, token).ConfigureAwait(true);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        try
        {
            var result = await context.ValidatePropertyAsync(_propertyName, token).ConfigureAwait(true);
            if (!token.IsCancellationRequested)
            {
                Apply(result.FirstError(_propertyName));
            }
        }
        catch (OperationCanceledException)
        {
            // A newer keystroke replaced this run.
        }
    }

    void Apply(string? error)
    {
        var hasError = !string.IsNullOrEmpty(error);
        Validation.SetHasError(_view, hasError);
        Validation.SetError(_view, error);

        if (ShouldApplyVisual())
        {
            CaptureBackground();
            VisualStateManager.GoToState(_view, hasError ? ValidationStates.Invalid : ValidationStates.Valid);
            _view.BackgroundColor = hasError
                ? Color.FromArgb(FormValidationOptions.Current.InvalidBackgroundHex)
                : _originalBackground;
        }

        UpdateGeneratedLabel(error);
    }

    void UpdateGeneratedLabel(string? error)
    {
        if (!ShouldShowMessage() || _view.Parent is not IList<IView> parent)
        {
            return;
        }

        if (HasSiblingMessageLabel())
        {
            return;
        }

        if (string.IsNullOrEmpty(error))
        {
            if (_generatedLabel is not null)
            {
                _generatedLabel.IsVisible = false;
                _generatedLabel.Text = string.Empty;
            }

            return;
        }

        if (_generatedLabel is null)
        {
            _generatedLabel = new Label
            {
                FontSize = 12,
                TextColor = Color.FromArgb(FormValidationOptions.Current.ErrorTextColorHex),
                Margin = new Thickness(4, 0, 4, 8)
            };

            var index = parent.IndexOf(_view);
            if (index >= 0)
            {
                parent.Insert(index + 1, _generatedLabel);
            }
            else
            {
                parent.Add(_generatedLabel);
            }
        }

        _generatedLabel.Text = error;
        _generatedLabel.IsVisible = true;
    }

    bool HasSiblingMessageLabel()
    {
        if (_view.Parent is not IEnumerable<IView> siblings)
        {
            return false;
        }

        foreach (var sibling in siblings)
        {
            if (sibling is Label label
                && string.Equals(Validation.GetMessageFor(label), _propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    void RemoveGeneratedLabel()
    {
        if (_generatedLabel is not null && _view.Parent is IList<IView> parent)
        {
            parent.Remove(_generatedLabel);
        }

        _generatedLabel = null;
    }

    void PushControlValue(IValidationContext context)
    {
        var value = _view switch
        {
            InputView input => input.Text,
            Picker picker => picker.SelectedItem,
            DatePicker date => date.Date,
            TimePicker time => time.Time,
            CheckBox check => check.IsChecked,
            Switch toggle => toggle.IsToggled,
            Slider slider => slider.Value,
            Stepper stepper => stepper.Value,
            _ => (object?)null
        };

        if (_view is InputView or Picker or DatePicker or TimePicker or CheckBox or Switch or Slider or Stepper)
        {
            context.Validator.TrySetProperty(context.Model, _propertyName, value);
        }
    }

    void SubscribeValueChanges(bool subscribe)
    {
        switch (_view)
        {
            case InputView input:
                if (subscribe) input.TextChanged += OnTextChanged;
                else input.TextChanged -= OnTextChanged;
                break;
            case Picker picker:
                if (subscribe) picker.SelectedIndexChanged += OnValueChanged;
                else picker.SelectedIndexChanged -= OnValueChanged;
                break;
            case DatePicker date:
                if (subscribe) date.DateSelected += OnDateSelected;
                else date.DateSelected -= OnDateSelected;
                break;
            case CheckBox check:
                if (subscribe) check.CheckedChanged += OnCheckedChanged;
                else check.CheckedChanged -= OnCheckedChanged;
                break;
            case Switch toggle:
                if (subscribe) toggle.Toggled += OnToggled;
                else toggle.Toggled -= OnToggled;
                break;
            case Slider slider:
                if (subscribe) slider.ValueChanged += OnSliderChanged;
                else slider.ValueChanged -= OnSliderChanged;
                break;
            case Stepper stepper:
                if (subscribe) stepper.ValueChanged += OnStepperChanged;
                else stepper.ValueChanged -= OnStepperChanged;
                break;
        }
    }

    void OnTextChanged(object? sender, TextChangedEventArgs e) => OnValueChanged(sender, e);
    void OnDateSelected(object? sender, DateChangedEventArgs e) => OnValueChanged(sender, e);
    void OnCheckedChanged(object? sender, CheckedChangedEventArgs e) => OnValueChanged(sender, e);
    void OnToggled(object? sender, ToggledEventArgs e) => OnValueChanged(sender, e);
    void OnSliderChanged(object? sender, ValueChangedEventArgs e) => OnValueChanged(sender, e);
    void OnStepperChanged(object? sender, ValueChangedEventArgs e) => OnValueChanged(sender, e);

    ValidationTrigger ResolveTrigger()
    {
        var trigger = Validation.GetTrigger(_view);
        return trigger == ValidationTrigger.Default ? FormValidationOptions.Current.Trigger : trigger;
    }

    bool ShouldShowMessage()
        => Validation.GetShowMessage(_view) ?? FormValidationOptions.Current.ShowMessage;

    bool ShouldApplyVisual()
        => Validation.GetApplyVisualState(_view) ?? FormValidationOptions.Current.ApplyVisualState;

    void CaptureBackground()
    {
        if (_originalBackgroundCaptured)
        {
            return;
        }

        _originalBackground = _view.BackgroundColor;
        _originalBackgroundCaptured = true;
    }

    void UnsubscribeContext()
    {
        if (_context is not null)
        {
            _context.ValidationChanged -= OnValidationChanged;
            _context = null;
        }
    }

    internal static IValidationContext? ResolveContext(Element view)
    {
        for (Element? current = view; current is not null; current = current.Parent)
        {
            if (Validation.GetContext(current) is { } attached
                && TryWrap(attached, current.BindingContext, out var fromAttached))
            {
                return fromAttached;
            }

            if (TryWrap(current.BindingContext, current.BindingContext, out var fromBinding))
            {
                return fromBinding;
            }
        }

        return null;
    }

    static bool TryWrap(object? candidate, object? model, out IValidationContext? context)
    {
        switch (candidate)
        {
            case IValidationContext validationContext:
                context = validationContext;
                return true;
            case IValidator validator when model is not null:
                context = new StandaloneValidationContext(model, validator);
                return true;
            default:
                context = null;
                return false;
        }
    }
}

internal sealed class MessageBinding : IDisposable
{
    readonly Label _label;
    readonly string _propertyName;
    IValidationContext? _context;

    public MessageBinding(Label label, string propertyName)
    {
        _label = label;
        _propertyName = propertyName;
    }

    public void Connect()
    {
        _label.HandlerChanged += OnChanged;
        _label.BindingContextChanged += OnChanged;
        _label.Unloaded += OnUnloaded;
        Reconnect();
    }

    public void Dispose()
    {
        _label.HandlerChanged -= OnChanged;
        _label.BindingContextChanged -= OnChanged;
        _label.Unloaded -= OnUnloaded;
        Unsubscribe();
    }

    void OnChanged(object? sender, EventArgs e) => Reconnect();

    void OnUnloaded(object? sender, EventArgs e) => Dispose();

    void Reconnect()
    {
        Unsubscribe();
        _context = FieldBinding.ResolveContext(_label);
        if (_context is null)
        {
            return;
        }

        _context.ValidationChanged += OnValidationChanged;
        Apply(_context.LastResult.FirstError(_propertyName));
    }

    void OnValidationChanged(object? sender, ValidationChangedEventArgs e)
    {
        if (e.PropertyName is null || string.Equals(e.PropertyName, _propertyName, StringComparison.OrdinalIgnoreCase))
        {
            Apply(e.Result.FirstError(_propertyName));
        }
    }

    void Apply(string? error)
    {
        _label.Text = error ?? string.Empty;
        _label.IsVisible = !string.IsNullOrEmpty(error);
        _label.TextColor = Color.FromArgb(FormValidationOptions.Current.ErrorTextColorHex);
    }

    void Unsubscribe()
    {
        if (_context is not null)
        {
            _context.ValidationChanged -= OnValidationChanged;
            _context = null;
        }
    }
}

internal sealed class StandaloneValidationContext : IValidationContext
{
    readonly HashSet<string> _touched = new(StringComparer.OrdinalIgnoreCase);

    public StandaloneValidationContext(object model, IValidator validator)
    {
        Model = model;
        Validator = validator;
    }

    public object Model { get; }
    public IValidator Validator { get; }
    public ValidationResult LastResult { get; private set; } = ValidationResult.Success;
    public event EventHandler<ValidationChangedEventArgs>? ValidationChanged;

    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        foreach (var name in Validator.PropertyNames)
        {
            MarkTouched(name);
        }

        LastResult = await Validator.ValidateAsync(Model, cancellationToken).ConfigureAwait(true);
        ValidationChanged?.Invoke(this, new ValidationChangedEventArgs(null, LastResult));
        return LastResult;
    }

    public async Task<ValidationResult> ValidatePropertyAsync(string propertyName, CancellationToken cancellationToken = default)
    {
        MarkTouched(propertyName);
        var result = await Validator.ValidatePropertyAsync(Model, propertyName, cancellationToken).ConfigureAwait(true);
        var others = LastResult.Errors.Where(error => !string.Equals(error.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
        LastResult = ValidationResult.Failure(others.Concat(result.Errors));
        ValidationChanged?.Invoke(this, new ValidationChangedEventArgs(propertyName, result));
        return result;
    }

    public void ClearValidation()
    {
        _touched.Clear();
        LastResult = ValidationResult.Success;
        ValidationChanged?.Invoke(this, new ValidationChangedEventArgs(null, LastResult));
    }

    public void MarkTouched(string propertyName) => _touched.Add(propertyName);

    public bool IsTouched(string propertyName) => _touched.Contains(propertyName);
}
