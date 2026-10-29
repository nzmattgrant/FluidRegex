using System.Globalization;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// How many times a piece is matched. Pass it as the last argument of the piece: <c>Digit(Times.Exactly(4))</c> emits
/// <c>[0-9]{4}</c>. Leave it out to match the piece once. The count covers the whole piece:
/// <c>Text("ab", Times.OneOrMore)</c> emits <c>(?:ab)+</c>.
/// </summary>
public readonly struct Times
{
    private const int NoLimit = -1;

    // default(Times) has _isSet == false and means Once.
    private readonly bool _isSet;
    private readonly int _minimum;
    private readonly int _maximum;
    private readonly bool _asFewAsPossible;

    private Times(int minimum, int maximum, bool asFewAsPossible)
    {
        _isSet = true;
        _minimum = minimum;
        _maximum = maximum;
        _asFewAsPossible = asFewAsPossible;
    }

    /// <summary>The piece may be missing. <c>Text("www.", Times.Optional)</c> emits <c>(?:www\.)?</c>.</summary>
    public static Times Optional => new(0, 1, false);

    /// <summary>The piece any number of times, or not at all. <c>Digit(Times.ZeroOrMore)</c> emits <c>[0-9]*</c>.</summary>
    public static Times ZeroOrMore => new(0, NoLimit, false);

    /// <summary>The piece at least once. <c>Digit(Times.OneOrMore)</c> emits <c>[0-9]+</c>.</summary>
    public static Times OneOrMore => new(1, NoLimit, false);

    internal static Times Once => default;

    internal int Minimum => _isSet ? _minimum : 1;

    /// <summary>The most times the piece is matched, or <see langword="null"/> when there is no upper limit.</summary>
    internal int? Maximum => !_isSet ? 1 : _maximum == NoLimit ? null : _maximum;

    internal bool IsAsFewAsPossible => _asFewAsPossible;

    internal bool IsOnce => Minimum == 1 && Maximum == 1;

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

    private bool IsFixed => Maximum == Minimum;

    /// <summary>The piece exactly <paramref name="count"/> times. <c>Digit(Times.Exactly(4))</c> emits <c>[0-9]{4}</c>.</summary>
    /// <param name="count">How many times; 1 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
    public static Times Exactly(int count)
    {
        Guard.Positive(count, nameof(count));
        return Between(count, count);
    }

    /// <summary>
    /// The piece <paramref name="minimum"/> times or more. <c>AnyCharacter(Times.AtLeast(8))</c> emits <c>[\s\S]{8,}</c>.
    /// </summary>
    /// <param name="minimum">The fewest times allowed; 0 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimum"/> is negative.</exception>
    public static Times AtLeast(int minimum)
    {
        Guard.NotNegative(minimum, nameof(minimum));
        return new Times(minimum, NoLimit, false);
    }

    /// <summary>
    /// The piece from <paramref name="minimum"/> to <paramref name="maximum"/> times.
    /// <c>Letter(Times.Between(2, 6))</c> emits <c>\p{L}{2,6}</c>.
    /// </summary>
    /// <param name="minimum">The fewest times allowed; 0 or more.</param>
    /// <param name="maximum">The most times allowed; 1 or more, and at least <paramref name="minimum"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minimum"/> is negative, or <paramref name="maximum"/> is less than 1 or less than <paramref name="minimum"/>.
    /// </exception>
    public static Times Between(int minimum, int maximum)
    {
        Guard.NotNegative(minimum, nameof(minimum));
        Guard.Positive(maximum, nameof(maximum));
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The maximum cannot be less than the minimum.");
        }

        return minimum == 1 && maximum == 1 ? Once : new Times(minimum, maximum, false);
    }

    /// <summary>
    /// The same count, but matching as few times as possible instead of as many as possible.
    /// In <c>&lt;a&gt;&lt;b&gt;</c>, <c>Text("&lt;").AnyCharacter(Times.OneOrMore.AsFewAsPossible()).Text("&gt;")</c> finds
    /// <c>&lt;a&gt;</c>; without it, it finds the whole text. Emits <c>+?</c>, <c>*?</c>, <c>??</c> or <c>{n,m}?</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The count is fixed, such as <c>Times.Exactly(4)</c>, so there is nothing to choose.</exception>
    public Times AsFewAsPossible()
    {
        if (IsFixed)
        {
            throw new InvalidOperationException(
                "AsFewAsPossible() only works with a count that can vary, such as Times.OneOrMore or Times.Between(2, 5). " +
                "Times.Exactly(...) always matches the same number of times, so there is nothing to choose.");
        }

        return new Times(_minimum, _maximum, true);
    }

    /// <summary>Returns the regex count text, for example <c>{4}</c> or <c>+</c>. Once is the empty string.</summary>
    public override string ToString() => Suffix;

    /// <summary>
    /// The same count with a minimum of at least one, used when the zero case is moved outside a capture.
    /// Optional becomes Once, ZeroOrMore becomes OneOrMore and Between(0, n) becomes Between(1, n).
    /// </summary>
    internal Times WithMinimumOfOne()
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

        return new Times(1, maximum ?? NoLimit, _asFewAsPossible);
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
