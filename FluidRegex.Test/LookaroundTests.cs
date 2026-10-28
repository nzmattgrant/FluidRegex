namespace FluidRegex.Test;

[TestClass]
public class LookaroundTests
{
    private static readonly TextPattern Digit = new TextPattern().Digit();

    [TestMethod]
    public void Every_Condition_Emits_Its_Lookaround()
    {
        var start = new TextPattern();
        Assert.AreEqual("(?=a)", start.IfFollowedBy("a").ToString());
        Assert.AreEqual("(?!a)", start.IfNotFollowedBy("a").ToString());
        Assert.AreEqual("(?<=a)", start.IfPrecededBy("a").ToString());
        Assert.AreEqual("(?<!a)", start.IfNotPrecededBy("a").ToString());
        Assert.AreEqual("(?=[0-9])", start.IfFollowedBy(Digit).ToString());
        Assert.AreEqual("(?![0-9])", start.IfNotFollowedBy(Digit).ToString());
        Assert.AreEqual("(?<=[0-9])", start.IfPrecededBy(Digit).ToString());
        Assert.AreEqual("(?<![0-9])", start.IfNotPrecededBy(Digit).ToString());
    }

    [TestMethod]
    public void Condition_Text_Is_Escaped()
    {
        var price = new TextPattern()
            .IfPrecededBy("$")
            .OneOrMore().Digit()
            .Optional().Pattern(new TextPattern().Text(".").Exactly(2).Digit());

        Assert.AreEqual(@"(?<=\$)[0-9]+(?:\.[0-9]{2})?", price.ToString());
        Assert.AreEqual("42.50", price.FindFirst("cost $42.50")!.Text);
        Assert.AreEqual(@"(?=\(a\|b\)\*)", new TextPattern().IfFollowedBy("(a|b)*").ToString());
    }

    [TestMethod]
    public void Conditions_Add_Nothing_To_What_Is_Found()
    {
        var builder = new TextPattern().Text("X").IfFollowedBy("Y");
        Assert.AreEqual("X", builder.FindFirst("XY")!.Text);
    }

    [TestMethod]
    public void A_Password_Rule_From_Several_Conditions()
    {
        var aDigitSomewhereAhead = new TextPattern().ZeroOrMore().AnyCharacter().Digit();
        var aLowercaseLetterSomewhereAhead = new TextPattern().ZeroOrMore().AnyCharacter().AnyCharacterInRange('a', 'z');

        var password = new TextPattern()
            .IfFollowedBy(aDigitSomewhereAhead)
            .IfFollowedBy(aLowercaseLetterSomewhereAhead)
            .AtLeast(8).AnyCharacter();

        Assert.AreEqual(@"(?=[\s\S]*[0-9])(?=[\s\S]*[a-z])[\s\S]{8,}", password.ToString());
        Assert.IsTrue(password.MatchesWholeText("passw0rd"));
        Assert.IsFalse(password.MatchesWholeText("password"));
        Assert.IsFalse(password.MatchesWholeText("PASSW0RD"));
        Assert.IsFalse(password.MatchesWholeText("abc1"));
    }

    [TestMethod]
    public void Empty_Conditions_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().IfFollowedBy(""));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().IfFollowedBy((string)null!));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().IfPrecededBy(new TextPattern()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().IfNotPrecededBy((TextPattern)null!));
    }
}
