namespace FluidRegex.Test;

[TestClass]
public class BackreferenceTests
{
    private static readonly TextPattern AWord = new TextPattern().Letter(Times.OneOrMore);
    private static readonly TextPattern A = new TextPattern().Text("a");

    [TestMethod]
    public void Saved_Text_Can_Be_Matched_Again()
    {
        var repeatedWord = new TextPattern()
            .StartOfWord()
            .SaveTextAs("word", AWord)
            .Whitespace(Times.OneOrMore)
            .SameTextAsSaved("word")
            .EndOfWord();

        Assert.AreEqual(@"\b(?=\w)(?<word>\p{L}+)\s+\k<word>\b(?<=\w)", repeatedWord.ToString());
        Assert.AreEqual("is", repeatedWord.FindFirst("this is is a test")!.Saved("word"));
        Assert.IsFalse(repeatedWord.IsFoundIn("the theory"));
    }

    [TestMethod]
    public void A_Reference_Can_Be_Counted()
    {
        Assert.AreEqual(@"\k<w>{2}", new TextPattern().SameTextAsSaved("w", Times.Exactly(2)).ToString());
        Assert.IsTrue(new TextPattern().SaveTextAs("w", A).SameTextAsSaved("w", Times.Exactly(2)).MatchesWholeText("aaa"));
    }

    [TestMethod]
    public void Invalid_Names_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().SameTextAsSaved("not valid"));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().SameTextAsSaved("1"), "numbers are not names");
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().SameTextAsSaved(null!));
    }

    [TestMethod]
    public void A_Reference_Before_Its_Save_Is_Reported()
    {
        var builder = new TextPattern()
            .SameTextAsSaved("w")
            .SaveTextAs("w", A);

        Assert.AreEqual(@"\k<w>(?<w>a)", builder.ToString(), "ToString never throws");
        var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.ToRegex());
        StringAssert.Contains(exception.Message, "SameTextAsSaved(\"w\") before SaveTextAs(\"w\", ...)");
        Assert.ThrowsExactly<ArgumentException>(() => builder.IsFoundIn("aa"));
    }

    [TestMethod]
    public void A_Reference_Inside_A_Lookbehind_Is_Not_Order_Checked()
    {
        // Lookbehinds run right to left, so reading order says nothing about them.
        var builder = new TextPattern()
            .TextBeforeHereEndsWith(new TextPattern().SameTextAsSaved("w"))
            .SaveTextAs("w", A);
        _ = builder.ToRegex();
    }

    [TestMethod]
    public void A_Name_That_Is_Never_Saved_Is_Reported_With_The_Pattern()
    {
        var builder = new TextPattern().SameTextAsSaved("missing");
        var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.ToRegex());
        StringAssert.Contains(exception.Message, @"\k<missing>");
        StringAssert.Contains(exception.Message, "nothing is saved as \"missing\"");
    }

    [TestMethod]
    public void A_Name_Saved_Inside_Raw_Regex_Can_Be_Matched_Again()
    {
        var builder = new TextPattern().RawRegex("(?<w>a)").SameTextAsSaved("w");
        Assert.IsTrue(builder.IsFoundIn("aa"));
    }
}
