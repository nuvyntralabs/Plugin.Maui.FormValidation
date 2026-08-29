namespace Plugin.Maui.FormValidation;

/// <summary>
/// View-model base with <see cref="INotifyPropertyChanged"/> and <see cref="INotifyDataErrorInfo"/>.
/// Inherit <see cref="ValidatableViewModel{TSelf}"/> to configure fluent rules in the constructor.
/// </summary>
public abstract class ValidatableViewModel : INotifyPropertyChanged, INotifyDataErrorInfo, IValidationContext
{
    readonly Dictionary<string, IReadOnlyList<string>> _errors = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _touched = new(StringComparer.OrdinalIgnoreCase);
    readonly object _gate = new();
    IValidator? _validator;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc />
    public event EventHandler<ValidationChangedEventArgs>? ValidationChanged;

    /// <inheritdoc />
    public bool HasErrors
    {
        get
        {
            lock (_gate)
            {
                return _errors.Count > 0;
            }
        }
    }

    /// <inheritdoc />
    public object Model => this;

    /// <inheritdoc />
    public IValidator Validator
        => _validator ?? throw new InvalidOperationException("Call Configure() or inherit ValidatableViewModel<TSelf> before validating.");

    /// <inheritdoc />
    public ValidationResult LastResult { get; private set; } = ValidationResult.Success;

    /// <summary>Assigns the validator used by this view-model.</summary>
    protected void SetValidator(IValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    /// <summary>Raises <see cref="PropertyChanged"/> and re-validates the property when it has been touched.</summary>
    protected bool SetProperty<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            throw new ArgumentException("Property name is required.", nameof(propertyName));
        }

        if (EqualityComparer<TValue>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        RaisePropertyChanged(propertyName);
        if (IsTouched(propertyName) && _validator is not null)
        {
            _ = ValidatePropertyAsync(propertyName);
        }

        return true;
    }

    /// <summary>Raises <see cref="PropertyChanged"/>.</summary>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <inheritdoc />
    public IEnumerable GetErrors(string? propertyName)
    {
        lock (_gate)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _errors.Values.SelectMany(static messages => messages).ToArray();
            }

            return _errors.TryGetValue(propertyName, out var messages) ? messages : Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        foreach (var name in Validator.PropertyNames)
        {
            MarkTouched(name);
        }

        var result = await Validator.ValidateAsync(this, cancellationToken).ConfigureAwait(true);
        Apply(null, result);
        return result;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidatePropertyAsync(string propertyName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        MarkTouched(propertyName);
        var result = await Validator.ValidatePropertyAsync(this, propertyName, cancellationToken).ConfigureAwait(true);
        Apply(propertyName, result);
        return result;
    }

    /// <inheritdoc />
    public void ClearValidation()
    {
        string[] names;
        lock (_gate)
        {
            names = _errors.Keys.ToArray();
            _errors.Clear();
            _touched.Clear();
        }

        LastResult = ValidationResult.Success;
        foreach (var name in names)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
        }

        ValidationChanged?.Invoke(this, new ValidationChangedEventArgs(null, LastResult));
    }

    /// <inheritdoc />
    public void MarkTouched(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        lock (_gate)
        {
            _touched.Add(propertyName);
        }
    }

    /// <inheritdoc />
    public bool IsTouched(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        lock (_gate)
        {
            return _touched.Contains(propertyName);
        }
    }

    void Apply(string? propertyName, ValidationResult result)
    {
        if (propertyName is null)
        {
            LastResult = result;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            lock (_gate)
            {
                foreach (var name in _errors.Keys)
                {
                    names.Add(name);
                }

                _errors.Clear();
                foreach (var group in result.Errors.GroupBy(static error => error.PropertyName, StringComparer.OrdinalIgnoreCase))
                {
                    _errors[group.Key] = group.Select(static error => error.Message).ToArray();
                    names.Add(group.Key);
                }
            }

            foreach (var name in names)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
            }
        }
        else
        {
            var others = LastResult.Errors.Where(error => !string.Equals(error.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
            LastResult = ValidationResult.Failure(others.Concat(result.Errors));
            var messages = result.ErrorsFor(propertyName).Select(static error => error.Message).ToArray();
            lock (_gate)
            {
                if (messages.Length == 0)
                {
                    _errors.Remove(propertyName);
                }
                else
                {
                    _errors[propertyName] = messages;
                }
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        ValidationChanged?.Invoke(this, new ValidationChangedEventArgs(propertyName, result));
    }
}

/// <summary>
/// Typed view-model that exposes a fluent <see cref="Validator"/> bound to <c>this</c>.
/// </summary>
/// <typeparam name="TSelf">The concrete view-model type.</typeparam>
/// <example>
/// <code>
/// public sealed class SignUpViewModel : ValidatableViewModel&lt;SignUpViewModel&gt;
/// {
///     public SignUpViewModel()
///     {
///         Validator
///             .Rule(x => x.Email)
///             .Required()
///             .Email();
///     }
/// }
/// </code>
/// </example>
public abstract class ValidatableViewModel<TSelf> : ValidatableViewModel
    where TSelf : ValidatableViewModel<TSelf>
{
    /// <summary>Fluent rule builder for this instance.</summary>
    public new IValidatorBuilder<TSelf> Validator { get; }

    /// <summary>Creates the typed validator bound to this instance.</summary>
    protected ValidatableViewModel()
    {
        Validator = Plugin.Maui.FormValidation.Validator.For((TSelf)this);
        SetValidator(Validator);
    }
}
