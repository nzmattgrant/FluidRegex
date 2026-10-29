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
    public void More_Than_Two_Choices()
    {
        Assert.AreEqual("(?:jan|feb|mar)", new TextPattern().EitherOf("jan", "feb", "mar").ToString());
    }

    [TestMethod]
    public void Pattern_Choices()
    {
        var digits = new TextPattern().Digit(Times.OneOrMore);
        var letters = new TextPattern().AnyCharacterInRange('a', 'z', Times.OneOrMore);
        var builder = new TextPattern().EitherOf(digits, letters);

        Assert.AreEqual("(?:[0-9]+|[a-z]+)", builder.ToString());
        Assert.IsTrue(builder.MatchesWholeText("123"));
        Assert.IsTrue(builder.MatchesWholeText("abc"));
        Assert.IsFalse(builder.MatchesWholeText("1a"));
    }

    [TestMethod]
    public void A_Count_Repeats_The_Whole_Either_Or()
    {
        var abOrCd = new TextPattern().EitherOf("ab", "cd");
        var builder = new TextPattern().Pattern(abOrCd, Times.OneOrMore);
        Assert.AreEqual("(?:ab|cd)+", builder.ToString());
        Assert.IsTrue(builder.MatchesWholeText("abcdab"));
    }

    [TestMethod]
    public void Inside_A_Condition_Or_A_Save_The_Inner_Group_Is_Dropped()
    {
        var catOrDog = new TextPattern().EitherOf("cat", "dog");
        Assert.AreEqual("(?=cat|dog)", new TextPattern().TextAfterHereStartsWith(catOrDog).ToString());
        Assert.AreEqual("(?<pet>cat|dog)", new TextPattern().SaveTextAs("pet", catOrDog).ToString());
        Assert.AreEqual("(?=(?:cat|dog)s)", new TextPattern().TextAfterHereStartsWith(catOrDog.Text("s")).ToString());
    }

    [TestMethod]
    public void Missing_Or_Empty_Choices_Throw()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().EitherOf("a", (string)null!));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().EitherOf("a", ""));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().EitherOf("a", "b", ""));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().EitherOf("a", "b", (string)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().EitherOf("a", "b", (string[])null!));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().EitherOf(new TextPattern().Text("a"), new TextPattern()));
    }
}
