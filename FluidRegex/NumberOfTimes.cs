using System.Diagnostics;
using System.Globalization;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// How many times a piece of the pattern is matched, such as <see cref="OneOrMore"/>, <see cref="Optional"/>
/// or <see cref="Between(int, int)"/>. It is always the last argument of the call it applies to, and it always
/// applies to the whole piece that call adds.
/// </summary>
/// <remarks>
/// Add <c>using static FluidRegex.NumberOfTimes;</c> so counts read as plain words:
/// <c>.AnyDigit(Exactly(4))</c>, <c>.TheText("www.", Optional)</c>.
/// The default value means <see cref="Once"/>.
/// </remarks>
[DebuggerDisplay("{DebuggerDescription,nq}")]
public readonly struct NumberOfTimes : IEquatable<NumberOfTimes>
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

    /// <summary>Exactly once. This is the default for every count parameter. Emits nothing.</summary>
    public static NumberOfTimes Once => default;

    /// <summary>Zero or one time: the piece may be missing. Emits <c>?</c>.</summary>
    public static NumberOfTimes Optional => new(0, 1, false);

    /// <summary>Zero or more times. Emits <c>*</c>.</summary>
    public static NumberOfTimes ZeroOrMore => new(0, NoLimit, false);

    /// <summary>One or more times. Emits <c>+</c>.</summary>
    public static NumberOfTimes OneOrMore => new(1, NoLimit, false);

    /// <summary>Exactly <paramref name="times"/> times. Emits <c>{n}</c>.</summary>
    /// <param name="times">How many times; zero or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is negative.</exception>
    public static NumberOfTimes Exactly(int times)
    {
        Guard.NotNegative(times, nameof(times));
        return Between(times, times);
    }

    /// <summary>At least <paramref name="minimum"/> times, with no upper limit. Emits <c>{n,}</c>.</summary>
    /// <param name="minimum">The fewest times allowed; zero or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimum"/> is negative.</exception>
    public static NumberOfTimes AtLeast(int minimum)
    {
        Guard.NotNegative(minimum, nameof(minimum));
        return new NumberOfTimes(minimum, NoLimit, false);
    }

    /// <summary>Between zero and <paramref name="maximum"/> times. Emits <c>{0,n}</c>.</summary>
    /// <param name="maximum">The most times allowed; zero or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximum"/> is negative.</exception>
    public static NumberOfTimes AtMost(int maximum)
    {
        Guard.NotNegative(maximum, nameof(maximum));
        return Between(0, maximum);
    }

    /// <summary>Between <paramref name="minimum"/> and <paramref name="maximum"/> times. Emits <c>{min,max}</c>.</summary>
    /// <param name="minimum">The fewest times allowed; zero or more.</param>
    /// <param name="maximum">The most times allowed; at least <paramref name="minimum"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minimum"/> is negative, or <paramref name="maximum"/> is less than <paramref name="minimum"/>.
    /// </exception>
    public static NumberOfTimes Between(int minimum, int maximum)
    {
        Guard.NotNegative(minimum, nameof(minimum));
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The maximum cannot be less than the minimum.");
        }

        return minimum == 1 && maximum == 1 ? Once : new NumberOfTimes(minimum, maximum, false);
    }

    /// <summary>
    /// Matches as few times as possible instead of as many as possible (a lazy count), for example
    /// <c>OneOrMore.AsFewAsPossible()</c> emits <c>+?</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The count is fixed, such as <see cref="Once"/> or <c>Exactly(4)</c>, so there is nothing to minimise.</exception>
    public NumberOfTimes AsFewAsPossible()
    {
        if (IsFixed)
        {
            throw new InvalidOperationException($"AsFewAsPossible() has no effect on a fixed count ({DebuggerDescription}).");
        }

        return new NumberOfTimes(_minimum, _maximum, true);
    }

    /// <summary>The fewest times the piece is matched.</summary>
    public int Minimum => _isSet ? _minimum : 1;

    /// <summary>The most times the piece is matched, or <see langword="null"/> when there is no upper limit.</summary>
    public int? Maximum => !_isSet ? 1 : _maximum == NoLimit ? null : _maximum;

    /// <summary>Whether the count matches as few times as possible (see <see cref="AsFewAsPossible"/>).</summary>
    public bool IsAsFewAsPossible => _asFewAsPossible;

    internal bool IsOnce => Minimum == 1 && Maximum == 1;

    internal bool IsFixed => Maximum == Minimum;

    /// <summary>The regex suffix for this count: "", "?", "*", "+", "{n}", "{n,}", "{0,n}" or "{n,m}", plus "?" when lazy.</summary>
    internal string Suffix
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
    internal NumberOfTimes WithMinimumOfOne()
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

    private string DebuggerDescription => IsOnce ? "Once" : Suffix;

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Returns the regex suffix this count emits, for example <c>{2,256}</c>, or an empty string for <see cref="Once"/>.</summary>
    public override string ToString() => Suffix;

    /// <inheritdoc/>
    public bool Equals(NumberOfTimes other) =>
        Minimum == other.Minimum && Maximum == other.Maximum && _asFewAsPossible == other._asFewAsPossible;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is NumberOfTimes other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Minimum;
            hash = (hash * 397) ^ (Maximum ?? -1);
            hash = (hash * 397) ^ (_asFewAsPossible ? 1 : 0);
            return hash;
        }
    }

    /// <summary>Whether two counts match the same number of times.</summary>
    public static bool operator ==(NumberOfTimes left, NumberOfTimes right) => left.Equals(right);

    /// <summary>Whether two counts match a different number of times.</summary>
    public static bool operator !=(NumberOfTimes left, NumberOfTimes right) => !left.Equals(right);
}
