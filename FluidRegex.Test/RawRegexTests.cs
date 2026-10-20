using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class RawRegexTests
{
    [TestMethod]
    public void Raw_Regex_Is_Always_Wrapped()
    {
        Assert.AreEqual(@"(?:\d{3})", new MatchThesePatterns().RawRegex(@"\d{3}").ToString());
        Assert.AreEqual(@"(?:\p{IsGreek})+", new MatchThesePatterns().RawRegex(@"\p{IsGreek}", OneOrMore).ToString());
    }

    [TestMethod]
    public void An_Alternation_Cannot_Leak_Out()
    {
        var builder = new MatchThesePatterns().StartOfText().RawRegex("cat|dog").EndOfText();
        Assert.IsTrue(builder.IsMatch("dog"));
        Assert.IsFalse(builder.IsMatch("xdog"));
    }

    [TestMethod]
    public void An_Inline_Option_Cannot_Leak_Out()
    {
        var builder = new MatchThesePatterns().StartOfText().RawRegex("(?i)abc").TheText("def").EndOfText();
        Assert.IsTrue(builder.IsMatch("ABCdef"));
        Assert.IsFalse(builder.IsMatch("ABCDEF"));
    }

    [TestMethod]
    public void A_Half_Written_Count_Cannot_Join_The_Next_Piece()
    {
        var builder = new MatchThesePatterns().StartOfText().RawRegex("a{2").TheCharacter('}').EndOfText();
        Assert.IsTrue(builder.IsMatch("a{2}"));
        Assert.IsFalse(builder.IsMatch("aa"));
    }

    [TestMethod]
    public void Raw_Regex_Can_Refer_To_An_Outer_Capture()
    {
        var builder = new MatchThesePatterns()
            .CaptureAs("w", new SubPattern().TheCharacter('a'))
            .RawRegex(@"\k<w>");
        Assert.IsTrue(builder.IsMatch("aa"));
    }

    [TestMethod]
    public void Invalid_Raw_Regex_Is_Reported_By_ToRegex_With_The_Pattern()
    {
        var builder = new MatchThesePatterns().TheText("x").RawRegex("(");
        Assert.AreEqual("x(?:()", builder.ToString(), "ToString never throws");
        var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.ToRegex());
        StringAssert.Contains(exception.Message, "x(?:()");
        Assert.IsNotNull(exception.InnerException);
    }

    [TestMethod]
    public void Null_Or_Empty_Raw_Regex_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().RawRegex(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().RawRegex(""));
    }
}
