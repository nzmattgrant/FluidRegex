using System.Text.RegularExpressions;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// One place where a <see cref="TextPattern"/> was found, from <see cref="TextPattern.FindFirst"/> or
/// <see cref="TextPattern.FindAll"/>.
/// </summary>
public sealed class FoundText
{
    private readonly Match _match;
    private readonly Regex _regex;

    internal FoundText(Match match, Regex regex)
    {
        _match = match;
        _regex = regex;
    }

    /// <summary>The text that was found.</summary>
    public string Text => _match.Value;

    /// <summary>Where the found text starts in the searched text. The first character is position 0.</summary>
    public int Position => _match.Index;

    /// <summary>
    /// The text saved with <c>SaveAs(name, ...)</c>. Empty when that part of the pattern was optional and missing.
    /// </summary>
    /// <param name="name">The name given to <see cref="PatternPieces.SaveAs"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">The pattern saves nothing under <paramref name="name"/>.</exception>
    public string Saved(string name)
    {
        Guard.SavedName(name, nameof(name));
        if (_regex.GroupNumberFromName(name) < 0)
        {
            throw new ArgumentException(NothingSavedMessage(name), nameof(name));
        }

        return _match.Groups[name].Value;
    }

    /// <summary>Returns <see cref="Text"/>.</summary>
    public override string ToString() => Text;

    private string NothingSavedMessage(string name)
    {
        // Unnamed groups, including group 0 (the whole match), have names that start with a digit.
        var savedNames = _regex.GetGroupNames().Where(groupName => !char.IsDigit(groupName[0])).ToArray();
        return savedNames.Length == 0
            ? $"The pattern saves nothing. Add SaveAs(\"{name}\", ...) to the pattern to save part of the text."
            : $"Nothing is saved as \"{name}\". The pattern saves: \"{string.Join("\", \"", savedNames)}\".";
    }
}
