# Early design notes

The original 2018 sketch of the API, kept as history. The idea of passing reusable sub-patterns into `Group(...)` instead of start/end calls started here. For the current API see the [README](https://github.com/nzmattgrant/FluidRegex/blob/master/README.md).

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
