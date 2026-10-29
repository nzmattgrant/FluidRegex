# API reference

This page lists every public member of FluidRegex. For a short introduction, see the
[README](https://github.com/nzmattgrant/FluidRegex/blob/master/README.md). For the reasons behind the names, see the
[design notes](https://github.com/nzmattgrant/FluidRegex/blob/master/docs/design-notes.md).

There are four public types:

- `TextPattern` describes the text to find.
- `Times` is how many times a piece is found.
- `CharacterSet` is a reusable set of characters.
- `FoundText` is one place where a pattern was found.

## TextPattern

A `TextPattern` describes the text to find, one piece at a time, in reading order. Each call gives a new pattern.
The original pattern does not change, so you can store a pattern and use it again, also across threads.

### Start

| Member | What it does |
| --- | --- |
| `TextPattern()` | Starts an empty pattern. |

### Pieces

A piece finds characters. Each piece except `EitherOf` takes a [count](#times) as its last argument. Without a count,
the piece is found one time. A count covers the whole piece: `Text("ab", Times.OneOrMore)` emits `(?:ab)+`.

| Member | Finds | Example | Emits |
| --- | --- | --- | --- |
| `Text(string text, Times times = default)` | the text, matched as written | `Text("www.")` | `www\.` |
| `AnyCharacter(Times times = default)` | any character, also a line break | `AnyCharacter()` | `[\s\S]` |
| `AnyCharacterExceptLineBreak(Times times = default)` | any character except `\r` and `\n` | `AnyCharacterExceptLineBreak()` | `[^\r\n]` |
| `Digit(Times times = default)` | a digit from 0 to 9 | `Digit(Times.Exactly(4))` | `[0-9]{4}` |
| `AnyCharacterExceptDigit(Times times = default)` | any character except a digit from 0 to 9 | `AnyCharacterExceptDigit()` | `[^0-9]` |
| `Letter(Times times = default)` | a letter in any language, for example `a`, `É` or `ж` | `Letter(Times.OneOrMore)` | `\p{L}+` |
| `AnyCharacterExceptLetter(Times times = default)` | any character except a letter | `AnyCharacterExceptLetter()` | `\P{L}` |
| `UppercaseLetter(Times times = default)` | an uppercase letter (with `IgnoringCase()`, also a lowercase letter) | `UppercaseLetter()` | `\p{Lu}` |
| `LowercaseLetter(Times times = default)` | a lowercase letter (with `IgnoringCase()`, also an uppercase letter) | `LowercaseLetter()` | `\p{Ll}` |
| `LetterOrDigit(Times times = default)` | a letter in any language, or a digit from 0 to 9 | `LetterOrDigit()` | `[\p{L}0-9]` |
| `Whitespace(Times times = default)` | a space, tab, line break or other blank character | `Whitespace(Times.OneOrMore)` | `\s+` |
| `AnyCharacterExceptWhitespace(Times times = default)` | any character except whitespace | `AnyCharacterExceptWhitespace()` | `\S` |
| `LineBreak(Times times = default)` | one line break: `\r\n`, `\n` or `\r` | `LineBreak()` | `(?:\r\n\|\r\|\n)` |
| `OneOfTheseCharacters(string characters, Times times = default)` | one of these characters, each matched as written | `OneOfTheseCharacters("-+.'")` | `[-+.']` |
| `OneOfTheseCharacters(CharacterSet characters, Times times = default)` | one of the characters in a `CharacterSet` | `OneOfTheseCharacters(hostCharacters)` | `[...]` |
| `AnyCharacterExcept(string characters, Times times = default)` | any character except these characters | `AnyCharacterExcept("<>")` | `[^<>]` |
| `AnyCharacterExcept(CharacterSet characters, Times times = default)` | any character except the characters in a `CharacterSet` | `AnyCharacterExcept(separators)` | `[^...]` |
| `AnyCharacterInRange(char from, char to, Times times = default)` | any character from `from` to `to` | `AnyCharacterInRange('a', 'z')` | `[a-z]` |
| `EitherOf(string first, string second, params string[] more)` | one of two texts or more, each matched as written | `EitherOf("cat", "dog")` | `(?:cat\|dog)` |
| `EitherOf(TextPattern first, TextPattern second, params TextPattern[] more)` | what one of two patterns or more finds | `EitherOf(theEndOfText, aSpace)` | `(?:\z\|\ )` |
| `Pattern(TextPattern pattern, Times times = default)` | what a pattern that you made before finds | `Pattern(catOrDog, Times.OneOrMore)` | `(?:cat\|dog)+` |
| `SaveTextAs(string name, TextPattern pattern, Times times = default)` | what the pattern finds, and saves that text under the name | `SaveTextAs("year", fourDigits)` | `(?<year>[0-9]{4})` |
| `SameTextAsSaved(string name, Times times = default)` | the same text that `SaveTextAs` saved under the name | `SameTextAsSaved("word")` | `\k<word>` |
| `RawRegex(string regexPattern, Times times = default)` | raw .NET regex, for experts | `RawRegex(@"\p{IsGreek}")` | `(?:\p{IsGreek})` |

Notes on the pieces:

- `Text`, `EitherOf`, `OneOfTheseCharacters` and `AnyCharacterExcept` match their text as written. You do not escape
  anything.
- A string of characters never makes a range. `OneOfTheseCharacters("a-z")` throws. For a range, use
  `AnyCharacterInRange` or `CharacterSet.CharactersInRange`.
- `EitherOf` takes no count. To repeat a choice, put it in a pattern first. Then give the count to `Pattern`.
- With a count, `SaveTextAs` saves all the repeats: `SaveTextAs("number", digit, Times.OneOrMore)` emits
  `(?<number>[0-9]+)`.
- `RawRegex` is not escaped. FluidRegex puts it in `(?:...)`, so a `|` or an inline option in it cannot change the
  rest of the pattern.

### Positions

A position matches a place in the text, not characters. A position takes no count.

| Member | Matches | Emits |
| --- | --- | --- |
| `StartOfText()` | at the start of the text | `\A` |
| `EndOfText()` | at the end of the text (a line break at the end is not accepted) | `\z` |
| `StartOfLine()` | at the start of the text, or just after `\r\n`, `\n` or `\r` | `(?<![^\r\n])(?!(?<=\r)\n)` |
| `EndOfLine()` | just before `\r\n`, `\n` or `\r`, or at the end of the text, but never between `\r` and `\n` | `(?![^\r\n])(?!(?<=\r)\n)` |
| `StartOfWord()` | where a word starts (a word is letters, digits and `_`) | `\b(?=\w)` |
| `EndOfWord()` | where a word ends | `\b(?<=\w)` |

### Conditions

A condition checks the text around a place, and adds nothing to the found text. A condition takes no count. Write
the condition at the place that it checks: `TextBeforeHereEndsWith("$").Digit(Times.OneOrMore)` emits
`(?<=\$)[0-9]+`.

| Member | Matches | Emits |
| --- | --- | --- |
| `TextAfterHereStartsWith(string text)` | only if the text after this place starts with the text | `(?=...)` |
| `TextAfterHereStartsWith(TextPattern pattern)` | only if the text after this place starts with what the pattern finds | `(?=...)` |
| `TextAfterHereDoesNotStartWith(string text)` | only if the text after this place does not start with the text | `(?!...)` |
| `TextAfterHereDoesNotStartWith(TextPattern pattern)` | only if the text after this place does not start with what the pattern finds | `(?!...)` |
| `TextBeforeHereEndsWith(string text)` | only if the text before this place ends with the text | `(?<=...)` |
| `TextBeforeHereEndsWith(TextPattern pattern)` | only if the text before this place ends with what the pattern finds | `(?<=...)` |
| `TextBeforeHereDoesNotEndWith(string text)` | only if the text before this place does not end with the text | `(?<!...)` |
| `TextBeforeHereDoesNotEndWith(TextPattern pattern)` | only if the text before this place does not end with what the pattern finds | `(?<!...)` |

### Settings

| Member | What it does |
| --- | --- |
| `IgnoringCase()` | Treats uppercase and lowercase as the same. If you use the pattern inside another pattern, only its own part ignores case. |
| `WithTimeLimit(TimeSpan timeLimit)` | Stops a search that takes longer than the time limit, with a `RegexMatchTimeoutException`. Use it for untrusted input. |

### Results

| Member | Gives |
| --- | --- |
| `MatchesWholeText(string text)` | `true` if all of the text fits the pattern. Use it to check input, for example an email address in a form. |
| `IsFoundIn(string text)` | `true` if the pattern is found somewhere in the text. |
| `FindFirst(string text)` | the first `FoundText`, or `null`. |
| `FindAll(string text)` | each `FoundText`, in order. The list is empty if the pattern is not found. |
| `ReplaceAll(string text, string replacement)` | the text, with the replacement in each place that the pattern found. The replacement is used as written. |
| `ToRegex()` | the .NET `Regex`, for everything else. FluidRegex builds it one time and keeps it. |
| `ToString()` | the regex pattern text. It never throws. |

A problem with a finished pattern throws an `ArgumentException` when you first use the pattern. The message quotes the
pattern. An example is `SameTextAsSaved("word")` before `SaveTextAs("word", ...)`.

## Times

A `Times` value is how many times a piece is found. Give it as the last argument of the piece. Always write `Times.`,
so that the count does not look like a value of the piece.

| Member | The piece is found | Emits |
| --- | --- | --- |
| `Times.Optional` | one time, or not at all | `?` |
| `Times.ZeroOrMore` | any number of times, or not at all | `*` |
| `Times.OneOrMore` | one time or more | `+` |
| `Times.Exactly(int count)` | exactly `count` times (`count` is 1 or more) | `{4}` |
| `Times.AtLeast(int minimum)` | `minimum` times or more | `{8,}` |
| `Times.Between(int minimum, int maximum)` | from `minimum` to `maximum` times (for "up to 3 times", write `Times.Between(0, 3)`) | `{2,6}` |
| `AsFewAsPossible()` | the same count, but as few times as possible (a fixed count such as `Times.Exactly(4)` throws) | `+?` |
| `ToString()` | gives the regex count text, for example `{4}`, or an empty string for one time | `{4}` |

A count finds as many repeats as possible. In `<a><b>`,
`Text("<").AnyCharacter(Times.OneOrMore.AsFewAsPossible()).Text(">")` finds `<a>`. Without `AsFewAsPossible()`, it
finds all of `<a><b>`.

## CharacterSet

A `CharacterSet` is a reusable set of characters. Give it to `OneOfTheseCharacters` or `AnyCharacterExcept`. Each call
gives a new set. The original set does not change.

| Member | What it does | Adds |
| --- | --- | --- |
| `CharacterSet()` | Starts an empty set. An empty set throws when you give it to a pattern. | |
| `Characters(string characters)` | Adds each character, matched as written. A range look-alike such as `"a-z"` throws. | `-._` |
| `CharactersInRange(char from, char to)` | Adds each character from `from` to `to`. | `a-z` |
| `Letters()` | Adds each letter in any language. | `\p{L}` |
| `UppercaseLetters()` | Adds each uppercase letter. | `\p{Lu}` |
| `LowercaseLetters()` | Adds each lowercase letter. | `\p{Ll}` |
| `Digits()` | Adds the digits 0 to 9. | `0-9` |
| `Whitespace()` | Adds each whitespace character. | `\s` |
| `ToString()` | Gives the set as regex text, for example `[-a-z0-9._]`. | |

```csharp
var hostCharacters = new CharacterSet()
    .CharactersInRange('a', 'z').Digits()
    .Characters("-._");
// [-a-z0-9._]
```

## FoundText

A `FoundText` is one place where a pattern was found. `FindFirst` and `FindAll` give it.

| Member | Gives |
| --- | --- |
| `Text` | the text that was found. |
| `Position` | where the found text starts in the searched text. The first character is position 0. |
| `Saved(string name)` | the text that `SaveTextAs` saved under the name. It is empty if that part was optional and missing. It throws if the pattern saves nothing under the name. |
| `ToString()` | the same as `Text`. |
