namespace Plugin.Maui.FormValidation.Internal;

internal interface IValidationRule
{
    string Code { get; }
    string? Message { get; set; }
    bool IsAsync { get; }
    bool SkipWhenEmpty { get; }
    ValidationError? Validate(RuleContext context);
    Task<ValidationError?> ValidateAsync(RuleContext context, CancellationToken cancellationToken);
}

internal sealed class RuleContext
{
    public required object Instance { get; init; }
    public required string PropertyName { get; init; }
    public required object? Value { get; init; }
}

internal abstract class SyncRule : IValidationRule
{
    public abstract string Code { get; }
    public string? Message { get; set; }
    public virtual bool IsAsync => false;
    public virtual bool SkipWhenEmpty => true;

    public abstract ValidationError? Validate(RuleContext context);

    public Task<ValidationError?> ValidateAsync(RuleContext context, CancellationToken cancellationToken)
        => Task.FromResult(Validate(context));

    protected ValidationError Fail(RuleContext context, string fallback)
        => new(context.PropertyName, Message ?? fallback, Code, context.Value);
}

internal abstract class AsyncRule : IValidationRule
{
    public abstract string Code { get; }
    public string? Message { get; set; }
    public bool IsAsync => true;
    public virtual bool SkipWhenEmpty => true;

    public ValidationError? Validate(RuleContext context) => null;

    public abstract Task<ValidationError?> ValidateAsync(RuleContext context, CancellationToken cancellationToken);

    protected ValidationError Fail(RuleContext context, string fallback)
        => new(context.PropertyName, Message ?? fallback, Code, context.Value);
}
