using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using FluidRegex.Internal;

namespace FluidRegex;

/// <summary>
/// A reusable set of characters, used with <c>OneOfTheseCharacters(set)</c> (emits <c>[...]</c>) and
/// <c>AnyCharacterExcept(set)</c> (emits <c>[^...]</c>). Every call returns a new set; the original never changes.
/// </summary>
/// <example>
/// <code>
/// var hostCharacters = new CharacterSet()
///     .CharactersInRange('a', 'z').Digits()
///     .Characters("-._");
/// // [-a-z0-9._]
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class CharacterSet
{
    private readonly SetItem[] _items;
    private string? _contents;

    /// <summary>Starts an empty set. Add characters to it before passing it in.</summary>
    public CharacterSet()
        : this([])
    {
    }

    private CharacterSet(SetItem[] items)
    {
        _items = items;
    }

    internal bool IsEmpty => _items.Length == 0;

    /// <summary>Adds each character of <paramref name="characters"/>, matched as written.</summary>
    /// <param name="characters">The characters, for example <c>"@:%._+~#="</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="characters"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="characters"/> is empty, or contains something that looks like a range such as <c>"a-z"</c>.
    /// Use <see cref="CharactersInRange"/> for a range, or put <c>'-'</c> first for a hyphen.
    /// </exception>
    [Pure]
    public CharacterSet Characters(string characters) =>
        AddRange(LiteralItems(characters, nameof(characters)));

    /// <summary>Adds every character from <paramref name="from"/> to <paramref name="to"/>, for example <c>'a'</c> to <c>'z'</c>.</summary>
    /// <param name="from">The first character of the range.</param>
    /// <param name="to">The last character of the range.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="to"/> comes before <paramref name="from"/>.</exception>
    [Pure]
    public CharacterSet CharactersInRange(char from, char to) => Add(SetItem.Range(from, to, nameof(to)));

    /// <summary>Adds every letter from any language, such as <c>a</c>, <c>É</c> or <c>ж</c> (<c>\p{L}</c>).</summary>
    [Pure]
    public CharacterSet Letters() => Add(SetItem.Token(@"\p{L}"));

    /// <summary>Adds every uppercase letter (<c>\p{Lu}</c>).</summary>
    [Pure]
    public CharacterSet UppercaseLetters() => Add(SetItem.Token(@"\p{Lu}"));

    /// <summary>Adds every lowercase letter (<c>\p{Ll}</c>).</summary>
    [Pure]
    public CharacterSet LowercaseLetters() => Add(SetItem.Token(@"\p{Ll}"));

    /// <summary>Adds the digits 0 to 9 (<c>0-9</c>).</summary>
    [Pure]
    public CharacterSet Digits() => CharactersInRange('0', '9');

    /// <summary>Adds every whitespace character: space, tab, line breaks and so on (<c>\s</c>).</summary>
    [Pure]
    public CharacterSet Whitespace() => Add(SetItem.Token(@"\s"));

    /// <summary>Returns the set as regex text, for example <c>[-a-z0-9._]</c>.</summary>
    public override string ToString() => "[" + Contents + "]";

    /// <summary>The text between the brackets. A literal '-' always comes first, so it can never form a range.</summary>
    internal string Contents => _contents ??= BuildContents();

    /// <summary>Turns a string of literal characters into set items, rejecting range look-alikes such as "a-z".</summary>
    internal static SetItem[] LiteralItems(string? characters, string paramName)
    {
        Guard.NotNullOrEmpty(characters, paramName);
        if (Escaping.TryFindRangeLookAlike(characters!, out var lookAlike))
        {
            var from = lookAlike[0];
            var to = lookAlike[2];
            throw new ArgumentException(
                $"\"{characters}\" contains \"{lookAlike}\", which looks like a range. Characters in a string are always matched as written. " +
                $"For a range use AnyCharacterInRange('{from}', '{to}') or new CharacterSet().CharactersInRange('{from}', '{to}'); " +
                "for a hyphen put '-' first, for example \"-" + from + to + "\".",
                paramName);
        }

        var items = new SetItem[characters!.Length];
        for (var i = 0; i < characters.Length; i++)
        {
            items[i] = SetItem.Single(characters[i]);
        }

        return items;
    }

    internal static CharacterSet FromItems(SetItem[] items) => new(items);

    private CharacterSet Add(SetItem item) => AddRange([item]);

    private CharacterSet AddRange(SetItem[] items)
    {
        var combined = new SetItem[_items.Length + items.Length];
        Array.Copy(_items, combined, _items.Length);
        Array.Copy(items, 0, combined, _items.Length, items.Length);
        return new CharacterSet(combined);
    }

    private string BuildContents()
    {
        var builder = new StringBuilder();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var hasHyphen = false;
        foreach (var item in _items)
        {
            if (item.IsSingle('-'))
            {
                hasHyphen = true;
            }
        }

        if (hasHyphen)
        {
            builder.Append('-');
        }

        foreach (var item in _items)
        {
            if (item.IsSingle('-') || !seen.Add(item.Key))
            {
                continue;
            }

            item.AppendTo(builder);
        }

        return builder.ToString();
    }

    /// <summary>One entry of a character set: a single character, a range, or a class token such as \d.</summary>
    internal readonly struct SetItem
    {
        private readonly char _from;
        private readonly char _to;
        private readonly string? _token;

        private SetItem(char from, char to, string? token)
        {
            _from = from;
            _to = to;
            _token = token;
        }

        public string Key => _token ?? (_from == _to ? _from.ToString() : _from + "-" + _to);

        public static SetItem Single(char character) => new(character, character, null);

        public static SetItem Range(char from, char to, string paramName)
        {
            if (to < from)
            {
                throw new ArgumentOutOfRangeException(paramName, to, $"The range '{from}' to '{to}' is in reverse order.");
            }

            return new SetItem(from, to, null);
        }

        public static SetItem Token(string token) => new(default, default, token);

        public bool IsSingle(char character) => _token is null && _from == character && _to == character;

        public void AppendTo(StringBuilder builder)
        {
            if (_token is not null)
            {
                builder.Append(_token);
            }
            else if (_from == _to)
            {
                Escaping.AppendInsideSet(builder, _from);
            }
            else
            {
                Escaping.AppendRangeEndpoint(builder, _from);
                builder.Append('-');
                Escaping.AppendRangeEndpoint(builder, _to);
            }
        }
    }
}
