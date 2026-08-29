namespace Plugin.Maui.FormValidation.Internal;

internal sealed class PropertyAccessor
{
    public required string Name { get; init; }
    public required Func<object, object?> GetValue { get; init; }
    public required Action<object, object?>? SetValue { get; init; }
    public required Type PropertyType { get; init; }

    public static PropertyAccessor Create<T, TProperty>(Expression<Func<T, TProperty>> expression)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(expression);

        var body = Unwrap(expression.Body);
        var members = new List<PropertyInfo>();
        var current = body;

        while (current is MemberExpression member)
        {
            if (member.Member is not PropertyInfo property)
            {
                throw new ArgumentException("Rule() requires a property accessor such as x => x.Email.", nameof(expression));
            }

            members.Add(property);
            current = member.Expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
                ? unary.Operand
                : member.Expression;
        }

        if (members.Count == 0)
        {
            throw new ArgumentException("Rule() requires a property accessor such as x => x.Email.", nameof(expression));
        }

        members.Reverse();
        var name = string.Join(".", members.Select(static property => property.Name));
        var leaf = members[^1];

        return new PropertyAccessor
        {
            Name = name,
            PropertyType = leaf.PropertyType,
            GetValue = instance =>
            {
                object? value = instance;
                foreach (var property in members)
                {
                    if (value is null)
                    {
                        return null;
                    }

                    value = property.GetValue(value);
                }

                return value;
            },
            SetValue = leaf.SetMethod is null
                ? null
                : (instance, value) =>
                {
                    object? target = instance;
                    for (var index = 0; index < members.Count - 1; index++)
                    {
                        target = members[index].GetValue(target);
                        if (target is null)
                        {
                            return;
                        }
                    }

                    leaf.SetValue(target, ValueHelpers.Coerce(value, leaf.PropertyType));
                }
        };
    }

    static Expression Unwrap(Expression expression)
        => expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
            ? unary.Operand
            : expression;
}
