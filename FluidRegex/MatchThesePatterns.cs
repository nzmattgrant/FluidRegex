using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// Starts a pattern. Chain the pieces in reading order, then call <see cref="ToRegex"/>, <see cref="IsMatch"/> or
/// <see cref="FluidRegexBuilderBase{T}.ToString"/>. Every call returns a new builder; the original never changes, so a
/// builder can be stored in a <c>static readonly</c> field and shared across threads.
/// </summary>
/// <example>
/// <code>
/// using static FluidRegex.NumberOfTimes;
///
/// var isoDate = new MatchThesePatterns()
///     .StartOfText()
///     .AnyDigit(Exactly(4)).TheCharacter('-')
///     .AnyDigit(Exactly(2)).TheCharacter('-')
///     .AnyDigit(Exactly(2))
///     .EndOfText();
/// // \A\d{4}-\d{2}-\d{2}\z
/// </code>
/// </example>
public sealed class MatchThesePatterns : FluidRegexBuilderBase<MatchThesePatterns>
{
    private Regex? _regex;

    /// <summary>Starts an empty pattern with no options and no match timeout.</summary>
    public MatchThesePatterns()
        : this(NodeList.Empty, RegexOptions.None, Regex.InfiniteMatchTimeout)
    {
    }

    private MatchThesePatterns(NodeList nodes, RegexOptions options, TimeSpan matchTimeout)
        : base(nodes)
    {
        Options = options;
        MatchTimeout = matchTimeout;
    }

    /// <summary>The options <see cref="ToRegex"/> builds the Regex with. They never change the pattern text.</summary>
    public RegexOptions Options { get; }

    /// <summary>How long a single match may run before it gives up; <see cref="Regex.InfiniteMatchTimeout"/> by default.</summary>
    public TimeSpan MatchTimeout { get; }

    /// <summary>Matches these patterns ignoring case: adds <see cref="RegexOptions.IgnoreCase"/>.</summary>
    [Pure]
    public MatchThesePatterns IgnoringCase() => WithOptions(RegexOptions.IgnoreCase);

    /// <summary>Adds .NET regex options. They are combined with any options already set.</summary>
    /// <param name="options">The options to add.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="options"/> includes <see cref="RegexOptions.Singleline"/>, which would silently change what
    /// <see cref="FluidRegexBuilderBase{T}.AnyCharacter"/> means. Use
    /// <see cref="FluidRegexBuilderBase{T}.AnyCharacterIncludingNewline"/> instead.
    /// </exception>
    [Pure]
    public MatchThesePatterns WithOptions(RegexOptions options)
    {
        if ((options & RegexOptions.Singleline) != 0)
        {
            throw new ArgumentException(
                "RegexOptions.Singleline would change what AnyCharacter() matches. Use AnyCharacterIncludingNewline() where newlines should match.",
                nameof(options));
        }

        return new MatchThesePatterns(Nodes, Options | options, MatchTimeout);
    }

    /// <summary>
    /// Gives up on a match that runs longer than <paramref name="timeout"/>, throwing
    /// <see cref="RegexMatchTimeoutException"/>. Recommended for patterns run against untrusted input.
    /// </summary>
    /// <param name="timeout">A positive time, or <see cref="Regex.InfiniteMatchTimeout"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is zero, negative, or longer than about 24 days.</exception>
    [Pure]
    public MatchThesePatterns WithMatchTimeout(TimeSpan timeout)
    {
        if (timeout != Regex.InfiniteMatchTimeout && (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue - 1))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be positive and at most about 24 days, or Regex.InfiniteMatchTimeout.");
        }

        return new MatchThesePatterns(Nodes, Options, timeout);
    }

    /// <summary>
    /// Builds the <see cref="Regex"/>, with <see cref="Options"/> and <see cref="MatchTimeout"/>. The result is cached.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The finished pattern is invalid, for example a <see cref="FluidRegexBuilderBase{T}.TheSameTextAsCapture(string, NumberOfTimes)"/>
    /// that comes before its capture, or a syntax error inside <see cref="FluidRegexBuilderBase{T}.RawRegex"/>.
    /// The message quotes the pattern.
    /// </exception>
    public Regex ToRegex()
    {
        var regex = Volatile.Read(ref _regex);
        if (regex is not null)
        {
            return regex;
        }

        Interlocked.CompareExchange(ref _regex, Build(), null);
        return _regex!;
    }

    /// <summary>Whether <paramref name="input"/> contains a match. Uses the cached <see cref="ToRegex"/> result.</summary>
    /// <param name="input">The text to search.</param>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentException">The finished pattern is invalid (see <see cref="ToRegex"/>).</exception>
    /// <exception cref="RegexMatchTimeoutException">The match ran longer than <see cref="MatchTimeout"/>.</exception>
    public bool IsMatch(string input)
    {
        Guard.NotNull(input, nameof(input));
        return ToRegex().IsMatch(input);
    }

    private protected override MatchThesePatterns With(NodeList nodes) => new(nodes, Options, MatchTimeout);

    private Regex Build()
    {
        var pattern = ToString();
        try
        {
            PatternChecks.Check(Nodes, Options);
            return new Regex(pattern, Options, MatchTimeout);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            throw new ArgumentException($"The pattern \"{pattern}\" is not valid: {exception.Message}", exception);
        }
    }
}
