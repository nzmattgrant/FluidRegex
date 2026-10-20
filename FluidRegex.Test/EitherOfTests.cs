using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class EitherOfTests
{
    [TestMethod]
    public void Either_Or_Is_Always_Grouped()
    {
        var builder = new MatchThesePatterns().StartOfText().EitherOf("cat", "dog").EndOfText();
        Assert.AreEqual(@"\A(?:cat|dog)\z", builder.ToString());
        Assert.IsTrue(builder.IsMatch("cat"));
        Assert.IsTrue(builder.IsMatch("dog"));
        Assert.IsFalse(builder.IsMatch("catx"));
        Assert.IsFalse(builder.IsMatch("xdog"));
    }

    [TestMethod]
    public void Text_Alternatives_Are_Escaped()
    {
        Assert.AreEqual(@"(?:a\.b|c)", new MatchThesePatterns().EitherOf("a.b", "c").ToString());
    }

    [TestMethod]
    public void A_Single_Alternative_Is_Just_That_Text()
    {
        Assert.AreEqual("cat", new MatchThesePatterns().EitherOf("cat").ToString());
    }

    [TestMethod]
    public void Sub_Pattern_Alternatives()
    {
        var digits = new SubPattern().AnyDigit(OneOrMore);
        var letters = new SubPattern().AnyCharacterInRange('a', 'z', OneOrMore);
        var builder = new MatchThesePatterns().StartOfText().EitherOf(digits, letters).EndOfText();

        Assert.AreEqual(@"\A(?:\d+|[a-z]+)\z", builder.ToString());
        Assert.IsTrue(builder.IsMatch("123"));
        Assert.IsTrue(builder.IsMatch("abc"));
        Assert.IsFalse(builder.IsMatch("1a"));
    }

    [TestMethod]
    public void Repeat_An_Either_Or_By_Grouping_It()
    {
        var builder = new MatchThesePatterns().Group(new SubPattern().EitherOf("ab", "cd"), OneOrMore);
        Assert.AreEqual("(?:ab|cd)+", builder.ToString());
    }

    [TestMethod]
    public void Inside_A_Lookaround_Or_Capture_The_Inner_Group_Is_Dropped()
    {
        var catOrDog = new SubPattern().EitherOf("cat", "dog");
        Assert.AreEqual("(?=cat|dog)", new MatchThesePatterns().IfFollowedBy(catOrDog).ToString());
        Assert.AreEqual("(cat|dog)", new MatchThesePatterns().Capture(catOrDog).ToString());
        Assert.AreEqual("(?=(?:cat|dog)s)", new MatchThesePatterns().IfFollowedBy(catOrDog.TheCharacter('s')).ToString());
    }

    [TestMethod]
    public void Missing_Or_Empty_Alternatives_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().EitherOf(new string[0]));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().EitherOf("a", null!));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().EitherOf("a", ""));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().EitherOf(new SubPattern().TheText("a"), new SubPattern()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().EitherOf((string[])null!));
    }
}
