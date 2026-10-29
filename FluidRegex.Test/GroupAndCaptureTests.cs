namespace FluidRegex.Test;

[TestClass]
public class GroupAndCaptureTests
{
    private static readonly TextPattern Digit = new TextPattern().Digit();
    private static readonly TextPattern Ab = new TextPattern().Text("ab");
    private static readonly TextPattern CatOrDog = new TextPattern().EitherOf("cat", "dog");

    [TestMethod]
    public void An_Uncounted_Pattern_Inlines_Its_Content()
    {
        Assert.AreEqual("x[0-9]y", new TextPattern().Text("x").Pattern(new TextPattern().Digit().Text("y")).ToString());
    }

    [TestMethod]
    public void A_Reused_Pattern_Does_Not_Save_Anything()
    {
        var builder = new TextPattern().Pattern(Ab, Times.OneOrMore);
        Assert.AreEqual("(?:ab)+", builder.ToString());
        Assert.AreEqual(1, builder.ToRegex().Match("abab").Groups.Count, "only group 0, the whole match");
    }

    [TestMethod]
    public void SaveTextAs_Puts_The_Count_Inside_So_It_Holds_The_Whole_Run()
    {
        var number = new TextPattern().SaveTextAs("number", Digit, Times.OneOrMore);
        Assert.AreEqual("(?<number>[0-9]+)", number.ToString());
        Assert.AreEqual("123", number.FindFirst("a123")!.Saved("number"));
    }

    [TestMethod]
    public void An_Optional_Saved_Part_Is_Empty_When_Missing()
    {
        var builder = new TextPattern().Text("x").SaveTextAs("abs", Ab, Times.ZeroOrMore);
        Assert.AreEqual("x(?<abs>(?:ab)+)?", builder.ToString());
        Assert.AreEqual("", builder.FindFirst("x")!.Saved("abs"));
        Assert.IsFalse(builder.ToRegex().Match("x").Groups["abs"].Success);
        Assert.AreEqual("abab", builder.FindFirst("xabab")!.Saved("abs"));

        Assert.AreEqual(@"(?<www>www\.)?", new TextPattern().SaveTextAs("www", new TextPattern().Text("www."), Times.Optional).ToString());
        Assert.AreEqual("(?<abs>(?:ab){1,5})?", new TextPattern().SaveTextAs("abs", Ab, Times.Between(0, 5)).ToString());
        Assert.AreEqual("(?<abs>(?:ab)+?)??", new TextPattern().SaveTextAs("abs", Ab, Times.ZeroOrMore.AsFewAsPossible()).ToString());
    }

    [TestMethod]
    public void A_Lone_Either_Or_In_A_Save_Is_Not_Double_Wrapped()
    {
        Assert.AreEqual("(?<pet>cat|dog)", new TextPattern().SaveTextAs("pet", CatOrDog).ToString());

        var repeated = new TextPattern().SaveTextAs("pets", CatOrDog, Times.OneOrMore);
        Assert.AreEqual("(?<pets>(?:cat|dog)+)", repeated.ToString());
        Assert.AreEqual("catdog", repeated.FindFirst("catdog")!.Saved("pets"));
    }

    [TestMethod]
    public void SaveTextAs_Names_The_Part()
    {
        var year = new TextPattern().SaveTextAs("year", new TextPattern().Digit(Times.Exactly(4)));
        Assert.AreEqual("(?<year>[0-9]{4})", year.ToString());
        Assert.AreEqual("2026", year.FindFirst("in 2026")!.Saved("year"));
        Assert.AreEqual("(?<year>[0-9]{4})", new TextPattern().SaveTextAs("year", Digit, Times.Exactly(4)).ToString());
    }

    [TestMethod]
    public void Saved_Names_Are_Checked()
    {
        foreach (var name in new[] { "year", "_x", "é1" })
        {
            Assert.AreEqual($"(?<{name}>a)", new TextPattern().SaveTextAs(name, new TextPattern().Text("a")).ToString());
        }

        foreach (var name in new[] { "1a", "my name", "a-b" })
        {
            Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().SaveTextAs(name, Ab)).ParamName);
        }

        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().SaveTextAs("", Ab));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().SaveTextAs(null!, Ab));
    }

    [TestMethod]
    public void Patterns_Nest()
    {
        var inner = new TextPattern().Digit(Times.OneOrMore);
        var middle = new TextPattern().Text("[").SaveTextAs("number", inner).Text("]");
        var outer = new TextPattern().Pattern(middle, Times.OneOrMore);
        var builder = new TextPattern().StartOfText().Pattern(outer).EndOfText();

        Assert.AreEqual(@"\A(?:\[(?<number>[0-9]+)])+\z", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("[1][22]"));
        Assert.AreEqual("22", builder.FindFirst("[1][22]")!.Saved("number"));
    }

    [TestMethod]
    public void Ignoring_Case_Stays_With_A_Reused_Pattern()
    {
        var cat = new TextPattern().IgnoringCase().Text("cat");
        var builder = new TextPattern().Text("my ").Pattern(cat);

        Assert.AreEqual(@"my\ (?i:cat)", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("my CAT"));
        Assert.IsFalse(builder.IsFoundIn("MY cat"), "only the reused part ignores case");
        Assert.AreEqual("(?<pet>(?i:cat|dog))", new TextPattern().SaveTextAs("pet", new TextPattern().IgnoringCase().EitherOf("cat", "dog")).ToString());
    }

    [TestMethod]
    public void Counting_A_Pattern_Of_Only_Positions_Throws()
    {
        var boundary = new TextPattern().StartOfWord();
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().Pattern(boundary, Times.OneOrMore));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().SaveTextAs("b", boundary, Times.Optional));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().SaveTextAs("b", boundary, Times.ZeroOrMore));
        Assert.AreEqual(@"\b(?=\w)", new TextPattern().Pattern(boundary).ToString(), "without a count it is fine");
    }

    [TestMethod]
    public void Empty_Patterns_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().Pattern(new TextPattern()));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().SaveTextAs("a", new TextPattern()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().Pattern(null!));
    }
}
