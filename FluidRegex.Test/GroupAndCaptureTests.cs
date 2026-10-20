using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class GroupAndCaptureTests
{
    private static readonly SubPattern Digit = new SubPattern().AnyDigit();
    private static readonly SubPattern Ab = new SubPattern().TheText("ab");
    private static readonly SubPattern CatOrDog = new SubPattern().EitherOf("cat", "dog");

    [TestMethod]
    public void An_Uncounted_Group_Inlines_Its_Content()
    {
        Assert.AreEqual(@"x\dy", new MatchThesePatterns().TheCharacter('x').Group(new SubPattern().AnyDigit().TheCharacter('y')).ToString());
    }

    [TestMethod]
    public void Group_Does_Not_Capture()
    {
        var builder = new MatchThesePatterns().Group(Ab, OneOrMore);
        Assert.AreEqual("(?:ab)+", builder.ToString());
        Assert.AreEqual(1, builder.ToRegex().Match("abab").Groups.Count, "only group 0, the whole match");
    }

    [TestMethod]
    public void Capture_Puts_The_Count_Inside_So_It_Holds_The_Whole_Run()
    {
        var digits = new MatchThesePatterns().Capture(Digit, OneOrMore);
        Assert.AreEqual(@"(\d+)", digits.ToString());
        Assert.AreEqual("123", digits.ToRegex().Match("a123").Groups[1].Value);
    }

    [TestMethod]
    public void An_Optional_Capture_Reports_No_Success_When_Missing()
    {
        var builder = new MatchThesePatterns().TheCharacter('x').Capture(Ab, ZeroOrMore);
        Assert.AreEqual("x((?:ab)+)?", builder.ToString());
        Assert.IsFalse(builder.ToRegex().Match("x").Groups[1].Success);
        Assert.AreEqual("abab", builder.ToRegex().Match("xabab").Groups[1].Value);

        Assert.AreEqual(@"(www\.)?", new MatchThesePatterns().Capture(new SubPattern().TheText("www."), Optional).ToString());
        Assert.AreEqual("((?:ab){1,5})?", new MatchThesePatterns().Capture(Ab, AtMost(5)).ToString());
        Assert.AreEqual("((?:ab)+?)??", new MatchThesePatterns().Capture(Ab, ZeroOrMore.AsFewAsPossible()).ToString());
    }

    [TestMethod]
    public void A_Lone_Either_Or_In_A_Capture_Is_Not_Double_Wrapped()
    {
        Assert.AreEqual("(cat|dog)", new MatchThesePatterns().Capture(CatOrDog).ToString());

        var repeated = new MatchThesePatterns().Capture(CatOrDog, OneOrMore);
        Assert.AreEqual("((?:cat|dog)+)", repeated.ToString());
        Assert.AreEqual("catdog", repeated.ToRegex().Match("catdog").Groups[1].Value);
    }

    [TestMethod]
    public void CaptureAs_Names_The_Capture()
    {
        var year = new MatchThesePatterns().CaptureAs("year", new SubPattern().AnyDigit(Exactly(4)));
        Assert.AreEqual(@"(?<year>\d{4})", year.ToString());
        Assert.AreEqual("2026", year.ToRegex().Match("in 2026").Groups["year"].Value);
        Assert.AreEqual(@"(?<year>\d{4})", new MatchThesePatterns().CaptureAs("year", Digit, Exactly(4)).ToString());
    }

    [TestMethod]
    public void Capture_Names_Are_Checked()
    {
        foreach (var name in new[] { "year", "_x", "é1" })
        {
            Assert.AreEqual($"(?<{name}>a)", new MatchThesePatterns().CaptureAs(name, new SubPattern().TheCharacter('a')).ToString());
        }

        foreach (var name in new[] { "1a", "my name", "a-b" })
        {
            Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().CaptureAs(name, Ab)).ParamName);
        }

        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().CaptureAs("", Ab));
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().CaptureAs(null!, Ab));
    }

    [TestMethod]
    public void Unnamed_Captures_Are_Numbered_Before_Named_Ones()
    {
        var builder = new MatchThesePatterns()
            .CaptureAs("named", new SubPattern().TheCharacter('x'))
            .Capture(new SubPattern().TheCharacter('y'));
        Assert.AreEqual("y", builder.ToRegex().Match("xy").Groups[1].Value);
    }

    [TestMethod]
    public void Sub_Patterns_Nest()
    {
        var inner = new SubPattern().AnyDigit(OneOrMore);
        var middle = new SubPattern().TheCharacter('[').Capture(inner).TheCharacter(']');
        var outer = new SubPattern().Group(middle, OneOrMore);
        var builder = new MatchThesePatterns().StartOfText().Group(outer).EndOfText();

        Assert.AreEqual(@"\A(?:\[(\d+)])+\z", builder.ToString());
        Assert.IsTrue(builder.IsMatch("[1][22]"));
        Assert.AreEqual("22", builder.ToRegex().Match("[1][22]").Groups[1].Value);
    }

    [TestMethod]
    public void Counting_A_Sub_Pattern_Of_Only_Positions_Throws()
    {
        var boundary = new SubPattern().WordBoundary();
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().Group(boundary, OneOrMore));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().Capture(boundary, Optional));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().CaptureAs("b", boundary, ZeroOrMore));
        Assert.AreEqual(@"\b", new MatchThesePatterns().Group(boundary).ToString(), "without a count it is fine");
    }

    [TestMethod]
    public void A_Capture_That_Can_Never_Match_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().Capture(Ab, Exactly(0)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().Capture(Ab, AtMost(0)));
    }

    [TestMethod]
    public void Empty_Sub_Patterns_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().Group(new SubPattern()));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().Capture(new SubPattern()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().Group(null!));
    }
}
