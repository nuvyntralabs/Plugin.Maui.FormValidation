namespace Plugin.Maui.FormValidation.Internal;

internal sealed class ValidatorBuilder<T> : IValidatorBuilder<T>
    where T : class
{
    readonly T? _instance;
    readonly Dictionary<string, PropertyRuleSet> _sets = new(StringComparer.OrdinalIgnoreCase);

    public ValidatorBuilder(T? instance) => _instance = instance;

    public Type ModelType => typeof(T);

    public IReadOnlyCollection<string> PropertyNames => _sets.Keys;

    public IReadOnlyDictionary<string, PropertyRuleSet> Sets => _sets;

    public IRuleBuilder<T, TProperty> Rule<TProperty>(Expression<Func<T, TProperty>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        var property = PropertyAccessor.Create(accessor);
        if (!_sets.TryGetValue(property.Name, out var set))
        {
            set = new PropertyRuleSet(property, FormValidationOptions.Current.CascadeMode);
            _sets[property.Name] = set;
        }

        return new RuleBuilder<T, TProperty>(this, set);
    }

    public IValidator<T> Build() => this;

    public ValidationResult Validate()
        => Validate(RequireInstance());

    public ValidationResult Validate(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return Combine(_sets.Values.Select(set => set.Validate(instance, includeAsync: false)));
    }

    public ValidationResult Validate(object instance)
        => Validate(Cast(instance));

    public ValidationResult ValidateProperty(T instance, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return _sets.TryGetValue(propertyName, out var set)
            ? set.Validate(instance, includeAsync: false)
            : ValidationResult.Success;
    }

    public ValidationResult ValidateProperty(object instance, string propertyName)
        => ValidateProperty(Cast(instance), propertyName);

    public Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        => ValidateAsync(RequireInstance(), cancellationToken);

    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var results = new List<ValidationResult>(_sets.Count);
        foreach (var set in _sets.Values)
        {
            results.Add(await set.ValidateAsync(instance, cancellationToken).ConfigureAwait(false));
        }

        return Combine(results);
    }

    public Task<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default)
        => ValidateAsync(Cast(instance), cancellationToken);

    public Task<ValidationResult> ValidatePropertyAsync(T instance, string propertyName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return _sets.TryGetValue(propertyName, out var set)
            ? set.ValidateAsync(instance, cancellationToken)
            : Task.FromResult(ValidationResult.Success);
    }

    public Task<ValidationResult> ValidatePropertyAsync(object instance, string propertyName, CancellationToken cancellationToken = default)
        => ValidatePropertyAsync(Cast(instance), propertyName, cancellationToken);

    public bool TrySetProperty(object instance, string propertyName, object? value)
    {
        if (!_sets.TryGetValue(propertyName, out var set) || set.Accessor.SetValue is null)
        {
            return false;
        }

        try
        {
            set.Accessor.SetValue(instance, value);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            return false;
        }
    }

    T RequireInstance()
        => _instance ?? throw new InvalidOperationException("Call Validate(instance) when the builder was created with Validator.For<T>().");

    static T Cast(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return instance as T ?? throw new ArgumentException($"Expected an instance of {typeof(T).Name}.", nameof(instance));
    }

    static ValidationResult Combine(IEnumerable<ValidationResult> results)
    {
        var errors = results.SelectMany(static result => result.Errors).ToArray();
        return errors.Length == 0 ? ValidationResult.Success : ValidationResult.Failure(errors);
    }
}

internal sealed class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty>
    where T : class
{
    readonly ValidatorBuilder<T> _owner;
    readonly PropertyRuleSet _set;

    public RuleBuilder(ValidatorBuilder<T> owner, PropertyRuleSet set)
    {
        _owner = owner;
        _set = set;
    }

    public Type ModelType => _owner.ModelType;

    public IReadOnlyCollection<string> PropertyNames => _owner.PropertyNames;

    public IRuleBuilder<T, TNext> Rule<TNext>(Expression<Func<T, TNext>> accessor)
        => _owner.Rule(accessor);

    public IValidator<T> Build() => _owner;

    public ValidationResult Validate() => _owner.Validate();
    public ValidationResult Validate(T instance) => _owner.Validate(instance);
    public ValidationResult Validate(object instance) => _owner.Validate(instance);
    public ValidationResult ValidateProperty(T instance, string propertyName) => _owner.ValidateProperty(instance, propertyName);
    public ValidationResult ValidateProperty(object instance, string propertyName) => _owner.ValidateProperty(instance, propertyName);
    public Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default) => _owner.ValidateAsync(cancellationToken);
    public Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default) => _owner.ValidateAsync(instance, cancellationToken);
    public Task<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default) => _owner.ValidateAsync(instance, cancellationToken);
    public Task<ValidationResult> ValidatePropertyAsync(T instance, string propertyName, CancellationToken cancellationToken = default) => _owner.ValidatePropertyAsync(instance, propertyName, cancellationToken);
    public Task<ValidationResult> ValidatePropertyAsync(object instance, string propertyName, CancellationToken cancellationToken = default) => _owner.ValidatePropertyAsync(instance, propertyName, cancellationToken);
    public bool TrySetProperty(object instance, string propertyName, object? value) => _owner.TrySetProperty(instance, propertyName, value);

    public IRuleBuilder<T, TProperty> Required(string? message = null)
        => Add(new RequiredRule { Message = message });

    public IRuleBuilder<T, TProperty> Email(string? message = null)
        => Add(new EmailRule { Message = message });

    public IRuleBuilder<T, TProperty> Phone(string? message = null)
        => Add(new PhoneRule { Message = message });

    public IRuleBuilder<T, TProperty> Url(string? message = null)
        => Add(new UrlRule { Message = message });

    public IRuleBuilder<T, TProperty> Numeric(string? message = null)
        => Add(new NumericRule { Message = message });

    public IRuleBuilder<T, TProperty> Regex(string pattern, string? message = null)
        => Regex(new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), message);

    public IRuleBuilder<T, TProperty> Regex(Regex pattern, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        return Add(new RegexRule(pattern) { Message = message });
    }

    public IRuleBuilder<T, TProperty> Min(IComparable minimum, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(minimum);
        return Add(new MinRule(minimum) { Message = message });
    }

    public IRuleBuilder<T, TProperty> Max(IComparable maximum, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(maximum);
        return Add(new MaxRule(maximum) { Message = message });
    }

    public IRuleBuilder<T, TProperty> MinLength(int length, string? message = null)
        => Add(new MinLengthRule(length) { Message = message });

    public IRuleBuilder<T, TProperty> MaxLength(int length, string? message = null)
        => Add(new MaxLengthRule(length) { Message = message });

    public IRuleBuilder<T, TProperty> Length(int min, int max, string? message = null)
        => Add(new LengthRule(min, max) { Message = message });

    public IRuleBuilder<T, TProperty> InclusiveBetween(IComparable from, IComparable to, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        return Add(new InclusiveBetweenRule(from, to) { Message = message });
    }

    public IRuleBuilder<T, TProperty> EqualTo(TProperty value, string? message = null)
        => Add(new EqualToValueRule(value, EqualityComparer<TProperty>.Default) { Message = message });

    public IRuleBuilder<T, TProperty> EqualTo(Expression<Func<T, TProperty>> other, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(other);
        var accessor = PropertyAccessor.Create(other);
        return Add(new EqualToPropertyRule(accessor.GetValue, EqualityComparer<TProperty>.Default) { Message = message });
    }

    public IRuleBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Must((_, value) => predicate(value), message);
    }

    public IRuleBuilder<T, TProperty> Must(Func<T, TProperty, bool> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Add(new PredicateRule<T, TProperty>(predicate) { Message = message });
    }

    public IRuleBuilder<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return MustAsync((_, value, cancellationToken) => predicate(value, cancellationToken), message);
    }

    public IRuleBuilder<T, TProperty> MustAsync(Func<T, TProperty, CancellationToken, Task<bool>> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Add(new AsyncPredicateRule<T, TProperty>(predicate) { Message = message });
    }

    public IRuleBuilder<T, TProperty> Server(Func<TProperty, CancellationToken, Task<ServerValidationResult>> validate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(validate);
        return Server((_, value, cancellationToken) => validate(value, cancellationToken), message);
    }

    public IRuleBuilder<T, TProperty> Server(Func<T, TProperty, CancellationToken, Task<ServerValidationResult>> validate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(validate);
        return Add(new ServerRule<T, TProperty>(validate) { Message = message });
    }

    public IRuleBuilder<T, TProperty> Server(Func<TProperty, CancellationToken, Task<string?>> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);
        return Server(async (value, cancellationToken) =>
        {
            var message = await validate(value, cancellationToken).ConfigureAwait(false);
            return message is null ? ServerValidationResult.Ok() : ServerValidationResult.Fail(message);
        });
    }

    public IRuleBuilder<T, TProperty> When(Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _set.AndCondition(instance => condition((T)instance));
        return this;
    }

    public IRuleBuilder<T, TProperty> Unless(Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return When(instance => !condition(instance));
    }

    public IRuleBuilder<T, TProperty> WithMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (_set.LastRule is { } rule)
        {
            rule.Message = message;
        }

        return this;
    }

    public IRuleBuilder<T, TProperty> Cascade(CascadeMode mode)
    {
        _set.CascadeMode = mode;
        return this;
    }

    IRuleBuilder<T, TProperty> Add(IValidationRule rule)
    {
        _set.Add(rule);
        return this;
    }
}
