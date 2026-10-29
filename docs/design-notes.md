# Design notes

These notes give the reasons for the shape of the FluidRegex API. For every method, see the
[API reference](https://github.com/nzmattgrant/FluidRegex/blob/master/docs/api-reference.md).

## The reader

The target reader knows a little C# and no regex, and does not read well. The test for each name is simple. When you
read the code aloud, it says what it does.

The worst mistake is code that reads correctly but does something different. Nothing warns the reader about it. Each
decision below removes one such mistake.

The decisions came from a debate between three AI reviewers in October 2026. All three agreed on the result. They
judged the code by reading it aloud. Nobody tested it with real readers.

## A count goes inside the brackets of its piece

```csharp
.Digit(Times.Exactly(4))
.Text("s", Times.Optional)
```

English puts a count before the noun ("4 digits") and also after it ("a digit, 4 times"). A reader uses both orders.
A count method before its piece, or after its piece, goes wrong without a warning when the reader uses the other order:

- With the count before the piece, `.Digit().Exactly(4).Text("-")` repeats the "-", not the digit.
- With the count after the piece, `fourDigitsAndHyphen.Times(3)` repeats only the last "-" of the stored pattern.

A count inside the brackets of its piece has no order to get wrong. It cannot reach a different piece.

Other rules for counts:

- Each count shows the word `Times`. Without it, `.Digit(Between(2, 6))` reads as "a digit with a value from 2 to 6".
  For this reason, the examples never use `using static FluidRegex.Times`.
- A count is never a bare number, because `.Digit(4)` reads as "the digit 4".
- Positions and conditions have no count argument, so the compiler rejects a count on them.
- There is no `AtMost`. "At most 3" also accepts no repeats, so an "up to 3 digits" check accepts an empty field.
  `Times.Between(0, 3)` shows the 0.

The 2018 sketch below also gave the count as an argument: `ThenMatchWord(RegexQuantifierType.MatchOneOrMoreTimes)`.

## No Match, Then or Or words

A word such as `Then` does work only when the reader can choose between two joins, for example `Then` and `Or`. An
`Or` between pieces hides how much it covers. `.Text("x").EndOfText().Or.Text(" ")` can mean "x, then the end of the
text or a space". It can also mean "(x, then the end of the text) or a space". Without `Or`, a `Then` method only
returns the same pattern. That is a word with no behavior.

`Match` as a first word clashes with `MatchesWholeText` and with the .NET `Regex.Match`.

## Either-or uses brackets

The brackets of `EitherOf(first, second, more...)` show which choices it covers. `EitherOf` needs two choices or more,
and the compiler checks this. With only one choice, `EitherOf("abc")` means the text "abc", with no warning.

`EitherOf` takes no count, because a `params` argument must come last. To repeat a choice, store it in a pattern and
use `Pattern(choice, Times.OneOrMore)`.

To choose between a position and text, give each choice a name:

```csharp
var theEndOfText = new TextPattern().EndOfText();
var aSpace = new TextPattern().Text(" ");

var abcThenX = new TextPattern()
    .OneOfTheseCharacters("abc")
    .Text("x")
    .EitherOf(theEndOfText, aSpace);
```

## Condition names say where they look

`TextBeforeHereEndsWith("$")` checks the text before the place where it is written. A name such as
`IfPrecededBy("$")` reads correctly in the wrong place: `.Digit(Times.OneOrMore).IfPrecededBy("$")` can never match,
because a digit comes just before that place. `TextBeforeHereEndsWith` sounds wrong in that place, so the reader sees
the mistake.

The words `StartsWith` and `EndsWith` are familiar to a C# reader from `string`.

## SaveTextAs

A name such as `SaveAs` reads like the Save As command in an app, which stores the thing itself. A reader then expects
`SameTextAsSaved("year")` to match any four digits. `SaveTextAs` says that the found text is saved. Then the reader
expects `SameTextAsSaved` to match that same text again, which is what it does.

## Known costs

- `.Text("(", Times.Optional)` reads less well than a count-first `.Optional().Text("(")`.
- "The end of the text or a space" needs two named patterns.
- `Times.Exactly(4).AsFewAsPossible()` compiles, and throws when it runs.

## The 2018 sketch

The first sketch of the API, kept as history. The idea of passing reusable sub-patterns into `Group(...)` instead of
start/end calls started here.

```text
translated to fluid C# email matcher
^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$

DefineRegexAs()
.MatchFromStartAndEnd()
.ThenMatchWord(RegexQuantifierType.MatchOneOrMoreTimes)
.ThenStartGroup()
.ThenMatchCustom("-+.'")
.ThenMatchWord(RegexQuantifierType.MatchOneOrMoreTimes)
.EndGroup()
.ThenCreateGroup(preMadeRegexGroupWithCatchyName)

Note sequence is super important here
It is hard to deal with groups in a readable way because you have to start then end
Maybe i should just pass in a regex group that way groups could be reusable as well

MatchStringStart()
.matchAnythingButWhiteSpace
.matchGroup(
	DefineRegexGroupAs blah, RegexQuantifierType.MatchManyTimes
)
.matchTheCharacter("@")
.matchAnythingButWhitespace
.matchGroup(DefineRegexGroupAs()
	.matchOneOfTheseCharacters("-",".")
	.matchAnythingButWhitespace(), 
	RegexQuantifierType.MatchManyTimes
)
```
