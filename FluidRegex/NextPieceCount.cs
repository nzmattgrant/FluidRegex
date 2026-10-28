using System.Diagnostics.Contracts;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// A count waiting for its piece, from <see cref="TextPattern.OneOrMore"/>, <see cref="TextPattern.Exactly"/> and the
/// other counts. Pick the piece next: <c>.Exactly(4).Digit()</c>. The count applies to the whole piece:
/// <c>.OneOrMore().Text("ab")</c> emits <c>(?:ab)+</c>.
/// </summary>
public sealed class NextPieceCount : PatternPieces
{
    private readonly TextPattern _pattern;

    internal NextPieceCount(TextPattern pattern, NumberOfTimes times)
        : base(times)
    {
        _pattern = pattern;
    }

    /// <summary>
    /// Matches the next piece as few times as possible instead of as many as possible.
    /// In <c>&lt;a&gt;&lt;b&gt;</c>, <c>Text("&lt;").OneOrMore().AsFewAsPossible().AnyCharacter().Text("&gt;")</c> finds
    /// <c>&lt;a&gt;</c>; without it, it finds the whole text. Emits <c>+?</c>, <c>*?</c>, <c>??</c> or <c>{n,m}?</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The count is fixed, such as <c>Exactly(4)</c>, so there is nothing to choose.</exception>
    [Pure]
    public NextPieceCount AsFewAsPossible() => new(_pattern, Times.AsFewAsPossible());

    private protected override TextPattern Append(RegexNode node) => _pattern.With(node);
}
