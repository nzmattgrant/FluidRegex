using System.Text.RegularExpressions;

namespace FluidRegex.Internal;

/// <summary>Argument checks. netstandard2.0 has no ArgumentNullException.ThrowIfNull and friends.</summary>
internal static class Guard
{
    private static readonly Regex SavedNamePattern = new(@"\A[^\W\d]\w*\z", RegexOptions.CultureInvariant);

    public static T NotNull<T>(T? value, string paramName)
        where T : class =>
        value ?? throw new ArgumentNullException(paramName);

    public static string NotNullOrEmpty(string? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("The value cannot be empty.", paramName);
        }

        return value;
    }

    public static int NotNegative(int value, string paramName) =>
        value < 0 ? throw new ArgumentOutOfRangeException(paramName, value, "The value must be 0 or more.") : value;

    public static int Positive(int value, string paramName) =>
        value <= 0 ? throw new ArgumentOutOfRangeException(paramName, value, "The value must be 1 or more.") : value;

    public static string SavedName(string? name, string paramName)
    {
        NotNullOrEmpty(name, paramName);
        if (!SavedNamePattern.IsMatch(name!))
        {
            throw new ArgumentException(
                $"\"{name}\" can't be used as a name. A name starts with a letter or underscore and contains only letters, digits and underscores.",
                paramName);
        }

        return name!;
    }

    public static T[] NotNullOrEmptyItems<T>(T[]? items, string paramName)
        where T : class
    {
        NotNull(items, paramName);
        if (items!.Length == 0)
        {
            throw new ArgumentException("At least one item is required.", paramName);
        }

        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] is null)
            {
                throw new ArgumentException($"Item {i} is null.", paramName);
            }
        }

        return items;
    }
}
