namespace Plugin.Maui.FormValidation.Internal;

internal static class ValueHelpers
{
    public static bool IsEmpty(object? value)
    {
        switch (value)
        {
            case null:
                return true;
            case string text:
                return string.IsNullOrWhiteSpace(text);
            case IEnumerable enumerable and not string:
            {
                var enumerator = enumerable.GetEnumerator();
                try
                {
                    return !enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }
            default:
                return false;
        }
    }

    public static string? AsString(object? value)
        => value switch
        {
            null => null,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
            _ => value.ToString()
        };

    public static bool TryToDecimal(object? value, out decimal number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case decimal dec:
                number = dec;
                return true;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return true;
            case float f when !float.IsNaN(f) && !float.IsInfinity(f):
                number = (decimal)f;
                return true;
            case double d when !double.IsNaN(d) && !double.IsInfinity(d):
                number = (decimal)d;
                return true;
            case string text:
                return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out number)
                    || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
            case IConvertible convertible:
                try
                {
                    number = convertible.ToDecimal(CultureInfo.CurrentCulture);
                    return true;
                }
                catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
                {
                    number = 0;
                    return false;
                }
            default:
                number = 0;
                return false;
        }
    }

    public static int Compare(object? value, IComparable boundary)
    {
        if (value is IComparable comparable && value.GetType() == boundary.GetType())
        {
            return comparable.CompareTo(boundary);
        }

        if (TryToDecimal(value, out var number) && TryToDecimal(boundary, out var limit))
        {
            return number.CompareTo(limit);
        }

        if (value is DateTime dateValue && boundary is DateTime dateLimit)
        {
            return dateValue.CompareTo(dateLimit);
        }

        if (value is DateTimeOffset offsetValue && boundary is DateTimeOffset offsetLimit)
        {
            return offsetValue.CompareTo(offsetLimit);
        }

        if (value is IComparable fallback)
        {
            try
            {
                return fallback.CompareTo(boundary);
            }
            catch (ArgumentException)
            {
                // Fall through.
            }
        }

        throw new InvalidOperationException($"Cannot compare '{value}' with '{boundary}'.");
    }

    public static object? Coerce(object? value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is null)
        {
            return Nullable.GetUnderlyingType(targetType) is not null || !targetType.IsValueType
                ? null
                : Activator.CreateInstance(targetType);
        }

        if (underlying.IsInstanceOfType(value))
        {
            return value;
        }

        if (value is string text)
        {
            if (underlying == typeof(string))
            {
                return text;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return Nullable.GetUnderlyingType(targetType) is not null || !targetType.IsValueType
                    ? null
                    : Activator.CreateInstance(targetType);
            }

            if (underlying == typeof(Guid) && Guid.TryParse(text, out var guid))
            {
                return guid;
            }

            return Convert.ChangeType(text, underlying, CultureInfo.CurrentCulture);
        }

        return Convert.ChangeType(value, underlying, CultureInfo.CurrentCulture);
    }

    public static string PhoneDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = new char[value.Length];
        var length = 0;
        foreach (var ch in value)
        {
            if (ch is '+' && length == 0)
            {
                chars[length++] = ch;
            }
            else if (char.IsAsciiDigit(ch))
            {
                chars[length++] = ch;
            }
        }

        return new string(chars, 0, length);
    }
}
