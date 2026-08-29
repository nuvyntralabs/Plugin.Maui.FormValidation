namespace Plugin.Maui.FormValidation.Internal;

internal sealed class RequiredRule : SyncRule
{
    public override string Code => "required";
    public override bool SkipWhenEmpty => false;

    public override ValidationError? Validate(RuleContext context)
        => ValueHelpers.IsEmpty(context.Value)
            ? Fail(context, ValidationMessages.Required(context.PropertyName))
            : null;
}

internal sealed class EmailRule : SyncRule
{
    public override string Code => "email";

    public override ValidationError? Validate(RuleContext context)
    {
        var text = ValueHelpers.AsString(context.Value)?.Trim();
        return text is not null && ValidationPatterns.Email().IsMatch(text)
            ? null
            : Fail(context, ValidationMessages.Email(context.PropertyName));
    }
}

internal sealed class PhoneRule : SyncRule
{
    public override string Code => "phone";

    public override ValidationError? Validate(RuleContext context)
    {
        var digits = ValueHelpers.PhoneDigits(ValueHelpers.AsString(context.Value));
        return ValidationPatterns.Phone().IsMatch(digits)
            ? null
            : Fail(context, ValidationMessages.Phone(context.PropertyName));
    }
}

internal sealed class UrlRule : SyncRule
{
    public override string Code => "url";

    public override ValidationError? Validate(RuleContext context)
    {
        var text = ValueHelpers.AsString(context.Value)?.Trim();
        if (text is null)
        {
            return Fail(context, ValidationMessages.Url(context.PropertyName));
        }

        if (!text.Contains("://", StringComparison.Ordinal) && text.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            text = "https://" + text;
        }

        return Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? null
            : Fail(context, ValidationMessages.Url(context.PropertyName));
    }
}

internal sealed class NumericRule : SyncRule
{
    public override string Code => "numeric";

    public override ValidationError? Validate(RuleContext context)
        => ValueHelpers.TryToDecimal(context.Value, out _)
            ? null
            : Fail(context, ValidationMessages.Numeric(context.PropertyName));
}

internal sealed class RegexRule : SyncRule
{
    readonly Regex _pattern;

    public RegexRule(Regex pattern) => _pattern = pattern;

    public override string Code => "regex";

    public override ValidationError? Validate(RuleContext context)
    {
        var text = ValueHelpers.AsString(context.Value);
        return text is not null && _pattern.IsMatch(text)
            ? null
            : Fail(context, ValidationMessages.Regex(context.PropertyName));
    }
}

internal sealed class MinRule : SyncRule
{
    readonly IComparable _minimum;

    public MinRule(IComparable minimum) => _minimum = minimum;

    public override string Code => "min";

    public override ValidationError? Validate(RuleContext context)
    {
        try
        {
            return ValueHelpers.Compare(context.Value, _minimum) >= 0
                ? null
                : Fail(context, ValidationMessages.Min(context.PropertyName, _minimum));
        }
        catch (InvalidOperationException)
        {
            return Fail(context, ValidationMessages.Min(context.PropertyName, _minimum));
        }
    }
}

internal sealed class MaxRule : SyncRule
{
    readonly IComparable _maximum;

    public MaxRule(IComparable maximum) => _maximum = maximum;

    public override string Code => "max";

    public override ValidationError? Validate(RuleContext context)
    {
        try
        {
            return ValueHelpers.Compare(context.Value, _maximum) <= 0
                ? null
                : Fail(context, ValidationMessages.Max(context.PropertyName, _maximum));
        }
        catch (InvalidOperationException)
        {
            return Fail(context, ValidationMessages.Max(context.PropertyName, _maximum));
        }
    }
}

internal sealed class MinLengthRule : SyncRule
{
    readonly int _length;

    public MinLengthRule(int length) => _length = length;

    public override string Code => "minlength";

    public override ValidationError? Validate(RuleContext context)
    {
        var text = ValueHelpers.AsString(context.Value) ?? string.Empty;
        return text.Length >= _length
            ? null
            : Fail(context, ValidationMessages.MinLength(context.PropertyName, _length));
    }
}

internal sealed class MaxLengthRule : SyncRule
{
    readonly int _length;

    public MaxLengthRule(int length) => _length = length;

    public override string Code => "maxlength";

    public override ValidationError? Validate(RuleContext context)
    {
        var text = ValueHelpers.AsString(context.Value) ?? string.Empty;
        return text.Length <= _length
            ? null
            : Fail(context, ValidationMessages.MaxLength(context.PropertyName, _length));
    }
}

internal sealed class LengthRule : SyncRule
{
    readonly int _min;
    readonly int _max;

    public LengthRule(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public override string Code => "length";

    public override ValidationError? Validate(RuleContext context)
    {
        var text = ValueHelpers.AsString(context.Value) ?? string.Empty;
        return text.Length >= _min && text.Length <= _max
            ? null
            : Fail(context, ValidationMessages.Length(context.PropertyName, _min, _max));
    }
}

internal sealed class InclusiveBetweenRule : SyncRule
{
    readonly IComparable _from;
    readonly IComparable _to;

    public InclusiveBetweenRule(IComparable from, IComparable to)
    {
        _from = from;
        _to = to;
    }

    public override string Code => "between";

    public override ValidationError? Validate(RuleContext context)
    {
        try
        {
            return ValueHelpers.Compare(context.Value, _from) >= 0 && ValueHelpers.Compare(context.Value, _to) <= 0
                ? null
                : Fail(context, ValidationMessages.InclusiveBetween(context.PropertyName, _from, _to));
        }
        catch (InvalidOperationException)
        {
            return Fail(context, ValidationMessages.InclusiveBetween(context.PropertyName, _from, _to));
        }
    }
}

internal sealed class EqualToValueRule : SyncRule
{
    readonly object? _expected;
    readonly IEqualityComparer _comparer;

    public EqualToValueRule(object? expected, IEqualityComparer comparer)
    {
        _expected = expected;
        _comparer = comparer;
    }

    public override string Code => "equalto";
    public override bool SkipWhenEmpty => false;

    public override ValidationError? Validate(RuleContext context)
        => _comparer.Equals(context.Value, _expected)
            ? null
            : Fail(context, ValidationMessages.EqualTo(context.PropertyName));
}

internal sealed class EqualToPropertyRule : SyncRule
{
    readonly Func<object, object?> _other;
    readonly IEqualityComparer _comparer;

    public EqualToPropertyRule(Func<object, object?> other, IEqualityComparer comparer)
    {
        _other = other;
        _comparer = comparer;
    }

    public override string Code => "equalto";
    public override bool SkipWhenEmpty => false;

    public override ValidationError? Validate(RuleContext context)
        => _comparer.Equals(context.Value, _other(context.Instance))
            ? null
            : Fail(context, ValidationMessages.EqualTo(context.PropertyName));
}

internal sealed class PredicateRule<T, TProperty> : SyncRule
    where T : class
{
    readonly Func<T, TProperty, bool> _predicate;

    public PredicateRule(Func<T, TProperty, bool> predicate) => _predicate = predicate;

    public override string Code => "must";
    public override bool SkipWhenEmpty => false;

    public override ValidationError? Validate(RuleContext context)
    {
        var value = context.Value is TProperty typed ? typed : default!;
        return _predicate((T)context.Instance, value)
            ? null
            : Fail(context, ValidationMessages.Must(context.PropertyName));
    }
}

internal sealed class AsyncPredicateRule<T, TProperty> : AsyncRule
    where T : class
{
    readonly Func<T, TProperty, CancellationToken, Task<bool>> _predicate;

    public AsyncPredicateRule(Func<T, TProperty, CancellationToken, Task<bool>> predicate)
        => _predicate = predicate;

    public override string Code => "must";
    public override bool SkipWhenEmpty => false;

    public override async Task<ValidationError?> ValidateAsync(RuleContext context, CancellationToken cancellationToken)
    {
        var value = context.Value is TProperty typed ? typed : default!;
        return await _predicate((T)context.Instance, value, cancellationToken).ConfigureAwait(false)
            ? null
            : Fail(context, ValidationMessages.Must(context.PropertyName));
    }
}

internal sealed class ServerRule<T, TProperty> : AsyncRule
    where T : class
{
    readonly Func<T, TProperty, CancellationToken, Task<ServerValidationResult>> _validate;

    public ServerRule(Func<T, TProperty, CancellationToken, Task<ServerValidationResult>> validate)
        => _validate = validate;

    public override string Code => "server";

    public override async Task<ValidationError?> ValidateAsync(RuleContext context, CancellationToken cancellationToken)
    {
        var value = context.Value is TProperty typed ? typed : default!;
        var result = await _validate((T)context.Instance, value, cancellationToken).ConfigureAwait(false);
        if (result.IsValid)
        {
            return null;
        }

        return new ValidationError(
            context.PropertyName,
            Message ?? result.ErrorMessage ?? ValidationMessages.Server(context.PropertyName),
            result.ErrorCode ?? Code,
            context.Value);
    }
}
