using System.Linq;

namespace FluidRegex.Test;

[TestClass]
public class ResultTests
{
    private static readonly TextPattern Number = new TextPattern().OneOrMore().Digit();

    private static readonly TextPattern Email = new TextPattern()
        .OneOrMore().LetterOrDigit()
        .Text("@")
        .OneOrMore().LetterOrDigit()
        .Text(".")
        .OneOrMore().Letter();

    [TestMethod]
    public void IsFoundIn_Searches_Anywhere_But_MatchesWholeText_Checks_All_Of_It()
    {
        Assert.IsTrue(Email.IsFoundIn("junk a@b.com junk"));
        Assert.IsFalse(Email.MatchesWholeText("junk a@b.com junk"));
        Assert.IsTrue(Email.MatchesWholeText("a@b.com"));
        Assert.IsFalse(Email.MatchesWholeText("a@b.com\n"));
    }

    [TestMethod]
    public void MatchesWholeText_Keeps_Either_Or_And_Conditions_Inside()
    {
        var catOrDog = new TextPattern().EitherOf("cat", "dog");
        Assert.IsTrue(catOrDog.MatchesWholeText("dog"));
        Assert.IsFalse(catOrDog.MatchesWholeText("cats"));
        Assert.IsFalse(catOrDog.MatchesWholeText("xcat"));

        Assert.IsTrue(new TextPattern().IfNotPrecededBy("x").Text("a").MatchesWholeText("a"));
        Assert.IsTrue(new TextPattern().MatchesWholeText(""));
    }

    [TestMethod]
    public void MatchesWholeText_Reports_A_Problem_With_The_Pattern_As_Written()
    {
        var builder = new TextPattern().Text("x").RawRegex("(");
        var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.MatchesWholeText("x"));
        StringAssert.Contains(exception.Message, "\"x(?:()\"");
    }

    [TestMethod]
    public void FindFirst_Returns_The_First_Place_Or_Null()
    {
        var found = Number.FindFirst("ab 12 cd 345");

        Assert.IsNotNull(found);
        Assert.AreEqual("12", found.Text);
        Assert.AreEqual(3, found.Position);
        Assert.AreEqual("12", found.ToString());
        Assert.IsNull(Number.FindFirst("none"));
    }

    [TestMethod]
    public void FindAll_Returns_Every_Place_In_Order()
    {
        var found = Number.FindAll("ab 12 cd 345");

        CollectionAssert.AreEqual(new[] { "12", "345" }, found.Select(f => f.Text).ToArray());
        CollectionAssert.AreEqual(new[] { 3, 9 }, found.Select(f => f.Position).ToArray());
        Assert.AreEqual(0, Number.FindAll("none").Count);
    }

    [TestMethod]
    public void Saved_Returns_The_Saved_Text()
    {
        var date = new TextPattern()
            .SaveAs("year", new TextPattern().Exactly(4).Digit())
            .Text("-")
            .SaveAs("month", new TextPattern().Exactly(2).Digit());

        var found = date.FindFirst("Due 2026-10")!;
        Assert.AreEqual("2026", found.Saved("year"));
        Assert.AreEqual("10", found.Saved("month"));
        Assert.AreEqual("2026-10", found.Text);
    }

    [TestMethod]
    public void Saved_Returns_An_Empty_String_For_A_Missing_Optional_Part()
    {
        var price = new TextPattern()
            .SaveAs("dollars", Number)
            .Optional().Pattern(new TextPattern().Text(".").SaveAs("cents", new TextPattern().Exactly(2).Digit()));

        Assert.AreEqual("50", price.FindFirst("42.50")!.Saved("cents"));
        Assert.AreEqual("", price.FindFirst("42")!.Saved("cents"));
    }

    [TestMethod]
    public void Saved_Rejects_A_Name_That_Was_Never_Saved()
    {
        var found = new TextPattern().SaveAs("year", Number).FindFirst("2026")!;

        var exception = Assert.ThrowsExactly<ArgumentException>(() => found.Saved("yaer"));
        Assert.AreEqual("name", exception.ParamName);
        StringAssert.Contains(exception.Message, "\"year\"");
        Assert.ThrowsExactly<ArgumentException>(() => found.Saved("0"), "the whole match is Text, not a saved part");
        Assert.ThrowsExactly<ArgumentNullException>(() => found.Saved(null!));

        var nothingSaved = Number.FindFirst("1")!;
        StringAssert.Contains(Assert.ThrowsExactly<ArgumentException>(() => nothingSaved.Saved("year")).Message, "saves nothing");
    }

    [TestMethod]
    public void ReplaceAll_Uses_The_Replacement_As_Written()
    {
        Assert.AreEqual("ab # cd #", Number.ReplaceAll("ab 12 cd 345", "#"));
        Assert.AreEqual("price $1.00", Number.ReplaceAll("price 12", "$1.00"), "$1 is not a substitution");
        Assert.AreEqual("ab", Number.ReplaceAll("a1b2", ""));
        Assert.AreEqual("none", Number.ReplaceAll("none", "#"));
    }

    [TestMethod]
    public void Null_Text_Throws()
    {
        Assert.AreEqual("text", Assert.ThrowsExactly<ArgumentNullException>(() => Number.IsFoundIn(null!)).ParamName);
        Assert.AreEqual("text", Assert.ThrowsExactly<ArgumentNullException>(() => Number.MatchesWholeText(null!)).ParamName);
        Assert.AreEqual("text", Assert.ThrowsExactly<ArgumentNullException>(() => Number.FindFirst(null!)).ParamName);
        Assert.AreEqual("text", Assert.ThrowsExactly<ArgumentNullException>(() => Number.FindAll(null!)).ParamName);
        Assert.AreEqual("text", Assert.ThrowsExactly<ArgumentNullException>(() => Number.ReplaceAll(null!, "#")).ParamName);
        Assert.AreEqual("replacement", Assert.ThrowsExactly<ArgumentNullException>(() => Number.ReplaceAll("1", null!)).ParamName);
    }
}
