using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FluidRegex.Internal;

/// <summary>
/// Escaping rules. Text outside a character set goes through <see cref="Regex.Escape"/>.
/// Inside [...] different rules apply, so sets have their own escaper.
/// </summary>
internal static class Escaping
{
    public static string OutsideSet(string text) => Regex.Escape(text);

    /// <summary>Escapes one literal character for use inside [...]. A '-' is never passed here: sets always write it first.</summary>
    public static void AppendInsideSet(StringBuilder builder, char character)
    {
        switch (character)
        {
            case '\\':
            case ']':
            case '[':
            case '^':
                builder.Append('\\').Append(character);
                break;
            case '\t':
                builder.Append(@"\t");
                break;
            case '\n':
                builder.Append(@"\n");
                break;
            case '\r':
                builder.Append(@"\r");
                break;
            case '\f':
                builder.Append(@"\f");
                break;
            case '\v':
                builder.Append(@"\v");
                break;
            default:
                if (char.IsControl(character))
                {
                    AppendUnicodeEscape(builder, character);
                }
                else
                {
                    builder.Append(character);
                }

                break;
        }
    }

    /// <summary>
    /// Writes one end of a range inside [...]. ASCII letters and digits are written as they are; anything else becomes
    /// \uXXXX, because .NET never starts a range at an escaped "\-" and a bare '-' end can join the wrong neighbour.
    /// </summary>
    public static void AppendRangeEndpoint(StringBuilder builder, char character)
    {
        if (IsAsciiLetterOrDigit(character))
        {
            builder.Append(character);
        }
        else
        {
            AppendUnicodeEscape(builder, character);
        }
    }

    /// <summary>
    /// Finds something that looks like a range, such as "a-z": an ASCII letter or digit, then '-', then another.
    /// Strings of characters are always literal, so such input almost certainly meant a range.
    /// </summary>
    public static bool TryFindRangeLookAlike(string characters, out string lookAlike)
    {
        for (var i = 0; i + 2 < characters.Length; i++)
        {
            if (characters[i + 1] == '-' && IsAsciiLetterOrDigit(characters[i]) && IsAsciiLetterOrDigit(characters[i + 2]))
            {
                lookAlike = characters.Substring(i, 3);
                return true;
            }
        }

        lookAlike = string.Empty;
        return false;
    }

    private static bool IsAsciiLetterOrDigit(char character) =>
        character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';

    private static void AppendUnicodeEscape(StringBuilder builder, char character) =>
        builder.Append(@"\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
}
