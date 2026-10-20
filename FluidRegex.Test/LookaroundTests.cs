using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class LookaroundTests
{
    private static readonly SubPattern Digit = new SubPattern().AnyDigit();

    [TestMethod]
    public void Every_Condition_Emits_Its_Lookaround()
    {
        var start = new MatchThesePatterns();
        Assert.AreEqual("(?=a)", start.IfFollowedBy("a").ToString());
        Assert.AreEqual("(?!a)", start.IfNotFollowedBy("a").ToString());
        Assert.AreEqual("(?<=a)", start.IfPrecededBy("a").ToString());
        Assert.AreEqual("(?<!a)", start.IfNotPrecededBy("a").ToString());
        Assert.AreEqual(@"(?=\d)", start.IfFollowedBy(Digit).ToString());
        Assert.AreEqual(@"(?!\d)", start.IfNotFollowedBy(Digit).ToString());
        Assert.AreEqual(@"(?<=\d)", start.IfPrecededBy(Digit).ToString());
        Assert.AreEqual(@"(?<!\d)", start.IfNotPrecededBy(Digit).ToString());
    }

    [TestMethod]
    public void Condition_Text_Is_Escaped()
    {
        var price = new MatchThesePatterns()
            .IfPrecededBy("$")
            .AnyDigit(OneOrMore)
            .Group(new SubPattern().TheCharacter('.').AnyDigit(Exactly(2)), Optional);

        Assert.AreEqual(@"(?<=\$)\d+(?:\.\d{2})?", price.ToString());
        Assert.AreEqual("42.50", price.ToRegex().Match("cost $42.50").Value);
        Assert.AreEqual(@"(?=\(a\|b\)\*)", new MatchThesePatterns().IfFollowedBy("(a|b)*").ToString());
    }

    [TestMethod]
    public void Conditions_Consume_Nothing()
    {
        var builder = new MatchThesePatterns().TheText("X").IfFollowedBy("Y");
        Assert.AreEqual("X", builder.ToRegex().Match("XY").Value);
    }

    [TestMethod]
    public void A_Password_Rule_From_Several_Conditions()
    {
        var aDigitSomewhereAhead = new SubPattern().AnyCharacter(ZeroOrMore).AnyDigit();
        var aLowercaseLetterSomewhereAhead = new SubPattern().AnyCharacter(ZeroOrMore).AnyCharacterInRange('a', 'z');

        var password = new MatchThesePatterns()
            .StartOfText()
            .IfFollowedBy(aDigitSomewhereAhead)
            .IfFollowedBy(aLowercaseLetterSomewhereAhead)
            .AnyCharacter(AtLeast(8))
            .EndOfText();

        Assert.AreEqual(@"\A(?=.*\d)(?=.*[a-z]).{8,}\z", password.ToString());
        Assert.IsTrue(password.IsMatch("passw0rd"));
        Assert.IsFalse(password.IsMatch("password"));
        Assert.IsFalse(password.IsMatch("PASSW0RD"));
        Assert.IsFalse(password.IsMatch("abc1"));
        Assert.IsFalse(password.IsMatch("abcdefg1\n"));
    }

    [TestMethod]
    public void Empty_Conditions_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().IfFollowedBy(""));
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().IfFollowedBy((string)null!));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().IfPrecededBy(new SubPattern()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().IfNotPrecededBy((SubPattern)null!));
    }
}
