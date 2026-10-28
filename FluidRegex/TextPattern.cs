using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// Describes the text to find, piece by piece in reading order. Check text with <see cref="IsFoundIn"/> or
/// <see cref="MatchesWholeText"/>, or get the text back with <see cref="FindFirst"/> and <see cref="FindAll"/>.
/// A pattern can also be used as one piece of another pattern with <see cref="PatternPieces.Pattern"/>.
/// Every call returns a new pattern; the original never changes, so a pattern can be stored in a
/// <c>static readonly</c> field and shared across threads.
/// </summary>
/// <example>
/// <code>
/// var isoDate = new TextPattern()
///     .Exactly(4).Digit().Text("-")
///     .Exactly(2).Digit().Text("-")
///     .Exactly(2).Digit();
/// // [0-9]{4}-[0-9]{2}-[0-9]{2}
///
/// isoDate.MatchesWholeText("2026-10-12");   // true
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TextPattern : PatternPieces
{
    // The longest timeout the Regex constructor accepts.
    private static readonly TimeSpan LongestTimeLimit = TimeSpan.FromMilliseconds(int.MaxValue - 1);

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
        : base(NumberOfTimes.Once)
    {
        Nodes = nodes;
        _ignoringCase = ignoringCase;
        _timeLimit = timeLimit;
    }

    internal NodeList Nodes { get; }

    internal bool IsEmpty => Nodes.Count == 0;

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
    /// Only if the text <paramref name="text"/> comes next. Emits <c>(?=...)</c>. The text is checked, not added to
    /// what is found.
    /// </summary>
    /// <param name="text">The text that must come next, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern IfFollowedBy(string text) => With(Lookaround(isBehind: false, isNegative: false, text));

    /// <summary>Only if <paramref name="pattern"/> matches next. Emits <c>(?=...)</c>. Nothing is added to what is found.</summary>
    /// <param name="pattern">The pattern that must come next.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern IfFollowedBy(TextPattern pattern) => With(Lookaround(isBehind: false, isNegative: false, pattern));

    /// <summary>Only if the text <paramref name="text"/> does not come next. Emits <c>(?!...)</c>.</summary>
    /// <param name="text">The text that must not come next, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern IfNotFollowedBy(string text) => With(Lookaround(isBehind: false, isNegative: true, text));

    /// <summary>Only if <paramref name="pattern"/> does not match next. Emits <c>(?!...)</c>.</summary>
    /// <param name="pattern">The pattern that must not come next.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern IfNotFollowedBy(TextPattern pattern) => With(Lookaround(isBehind: false, isNegative: true, pattern));

    /// <summary>
    /// Only if the text <paramref name="text"/> comes just before. Emits <c>(?&lt;=...)</c>. Write it before the
    /// piece it checks: <c>IfPrecededBy("$").OneOrMore().Digit()</c> emits <c>(?&lt;=\$)[0-9]+</c>.
    /// </summary>
    /// <param name="text">The text that must come just before, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern IfPrecededBy(string text) => With(Lookaround(isBehind: true, isNegative: false, text));

    /// <summary>
    /// Only if <paramref name="pattern"/> matches just before. Emits <c>(?&lt;=...)</c>.
    /// It is checked right to left, so a <see cref="PatternPieces.SameTextAsSaved"/> inside it only sees text saved to its right.
    /// </summary>
    /// <param name="pattern">The pattern that must come just before.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern IfPrecededBy(TextPattern pattern) => With(Lookaround(isBehind: true, isNegative: false, pattern));

    /// <summary>Only if the text <paramref name="text"/> does not come just before. Emits <c>(?&lt;!...)</c>.</summary>
    /// <param name="text">The text that must not come just before, matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern IfNotPrecededBy(string text) => With(Lookaround(isBehind: true, isNegative: true, text));

    /// <summary>Only if <paramref name="pattern"/> does not match just before. Emits <c>(?&lt;!...)</c>.</summary>
    /// <param name="pattern">The pattern that must not come just before.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    [Pure]
    public TextPattern IfNotPrecededBy(TextPattern pattern) => With(Lookaround(isBehind: true, isNegative: true, pattern));

    // ---------- Counts: say how many, then pick the piece ----------

    /// <summary>The next piece may be missing. <c>Optional().Text("www.")</c> emits <c>(?:www\.)?</c>.</summary>
    [Pure]
    public NextPieceCount Optional() => new(this, NumberOfTimes.Optional);

    /// <summary>The next piece any number of times, or not at all. <c>ZeroOrMore().Digit()</c> emits <c>[0-9]*</c>.</summary>
    [Pure]
    public NextPieceCount ZeroOrMore() => new(this, NumberOfTimes.ZeroOrMore);

    /// <summary>The next piece at least once. <c>OneOrMore().Digit()</c> emits <c>[0-9]+</c>.</summary>
    [Pure]
    public NextPieceCount OneOrMore() => new(this, NumberOfTimes.OneOrMore);

    /// <summary>The next piece exactly <paramref name="count"/> times. <c>Exactly(4).Digit()</c> emits <c>[0-9]{4}</c>.</summary>
    /// <param name="count">How many times; 1 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
    [Pure]
    public NextPieceCount Exactly(int count) => new(this, NumberOfTimes.Exactly(count));

    /// <summary>The next piece <paramref name="minimum"/> times or more. <c>AtLeast(8).AnyCharacter()</c> emits <c>[\s\S]{8,}</c>.</summary>
    /// <param name="minimum">The fewest times allowed; 0 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimum"/> is negative.</exception>
    [Pure]
    public NextPieceCount AtLeast(int minimum) => new(this, NumberOfTimes.AtLeast(minimum));

    /// <summary>The next piece up to <paramref name="maximum"/> times, or not at all. <c>AtMost(3).Digit()</c> emits <c>[0-9]{0,3}</c>.</summary>
    /// <param name="maximum">The most times allowed; 1 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximum"/> is less than 1.</exception>
    [Pure]
    public NextPieceCount AtMost(int maximum) => new(this, NumberOfTimes.AtMost(maximum));

    /// <summary>
    /// The next piece from <paramref name="minimum"/> to <paramref name="maximum"/> times.
    /// <c>Between(2, 6).Letter()</c> emits <c>\p{L}{2,6}</c>.
    /// </summary>
    /// <param name="minimum">The fewest times allowed; 0 or more.</param>
    /// <param name="maximum">The most times allowed; 1 or more, and at least <paramref name="minimum"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minimum"/> is negative, or <paramref name="maximum"/> is less than 1 or less than <paramref name="minimum"/>.
    /// </exception>
    [Pure]
    public NextPieceCount Between(int minimum, int maximum) => new(this, NumberOfTimes.Between(minimum, maximum));

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
    /// The pattern is not valid, for example <see cref="PatternPieces.SameTextAsSaved"/> comes before its
    /// <see cref="PatternPieces.SaveAs"/>, or <see cref="PatternPieces.RawRegex"/> has a syntax error.
    /// The message quotes the pattern.
    /// </exception>
    public Regex ToRegex() => Volatile.Read(ref _regex) ?? LazyInitializer.EnsureInitialized(ref _regex, BuildRegex)!;

    /// <summary>Returns the regex pattern text built so far. Never throws.</summary>
    public override string ToString() => _pattern ??= PatternWriter.Render(Nodes);

    // ---------- Plumbing ----------

    internal TextPattern With(RegexNode node) => new(Nodes.Append(node), _ignoringCase, _timeLimit);

    /// <summary>This pattern's content as one piece of another pattern. Ignoring case travels with it as (?i:...).</summary>
    internal RegexNode AsPiece()
    {
        var content = new SequenceNode(Nodes);
        return _ignoringCase ? new IgnoreCaseNode(content) : content;
    }

    private protected override TextPattern Append(RegexNode node) => With(node);

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
