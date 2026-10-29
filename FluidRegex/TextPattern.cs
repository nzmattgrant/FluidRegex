using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// Describes the text to find, piece by piece in reading order. Check text with <see cref="IsFoundIn"/> or
/// <see cref="MatchesWholeText"/>, or get the text back with <see cref="FindFirst"/> and <see cref="FindAll"/>.
/// To repeat a piece, pass a <see cref="Times"/> as its last argument: <c>Digit(Times.Exactly(4))</c>. The count
/// covers the whole piece: <c>Text("ab", Times.OneOrMore)</c> emits <c>(?:ab)+</c>.
/// A pattern can also be used as one piece of another pattern with <see cref="Pattern"/>.
/// Every call returns a new pattern; the original never changes, so a pattern can be stored in a
/// <c>static readonly</c> field and shared across threads.
/// </summary>
/// <remarks>Every string is matched exactly as written, except in <see cref="RawRegex"/>.</remarks>
/// <example>
/// <code>
/// var isoDate = new TextPattern()
///     .Digit(Times.Exactly(4)).Text("-")
///     .Digit(Times.Exactly(2)).Text("-")
///     .Digit(Times.Exactly(2));
/// // [0-9]{4}-[0-9]{2}-[0-9]{2}
///
/// isoDate.MatchesWholeText("2026-10-12");   // true
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TextPattern
{
    // The longest timeout the Regex constructor accepts.
    private static readonly TimeSpan LongestTimeLimit = TimeSpan.FromMilliseconds(int.MaxValue - 1);

    private static readonly CharacterSet Digits = new CharacterSet().Digits();
    private static readonly CharacterSet LettersAndDigits = new CharacterSet().Letters().Digits();

    private readonly bool _ignoringCase;
    private readonly TimeSpan _timeLimit;
    private string? _pattern;
    private Regex? _regex;
    private Regex? _wholeTextRegex;

    /// <summary>Starts an empty pattern.</summary>
    public TextPattern()
        : this(NodeList.Empty, ignoringCase: false, Regex.InfiniteMatchTimeout)
    {
    }

    private TextPattern(NodeList nodes, bool ignoringCase, TimeSpan timeLimit)
    {
        Nodes = nodes;
        _ignoringCase = ignoringCase;
        _timeLimit = timeLimit;
    }

    internal NodeList Nodes { get; }

    internal bool IsEmpty => Nodes.Count == 0;

    // ---------- Text ----------

    /// <summary>The exact text <paramref name="text"/>. <c>Text("www.", Times.Optional)</c> emits <c>(?:www\.)?</c>.</summary>
    /// <param name="text">The text to find. Every character is matched as written.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern Text(string text, Times times = default) =>
        Add(new TextNode(Guard.NotNullOrEmpty(text, nameof(text))), times);

    // ---------- Kinds of character ----------

    /// <summary>Any character at all, including line breaks. Emits <c>[\s\S]</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern AnyCharacter(Times times = default) => Add(new TokenNode(@"[\s\S]"), times);

    /// <summary>Any character except a line break (<c>\r</c> or <c>\n</c>). Emits <c>[^\r\n]</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern AnyCharacterExceptLineBreak(Times times = default) => Add(new TokenNode(@"[^\r\n]"), times);

    /// <summary>A digit from 0 to 9. Emits <c>[0-9]</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern Digit(Times times = default) => Add(new SetNode(Digits, negated: false), times);

    /// <summary>Any character except a digit from 0 to 9. Emits <c>[^0-9]</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern AnyCharacterExceptDigit(Times times = default) => Add(new SetNode(Digits, negated: true), times);

    /// <summary>A letter from any language, such as <c>a</c>, <c>É</c> or <c>ж</c>. Emits <c>\p{L}</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern Letter(Times times = default) => Add(new TokenNode(@"\p{L}"), times);

    /// <summary>Any character except a letter. Emits <c>\P{L}</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern AnyCharacterExceptLetter(Times times = default) => Add(new TokenNode(@"\P{L}"), times);

    /// <summary>
    /// An uppercase letter. Emits <c>\p{Lu}</c>. With <see cref="IgnoringCase"/> it also matches lowercase letters.
    /// </summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern UppercaseLetter(Times times = default) => Add(new TokenNode(@"\p{Lu}"), times);

    /// <summary>
    /// A lowercase letter. Emits <c>\p{Ll}</c>. With <see cref="IgnoringCase"/> it also matches uppercase letters.
    /// </summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern LowercaseLetter(Times times = default) => Add(new TokenNode(@"\p{Ll}"), times);

    /// <summary>A letter from any language, or a digit from 0 to 9. Emits <c>[\p{L}0-9]</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern LetterOrDigit(Times times = default) => Add(new SetNode(LettersAndDigits, negated: false), times);

    /// <summary>A space, tab, line break or other blank character. Emits <c>\s</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern Whitespace(Times times = default) => Add(new TokenNode(@"\s"), times);

    /// <summary>Any character except whitespace. Emits <c>\S</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern AnyCharacterExceptWhitespace(Times times = default) => Add(new TokenNode(@"\S"), times);

    /// <summary>One line break in Windows (<c>\r\n</c>), Unix (<c>\n</c>) or old Mac (<c>\r</c>) style. Emits <c>(?:\r\n|\r|\n)</c>.</summary>
    /// <param name="times">How many times; once when left out.</param>
    [Pure]
    public TextPattern LineBreak(Times times = default) => Add(new TokenNode(@"(?:\r\n|\r|\n)"), times);

    // ---------- Chosen characters ----------

    /// <summary>
    /// One of the characters in <paramref name="characters"/>, each matched as written.
    /// <c>OneOfTheseCharacters("-+.'")</c> emits <c>[-+.']</c>.
    /// </summary>
    /// <param name="characters">The characters to choose from.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="characters"/> is empty, or contains something that looks like a range such as <c>"a-z"</c>.
    /// Use <see cref="AnyCharacterInRange"/> or <see cref="CharacterSet.CharactersInRange"/> for ranges, or put
    /// <c>'-'</c> first for a hyphen.
    /// </exception>
    [Pure]
    public TextPattern OneOfTheseCharacters(string characters, Times times = default) =>
        Add(new SetNode(CharacterSet.FromItems(CharacterSet.LiteralItems(characters, nameof(characters))), negated: false), times);

    /// <summary>One of the characters in <paramref name="characters"/>. Emits <c>[...]</c>.</summary>
    /// <param name="characters">A reusable set of characters.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty.</exception>
    [Pure]
    public TextPattern OneOfTheseCharacters(CharacterSet characters, Times times = default) =>
        Add(new SetNode(NotEmpty(characters, nameof(characters)), negated: false), times);

    /// <summary>
    /// Any character except those in <paramref name="characters"/>, each matched as written.
    /// <c>AnyCharacterExcept("&lt;&gt;", Times.OneOrMore)</c> emits <c>[^&lt;&gt;]+</c>.
    /// </summary>
    /// <param name="characters">The characters to leave out.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty or contains a range look-alike such as <c>"a-z"</c>.</exception>
    [Pure]
    public TextPattern AnyCharacterExcept(string characters, Times times = default) =>
        Add(new SetNode(CharacterSet.FromItems(CharacterSet.LiteralItems(characters, nameof(characters))), negated: true), times);

    /// <summary>Any character except those in <paramref name="characters"/>. Emits <c>[^...]</c>.</summary>
    /// <param name="characters">A reusable set of characters to leave out.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty.</exception>
    [Pure]
    public TextPattern AnyCharacterExcept(CharacterSet characters, Times times = default) =>
        Add(new SetNode(NotEmpty(characters, nameof(characters)), negated: true), times);

    /// <summary>
    /// Any character from <paramref name="from"/> to <paramref name="to"/>.
    /// <c>AnyCharacterInRange('a', 'z', Times.Between(2, 6))</c> emits <c>[a-z]{2,6}</c>.
    /// </summary>
    /// <param name="from">The first character of the range.</param>
    /// <param name="to">The last character of the range.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="to"/> comes before <paramref name="from"/>.</exception>
    [Pure]
    public TextPattern AnyCharacterInRange(char from, char to, Times times = default) =>
        Add(new SetNode(new CharacterSet().CharactersInRange(from, to), negated: false), times);

    // ---------- Choices and reused patterns ----------

    /// <summary>
    /// One of several texts, each matched as written. <c>EitherOf("cat", "dog")</c> emits <c>(?:cat|dog)</c>.
    /// To repeat the choice, store it in a pattern and use <see cref="Pattern"/> with a count.
    /// </summary>
    /// <param name="first">The first choice.</param>
    /// <param name="second">The second choice.</param>
    /// <param name="more">More choices. All choices are tried in order.</param>
    /// <exception cref="ArgumentNullException">A choice, or <paramref name="more"/>, is null.</exception>
    /// <exception cref="ArgumentException">A choice is empty.</exception>
    [Pure]
    public TextPattern EitherOf(string first, string second, params string[] more) =>
        With(new AlternationNode(Choices(first, second, more, TextChoice)));

    /// <summary>
    /// One of several patterns. <c>EitherOf(endOfText, aSpace)</c> emits <c>(?:\z|\ )</c>.
    /// To repeat the choice, store it in a pattern and use <see cref="Pattern"/> with a count.
    /// </summary>
    /// <param name="first">The first choice.</param>
    /// <param name="second">The second choice.</param>
    /// <param name="more">More choices. All choices are tried in order.</param>
    /// <exception cref="ArgumentNullException">A choice, or <paramref name="more"/>, is null.</exception>
    /// <exception cref="ArgumentException">A choice is an empty pattern.</exception>
    [Pure]
    public TextPattern EitherOf(TextPattern first, TextPattern second, params TextPattern[] more) =>
        With(new AlternationNode(Choices(first, second, more, PatternChoice)));

    /// <summary>
    /// A pattern built earlier, used here as one piece. Patterns never change, so one can be used any number of times.
    /// <c>Pattern(dotThenWord, Times.ZeroOrMore)</c> emits <c>(?:\.\p{L}+)*</c>.
    /// </summary>
    /// <param name="pattern">The pattern to use.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern"/> is empty, or it only matches positions and a count was given.
    /// </exception>
    [Pure]
    public TextPattern Pattern(TextPattern pattern, Times times = default) =>
        Add(Content(pattern, nameof(pattern), times), times);

    // ---------- Saving text ----------

    /// <summary>
    /// Saves the text that <paramref name="pattern"/> matches under <paramref name="name"/>. Read it back with
    /// <see cref="FoundText.Saved"/>, or match it again with <see cref="SameTextAsSaved"/>.
    /// <c>SaveTextAs("year", fourDigits)</c> emits <c>(?&lt;year&gt;[0-9]{4})</c>. With a count the saved text holds
    /// the whole run: <c>SaveTextAs("number", digit, Times.OneOrMore)</c> emits <c>(?&lt;number&gt;[0-9]+)</c>.
    /// </summary>
    /// <param name="name">The name to save under: a letter or underscore, then letters, digits or underscores.</param>
    /// <param name="pattern">The pattern whose text is saved.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is not a valid name, or <paramref name="pattern"/> is empty or only matches positions
    /// and a count was given.
    /// </exception>
    [Pure]
    public TextPattern SaveTextAs(string name, TextPattern pattern, Times times = default) =>
        With(new CaptureNode(Guard.SavedName(name, nameof(name)), Content(pattern, nameof(pattern), times), times));

    /// <summary>
    /// The same text that was saved earlier with <see cref="SaveTextAs"/> under <paramref name="name"/>. Emits <c>\k&lt;name&gt;</c>.
    /// </summary>
    /// <param name="name">The name given to <see cref="SaveTextAs"/>.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid name.</exception>
    [Pure]
    public TextPattern SameTextAsSaved(string name, Times times = default) =>
        Add(new BackreferenceNode(Guard.SavedName(name, nameof(name))), times);

    // ---------- Raw regex escape hatch ----------

    /// <summary>
    /// Raw .NET regex syntax, for anything the other methods don't cover. It is not escaped, and it is always wrapped
    /// in <c>(?:...)</c> so a <c>|</c> or an inline option can't leak into the rest: <c>RawRegex(@"\p{IsGreek}", Times.OneOrMore)</c>
    /// emits <c>(?:\p{IsGreek})+</c>. Syntax errors are reported when the pattern is first used.
    /// </summary>
    /// <param name="regexPattern">The raw pattern.</param>
    /// <param name="times">How many times; once when left out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="regexPattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="regexPattern"/> is empty.</exception>
    [Pure]
    public TextPattern RawRegex([StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern, Times times = default) =>
        Add(new RawNode(Guard.NotNullOrEmpty(regexPattern, nameof(regexPattern))), times);

    // ---------- Positions (match no characters, so they take no count) ----------

    /// <summary>At the very start of the text. Emits <c>\A</c>.</summary>
    [Pure]
    public TextPattern StartOfText() => With(new AnchorNode(@"\A"));

    /// <summary>At the very end of the text. Emits <c>\z</c>. Unlike <c>$</c>, a trailing newline is not accepted.</summary>
    [Pure]
    public TextPattern EndOfText() => With(new AnchorNode(@"\z"));

    /// <summary>
    /// At the start of a line: the start of the text, or just after <c>\r\n</c>, <c>\n</c> or <c>\r</c>.
    /// Emits <c>(?&lt;![^\r\n])(?!(?&lt;=\r)\n)</c>.
    /// </summary>
    [Pure]
    public TextPattern StartOfLine() => With(new AnchorNode(@"(?<![^\r\n])(?!(?<=\r)\n)"));

    /// <summary>
    /// At the end of a line: just before <c>\r\n</c>, <c>\n</c> or <c>\r</c>, or at the end of the text; never between
    /// <c>\r</c> and <c>\n</c>. Emits <c>(?![^\r\n])(?!(?&lt;=\r)\n)</c>.
    /// </summary>
    [Pure]
    public TextPattern EndOfLine() => With(new AnchorNode(@"(?![^\r\n])(?!(?<=\r)\n)"));

    /// <summary>
    /// At the start of a word: a letter, digit or underscore comes next, and none comes just before. Emits <c>\b(?=\w)</c>.
    /// </summary>
    [Pure]
    public TextPattern StartOfWord() => With(new AnchorNode(@"\b(?=\w)"));

    /// <summary>
    /// At the end of a word: a letter, digit or underscore comes just before, and none comes next. Emits <c>\b(?&lt;=\w)</c>.
    /// </summary>
    [Pure]
    public TextPattern EndOfWord() => With(new AnchorNode(@"\b(?<=\w)"));

    // ---------- Conditions on the surrounding text (match no characters, so they take no count) ----------

    /// <summary>
    /// Only if the text after this point starts with <paramref name="text"/>. Emits <c>(?=...)</c>. The text is
    /// checked, not added to what is found.
    /// </summary>
    /// <param name="text">The text that must come next, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern TextAfterHereStartsWith(string text) => With(Lookaround(isBehind: false, isNegative: false, text));

    /// <summary>
    /// Only if the text after this point starts with something that <paramref name="pattern"/> matches. Emits
    /// <c>(?=...)</c>. Nothing is added to what is found.
    /// </summary>
    /// <param name="pattern">The pattern that must match next.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern TextAfterHereStartsWith(TextPattern pattern) => With(Lookaround(isBehind: false, isNegative: false, pattern));

    /// <summary>Only if the text after this point does not start with <paramref name="text"/>. Emits <c>(?!...)</c>.</summary>
    /// <param name="text">The text that must not come next, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern TextAfterHereDoesNotStartWith(string text) => With(Lookaround(isBehind: false, isNegative: true, text));

    /// <summary>
    /// Only if the text after this point does not start with something that <paramref name="pattern"/> matches.
    /// Emits <c>(?!...)</c>.
    /// </summary>
    /// <param name="pattern">The pattern that must not match next.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern TextAfterHereDoesNotStartWith(TextPattern pattern) => With(Lookaround(isBehind: false, isNegative: true, pattern));

    /// <summary>
    /// Only if the text before this point ends with <paramref name="text"/>. Emits <c>(?&lt;=...)</c>. Write it before
    /// the piece it checks: <c>TextBeforeHereEndsWith("$").Digit(Times.OneOrMore)</c> emits <c>(?&lt;=\$)[0-9]+</c>.
    /// </summary>
    /// <param name="text">The text that must come just before, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern TextBeforeHereEndsWith(string text) => With(Lookaround(isBehind: true, isNegative: false, text));

    /// <summary>
    /// Only if the text before this point ends with something that <paramref name="pattern"/> matches. Emits
    /// <c>(?&lt;=...)</c>. It is checked right to left, so a <see cref="SameTextAsSaved"/> inside it only sees text
    /// saved to its right.
    /// </summary>
    /// <param name="pattern">The pattern that must match just before.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern TextBeforeHereEndsWith(TextPattern pattern) => With(Lookaround(isBehind: true, isNegative: false, pattern));

    /// <summary>Only if the text before this point does not end with <paramref name="text"/>. Emits <c>(?&lt;!...)</c>.</summary>
    /// <param name="text">The text that must not come just before, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern TextBeforeHereDoesNotEndWith(string text) => With(Lookaround(isBehind: true, isNegative: true, text));

    /// <summary>
    /// Only if the text before this point does not end with something that <paramref name="pattern"/> matches.
    /// Emits <c>(?&lt;!...)</c>.
    /// </summary>
    /// <param name="pattern">The pattern that must not match just before.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern TextBeforeHereDoesNotEndWith(TextPattern pattern) => With(Lookaround(isBehind: true, isNegative: true, pattern));

    // ---------- Settings ----------

    /// <summary>
    /// Treats uppercase and lowercase as the same, so <c>Text("cat")</c> also finds <c>CAT</c>. When this pattern is used
    /// inside another one, only this pattern's part ignores case.
    /// </summary>
    [Pure]
    public TextPattern IgnoringCase() => new(Nodes, ignoringCase: true, _timeLimit);

    /// <summary>
    /// Gives up on a search that runs longer than <paramref name="timeLimit"/>, throwing
    /// <see cref="RegexMatchTimeoutException"/>. Use it when the text comes from users or other untrusted places.
    /// </summary>
    /// <param name="timeLimit">A positive time, or <see cref="Regex.InfiniteMatchTimeout"/> for no limit.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeLimit"/> is zero, negative, or longer than about 24 days.</exception>
    [Pure]
    public TextPattern WithTimeLimit(TimeSpan timeLimit)
    {
        if (timeLimit != Regex.InfiniteMatchTimeout && (timeLimit <= TimeSpan.Zero || timeLimit > LongestTimeLimit))
        {
            throw new ArgumentOutOfRangeException(nameof(timeLimit), timeLimit, "The time limit must be positive and at most about 24 days, or Regex.InfiniteMatchTimeout.");
        }

        return new TextPattern(Nodes, _ignoringCase, timeLimit);
    }

    // ---------- Results ----------

    /// <summary>
    /// Whether the pattern is found anywhere in <paramref name="text"/>. To check that the whole text fits the
    /// pattern, for example when checking an email address, use <see cref="MatchesWholeText"/>.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException">The pattern is not valid (see <see cref="ToRegex"/>).</exception>
    /// <exception cref="RegexMatchTimeoutException">The search ran longer than the time limit.</exception>
    public bool IsFoundIn(string text) => ToRegex().IsMatch(Guard.NotNull(text, nameof(text)));

    /// <summary>
    /// Whether all of <paramref name="text"/>, from its first character to its last, fits the pattern.
    /// <c>"junk a@b.com"</c> does not match an email pattern, even though <see cref="IsFoundIn"/> finds one in it.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException">The pattern is not valid (see <see cref="ToRegex"/>).</exception>
    /// <exception cref="RegexMatchTimeoutException">The check ran longer than the time limit.</exception>
    public bool MatchesWholeText(string text)
    {
        Guard.NotNull(text, nameof(text));
        var regex = Volatile.Read(ref _wholeTextRegex) ?? LazyInitializer.EnsureInitialized(ref _wholeTextRegex, BuildWholeTextRegex)!;
        return regex.IsMatch(text);
    }

    /// <summary>The first place the pattern is found in <paramref name="text"/>, or <see langword="null"/> when it is not found.</summary>
    /// <param name="text">The text to search.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException">The pattern is not valid (see <see cref="ToRegex"/>).</exception>
    /// <exception cref="RegexMatchTimeoutException">The search ran longer than the time limit.</exception>
    public FoundText? FindFirst(string text)
    {
        var regex = ToRegex();
        var match = regex.Match(Guard.NotNull(text, nameof(text)));
        return match.Success ? new FoundText(match, regex) : null;
    }

    /// <summary>Every place the pattern is found in <paramref name="text"/>, in order. Empty when it is not found.</summary>
    /// <param name="text">The text to search.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException">The pattern is not valid (see <see cref="ToRegex"/>).</exception>
    /// <exception cref="RegexMatchTimeoutException">The search ran longer than the time limit.</exception>
    public IReadOnlyList<FoundText> FindAll(string text)
    {
        var regex = ToRegex();
        var matches = regex.Matches(Guard.NotNull(text, nameof(text)));
        var found = new FoundText[matches.Count];
        for (var i = 0; i < found.Length; i++)
        {
            found[i] = new FoundText(matches[i], regex);
        }

        return found;
    }

    /// <summary>
    /// Returns <paramref name="text"/> with every place the pattern is found replaced by <paramref name="replacement"/>.
    /// The replacement is used exactly as written.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="replacement">The text to put in each place; it may be empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="replacement"/> is null.</exception>
    /// <exception cref="ArgumentException">The pattern is not valid (see <see cref="ToRegex"/>).</exception>
    /// <exception cref="RegexMatchTimeoutException">The search ran longer than the time limit.</exception>
    public string ReplaceAll(string text, string replacement)
    {
        Guard.NotNull(text, nameof(text));
        Guard.NotNull(replacement, nameof(replacement));

        // "$" starts a substitution such as "$1" in a .NET replacement; "$$" is a plain "$".
        return ToRegex().Replace(text, replacement.Replace("$", "$$"));
    }

    /// <summary>
    /// Builds the .NET <see cref="Regex"/>, for anything the other result methods don't cover. The result is cached.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The pattern is not valid, for example <see cref="SameTextAsSaved"/> comes before its <see cref="SaveTextAs"/>,
    /// or <see cref="RawRegex"/> has a syntax error. The message quotes the pattern.
    /// </exception>
    public Regex ToRegex() => Volatile.Read(ref _regex) ?? LazyInitializer.EnsureInitialized(ref _regex, BuildRegex)!;

    /// <summary>Returns the regex pattern text built so far. Never throws.</summary>
    public override string ToString() => _pattern ??= PatternWriter.Render(Nodes);

    // ---------- Plumbing ----------

    /// <summary>This pattern's content as one piece of another pattern. Ignoring case travels with it as (?i:...).</summary>
    internal RegexNode AsPiece()
    {
        var content = new SequenceNode(Nodes);
        return _ignoringCase ? new IgnoreCaseNode(content) : content;
    }

    private TextPattern With(RegexNode node) => new(Nodes.Append(node), _ignoringCase, _timeLimit);

    private TextPattern Add(RegexNode piece, Times times) => With(times.IsOnce ? piece : new QuantifiedNode(piece, times));

    private static RegexNode NonEmptyPiece(TextPattern pattern, string paramName)
    {
        Guard.NotNull(pattern, paramName);
        if (pattern.IsEmpty)
        {
            throw new ArgumentException("The pattern is empty.", paramName);
        }

        return pattern.AsPiece();
    }

    private static RegexNode Content(TextPattern pattern, string paramName, Times times)
    {
        var piece = NonEmptyPiece(pattern, paramName);
        if (!times.IsOnce && piece.IsZeroWidth)
        {
            throw new ArgumentException(
                "The pattern only matches positions (such as StartOfWord() or TextAfterHereStartsWith(...)), not characters, so a count changes nothing.",
                paramName);
        }

        return piece;
    }

    private static CharacterSet NotEmpty(CharacterSet characters, string paramName)
    {
        Guard.NotNull(characters, paramName);
        if (characters.IsEmpty)
        {
            throw new ArgumentException("The character set is empty.", paramName);
        }

        return characters;
    }

    private static RegexNode[] Choices<T>(T first, T second, T[] more, Func<T, string, RegexNode> toNode)
        where T : class
    {
        Guard.NotNull(more, nameof(more));
        var choices = new RegexNode[more.Length + 2];
        choices[0] = toNode(Guard.NotNull(first, nameof(first)), nameof(first));
        choices[1] = toNode(Guard.NotNull(second, nameof(second)), nameof(second));
        for (var i = 0; i < more.Length; i++)
        {
            choices[i + 2] = toNode(Guard.NotNull(more[i], nameof(more)), nameof(more));
        }

        return choices;
    }

    private static RegexNode TextChoice(string text, string paramName) =>
        text.Length == 0
            ? throw new ArgumentException("A choice is empty, which would make the whole EitherOf optional.", paramName)
            : new TextNode(text);

    private static RegexNode PatternChoice(TextPattern pattern, string paramName) =>
        pattern.IsEmpty
            ? throw new ArgumentException("A choice is an empty pattern, which would make the whole EitherOf optional.", paramName)
            : pattern.AsPiece();

    private static LookaroundNode Lookaround(bool isBehind, bool isNegative, string text) =>
        new(isBehind, isNegative, new TextNode(Guard.NotNullOrEmpty(text, nameof(text))));

    private static LookaroundNode Lookaround(bool isBehind, bool isNegative, TextPattern pattern) =>
        new(isBehind, isNegative, NonEmptyPiece(pattern, nameof(pattern)));

    private Regex BuildRegex()
    {
        var pattern = ToString();
        try
        {
            PatternChecks.Check(Nodes);
            return new Regex(pattern, _ignoringCase ? RegexOptions.IgnoreCase : RegexOptions.None, _timeLimit);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException($"The pattern \"{pattern}\" is not valid: {exception.Message}", exception);
        }
    }

    private Regex BuildWholeTextRegex()
    {
        // Built from ToRegex() so that a problem is reported with the pattern as the user wrote it. No (?:...) is
        // needed around it: either-or is always grouped, so the pattern never has a top-level '|'.
        var regex = ToRegex();
        return new Regex(@"\A" + regex + @"\z", regex.Options, regex.MatchTimeout);
    }
}
