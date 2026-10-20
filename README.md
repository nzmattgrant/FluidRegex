# FluidRegex

[![NuGet](https://img.shields.io/nuget/v/FluidRegex.svg)](https://www.nuget.org/packages/FluidRegex)
[![CI](https://github.com/nzmattgrant/FluidRegex/actions/workflows/ci.yml/badge.svg)](https://github.com/nzmattgrant/FluidRegex/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/nzmattgrant/FluidRegex/blob/master/LICENSE)

Build .NET regular expressions with method chains that read like an English description of the text you want to find.

```csharp
using FluidRegex;
using static FluidRegex.NumberOfTimes;

var isoDate = new MatchThesePatterns()
    .StartOfText()
    .AnyDigit(Exactly(4)).TheCharacter('-')
    .AnyDigit(Exactly(2)).TheCharacter('-')
    .AnyDigit(Exactly(2))
    .EndOfText();

isoDate.IsMatch("2026-10-12");   // true
isoDate.ToString();              // \A\d{4}-\d{2}-\d{2}\z
```

The result is an ordinary `System.Text.RegularExpressions.Regex`. The package has no dependencies.

## Why FluidRegex

Regexes are dense, and the mistakes in them are easy to make and hard to see. Here is an email check written by hand:

```text
^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$
```

And the same pattern in FluidRegex:

```csharp
var wordsWithAllowedStartSymbols = new SubPattern()
    .OneOfTheseCharacters("-+.'")
    .AnyWordCharacter(OneOrMore);
var wordsWithHyphenAndDots = new SubPattern()
    .OneOfTheseCharacters("-.")
    .AnyWordCharacter(OneOrMore);

var email = new MatchThesePatterns()
    .StartOfText()
    .AnyWordCharacter(OneOrMore)
    .Group(wordsWithAllowedStartSymbols, ZeroOrMore)
    .TheCharacter('@')
    .AnyWordCharacter(OneOrMore)
    .Group(wordsWithHyphenAndDots, ZeroOrMore)
    .TheCharacter('.')
    .AnyWordCharacter(OneOrMore)
    .Group(wordsWithHyphenAndDots, ZeroOrMore)
    .EndOfText();
// \A\w+(?:[-+.']\w+)*@\w+(?:[-.]\w+)*\.\w+(?:[-.]\w+)*\z
```

The builder also handles the details that are easy to get wrong:

- **Literal text is always escaped.** For example, `TheText("a+b")` matches the text `a+b`.
- **Counts apply to the whole piece.** `TheText("abc", OneOrMore)` emits `(?:abc)+`, not `abc+`.
- **Either-or is always grouped.** `|` can never swallow its neighbours.
- **Sub-patterns are reusable.** Builders are immutable, so you can share them freely, even across threads.
- **Mistakes throw at the call that made them.** For example, writing `OneOfTheseCharacters("a-z")` instead of a range
  throws with a hint pointing you to the range method.

Compared with [VerbalExpressions](https://github.com/VerbalExpressions/CSharpVerbalExpressions), FluidRegex:

- names methods after the text they match;
- applies a count to the whole item, not just its last character;
- builds pieces as reusable sub-patterns instead of start/end calls.

## Install

```shell
dotnet add package FluidRegex
```

FluidRegex supports netstandard2.0, so it runs on .NET Framework 4.6.1+, Mono and Unity. It also targets net10.0.

## Quick start

1. Add `using FluidRegex;` and `using static FluidRegex.NumberOfTimes;`. The second lets counts read as plain words,
   for example `AnyDigit(Exactly(4))` and `TheText("www.", Optional)`.
2. Start with `new MatchThesePatterns()` and add the pieces in reading order.
3. Call `IsMatch(text)`, `ToRegex()` for the full `Regex` API, or `ToString()` for the pattern text.

Every call returns a new builder and leaves the original unchanged. The count is always the last argument and
defaults to `Once`.

## Cheat sheet

| You write | Reads as | Emits |
| --- | --- | --- |
| `StartOfText()` / `EndOfText()` | at the start / end of the text | `\A` / `\z` |
| `StartOfLine()` / `EndOfLine()` | at the start / end of a line (`\r\n`, `\n` or `\r`) | lookarounds |
| `WordBoundary()` / `NotAWordBoundary()` | at / not at the edge of a word | `\b` / `\B` |
| `TheText("www.", Optional)` | the text "www.", optional | `(?:www\.)?` |
| `TheCharacter('@')` | the character @ | `@` |
| `AnyCharacter(AtLeast(8))` | any character except a newline, at least 8 times | `.{8,}` |
| `AnyCharacterIncludingNewline()` | any character at all | `[\s\S]` |
| `AnyDigit(Exactly(4))` / `AnyNonDigit()` | any digit, exactly 4 times / any non-digit | `\d{4}` / `\D` |
| `AnyLetter()` / `AnyNonLetter()` | any letter / any non-letter | `\p{L}` / `\P{L}` |
| `AnyUppercaseLetter()` / `AnyLowercaseLetter()` | any uppercase / lowercase letter | `\p{Lu}` / `\p{Ll}` |
| `AnyWordCharacter(OneOrMore)` / `AnyNonWordCharacter()` | letters, digits or underscores / anything else | `\w+` / `\W` |
| `AnyWhitespace()` / `AnyNonWhitespace()` | any whitespace / any non-whitespace | `\s` / `\S` |
| `AnyLineBreak()` | a line break in any style | `(?:\r\n\|\r\|\n)` |
| `OneOfTheseCharacters("-+.'")` | one of these characters | `[-+.']` |
| `OneOfTheseCharacters(set, Between(2, 256))` | one of the characters in a `CharacterSet`, 2 to 256 times | `[...]{2,256}` |
| `AnyCharacterExcept("<>", OneOrMore)` | any character except < or > | `[^<>]+` |
| `AnyCharacterInRange('a', 'z')` | any character from a to z | `[a-z]` |
| `Group(subPattern, ZeroOrMore)` | the sub-pattern, zero or more times | `(?:...)*` |
| `Capture(subPattern)` | capture the sub-pattern's text | `(...)` |
| `CaptureAs("year", subPattern)` | capture it as "year" | `(?<year>...)` |
| `EitherOf("cat", "dog")` | either "cat" or "dog" | `(?:cat\|dog)` |
| `TheSameTextAsCapture("word")` | the same text as the capture "word" | `\k<word>` |
| `IfFollowedBy("Y")` / `IfNotFollowedBy("Y")` | only if "Y" comes / doesn't come next | `(?=Y)` / `(?!Y)` |
| `IfPrecededBy("$")` / `IfNotPrecededBy("$")` | only if "$" comes / doesn't come just before | `(?<=\$)` / `(?<!\$)` |
| `RawRegex(@"\p{IsGreek}")` | this raw regex | `(?:\p{IsGreek})` |

**Counts** (`NumberOfTimes`):

| Count | Emits |
| --- | --- |
| `Once` (the default) | nothing |
| `Optional` | `?` |
| `ZeroOrMore` | `*` |
| `OneOrMore` | `+` |
| `Exactly(n)` | `{n}` |
| `AtLeast(n)` | `{n,}` |
| `AtMost(n)` | `{0,n}` |
| `Between(min, max)` | `{min,max}` |

Add `.AsFewAsPossible()` to any count that isn't fixed for the lazy form, for example `OneOrMore.AsFewAsPossible()`
emits `+?`.

**Options** go straight after `new MatchThesePatterns()`. They never change the pattern text.

- `IgnoringCase()`
- `WithOptions(RegexOptions...)`
- `WithMatchTimeout(TimeSpan)`

## Recipes

### URL

```csharp
var hostCharacters = new CharacterSet()
    .TheCharacter('-')
    .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').CharactersInRange('0', '9')
    .TheCharacters("@:%._+~#=");
var pathCharacters = new CharacterSet()
    .TheCharacter('-')
    .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').CharactersInRange('0', '9')
    .TheCharacters("@:%_+.~#()?&/=");
var path = new SubPattern().OneOfTheseCharacters(pathCharacters, ZeroOrMore);

var url = new MatchThesePatterns()
    .TheText("http")
    .TheCharacter('s', Optional)
    .TheText("://")
    .TheText("www.", Optional)
    .OneOfTheseCharacters(hostCharacters, Between(2, 256))
    .TheCharacter('.')
    .AnyCharacterInRange('a', 'z', Between(2, 6))
    .WordBoundary()
    .Capture(path);
// https?://(?:www\.)?[-a-zA-Z0-9@:%._+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_+.~#()?&/=]*)

url.ToRegex().Match("https://www.example.com/path?q=1").Groups[1].Value;   // "/path?q=1"
```

### ISO date with named captures

```csharp
var fourDigits = new SubPattern().AnyDigit(Exactly(4));
var twoDigits = new SubPattern().AnyDigit(Exactly(2));

var isoDate = new MatchThesePatterns()
    .StartOfText()
    .CaptureAs("year", fourDigits)
    .TheCharacter('-')
    .CaptureAs("month", twoDigits)
    .TheCharacter('-')
    .CaptureAs("day", twoDigits)
    .EndOfText();
// \A(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\z

isoDate.ToRegex().Match("2026-10-12").Groups["year"].Value;   // "2026"
```

### US phone number

```csharp
var usPhone = new MatchThesePatterns()
    .StartOfText()
    .TheCharacter('(', Optional)
    .AnyDigit(Exactly(3))
    .TheCharacter(')', Optional)
    .OneOfTheseCharacters("-. ", Optional)
    .AnyDigit(Exactly(3))
    .OneOfTheseCharacters("-. ", Optional)
    .AnyDigit(Exactly(4))
    .EndOfText();
// \A\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}\z

usPhone.IsMatch("(555) 123-4567");   // true
```

### Either-or, ignoring case

```csharp
var catsOrDogs = new MatchThesePatterns()
    .IgnoringCase()
    .WordBoundary()
    .EitherOf("cat", "dog")
    .TheCharacter('s', Optional)
    .WordBoundary();
// \b(?:cat|dog)s?\b

catsOrDogs.IsMatch("I have two DOGS");   // true
catsOrDogs.IsMatch("concatenate");       // false
```

### Repeated words

```csharp
var aWord = new SubPattern().AnyWordCharacter(OneOrMore);

var repeatedWord = new MatchThesePatterns()
    .WordBoundary()
    .CaptureAs("word", aWord)
    .AnyWhitespace(OneOrMore)
    .TheSameTextAsCapture("word")
    .WordBoundary();
// \b(?<word>\w+)\s+\k<word>\b

repeatedWord.ToRegex().Match("Paris in the the spring").Groups["word"].Value;   // "the"
```

### Password rule

```csharp
var aDigitSomewhereAhead = new SubPattern().AnyCharacter(ZeroOrMore).AnyDigit();
var aLowercaseLetterSomewhereAhead = new SubPattern().AnyCharacter(ZeroOrMore).AnyCharacterInRange('a', 'z');

var password = new MatchThesePatterns()
    .StartOfText()
    .IfFollowedBy(aDigitSomewhereAhead)
    .IfFollowedBy(aLowercaseLetterSomewhereAhead)
    .AnyCharacter(AtLeast(8))
    .EndOfText();
// \A(?=.*\d)(?=.*[a-z]).{8,}\z

password.IsMatch("passw0rd");   // true
password.IsMatch("password");   // false
```

## Escaping and safety

- **Text arguments are literal.** Every string you pass is matched exactly as written. This covers `TheText`,
  `EitherOf`, the `If...` conditions and `OneOfTheseCharacters`.
- **Character strings never form ranges.** A string such as `"a-z"` throws instead of quietly meaning three
  characters. Use `AnyCharacterInRange` or `CharacterSet.CharactersInRange` for ranges.
- **`RawRegex` is the escape hatch.** It isn't escaped, but it is always wrapped in `(?:...)`, so a `|` or an inline
  option inside it can't affect the rest of the pattern.
- **`ToRegex()` reports problems with the pattern quoted.** That covers invalid raw regex and a reference that comes
  before its capture. `ToString()` never throws.
- **Use a match timeout for untrusted input,** to guard against catastrophic backtracking:
  `new MatchThesePatterns().WithMatchTimeout(TimeSpan.FromSeconds(1))`.
- **`RegexOptions.Singleline` is rejected,** because it would silently change what `AnyCharacter()` means. Use
  `AnyCharacterIncludingNewline()` instead.

## Unicode notes

- **`AnyDigit()`, `AnyWordCharacter()` and `AnyLetter()` follow .NET's Unicode rules.** They match digits and letters
  from every script, for example `é` and Arabic-Indic digits. For ASCII only, use
  `AnyCharacterInRange('0', '9')` or `AnyCharacterInRange('a', 'z')`.
- **Case classes follow `IgnoringCase()`.** With it, `AnyUppercaseLetter()` and `AnyLowercaseLetter()` match both
  cases.

## Versioning

FluidRegex is at 0.x while the English names settle, so minor versions may rename members. From 1.0.0 it follows
semantic versioning.

## Contributing

```shell
dotnet test FluidRegex.slnx
```

The tests run on net10.0 and, on Windows, on net48 (the shipped netstandard2.0 build on .NET Framework).

## License

[MIT](https://github.com/nzmattgrant/FluidRegex/blob/master/LICENSE)
