# FluidRegex

[![NuGet](https://img.shields.io/nuget/v/FluidRegex.svg)](https://www.nuget.org/packages/FluidRegex)
[![CI](https://github.com/nzmattgrant/FluidRegex/actions/workflows/ci.yml/badge.svg)](https://github.com/nzmattgrant/FluidRegex/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/nzmattgrant/FluidRegex/blob/master/LICENSE)

Build .NET regular expressions with method chains that read like a description of the text you want to find.
You do not need to know regex syntax.

```csharp
using FluidRegex;

var isoDate = new TextPattern()
    .Exactly(4).Digit().Text("-")
    .Exactly(2).Digit().Text("-")
    .Exactly(2).Digit();

isoDate.MatchesWholeText("2026-10-12");   // true
isoDate.ToString();                       // [0-9]{4}-[0-9]{2}-[0-9]{2}
```

FluidRegex builds an ordinary `System.Text.RegularExpressions.Regex`. The package has no dependencies.

## Why FluidRegex

Regexes are dense. Mistakes in them are easy to make and hard to see. Here is an email check written by hand:

```text
^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$
```

Here is a similar check in FluidRegex:

```csharp
var word = new TextPattern()
    .OneOrMore().OneOfTheseCharacters(new CharacterSet().Letters().Digits().Characters("_"));
var symbolThenWord = new TextPattern().OneOfTheseCharacters("-+.'").Pattern(word);
var dotOrHyphenThenWord = new TextPattern().OneOfTheseCharacters("-.").Pattern(word);

var email = new TextPattern()
    .Pattern(word)
    .ZeroOrMore().Pattern(symbolThenWord)
    .Text("@")
    .Pattern(word)
    .ZeroOrMore().Pattern(dotOrHyphenThenWord)
    .Text(".")
    .Pattern(word)
    .ZeroOrMore().Pattern(dotOrHyphenThenWord);

email.MatchesWholeText("john.smith@example.co.uk");   // true
email.MatchesWholeText("junk john@example.com");      // false
```

FluidRegex also prevents the mistakes that are easy to make:

- **Text is matched as written.** `Text("a+b")` finds the text `a+b`. You do not escape anything.
- **A count covers the whole piece.** `OneOrMore().Text("abc")` repeats `abc`, not only the `c`.
- **A count comes before its piece.** After `OneOrMore()`, the method list shows only the pieces that a count can
  apply to. `OneOrMore().StartOfText()` does not compile.
- **Checking input and searching text are different methods.** `MatchesWholeText` checks all of the text, so an email
  check cannot accept `"junk a@b.com junk"` by accident. `IsFoundIn` searches.
- **`Digit()` matches only 0 to 9.** Plain `\d` also matches Arabic, Thai and other digits.
- **`AnyCharacter()` matches any character.** Plain `.` does not match a line break.
- **Mistakes throw at the call that made them.** For example, `OneOfTheseCharacters("a-z")` throws, and the message
  tells you to use `AnyCharacterInRange('a', 'z')`.
- **Patterns are reusable.** A pattern never changes, so you can share it, also across threads.

Compared with [VerbalExpressions](https://github.com/VerbalExpressions/CSharpVerbalExpressions), FluidRegex:

- names methods after the text they match.
- applies a count to the whole piece, not only to its last character.
- builds groups from reusable patterns instead of start and end calls.

## Install

```shell
dotnet add package FluidRegex
```

FluidRegex supports netstandard2.0, so it runs on .NET Framework 4.6.1 and later, Mono and Unity. It also targets
net10.0.

## Quick start

1. Add `using FluidRegex;`.
2. Start with `new TextPattern()`.
3. Add the pieces in reading order.
4. To repeat a piece, put a count before it: `.Exactly(4).Digit()`.
5. Use the pattern:
   - `MatchesWholeText(text)` checks that all of the text fits the pattern, for example an email address in a form.
   - `IsFoundIn(text)` checks that the pattern is somewhere in the text.
   - `FindFirst(text)` and `FindAll(text)` give the text that the pattern found.
   - `ReplaceAll(text, replacement)` replaces each place that the pattern found.

Each call gives a new pattern. The original pattern does not change.

## Cheat sheet

**Pieces.** You can put a count before each piece.

| You write | Finds | Emits |
| --- | --- | --- |
| `Text("www.")` | the text "www." | `www\.` |
| `AnyCharacter()` | any character, also a line break | `[\s\S]` |
| `AnyCharacterExceptLineBreak()` | any character except a line break | `[^\r\n]` |
| `Digit()` / `AnyCharacterExceptDigit()` | a digit from 0 to 9 / any other character | `[0-9]` / `[^0-9]` |
| `Letter()` / `AnyCharacterExceptLetter()` | a letter in any language / any other character | `\p{L}` / `\P{L}` |
| `UppercaseLetter()` / `LowercaseLetter()` | an uppercase / lowercase letter | `\p{Lu}` / `\p{Ll}` |
| `LetterOrDigit()` | a letter, or a digit from 0 to 9 | `[\p{L}0-9]` |
| `Whitespace()` / `AnyCharacterExceptWhitespace()` | a space, tab or line break / any other character | `\s` / `\S` |
| `LineBreak()` | a line break in any style | `(?:\r\n\|\r\|\n)` |
| `OneOfTheseCharacters("-+.'")` | one of these characters | `[-+.']` |
| `OneOfTheseCharacters(set)` | one of the characters in a `CharacterSet` | `[...]` |
| `AnyCharacterExcept("<>")` | any character except < and > | `[^<>]` |
| `AnyCharacterInRange('a', 'z')` | any character from a to z | `[a-z]` |
| `EitherOf("cat", "dog")` | "cat" or "dog" | `(?:cat\|dog)` |
| `Pattern(otherPattern)` | a pattern that you made before | its content |
| `SaveAs("year", otherPattern)` | a pattern, and saves its text as "year" | `(?<year>...)` |
| `SameTextAsSaved("word")` | the same text that was saved as "word" | `\k<word>` |
| `RawRegex(@"\p{IsGreek}")` | raw .NET regex, for experts | `(?:\p{IsGreek})` |

**Counts.** Put a count before the piece that it applies to.

| You write | The next piece | Emits |
| --- | --- | --- |
| `Optional()` | can be missing | `?` |
| `ZeroOrMore()` | any number of times, or not at all | `*` |
| `OneOrMore()` | one time or more | `+` |
| `Exactly(4)` | exactly 4 times | `{4}` |
| `AtLeast(8)` | 8 times or more | `{8,}` |
| `AtMost(3)` | 0 to 3 times | `{0,3}` |
| `Between(2, 6)` | 2 to 6 times | `{2,6}` |

A count matches as many times as possible. To match as few times as possible, add `.AsFewAsPossible()` after a count
that can change. For example, `Text("<").OneOrMore().AsFewAsPossible().AnyCharacter().Text(">")` finds `<a>` in
`<a><b>`. Without `AsFewAsPossible()`, it finds all of `<a><b>`.

**Positions and conditions.** These match a place in the text, not characters, so they take no count.

| You write | Matches | Emits |
| --- | --- | --- |
| `StartOfText()` / `EndOfText()` | at the start / end of the text | `\A` / `\z` |
| `StartOfLine()` / `EndOfLine()` | at the start / end of a line (`\r\n`, `\n` or `\r`) | lookarounds |
| `StartOfWord()` / `EndOfWord()` | at the start / end of a word | `\b(?=\w)` / `\b(?<=\w)` |
| `IfFollowedBy("Y")` / `IfNotFollowedBy("Y")` | only if "Y" comes / does not come next | `(?=Y)` / `(?!Y)` |
| `IfPrecededBy("$")` / `IfNotPrecededBy("$")` | only if "$" comes / does not come just before | `(?<=\$)` / `(?<!\$)` |

**Settings.**

- `IgnoringCase()` treats uppercase and lowercase as the same. If you use the pattern inside another pattern, only
  its own part ignores case.
- `WithTimeLimit(TimeSpan)` stops a search that takes too long.

**Results.**

| You write | Gives |
| --- | --- |
| `MatchesWholeText(text)` | `true` if all of the text fits the pattern |
| `IsFoundIn(text)` | `true` if the pattern is somewhere in the text |
| `FindFirst(text)` | the first `FoundText`, or `null` |
| `FindAll(text)` | each `FoundText`, in order |
| `ReplaceAll(text, replacement)` | the text, with the replacement in each place that the pattern found |
| `ToRegex()` | the .NET `Regex`, for everything else |
| `ToString()` | the regex pattern text |

A `FoundText` has `Text`, `Position` and `Saved("name")`.

## Recipes

### URL

```csharp
var hostCharacters = new CharacterSet()
    .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').Digits()
    .Characters("-@:%._+~#=");
var pathCharacters = new CharacterSet()
    .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').Digits()
    .Characters("-@:%_+.~#()?&/=");
var path = new TextPattern().ZeroOrMore().OneOfTheseCharacters(pathCharacters);

var url = new TextPattern()
    .Text("http")
    .Optional().Text("s")
    .Text("://")
    .Optional().Text("www.")
    .Between(2, 256).OneOfTheseCharacters(hostCharacters)
    .Text(".")
    .Between(2, 6).AnyCharacterInRange('a', 'z')
    .EndOfWord()
    .SaveAs("path", path);
// https?://(?:www\.)?[-a-zA-Z0-9@:%._+~#=]{2,256}\.[a-z]{2,6}\b(?<=\w)(?<path>[-a-zA-Z0-9@:%_+.~#()?&/=]*)

url.FindFirst("Go to https://www.example.com/path?q=1")?.Saved("path");   // "/path?q=1"
```

### ISO date with saved parts

```csharp
var fourDigits = new TextPattern().Exactly(4).Digit();
var twoDigits = new TextPattern().Exactly(2).Digit();

var isoDate = new TextPattern()
    .SaveAs("year", fourDigits)
    .Text("-")
    .SaveAs("month", twoDigits)
    .Text("-")
    .SaveAs("day", twoDigits);
// (?<year>[0-9]{4})-(?<month>[0-9]{2})-(?<day>[0-9]{2})

var found = isoDate.FindFirst("Due on 2026-10-12.");
found?.Saved("year");    // "2026"
found?.Saved("month");   // "10"
```

### US phone number

```csharp
var usPhone = new TextPattern()
    .Optional().Text("(")
    .Exactly(3).Digit()
    .Optional().Text(")")
    .Optional().OneOfTheseCharacters("-. ")
    .Exactly(3).Digit()
    .Optional().OneOfTheseCharacters("-. ")
    .Exactly(4).Digit();
// \(?[0-9]{3}\)?[-. ]?[0-9]{3}[-. ]?[0-9]{4}

usPhone.MatchesWholeText("(555) 123-4567");   // true
usPhone.MatchesWholeText("555-1234");         // false
```

### Either-or, ignoring case

```csharp
var catsOrDogs = new TextPattern()
    .IgnoringCase()
    .StartOfWord()
    .EitherOf("cat", "dog")
    .Optional().Text("s")
    .EndOfWord();
// \b(?=\w)(?:cat|dog)s?\b(?<=\w)

catsOrDogs.IsFoundIn("I have two DOGS");   // true
catsOrDogs.IsFoundIn("concatenate");       // false
```

### Repeated words

```csharp
var aWord = new TextPattern().OneOrMore().Letter();

var repeatedWord = new TextPattern()
    .StartOfWord()
    .SaveAs("word", aWord)
    .OneOrMore().Whitespace()
    .SameTextAsSaved("word")
    .EndOfWord();
// \b(?=\w)(?<word>\p{L}+)\s+\k<word>\b(?<=\w)

repeatedWord.FindFirst("Paris in the the spring")?.Saved("word");   // "the"
```

### Password rule

```csharp
var aDigitSomewhereAhead = new TextPattern().ZeroOrMore().AnyCharacter().Digit();
var aLowercaseLetterSomewhereAhead = new TextPattern().ZeroOrMore().AnyCharacter().AnyCharacterInRange('a', 'z');

var password = new TextPattern()
    .IfFollowedBy(aDigitSomewhereAhead)
    .IfFollowedBy(aLowercaseLetterSomewhereAhead)
    .AtLeast(8).AnyCharacter();
// (?=[\s\S]*[0-9])(?=[\s\S]*[a-z])[\s\S]{8,}

password.MatchesWholeText("passw0rd");   // true
password.MatchesWholeText("password");   // false
```

### Find and replace

```csharp
var fourDigitsAndHyphen = new TextPattern().Exactly(4).Digit().Text("-");

var cardNumber = new TextPattern()
    .Exactly(3).Pattern(fourDigitsAndHyphen)
    .Exactly(4).Digit();
// (?:[0-9]{4}-){3}[0-9]{4}

cardNumber.ReplaceAll("Card 1234-5678-9012-3456 was used.", "[hidden]");   // "Card [hidden] was used."
```

## Escaping and safety

- **Text arguments are matched as written.** This applies to `Text`, `EitherOf`, the `If...` conditions,
  `OneOfTheseCharacters` and `AnyCharacterExcept`. The replacement in `ReplaceAll` is also used as written.
- **Character strings never make ranges.** A string such as `"a-z"` throws. It does not quietly mean three
  characters. For a range, use `AnyCharacterInRange` or `CharacterSet.CharactersInRange`.
- **`RawRegex` is for experts.** FluidRegex does not escape it. FluidRegex always puts it in `(?:...)`, so a `|` or an
  inline option in it cannot change the rest of the pattern.
- **Problems with a finished pattern are reported when you first use the pattern.** The message quotes the pattern.
  Examples are `SameTextAsSaved("word")` before `SaveAs("word", ...)`, a name that is never saved, and invalid raw
  regex. `ToString()` never throws.
- **Use a time limit for untrusted input.** Some patterns become very slow on some input. A time limit stops the search:
  `new TextPattern().WithTimeLimit(TimeSpan.FromSeconds(1))`.

## Unicode notes

- **`Letter()` follows Unicode.** It matches letters in every language, for example `é` and `ж`. For ASCII letters
  only, use `AnyCharacterInRange('a', 'z')`.
- **`Digit()` matches only 0 to 9.** For digits in every script, use `RawRegex(@"\d")`.
- **`StartOfWord()` and `EndOfWord()` use the .NET word characters.** These are letters, digits and the underscore,
  so `user_name` is one word.
- **Case follows `IgnoringCase()`.** With it, `UppercaseLetter()` and `LowercaseLetter()` match both cases.

## Versioning

FluidRegex is at 0.x while the English names settle, so minor versions can rename members. From 1.0.0, FluidRegex
follows semantic versioning.

## Contributing

```shell
dotnet test FluidRegex.slnx
```

The tests run on net10.0. On Windows, they also run on net48, which tests the shipped netstandard2.0 build on .NET
Framework.

## License

[MIT](https://github.com/nzmattgrant/FluidRegex/blob/master/LICENSE)
