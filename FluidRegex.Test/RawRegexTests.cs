namespace FluidRegex.Test;

[TestClass]
public class RawRegexTests
{
    [TestMethod]
    public void Raw_Regex_Is_Always_Wrapped()
    {
        Assert.AreEqual(@"(?:\d{3})", new TextPattern().RawRegex(@"\d{3}").ToString());
        Assert.AreEqual(@"(?:\p{IsGreek})+", new TextPattern().OneOrMore().RawRegex(@"\p{IsGreek}").ToString());
    }

    [TestMethod]
    public void An_Alternation_Cannot_Leak_Out()
    {
        var builder = new TextPattern().StartOfText().RawRegex("cat|dog").EndOfText();
        Assert.IsTrue(builder.IsFoundIn("dog"));
        Assert.IsFalse(builder.IsFoundIn("xdog"));
    }

    [TestMethod]
    public void An_Inline_Option_Cannot_Leak_Out()
    {
        var builder = new TextPattern().RawRegex("(?i)abc").Text("def");
        Assert.IsTrue(builder.MatchesWholeText("ABCdef"));
        Assert.IsFalse(builder.MatchesWholeText("ABCDEF"));
    }

    [TestMethod]
    public void A_Half_Written_Count_Cannot_Join_The_Next_Piece()
    {
        var builder = new TextPattern().RawRegex("a{2").Text("}");
        Assert.IsTrue(builder.MatchesWholeText("a{2}"));
        Assert.IsFalse(builder.MatchesWholeText("aa"));
    }

    [TestMethod]
    public void Raw_Regex_Can_Refer_To_Saved_Text()
    {
        var builder = new TextPattern()
            .SaveAs("w", new TextPattern().Text("a"))
            .RawRegex(@"\k<w>");
        Assert.IsTrue(builder.IsFoundIn("aa"));
    }

    [TestMethod]
    public void Invalid_Raw_Regex_Is_Reported_With_The_Pattern()
    {
        var builder = new TextPattern().Text("x").RawRegex("(");
        Assert.AreEqual("x(?:()", builder.ToString(), "ToString never throws");
        var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.ToRegex());
        StringAssert.Contains(exception.Message, "x(?:()");
        Assert.IsNotNull(exception.InnerException);
        Assert.ThrowsExactly<ArgumentException>(() => builder.IsFoundIn("x"));
    }

    [TestMethod]
    public void Null_Or_Empty_Raw_Regex_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().RawRegex(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().RawRegex(""));
    }
}
