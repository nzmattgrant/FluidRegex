using System.Text.RegularExpressions;

namespace FluidRegex.Test;

[TestClass]
public class OptionsAndOutputTests
{
    [TestMethod]
    public void IgnoringCase_Sets_The_Option()
    {
        var builder = new MatchThesePatterns().IgnoringCase().TheText("cat");
        Assert.AreEqual(RegexOptions.IgnoreCase, builder.Options);
        Assert.AreEqual(RegexOptions.IgnoreCase, builder.ToRegex().Options);
        Assert.IsTrue(builder.IsMatch("CAT"));
    }

    [TestMethod]
    public void WithOptions_Combines_Options()
    {
        var builder = new MatchThesePatterns()
            .WithOptions(RegexOptions.CultureInvariant)
            .IgnoringCase();
        Assert.AreEqual(RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, builder.Options);
    }

    [TestMethod]
    public void Singleline_Is_Rejected_With_A_Hint()
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().WithOptions(RegexOptions.Singleline));
        Assert.AreEqual("options", exception.ParamName);
        StringAssert.Contains(exception.Message, "AnyCharacterIncludingNewline");
    }

    [TestMethod]
    public void Options_Never_Change_The_Pattern_Text()
    {
        var plain = new MatchThesePatterns().TheText("a b");
        var withOptions = new MatchThesePatterns().IgnoringCase().WithOptions(RegexOptions.Multiline).TheText("a b");
        Assert.AreEqual(plain.ToString(), withOptions.ToString());
    }

    [TestMethod]
    public void Options_Carry_Through_Later_Calls()
    {
        var builder = new MatchThesePatterns().IgnoringCase().TheText("a").TheText("b");
        Assert.AreEqual(RegexOptions.IgnoreCase, builder.Options);
    }

    [TestMethod]
    public void ToRegex_Is_Cached()
    {
        var builder = new MatchThesePatterns().TheText("a");
        Assert.AreSame(builder.ToRegex(), builder.ToRegex());
    }

    [TestMethod]
    public void The_Match_Timeout_Is_Used()
    {
        var builder = new MatchThesePatterns().WithMatchTimeout(TimeSpan.FromSeconds(1)).TheText("a");
        Assert.AreEqual(TimeSpan.FromSeconds(1), builder.MatchTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(1), builder.ToRegex().MatchTimeout);
        Assert.AreEqual(Regex.InfiniteMatchTimeout, new MatchThesePatterns().ToRegex().MatchTimeout);
    }

    [TestMethod]
    public void Invalid_Timeouts_Throw()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().WithMatchTimeout(TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().WithMatchTimeout(TimeSpan.FromSeconds(-1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().WithMatchTimeout(TimeSpan.FromDays(30)));
        _ = new MatchThesePatterns().WithMatchTimeout(Regex.InfiniteMatchTimeout);
    }

    [TestMethod]
    public void IsMatch_Rejects_Null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().TheText("a").IsMatch(null!));
    }

#if NET
    [TestMethod]
    public void NonBacktracking_With_A_Lookaround_Is_Reported()
    {
        var builder = new MatchThesePatterns().WithOptions(RegexOptions.NonBacktracking).TheText("a").IfFollowedBy("b");
        Assert.ThrowsExactly<ArgumentException>(() => builder.ToRegex());
    }
#endif
}
