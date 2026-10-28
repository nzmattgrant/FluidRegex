using System.Globalization;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// How many times a piece of the pattern is matched. The public API sets it with the count methods on
/// <see cref="TextPattern"/>, such as <see cref="TextPattern.OneOrMore"/>. The default value means <see cref="Once"/>.
/// </summary>
internal readonly struct NumberOfTimes
{
    private const int NoLimit = -1;

    // default(NumberOfTimes) has _isSet == false and means Once.
    private readonly bool _isSet;
    private readonly int _minimum;
    private readonly int _maximum;
    private readonly bool _asFewAsPossible;

    private NumberOfTimes(int minimum, int maximum, bool asFewAsPossible)
    {
        _isSet = true;
        _minimum = minimum;
        _maximum = maximum;
        _asFewAsPossible = asFewAsPossible;
    }

    public static NumberOfTimes Once => default;

    public static NumberOfTimes Optional => new(0, 1, false);

    public static NumberOfTimes ZeroOrMore => new(0, NoLimit, false);

    public static NumberOfTimes OneOrMore => new(1, NoLimit, false);

    public static NumberOfTimes Exactly(int count)
    {
        Guard.Positive(count, nameof(count));
        return Between(count, count);
    }

    public static NumberOfTimes AtLeast(int minimum)
    {
        Guard.NotNegative(minimum, nameof(minimum));
        return new NumberOfTimes(minimum, NoLimit, false);
    }

    public static NumberOfTimes AtMost(int maximum)
    {
        Guard.Positive(maximum, nameof(maximum));
        return Between(0, maximum);
    }

    public static NumberOfTimes Between(int minimum, int maximum)
    {
        Guard.NotNegative(minimum, nameof(minimum));
        Guard.Positive(maximum, nameof(maximum));
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The maximum cannot be less than the minimum.");
        }

        return minimum == 1 && maximum == 1 ? Once : new NumberOfTimes(minimum, maximum, false);
    }

    public NumberOfTimes AsFewAsPossible()
    {
        if (IsFixed)
        {
            throw new InvalidOperationException(
                "AsFewAsPossible() only works with a count that can vary, such as OneOrMore() or Between(2, 5). " +
                "Exactly(...) always matches the same number of times, so there is nothing to choose.");
        }

        return new NumberOfTimes(_minimum, _maximum, true);
    }

    public int Minimum => _isSet ? _minimum : 1;

    /// <summary>The most times the piece is matched, or <see langword="null"/> when there is no upper limit.</summary>
    public int? Maximum => !_isSet ? 1 : _maximum == NoLimit ? null : _maximum;

    public bool IsAsFewAsPossible => _asFewAsPossible;

    public bool IsOnce => Minimum == 1 && Maximum == 1;

    private bool IsFixed => Maximum == Minimum;

    /// <summary>The regex suffix for this count: "", "?", "*", "+", "{n}", "{n,}", "{0,n}" or "{n,m}", plus "?" when lazy.</summary>
    public string Suffix
    {
        get
        {
            var minimum = Minimum;
            var maximum = Maximum;
            var suffix = (minimum, maximum) switch
            {
                (1, 1) => string.Empty,
                (0, 1) => "?",
                (0, null) => "*",
                (1, null) => "+",
                (_, null) => "{" + Format(minimum) + ",}",
                _ when minimum == maximum => "{" + Format(minimum) + "}",
                _ => "{" + Format(minimum) + "," + Format(maximum!.Value) + "}",
            };
            return _asFewAsPossible ? suffix + "?" : suffix;
        }
    }

    /// <summary>
    /// The same count with a minimum of at least one, used when the zero case is moved outside a capture.
    /// Optional becomes Once, ZeroOrMore becomes OneOrMore and AtMost(n) becomes Between(1, n).
    /// </summary>
    public NumberOfTimes WithMinimumOfOne()
    {
        if (Minimum >= 1)
        {
            return this;
        }

        var maximum = Maximum;
        if (maximum == 1)
        {
            return Once;
        }

        return new NumberOfTimes(1, maximum ?? NoLimit, _asFewAsPossible);
    }

    public override string ToString() => Suffix;

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
