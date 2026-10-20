using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// A reusable part of a pattern. Build it with the same chain methods as <see cref="MatchThesePatterns"/>, then pass
/// it to <c>Group</c>, <c>Capture</c>, <c>CaptureAs</c>, <c>EitherOf</c> or the <c>If...</c> conditions.
/// Sub-patterns are immutable, so one instance can be used any number of times and can contain other sub-patterns.
/// </summary>
/// <example>
/// <code>
/// var wordsWithHyphenAndDots = new SubPattern()
///     .OneOfTheseCharacters("-.")
///     .AnyWordCharacter(OneOrMore);
///
/// new MatchThesePatterns().AnyWordCharacter(OneOrMore).Group(wordsWithHyphenAndDots, ZeroOrMore);
/// // \w+(?:[-.]\w+)*
/// </code>
/// </example>
public sealed class SubPattern : FluidRegexBuilderBase<SubPattern>
{
    /// <summary>Starts an empty sub-pattern.</summary>
    public SubPattern()
        : base(NodeList.Empty)
    {
    }

    private SubPattern(NodeList nodes)
        : base(nodes)
    {
    }

    private protected override SubPattern With(NodeList nodes) => new(nodes);
}
