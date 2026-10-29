using System.Collections.Generic;

namespace FluidRegex.Test;

/// <summary>Every invalid argument throws an ArgumentException subtype naming the parameter, at the call that received it.</summary>
[TestClass]
public class ArgumentValidationTests
{
    private static readonly TextPattern Start = new();
    private static readonly TextPattern Empty = new();
    private static readonly TextPattern Boundary = new TextPattern().StartOfWord();
    private static readonly TextPattern Ab = new TextPattern().Text("ab");

    public static IEnumerable<object[]> InvalidCalls()
    {
        yield return Row("Text(null)", () => Start.Text(null!), typeof(ArgumentNullException), "text");
        yield return Row("Text(\"\")", () => Start.Text(""), typeof(ArgumentException), "text");
        yield return Row("OneOfTheseCharacters(\"\")", () => Start.OneOfTheseCharacters(""), typeof(ArgumentException), "characters");
        yield return Row("OneOfTheseCharacters(\"a-z\")", () => Start.OneOfTheseCharacters("a-z"), typeof(ArgumentException), "characters");
        yield return Row("OneOfTheseCharacters((string)null)", () => Start.OneOfTheseCharacters((string)null!), typeof(ArgumentNullException), "characters");
        yield return Row("OneOfTheseCharacters(empty set)", () => Start.OneOfTheseCharacters(new CharacterSet()), typeof(ArgumentException), "characters");
        yield return Row("AnyCharacterExcept((CharacterSet)null)", () => Start.AnyCharacterExcept((CharacterSet)null!), typeof(ArgumentNullException), "characters");
        yield return Row("AnyCharacterInRange('z', 'a')", () => Start.AnyCharacterInRange('z', 'a'), typeof(ArgumentOutOfRangeException), "to");
        yield return Row("CharactersInRange('z', 'a')", () => new CharacterSet().CharactersInRange('z', 'a'), typeof(ArgumentOutOfRangeException), "to");
        yield return Row("Characters(\"0-9\")", () => new CharacterSet().Characters("0-9"), typeof(ArgumentException), "characters");
        yield return Row("Pattern(null)", () => Start.Pattern(null!), typeof(ArgumentNullException), "pattern");
        yield return Row("Pattern(empty)", () => Start.Pattern(Empty), typeof(ArgumentException), "pattern");
        yield return Row("Pattern(boundary, Times.OneOrMore)", () => Start.Pattern(Boundary, Times.OneOrMore), typeof(ArgumentException), "pattern");
        yield return Row("SaveTextAs(\"1a\", ab)", () => Start.SaveTextAs("1a", Ab), typeof(ArgumentException), "name");
        yield return Row("SaveTextAs(\"a-b\", ab)", () => Start.SaveTextAs("a-b", Ab), typeof(ArgumentException), "name");
        yield return Row("EitherOf(\"a\", \"\")", () => Start.EitherOf("a", ""), typeof(ArgumentException), "second");
        yield return Row("EitherOf(ab, empty)", () => Start.EitherOf(Ab, Empty), typeof(ArgumentException), "second");
        yield return Row("EitherOf(\"a\", \"b\", \"\")", () => Start.EitherOf("a", "b", ""), typeof(ArgumentException), "more");
        yield return Row("EitherOf(\"a\", \"b\", null)", () => Start.EitherOf("a", "b", (string[])null!), typeof(ArgumentNullException), "more");
        yield return Row("SameTextAsSaved(\"my name\")", () => Start.SameTextAsSaved("my name"), typeof(ArgumentException), "name");
        yield return Row("TextAfterHereStartsWith(\"\")", () => Start.TextAfterHereStartsWith(""), typeof(ArgumentException), "text");
        yield return Row("TextBeforeHereEndsWith(empty)", () => Start.TextBeforeHereEndsWith(Empty), typeof(ArgumentException), "pattern");
        yield return Row("RawRegex(\"\")", () => Start.RawRegex(""), typeof(ArgumentException), "regexPattern");
        yield return Row("WithTimeLimit(0)", () => Start.WithTimeLimit(TimeSpan.Zero), typeof(ArgumentOutOfRangeException), "timeLimit");
        yield return Row("Times.Exactly(0)", () => Times.Exactly(0), typeof(ArgumentOutOfRangeException), "count");
        yield return Row("Times.AtLeast(-1)", () => Times.AtLeast(-1), typeof(ArgumentOutOfRangeException), "minimum");
        yield return Row("Times.Between(-1, 2)", () => Times.Between(-1, 2), typeof(ArgumentOutOfRangeException), "minimum");
        yield return Row("Times.Between(3, 2)", () => Times.Between(3, 2), typeof(ArgumentOutOfRangeException), "maximum");
        yield return Row("Times.Between(0, 0)", () => Times.Between(0, 0), typeof(ArgumentOutOfRangeException), "maximum");
        yield return Row("FindFirst(null)", () => Ab.FindFirst(null!), typeof(ArgumentNullException), "text");
        yield return Row("ReplaceAll(\"ab\", null)", () => Ab.ReplaceAll("ab", null!), typeof(ArgumentNullException), "replacement");
    }

    [TestMethod]
    [DynamicData(nameof(InvalidCalls))]
    public void Invalid_Arguments_Throw_And_Name_The_Parameter(string call, Action action, Type expectedType, string expectedParamName)
    {
        try
        {
            action();
        }
        catch (ArgumentException exception)
        {
            Assert.AreEqual(expectedType, exception.GetType(), call);
            Assert.AreEqual(expectedParamName, exception.ParamName, call);
            return;
        }

        Assert.Fail($"{call} did not throw.");
    }

    private static object[] Row(string call, Action action, Type expectedType, string expectedParamName) =>
        [call, action, expectedType, expectedParamName];

    private static object[] Row<T>(string call, Func<T> action, Type expectedType, string expectedParamName) =>
        Row(call, () => { _ = action(); }, expectedType, expectedParamName);
}
