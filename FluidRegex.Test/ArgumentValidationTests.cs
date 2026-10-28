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
        yield return Row("OneOrMore().Pattern(boundary)", () => Start.OneOrMore().Pattern(Boundary), typeof(ArgumentException), "pattern");
        yield return Row("SaveAs(\"1a\", ab)", () => Start.SaveAs("1a", Ab), typeof(ArgumentException), "name");
        yield return Row("SaveAs(\"a-b\", ab)", () => Start.SaveAs("a-b", Ab), typeof(ArgumentException), "name");
        yield return Row("EitherOf(\"a\", \"\")", () => Start.EitherOf("a", ""), typeof(ArgumentException), "texts");
        yield return Row("EitherOf(ab, empty)", () => Start.EitherOf(Ab, Empty), typeof(ArgumentException), "patterns");
        yield return Row("OneOrMore().EitherOf(boundary, boundary)", () => Start.OneOrMore().EitherOf(Boundary, Boundary), typeof(ArgumentException), "patterns");
        yield return Row("SameTextAsSaved(\"my name\")", () => Start.SameTextAsSaved("my name"), typeof(ArgumentException), "name");
        yield return Row("IfFollowedBy(\"\")", () => Start.IfFollowedBy(""), typeof(ArgumentException), "text");
        yield return Row("IfPrecededBy(empty)", () => Start.IfPrecededBy(Empty), typeof(ArgumentException), "pattern");
        yield return Row("RawRegex(\"\")", () => Start.RawRegex(""), typeof(ArgumentException), "regexPattern");
        yield return Row("WithTimeLimit(0)", () => Start.WithTimeLimit(TimeSpan.Zero), typeof(ArgumentOutOfRangeException), "timeLimit");
        yield return Row("Exactly(0)", () => Start.Exactly(0), typeof(ArgumentOutOfRangeException), "count");
        yield return Row("AtLeast(-1)", () => Start.AtLeast(-1), typeof(ArgumentOutOfRangeException), "minimum");
        yield return Row("AtMost(0)", () => Start.AtMost(0), typeof(ArgumentOutOfRangeException), "maximum");
        yield return Row("Between(-1, 2)", () => Start.Between(-1, 2), typeof(ArgumentOutOfRangeException), "minimum");
        yield return Row("Between(3, 2)", () => Start.Between(3, 2), typeof(ArgumentOutOfRangeException), "maximum");
        yield return Row("Between(0, 0)", () => Start.Between(0, 0), typeof(ArgumentOutOfRangeException), "maximum");
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
