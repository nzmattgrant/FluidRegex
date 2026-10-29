using System.Text.RegularExpressions;

namespace FluidRegex.Test;

[TestClass]
public class SettingsTests
{
    [TestMethod]
    public void IgnoringCase_Sets_The_Option()
    {
        var builder = new TextPattern().IgnoringCase().Text("cat");
        Assert.AreEqual(RegexOptions.IgnoreCase, builder.ToRegex().Options);
        Assert.IsTrue(builder.IsFoundIn("CAT"));
        Assert.IsTrue(builder.MatchesWholeText("Cat"));
    }

    [TestMethod]
    public void IgnoringCase_Never_Changes_The_Pattern_Text()
    {
        var plain = new TextPattern().Text("a b");
        var ignoringCase = new TextPattern().IgnoringCase().Text("a b");
        Assert.AreEqual(plain.ToString(), ignoringCase.ToString());
    }

    [TestMethod]
    public void IgnoringCase_Carries_Through_Later_Calls()
    {
        Assert.AreEqual(RegexOptions.IgnoreCase, new TextPattern().IgnoringCase().Text("a").Text("b").ToRegex().Options);
        Assert.IsTrue(new TextPattern().IgnoringCase().Text("a", Times.OneOrMore).MatchesWholeText("aA"));
    }

    [TestMethod]
    public void ToRegex_Is_Cached()
    {
        var builder = new TextPattern().Text("a");
        Assert.AreSame(builder.ToRegex(), builder.ToRegex());
    }

    [TestMethod]
    public void The_Time_Limit_Is_Used()
    {
        var builder = new TextPattern().WithTimeLimit(TimeSpan.FromSeconds(1)).Text("a");
        Assert.AreEqual(TimeSpan.FromSeconds(1), builder.ToRegex().MatchTimeout);
        Assert.AreEqual(Regex.InfiniteMatchTimeout, new TextPattern().ToRegex().MatchTimeout);
    }

    [TestMethod]
    public void Invalid_Time_Limits_Throw()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TextPattern().WithTimeLimit(TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TextPattern().WithTimeLimit(TimeSpan.FromSeconds(-1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TextPattern().WithTimeLimit(TimeSpan.FromDays(30)));
        _ = new TextPattern().WithTimeLimit(Regex.InfiniteMatchTimeout);
    }
}
