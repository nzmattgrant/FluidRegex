using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// The chain methods shared by <see cref="MatchThesePatterns"/> and <see cref="SubPattern"/>. Each call adds the next
/// piece of the pattern in reading order and returns a new builder; the original never changes.
/// </summary>
/// <typeparam name="T">The builder type each chain method returns.</typeparam>
/// <remarks>
/// <para>Counts are always the last argument (<see cref="NumberOfTimes"/>, default <see cref="NumberOfTimes.Once"/>)
/// and always apply to the whole piece the call adds: <c>TheText("abc", OneOrMore)</c> emits <c>(?:abc)+</c>.</para>
/// <para>Every string is literal text and is escaped, except in <see cref="RawRegex"/>.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class FluidRegexBuilderBase<T>
    where T : FluidRegexBuilderBase<T>
{
    private string? _pattern;

    private protected FluidRegexBuilderBase(NodeList nodes)
    {
        Nodes = nodes;
    }

    internal NodeList Nodes { get; }

    internal bool IsEmpty => Nodes.Count == 0;

    // ---------- Positions (match no characters, so they take no count) ----------

    /// <summary>At the very start of the text. Emits <c>\A</c>, which RegexOptions.Multiline cannot change.</summary>
    [Pure]
    public T StartOfText() => Append(new AnchorNode(@"\A"));

    /// <summary>
    /// At the very end of the text. Emits <c>\z</c>. Unlike <c>$</c>, a trailing newline is not accepted.
    /// </summary>
    [Pure]
    public T EndOfText() => Append(new AnchorNode(@"\z"));

    /// <summary>
    /// At the start of a line: the start of the text, or just after <c>\r\n</c>, <c>\n</c> or <c>\r</c>.
    /// Emits <c>(?&lt;![^\r\n])(?!(?&lt;=\r)\n)</c>, which needs no RegexOptions.Multiline.
    /// </summary>
    [Pure]
    public T StartOfLine() => Append(new AnchorNode(@"(?<![^\r\n])(?!(?<=\r)\n)"));

    /// <summary>
    /// At the end of a line: just before <c>\r\n</c>, <c>\n</c> or <c>\r</c>, or at the end of the text; never between
    /// <c>\r</c> and <c>\n</c>. Emits <c>(?![^\r\n])(?!(?&lt;=\r)\n)</c>.
    /// </summary>
    [Pure]
    public T EndOfLine() => Append(new AnchorNode(@"(?![^\r\n])(?!(?<=\r)\n)"));

    /// <summary>At the edge of a word, between a word character and a non-word character. Emits <c>\b</c>.</summary>
    [Pure]
    public T WordBoundary() => Append(new AnchorNode(@"\b"));

    /// <summary>Not at the edge of a word, for example inside a word. Emits <c>\B</c>.</summary>
    [Pure]
    public T NotAWordBoundary() => Append(new AnchorNode(@"\B"));

    // ---------- Literal text (always escaped) ----------

    /// <summary>
    /// The text <paramref name="text"/>, taken literally. <c>TheText("www.", Optional)</c> emits <c>(?:www\.)?</c>.
    /// </summary>
    /// <param name="text">The text to match; every special character is escaped.</param>
    /// <param name="times">How many times the whole text repeats.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public T TheText(string text, NumberOfTimes times = default) =>
        Append(new TextNode(Guard.NotNullOrEmpty(text, nameof(text))), times);

    /// <summary>The character <paramref name="character"/>, taken literally. <c>TheCharacter('(', Optional)</c> emits <c>\(?</c>.</summary>
    /// <param name="character">The character to match; it is escaped if needed.</param>
    /// <param name="times">How many times the character repeats.</param>
    [Pure]
    public T TheCharacter(char character, NumberOfTimes times = default) =>
        Append(new TextNode(character.ToString()), times);

    // ---------- Kinds of character ----------

    /// <summary>Any character except a newline (<c>\n</c>). Emits <c>.</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyCharacter(NumberOfTimes times = default) => Append(new TokenNode("."), times);

    /// <summary>Any character at all, including newlines. Emits <c>[\s\S]</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyCharacterIncludingNewline(NumberOfTimes times = default) => Append(new TokenNode(@"[\s\S]"), times);

    /// <summary>
    /// Any digit. Emits <c>\d</c>. In .NET this includes digits from every script; for 0-9 only use
    /// <c>AnyCharacterInRange('0', '9')</c>.
    /// </summary>
    /// <param name="times">How many digits.</param>
    [Pure]
    public T AnyDigit(NumberOfTimes times = default) => Append(new TokenNode(@"\d"), times);

    /// <summary>Any character that is not a digit. Emits <c>\D</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyNonDigit(NumberOfTimes times = default) => Append(new TokenNode(@"\D"), times);

    /// <summary>Any letter, including accented and non-Latin letters. Emits <c>\p{L}</c>.</summary>
    /// <param name="times">How many letters.</param>
    [Pure]
    public T AnyLetter(NumberOfTimes times = default) => Append(new TokenNode(@"\p{L}"), times);

    /// <summary>Any character that is not a letter. Emits <c>\P{L}</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyNonLetter(NumberOfTimes times = default) => Append(new TokenNode(@"\P{L}"), times);

    /// <summary>
    /// Any uppercase letter. Emits <c>\p{Lu}</c>. With <see cref="MatchThesePatterns.IgnoringCase"/> it also matches
    /// lowercase letters.
    /// </summary>
    /// <param name="times">How many letters.</param>
    [Pure]
    public T AnyUppercaseLetter(NumberOfTimes times = default) => Append(new TokenNode(@"\p{Lu}"), times);

    /// <summary>
    /// Any lowercase letter. Emits <c>\p{Ll}</c>. With <see cref="MatchThesePatterns.IgnoringCase"/> it also matches
    /// uppercase letters.
    /// </summary>
    /// <param name="times">How many letters.</param>
    [Pure]
    public T AnyLowercaseLetter(NumberOfTimes times = default) => Append(new TokenNode(@"\p{Ll}"), times);

    /// <summary>Any letter, digit or underscore. Emits <c>\w</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyWordCharacter(NumberOfTimes times = default) => Append(new TokenNode(@"\w"), times);

    /// <summary>Any character that is not a letter, digit or underscore. Emits <c>\W</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyNonWordCharacter(NumberOfTimes times = default) => Append(new TokenNode(@"\W"), times);

    /// <summary>Any whitespace: space, tab, line breaks and so on. Emits <c>\s</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyWhitespace(NumberOfTimes times = default) => Append(new TokenNode(@"\s"), times);

    /// <summary>Any character that is not whitespace. Emits <c>\S</c>.</summary>
    /// <param name="times">How many characters.</param>
    [Pure]
    public T AnyNonWhitespace(NumberOfTimes times = default) => Append(new TokenNode(@"\S"), times);

    /// <summary>A line break in Windows (<c>\r\n</c>), Unix (<c>\n</c>) or old Mac (<c>\r</c>) style. Emits <c>(?:\r\n|\r|\n)</c>.</summary>
    /// <param name="times">How many line breaks.</param>
    [Pure]
    public T AnyLineBreak(NumberOfTimes times = default) => Append(new TokenNode(@"(?:\r\n|\r|\n)"), times);

    // ---------- Character sets ----------

    /// <summary>
    /// One of the characters in <paramref name="characters"/>, each taken literally.
    /// <c>OneOfTheseCharacters("-+.'")</c> emits <c>[-+.']</c>.
    /// </summary>
    /// <param name="characters">The characters to choose from.</param>
    /// <param name="times">How many characters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="characters"/> is empty, or contains something that looks like a range such as <c>"a-z"</c>.
    /// Use <see cref="AnyCharacterInRange"/> or <see cref="CharacterSet.CharactersInRange"/> for ranges, or put
    /// <c>'-'</c> first for a literal hyphen.
    /// </exception>
    [Pure]
    public T OneOfTheseCharacters(string characters, NumberOfTimes times = default) =>
        Append(new SetNode(CharacterSet.FromItems(CharacterSet.LiteralItems(characters, nameof(characters))), negated: false), times);

    /// <summary>One of the characters in <paramref name="characters"/>. Emits <c>[...]</c>.</summary>
    /// <param name="characters">A reusable set of characters.</param>
    /// <param name="times">How many characters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty.</exception>
    [Pure]
    public T OneOfTheseCharacters(CharacterSet characters, NumberOfTimes times = default) =>
        Append(new SetNode(NotEmpty(characters, nameof(characters)), negated: false), times);

    /// <summary>
    /// Any character except those in <paramref name="characters"/>, each taken literally.
    /// <c>AnyCharacterExcept("&lt;&gt;", OneOrMore)</c> emits <c>[^&lt;&gt;]+</c>.
    /// </summary>
    /// <param name="characters">The characters to exclude.</param>
    /// <param name="times">How many characters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty or contains a range look-alike such as <c>"a-z"</c>.</exception>
    [Pure]
    public T AnyCharacterExcept(string characters, NumberOfTimes times = default) =>
        Append(new SetNode(CharacterSet.FromItems(CharacterSet.LiteralItems(characters, nameof(characters))), negated: true), times);

    /// <summary>Any character except those in <paramref name="characters"/>. Emits <c>[^...]</c>.</summary>
    /// <param name="characters">A reusable set of characters to exclude.</param>
    /// <param name="times">How many characters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty.</exception>
    [Pure]
    public T AnyCharacterExcept(CharacterSet characters, NumberOfTimes times = default) =>
        Append(new SetNode(NotEmpty(characters, nameof(characters)), negated: true), times);

    /// <summary>
    /// Any character from <paramref name="from"/> to <paramref name="to"/>.
    /// <c>AnyCharacterInRange('a', 'z', Between(2, 6))</c> emits <c>[a-z]{2,6}</c>.
    /// </summary>
    /// <param name="from">The first character of the range.</param>
    /// <param name="to">The last character of the range.</param>
    /// <param name="times">How many characters.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="to"/> comes before <paramref name="from"/>.</exception>
    [Pure]
    public T AnyCharacterInRange(char from, char to, NumberOfTimes times = default) =>
        Append(new SetNode(new CharacterSet().CharactersInRange(from, to), negated: false), times);

    // ---------- Groups and captures ----------

    /// <summary>
    /// The reusable <paramref name="pattern"/>, treated as one unit so <paramref name="times"/> applies to all of it.
    /// Does not capture: <c>Group(wordsWithHyphenAndDots, ZeroOrMore)</c> emits <c>(?:[-.]\w+)*</c>.
    /// Use <see cref="Capture"/> or <see cref="CaptureAs"/> to get the text back.
    /// </summary>
    /// <param name="pattern">The sub-pattern; it can be reused any number of times.</param>
    /// <param name="times">How many times the whole sub-pattern repeats.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern"/> is empty, or it matches no characters (only positions) and a count is given.
    /// </exception>
    [Pure]
    public T Group(SubPattern pattern, NumberOfTimes times = default)
    {
        var content = SubPatternContent(pattern, nameof(pattern), times);
        return Append(content, times);
    }

    /// <summary>
    /// Captures the text matched by <paramref name="pattern"/> as a numbered group. Emits <c>(...)</c>.
    /// The count goes inside the capture, so it holds the whole run: <c>Capture(digit, OneOrMore)</c> emits <c>(\d+)</c>.
    /// When the count allows zero, a missing part reports <c>Success == false</c>.
    /// </summary>
    /// <param name="pattern">The sub-pattern to capture.</param>
    /// <param name="times">How many times the sub-pattern repeats inside the capture.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty, or matches no characters and a count is given.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> has a maximum of zero, so nothing could ever be captured.</exception>
    [Pure]
    public T Capture(SubPattern pattern, NumberOfTimes times = default) =>
        Append(CaptureNode(null, pattern, times));

    /// <summary>
    /// Captures the text matched by <paramref name="pattern"/> under the name <paramref name="name"/>.
    /// <c>CaptureAs("year", fourDigits)</c> emits <c>(?&lt;year&gt;\d{4})</c>; read it back with
    /// <c>match.Groups["year"].Value</c>.
    /// </summary>
    /// <param name="name">The capture name: a letter or underscore, then letters, digits or underscores.</param>
    /// <param name="pattern">The sub-pattern to capture.</param>
    /// <param name="times">How many times the sub-pattern repeats inside the capture.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid capture name, or <paramref name="pattern"/> is empty or matches no characters and a count is given.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> has a maximum of zero.</exception>
    [Pure]
    public T CaptureAs(string name, SubPattern pattern, NumberOfTimes times = default) =>
        Append(CaptureNode(Guard.CaptureName(name, nameof(name)), pattern, times));

    // ---------- Either / or ----------

    /// <summary>
    /// Either one text or another, each taken literally. <c>EitherOf("cat", "dog")</c> emits <c>(?:cat|dog)</c>.
    /// To repeat an either-or, wrap it: <c>Group(new SubPattern().EitherOf("ab", "cd"), OneOrMore)</c>.
    /// </summary>
    /// <param name="texts">The alternatives, tried in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="texts"/> is null.</exception>
    /// <exception cref="ArgumentException">No texts are given, or one of them is null or empty.</exception>
    [Pure]
    public T EitherOf(params string[] texts)
    {
        Guard.NotNullOrEmptyItems(texts, nameof(texts));
        var alternatives = new RegexNode[texts.Length];
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].Length == 0)
            {
                throw new ArgumentException($"Alternative {i} is empty, which would make the whole either-or optional.", nameof(texts));
            }

            alternatives[i] = new TextNode(texts[i]);
        }

        return Append(Alternation(alternatives));
    }

    /// <summary>
    /// Either one sub-pattern or another. <c>EitherOf(someDigits, someLetters)</c> emits <c>(?:\d+|\p{L}+)</c>.
    /// </summary>
    /// <param name="patterns">The alternatives, tried in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="patterns"/> is null.</exception>
    /// <exception cref="ArgumentException">No sub-patterns are given, or one of them is null or empty.</exception>
    [Pure]
    public T EitherOf(params SubPattern[] patterns)
    {
        Guard.NotNullOrEmptyItems(patterns, nameof(patterns));
        var alternatives = new RegexNode[patterns.Length];
        for (var i = 0; i < patterns.Length; i++)
        {
            if (patterns[i].IsEmpty)
            {
                throw new ArgumentException($"Alternative {i} is an empty sub-pattern, which would make the whole either-or optional.", nameof(patterns));
            }

            alternatives[i] = new SequenceNode(patterns[i].Nodes);
        }

        return Append(Alternation(alternatives));
    }

    // ---------- The same text again (backreferences) ----------

    /// <summary>
    /// The same text that the capture named <paramref name="captureName"/> matched. Emits <c>\k&lt;name&gt;</c>.
    /// It must come after <see cref="CaptureAs"/> in reading order.
    /// </summary>
    /// <param name="captureName">The name given to <see cref="CaptureAs"/>.</param>
    /// <param name="times">How many times the text repeats.</param>
    /// <exception cref="ArgumentNullException"><paramref name="captureName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="captureName"/> is not a valid capture name.</exception>
    [Pure]
    public T TheSameTextAsCapture(string captureName, NumberOfTimes times = default) =>
        Append(BackreferenceNode.ToName(Guard.CaptureName(captureName, nameof(captureName))), times);

    /// <summary>
    /// The same text that capture number <paramref name="captureNumber"/> matched. Emits <c>\k&lt;1&gt;</c>.
    /// .NET numbers unnamed captures first, left to right, then named ones, so prefer names.
    /// </summary>
    /// <param name="captureNumber">The capture number, starting at 1.</param>
    /// <param name="times">How many times the text repeats.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="captureNumber"/> is less than 1.</exception>
    [Pure]
    public T TheSameTextAsCapture(int captureNumber, NumberOfTimes times = default)
    {
        if (captureNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(captureNumber), captureNumber, "Capture numbers start at 1.");
        }

        return Append(BackreferenceNode.ToNumber(captureNumber), times);
    }

    // ---------- Conditions on the surrounding text (match no characters, so they take no count) ----------

    /// <summary>Only if the text <paramref name="text"/> comes next. Emits <c>(?=...)</c>; nothing is consumed.</summary>
    /// <param name="text">The text that must follow, taken literally.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public T IfFollowedBy(string text) => Append(Lookaround(isBehind: false, isNegative: false, text));

    /// <summary>Only if <paramref name="pattern"/> matches next. Emits <c>(?=...)</c>; nothing is consumed.</summary>
    /// <param name="pattern">The sub-pattern that must follow.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public T IfFollowedBy(SubPattern pattern) => Append(Lookaround(isBehind: false, isNegative: false, pattern));

    /// <summary>Only if the text <paramref name="text"/> does not come next. Emits <c>(?!...)</c>.</summary>
    /// <param name="text">The text that must not follow, taken literally.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public T IfNotFollowedBy(string text) => Append(Lookaround(isBehind: false, isNegative: true, text));

    /// <summary>Only if <paramref name="pattern"/> does not match next. Emits <c>(?!...)</c>.</summary>
    /// <param name="pattern">The sub-pattern that must not follow.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public T IfNotFollowedBy(SubPattern pattern) => Append(Lookaround(isBehind: false, isNegative: true, pattern));

    /// <summary>
    /// Only if the text <paramref name="text"/> comes just before. Emits <c>(?&lt;=...)</c>. Write it before the
    /// piece it guards: <c>IfPrecededBy("$").AnyDigit(OneOrMore)</c> emits <c>(?&lt;=\$)\d+</c>.
    /// </summary>
    /// <param name="text">The text that must come before, taken literally.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public T IfPrecededBy(string text) => Append(Lookaround(isBehind: true, isNegative: false, text));

    /// <summary>
    /// Only if <paramref name="pattern"/> matches just before. Emits <c>(?&lt;=...)</c>.
    /// Lookbehinds run right to left, so a reference inside one only sees captures to its right.
    /// </summary>
    /// <param name="pattern">The sub-pattern that must come before.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public T IfPrecededBy(SubPattern pattern) => Append(Lookaround(isBehind: true, isNegative: false, pattern));

    /// <summary>Only if the text <paramref name="text"/> does not come just before. Emits <c>(?&lt;!...)</c>.</summary>
    /// <param name="text">The text that must not come before, taken literally.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public T IfNotPrecededBy(string text) => Append(Lookaround(isBehind: true, isNegative: true, text));

    /// <summary>Only if <paramref name="pattern"/> does not match just before. Emits <c>(?&lt;!...)</c>.</summary>
    /// <param name="pattern">The sub-pattern that must not come before.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public T IfNotPrecededBy(SubPattern pattern) => Append(Lookaround(isBehind: true, isNegative: true, pattern));

    // ---------- Raw regex escape hatch ----------

    /// <summary>
    /// Raw .NET regex syntax for anything the English methods don't cover. It is not escaped, and it is always wrapped
    /// in <c>(?:...)</c> so a <c>|</c> or an inline option can't leak into the rest: <c>RawRegex(@"\p{IsGreek}", OneOrMore)</c>
    /// emits <c>(?:\p{IsGreek})+</c>. Syntax errors are reported by <see cref="MatchThesePatterns.ToRegex"/>.
    /// </summary>
    /// <param name="regexPattern">The raw pattern.</param>
    /// <param name="times">How many times the whole raw pattern repeats.</param>
    /// <exception cref="ArgumentNullException"><paramref name="regexPattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="regexPattern"/> is empty.</exception>
    [Pure]
    public T RawRegex([StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern, NumberOfTimes times = default) =>
        Append(new RawNode(Guard.NotNullOrEmpty(regexPattern, nameof(regexPattern))), times);

    // ---------- Output ----------

    /// <summary>Returns the pattern text built so far. Never throws.</summary>
    public override string ToString() => _pattern ??= PatternWriter.Render(Nodes);

    // ---------- Plumbing ----------

    private protected abstract T With(NodeList nodes);

    private T Append(RegexNode node) => With(Nodes.Append(node));

    private T Append(RegexNode node, NumberOfTimes times) =>
        Append(times.IsOnce ? node : new QuantifiedNode(node, times));

    private static CharacterSet NotEmpty(CharacterSet characters, string paramName)
    {
        Guard.NotNull(characters, paramName);
        if (characters.IsEmpty)
        {
            throw new ArgumentException("The character set is empty.", paramName);
        }

        return characters;
    }

    private static SequenceNode SubPatternContent(SubPattern pattern, string paramName, NumberOfTimes times)
    {
        Guard.NotNull(pattern, paramName);
        if (pattern.IsEmpty)
        {
            throw new ArgumentException("The sub-pattern is empty.", paramName);
        }

        var content = new SequenceNode(pattern.Nodes);
        if (!times.IsOnce && content.IsZeroWidth)
        {
            throw new ArgumentException(
                "The sub-pattern only matches positions (anchors or If... conditions), so repeating it changes nothing.",
                nameof(times));
        }

        return content;
    }

    private static CaptureNode CaptureNode(string? name, SubPattern pattern, NumberOfTimes times)
    {
        var content = SubPatternContent(pattern, nameof(pattern), times);
        if (times.Maximum == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(times), times, "A capture that repeats at most zero times can never capture anything.");
        }

        return new CaptureNode(name, content, times);
    }

    private static RegexNode Alternation(RegexNode[] alternatives) =>
        alternatives.Length == 1 ? alternatives[0] : new AlternationNode(alternatives);

    private static LookaroundNode Lookaround(bool isBehind, bool isNegative, string text) =>
        new(isBehind, isNegative, new TextNode(Guard.NotNullOrEmpty(text, nameof(text))));

    private static LookaroundNode Lookaround(bool isBehind, bool isNegative, SubPattern pattern)
    {
        Guard.NotNull(pattern, nameof(pattern));
        if (pattern.IsEmpty)
        {
            throw new ArgumentException("The sub-pattern is empty.", nameof(pattern));
        }

        return new LookaroundNode(isBehind, isNegative, new SequenceNode(pattern.Nodes));
    }
}
