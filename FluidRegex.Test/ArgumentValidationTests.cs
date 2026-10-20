using System.Collections.Generic;
using System.Text.RegularExpressions;
using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

/// <summary>Every invalid argument throws an ArgumentException subtype naming the parameter, at the call that received it.</summary>
[TestClass]
public class ArgumentValidationTests
{
    private static readonly MatchThesePatterns Start = new();
    private static readonly SubPattern Empty = new();
    private static readonly SubPattern Boundary = new SubPattern().WordBoundary();
    private static readonly SubPattern Ab = new SubPattern().TheText("ab");

    public static IEnumerable<object[]> InvalidCalls()
    {
        yield return Row("TheText(null)", () => Start.TheText(null!), typeof(ArgumentNullException), "text");
        yield return Row("TheText(\"\")", () => Start.TheText(""), typeof(ArgumentException), "text");
        yield return Row("OneOfTheseCharacters(\"\")", () => Start.OneOfTheseCharacters(""), typeof(ArgumentException), "characters");
        yield return Row("OneOfTheseCharacters(\"a-z\")", () => Start.OneOfTheseCharacters("a-z"), typeof(ArgumentException), "characters");
        yield return Row("OneOfTheseCharacters((string)null)", () => Start.OneOfTheseCharacters((string)null!), typeof(ArgumentNullException), "characters");
        yield return Row("OneOfTheseCharacters(empty set)", () => Start.OneOfTheseCharacters(new CharacterSet()), typeof(ArgumentException), "characters");
        yield return Row("AnyCharacterExcept((CharacterSet)null)", () => Start.AnyCharacterExcept((CharacterSet)null!), typeof(ArgumentNullException), "characters");
        yield return Row("AnyCharacterInRange('z', 'a')", () => Start.AnyCharacterInRange('z', 'a'), typeof(ArgumentOutOfRangeException), "to");
        yield return Row("CharactersInRange('z', 'a')", () => new CharacterSet().CharactersInRange('z', 'a'), typeof(ArgumentOutOfRangeException), "to");
        yield return Row("TheCharacters(\"0-9\")", () => new CharacterSet().TheCharacters("0-9"), typeof(ArgumentException), "characters");
        yield return Row("Group(null)", () => Start.Group(null!), typeof(ArgumentNullException), "pattern");
        yield return Row("Group(empty)", () => Start.Group(Empty), typeof(ArgumentException), "pattern");
        yield return Row("Group(boundary, OneOrMore)", () => Start.Group(Boundary, OneOrMore), typeof(ArgumentException), "times");
        yield return Row("Capture(ab, Exactly(0))", () => Start.Capture(Ab, Exactly(0)), typeof(ArgumentOutOfRangeException), "times");
        yield return Row("CaptureAs(\"1a\", ab)", () => Start.CaptureAs("1a", Ab), typeof(ArgumentException), "name");
        yield return Row("CaptureAs(\"a-b\", ab)", () => Start.CaptureAs("a-b", Ab), typeof(ArgumentException), "name");
        yield return Row("EitherOf(\"a\", \"\")", () => Start.EitherOf("a", ""), typeof(ArgumentException), "texts");
        yield return Row("EitherOf(ab, empty)", () => Start.EitherOf(Ab, Empty), typeof(ArgumentException), "patterns");
        yield return Row("TheSameTextAsCapture(0)", () => Start.TheSameTextAsCapture(0), typeof(ArgumentOutOfRangeException), "captureNumber");
        yield return Row("TheSameTextAsCapture(\"my name\")", () => Start.TheSameTextAsCapture("my name"), typeof(ArgumentException), "captureName");
        yield return Row("IfFollowedBy(\"\")", () => Start.IfFollowedBy(""), typeof(ArgumentException), "text");
        yield return Row("IfPrecededBy(empty)", () => Start.IfPrecededBy(Empty), typeof(ArgumentException), "pattern");
        yield return Row("RawRegex(\"\")", () => Start.RawRegex(""), typeof(ArgumentException), "regexPattern");
        yield return Row("WithOptions(Singleline)", () => Start.WithOptions(RegexOptions.Singleline), typeof(ArgumentException), "options");
        yield return Row("WithMatchTimeout(0)", () => Start.WithMatchTimeout(TimeSpan.Zero), typeof(ArgumentOutOfRangeException), "timeout");
        yield return Row("Between(3, 2)", () => Between(3, 2), typeof(ArgumentOutOfRangeException), "maximum");
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
