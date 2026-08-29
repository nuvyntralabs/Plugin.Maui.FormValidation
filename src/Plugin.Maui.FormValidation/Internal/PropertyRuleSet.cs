namespace Plugin.Maui.FormValidation.Internal;

internal sealed class PropertyRuleSet
{
    readonly List<IValidationRule> _rules = [];
    Func<object, bool>? _condition;

    public PropertyRuleSet(PropertyAccessor accessor, CascadeMode cascadeMode)
    {
        Accessor = accessor;
        CascadeMode = cascadeMode;
    }

    public PropertyAccessor Accessor { get; }
    public CascadeMode CascadeMode { get; set; }
    public IReadOnlyList<IValidationRule> Rules => _rules;
    public IValidationRule? LastRule => _rules.Count == 0 ? null : _rules[^1];

    public void Add(IValidationRule rule) => _rules.Add(rule);

    public void AndCondition(Func<object, bool> condition)
    {
        var previous = _condition;
        _condition = previous is null
            ? condition
            : instance => previous(instance) && condition(instance);
    }

    public bool ShouldRun(object instance)
        => _condition is null || _condition(instance);

    public ValidationResult Validate(object instance, bool includeAsync)
    {
        if (!ShouldRun(instance))
        {
            return ValidationResult.Success;
        }

        var context = CreateContext(instance);
        var errors = new List<ValidationError>();

        foreach (var rule in _rules)
        {
            if (rule.SkipWhenEmpty && ValueHelpers.IsEmpty(context.Value))
            {
                continue;
            }

            if (rule.IsAsync)
            {
                if (!includeAsync)
                {
                    continue;
                }

                throw new InvalidOperationException($"Rule '{rule.Code}' on '{Accessor.Name}' is async. Call ValidateAsync.");
            }

            var error = rule.Validate(context);
            if (error is null)
            {
                continue;
            }

            errors.Add(error);
            if (CascadeMode == CascadeMode.Stop)
            {
                break;
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Failure(errors);
    }

    public async Task<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken)
    {
        if (!ShouldRun(instance))
        {
            return ValidationResult.Success;
        }

        var context = CreateContext(instance);
        var errors = new List<ValidationError>();

        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rule.SkipWhenEmpty && ValueHelpers.IsEmpty(context.Value))
            {
                continue;
            }

            var error = rule.IsAsync
                ? await rule.ValidateAsync(context, cancellationToken).ConfigureAwait(false)
                : rule.Validate(context);

            if (error is null)
            {
                continue;
            }

            errors.Add(error);
            if (CascadeMode == CascadeMode.Stop)
            {
                break;
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Failure(errors);
    }

    RuleContext CreateContext(object instance)
        => new()
        {
            Instance = instance,
            PropertyName = Accessor.Name,
            Value = Accessor.GetValue(instance)
        };
}
