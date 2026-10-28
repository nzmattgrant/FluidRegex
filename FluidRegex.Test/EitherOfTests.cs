namespace FluidRegex.Test;

[TestClass]
public class EitherOfTests
{
    [TestMethod]
    public void Either_Or_Is_Always_Grouped()
    {
        var builder = new TextPattern().StartOfText().EitherOf("cat", "dog").EndOfText();
        Assert.AreEqual(@"\A(?:cat|dog)\z", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("cat"));
        Assert.IsTrue(builder.IsFoundIn("dog"));
        Assert.IsFalse(builder.IsFoundIn("catx"));
        Assert.IsFalse(builder.IsFoundIn("xdog"));
    }

    [TestMethod]
    public void Text_Choices_Are_Escaped()
    {
        Assert.AreEqual(@"(?:a\.b|c)", new TextPattern().EitherOf("a.b", "c").ToString());
    }

    [TestMethod]
    public void A_Single_Choice_Is_Just_That_Text()
    {
        Assert.AreEqual("cat", new TextPattern().EitherOf("cat").ToString());
    }

    [TestMethod]
    public void Pattern_Choices()
    {
        var digits = new TextPattern().OneOrMore().Digit();
        var letters = new TextPattern().OneOrMore().AnyCharacterInRange('a', 'z');
        var builder = new TextPattern().EitherOf(digits, letters);

        Assert.AreEqual("(?:[0-9]+|[a-z]+)", builder.ToString());
        Assert.IsTrue(builder.MatchesWholeText("123"));
        Assert.IsTrue(builder.MatchesWholeText("abc"));
        Assert.IsFalse(builder.MatchesWholeText("1a"));
    }

    [TestMethod]
    public void A_Count_Repeats_The_Whole_Either_Or()
    {
        var builder = new TextPattern().OneOrMore().EitherOf("ab", "cd");
        Assert.AreEqual("(?:ab|cd)+", builder.ToString());
        Assert.IsTrue(builder.MatchesWholeText("abcdab"));
    }

    [TestMethod]
    public void Inside_A_Condition_Or_A_Save_The_Inner_Group_Is_Dropped()
    {
        var catOrDog = new TextPattern().EitherOf("cat", "dog");
        Assert.AreEqual("(?=cat|dog)", new TextPattern().IfFollowedBy(catOrDog).ToString());
        Assert.AreEqual("(?<pet>cat|dog)", new TextPattern().SaveAs("pet", catOrDog).ToString());
        Assert.AreEqual("(?=(?:cat|dog)s)", new TextPattern().IfFollowedBy(catOrDog.Text("s")).ToString());
    }

    [TestMethod]
    public void Missing_Or_Empty_Choices_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().EitherOf(new string[0]));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().EitherOf("a", null!));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().EitherOf("a", ""));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().EitherOf(new TextPattern().Text("a"), new TextPattern()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().EitherOf((string[])null!));
    }
}
