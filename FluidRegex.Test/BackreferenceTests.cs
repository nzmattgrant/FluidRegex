using System.Text.RegularExpressions;
using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class BackreferenceTests
{
    private static readonly SubPattern AWord = new SubPattern().AnyWordCharacter(OneOrMore);

    [TestMethod]
    public void A_Named_Reference_Matches_The_Same_Text_Again()
    {
        var repeatedWord = new MatchThesePatterns()
            .WordBoundary()
            .CaptureAs("word", AWord)
            .AnyWhitespace(OneOrMore)
            .TheSameTextAsCapture("word")
            .WordBoundary();

        Assert.AreEqual(@"\b(?<word>\w+)\s+\k<word>\b", repeatedWord.ToString());
        Assert.AreEqual("is", repeatedWord.ToRegex().Match("this is is a test").Groups["word"].Value);
        Assert.IsFalse(repeatedWord.IsMatch("the theory"));
    }

    [TestMethod]
    public void A_Numbered_Reference_Is_Not_Extended_By_A_Following_Digit()
    {
        var builder = new MatchThesePatterns()
            .Capture(new SubPattern().TheText("ab"))
            .TheSameTextAsCapture(1)
            .TheCharacter('0');

        Assert.AreEqual(@"(ab)\k<1>0", builder.ToString());
        Assert.IsTrue(builder.IsMatch("abab0"));
    }

    [TestMethod]
    public void A_Reference_Can_Be_Counted()
    {
        Assert.AreEqual(@"\k<w>{2}", new MatchThesePatterns().TheSameTextAsCapture("w", Exactly(2)).ToString());
    }

    [TestMethod]
    public void Invalid_References_Throw()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().TheSameTextAsCapture(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().TheSameTextAsCapture(-1));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().TheSameTextAsCapture("not valid"));
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().TheSameTextAsCapture(null!));
    }

    [TestMethod]
    public void A_Reference_Before_Its_Capture_Is_Reported_By_ToRegex()
    {
        var builder = new MatchThesePatterns()
            .TheSameTextAsCapture("w")
            .CaptureAs("w", new SubPattern().TheCharacter('a'));

        Assert.AreEqual(@"\k<w>(?<w>a)", builder.ToString(), "ToString never throws");
        var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.ToRegex());
        StringAssert.Contains(exception.Message, "TheSameTextAsCapture(\"w\")");
    }

    [TestMethod]
    public void A_Reference_Inside_A_Lookbehind_Is_Not_Order_Checked()
    {
        // Lookbehinds run right to left, so reading order says nothing about them.
        var builder = new MatchThesePatterns()
            .IfPrecededBy(new SubPattern().TheSameTextAsCapture("w"))
            .CaptureAs("w", new SubPattern().TheCharacter('a'));
        _ = builder.ToRegex();
    }

    [TestMethod]
    public void An_Undefined_Name_Is_Reported_With_The_Pattern()
    {
        var builder = new MatchThesePatterns().TheSameTextAsCapture("missing");
        var exception = Assert.ThrowsExactly<ArgumentException>(() => builder.ToRegex());
        StringAssert.Contains(exception.Message, @"\k<missing>");
    }

    [TestMethod]
    public void ExplicitCapture_Conflicts_Are_Reported()
    {
        var unnamed = new MatchThesePatterns()
            .WithOptions(RegexOptions.ExplicitCapture)
            .Capture(new SubPattern().TheCharacter('a'));
        Assert.ThrowsExactly<ArgumentException>(() => unnamed.ToRegex());

        var numbered = new MatchThesePatterns()
            .WithOptions(RegexOptions.ExplicitCapture)
            .CaptureAs("a", new SubPattern().TheCharacter('a'))
            .TheSameTextAsCapture(1);
        Assert.ThrowsExactly<ArgumentException>(() => numbered.ToRegex());

        var named = new MatchThesePatterns()
            .WithOptions(RegexOptions.ExplicitCapture)
            .CaptureAs("a", new SubPattern().TheCharacter('a'))
            .TheSameTextAsCapture("a");
        Assert.IsTrue(named.IsMatch("aa"));
    }
}
