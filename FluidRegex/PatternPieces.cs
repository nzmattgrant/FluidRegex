using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// The pieces a <see cref="TextPattern"/> is built from. Each call adds the next piece in reading order and returns a
/// new pattern; the original never changes. After a count such as <see cref="TextPattern.OneOrMore"/> the same pieces
/// are offered again, and the count applies to the whole piece: <c>OneOrMore().Text("ab")</c> emits <c>(?:ab)+</c>.
/// </summary>
/// <remarks>Every string is matched exactly as written, except in <see cref="RawRegex"/>.</remarks>
public abstract class PatternPieces
{
    private static readonly CharacterSet Digits = new CharacterSet().Digits();
    private static readonly CharacterSet LettersAndDigits = new CharacterSet().Letters().Digits();

    private protected PatternPieces(NumberOfTimes times)
    {
        Times = times;
    }

    private protected NumberOfTimes Times { get; }

    // ---------- Text ----------

    /// <summary>The exact text <paramref name="text"/>. <c>Optional().Text("www.")</c> emits <c>(?:www\.)?</c>.</summary>
    /// <param name="text">The text to find. Every character is matched as written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    [Pure]
    public TextPattern Text(string text) => Add(new TextNode(Guard.NotNullOrEmpty(text, nameof(text))));

    // ---------- Kinds of character ----------

    /// <summary>Any character at all, including line breaks. Emits <c>[\s\S]</c>.</summary>
    [Pure]
    public TextPattern AnyCharacter() => Add(new TokenNode(@"[\s\S]"));

    /// <summary>Any character except a line break (<c>\r</c> or <c>\n</c>). Emits <c>[^\r\n]</c>.</summary>
    [Pure]
    public TextPattern AnyCharacterExceptLineBreak() => Add(new TokenNode(@"[^\r\n]"));

    /// <summary>A digit from 0 to 9. Emits <c>[0-9]</c>.</summary>
    [Pure]
    public TextPattern Digit() => Add(new SetNode(Digits, negated: false));

    /// <summary>Any character except a digit from 0 to 9. Emits <c>[^0-9]</c>.</summary>
    [Pure]
    public TextPattern AnyCharacterExceptDigit() => Add(new SetNode(Digits, negated: true));

    /// <summary>A letter from any language, such as <c>a</c>, <c>É</c> or <c>ж</c>. Emits <c>\p{L}</c>.</summary>
    [Pure]
    public TextPattern Letter() => Add(new TokenNode(@"\p{L}"));

    /// <summary>Any character except a letter. Emits <c>\P{L}</c>.</summary>
    [Pure]
    public TextPattern AnyCharacterExceptLetter() => Add(new TokenNode(@"\P{L}"));

    /// <summary>
    /// An uppercase letter. Emits <c>\p{Lu}</c>. With <see cref="TextPattern.IgnoringCase"/> it also matches lowercase letters.
    /// </summary>
    [Pure]
    public TextPattern UppercaseLetter() => Add(new TokenNode(@"\p{Lu}"));

    /// <summary>
    /// A lowercase letter. Emits <c>\p{Ll}</c>. With <see cref="TextPattern.IgnoringCase"/> it also matches uppercase letters.
    /// </summary>
    [Pure]
    public TextPattern LowercaseLetter() => Add(new TokenNode(@"\p{Ll}"));

    /// <summary>A letter from any language, or a digit from 0 to 9. Emits <c>[\p{L}0-9]</c>.</summary>
    [Pure]
    public TextPattern LetterOrDigit() => Add(new SetNode(LettersAndDigits, negated: false));

    /// <summary>A space, tab, line break or other blank character. Emits <c>\s</c>.</summary>
    [Pure]
    public TextPattern Whitespace() => Add(new TokenNode(@"\s"));

    /// <summary>Any character except whitespace. Emits <c>\S</c>.</summary>
    [Pure]
    public TextPattern AnyCharacterExceptWhitespace() => Add(new TokenNode(@"\S"));

    /// <summary>One line break in Windows (<c>\r\n</c>), Unix (<c>\n</c>) or old Mac (<c>\r</c>) style. Emits <c>(?:\r\n|\r|\n)</c>.</summary>
    [Pure]
    public TextPattern LineBreak() => Add(new TokenNode(@"(?:\r\n|\r|\n)"));

    // ---------- Chosen characters ----------

    /// <summary>
    /// One of the characters in <paramref name="characters"/>, each matched as written.
    /// <c>OneOfTheseCharacters("-+.'")</c> emits <c>[-+.']</c>.
    /// </summary>
    /// <param name="characters">The characters to choose from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="characters"/> is empty, or contains something that looks like a range such as <c>"a-z"</c>.
    /// Use <see cref="AnyCharacterInRange"/> or <see cref="CharacterSet.CharactersInRange"/> for ranges, or put
    /// <c>'-'</c> first for a hyphen.
    /// </exception>
    [Pure]
    public TextPattern OneOfTheseCharacters(string characters) =>
        Add(new SetNode(CharacterSet.FromItems(CharacterSet.LiteralItems(characters, nameof(characters))), negated: false));

    /// <summary>One of the characters in <paramref name="characters"/>. Emits <c>[...]</c>.</summary>
    /// <param name="characters">A reusable set of characters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty.</exception>
    [Pure]
    public TextPattern OneOfTheseCharacters(CharacterSet characters) =>
        Add(new SetNode(NotEmpty(characters, nameof(characters)), negated: false));

    /// <summary>
    /// Any character except those in <paramref name="characters"/>, each matched as written.
    /// <c>OneOrMore().AnyCharacterExcept("&lt;&gt;")</c> emits <c>[^&lt;&gt;]+</c>.
    /// </summary>
    /// <param name="characters">The characters to leave out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty or contains a range look-alike such as <c>"a-z"</c>.</exception>
    [Pure]
    public TextPattern AnyCharacterExcept(string characters) =>
        Add(new SetNode(CharacterSet.FromItems(CharacterSet.LiteralItems(characters, nameof(characters))), negated: true));

    /// <summary>Any character except those in <paramref name="characters"/>. Emits <c>[^...]</c>.</summary>
    /// <param name="characters">A reusable set of characters to leave out.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="characters"/> is empty.</exception>
    [Pure]
    public TextPattern AnyCharacterExcept(CharacterSet characters) =>
        Add(new SetNode(NotEmpty(characters, nameof(characters)), negated: true));

    /// <summary>
    /// Any character from <paramref name="from"/> to <paramref name="to"/>.
    /// <c>Between(2, 6).AnyCharacterInRange('a', 'z')</c> emits <c>[a-z]{2,6}</c>.
    /// </summary>
    /// <param name="from">The first character of the range.</param>
    /// <param name="to">The last character of the range.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="to"/> comes before <paramref name="from"/>.</exception>
    [Pure]
    public TextPattern AnyCharacterInRange(char from, char to) =>
        Add(new SetNode(new CharacterSet().CharactersInRange(from, to), negated: false));

    // ---------- Choices and reused patterns ----------

    /// <summary>
    /// One of several texts, each matched as written. <c>EitherOf("cat", "dog")</c> emits <c>(?:cat|dog)</c>.
    /// </summary>
    /// <param name="texts">The choices, tried in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="texts"/> is null.</exception>
    /// <exception cref="ArgumentException">No texts are given, or one of them is null or empty.</exception>
    [Pure]
    public TextPattern EitherOf(params string[] texts)
    {
        Guard.NotNullOrEmptyItems(texts, nameof(texts));
        var choices = new RegexNode[texts.Length];
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].Length == 0)
            {
                throw new ArgumentException($"Choice {i} is empty, which would make the whole EitherOf optional.", nameof(texts));
            }

            choices[i] = new TextNode(texts[i]);
        }

        return Add(Alternation(choices));
    }

    /// <summary>
    /// One of several patterns. <c>EitherOf(someDigits, someLetters)</c> emits <c>(?:[0-9]+|\p{L}+)</c>.
    /// </summary>
    /// <param name="patterns">The choices, tried in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="patterns"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// No patterns are given, one of them is null or empty, or they only match positions and a count was given.
    /// </exception>
    [Pure]
    public TextPattern EitherOf(params TextPattern[] patterns)
    {
        Guard.NotNullOrEmptyItems(patterns, nameof(patterns));
        var choices = new RegexNode[patterns.Length];
        for (var i = 0; i < patterns.Length; i++)
        {
            if (patterns[i].IsEmpty)
            {
                throw new ArgumentException($"Choice {i} is an empty pattern, which would make the whole EitherOf optional.", nameof(patterns));
            }

            choices[i] = patterns[i].AsPiece();
        }

        return Add(Countable(Alternation(choices), nameof(patterns)));
    }

    /// <summary>
    /// A pattern built earlier, used here as one piece. Patterns never change, so one can be used any number of times.
    /// <c>ZeroOrMore().Pattern(dotThenWord)</c> emits <c>(?:\.\p{L}+)*</c>.
    /// </summary>
    /// <param name="pattern">The pattern to use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern"/> is empty, or it only matches positions and a count was given.
    /// </exception>
    [Pure]
    public TextPattern Pattern(TextPattern pattern) => Add(Content(pattern, nameof(pattern)));

    // ---------- Saving text ----------

    /// <summary>
    /// Saves the text that <paramref name="pattern"/> matches under <paramref name="name"/>. Read it back with
    /// <see cref="FoundText.Saved"/>, or match it again with <see cref="SameTextAsSaved"/>.
    /// <c>SaveAs("year", fourDigits)</c> emits <c>(?&lt;year&gt;[0-9]{4})</c>. After a count the saved text holds
    /// the whole run: <c>OneOrMore().SaveAs("number", digit)</c> emits <c>(?&lt;number&gt;[0-9]+)</c>.
    /// </summary>
    /// <param name="name">The name to save under: a letter or underscore, then letters, digits or underscores.</param>
    /// <param name="pattern">The pattern whose text is saved.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="pattern"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is not a valid name, or <paramref name="pattern"/> is empty or only matches positions
    /// and a count was given.
    /// </exception>
    [Pure]
    public TextPattern SaveAs(string name, TextPattern pattern) =>
        Append(new CaptureNode(Guard.SavedName(name, nameof(name)), Content(pattern, nameof(pattern)), Times));

    /// <summary>
    /// The same text that was saved earlier with <see cref="SaveAs"/> under <paramref name="name"/>. Emits <c>\k&lt;name&gt;</c>.
    /// </summary>
    /// <param name="name">The name given to <see cref="SaveAs"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid name.</exception>
    [Pure]
    public TextPattern SameTextAsSaved(string name) => Add(new BackreferenceNode(Guard.SavedName(name, nameof(name))));

    // ---------- Raw regex escape hatch ----------

    /// <summary>
    /// Raw .NET regex syntax, for anything the other methods don't cover. It is not escaped, and it is always wrapped
    /// in <c>(?:...)</c> so a <c>|</c> or an inline option can't leak into the rest: <c>OneOrMore().RawRegex(@"\p{IsGreek}")</c>
    /// emits <c>(?:\p{IsGreek})+</c>. Syntax errors are reported when the pattern is first used.
    /// </summary>
    /// <param name="regexPattern">The raw pattern.</param>
    /// <exception cref="ArgumentNullException"><paramref name="regexPattern"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="regexPattern"/> is empty.</exception>
    [Pure]
    public TextPattern RawRegex([StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern) =>
        Add(new RawNode(Guard.NotNullOrEmpty(regexPattern, nameof(regexPattern))));

    // ---------- Plumbing ----------

    private protected abstract TextPattern Append(RegexNode node);

    private protected static RegexNode NonEmptyPiece(TextPattern pattern, string paramName)
    {
        Guard.NotNull(pattern, paramName);
        if (pattern.IsEmpty)
        {
            throw new ArgumentException("The pattern is empty.", paramName);
        }

        return pattern.AsPiece();
    }

    private TextPattern Add(RegexNode piece) => Append(Times.IsOnce ? piece : new QuantifiedNode(piece, Times));

    private RegexNode Content(TextPattern pattern, string paramName) =>
        Countable(NonEmptyPiece(pattern, paramName), paramName);

    private RegexNode Countable(RegexNode piece, string paramName)
    {
        if (!Times.IsOnce && piece.IsZeroWidth)
        {
            throw new ArgumentException(
                "The pattern only matches positions (such as StartOfWord() or IfFollowedBy(...)), not characters, so a count changes nothing.",
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

    private static RegexNode Alternation(RegexNode[] choices) =>
        choices.Length == 1 ? choices[0] : new AlternationNode(choices);
}
