namespace FluidRegex.Test;

[TestClass]
public class LookaroundTests
{
    private static readonly TextPattern Digit = new TextPattern().Digit();

    [TestMethod]
    public void Every_Condition_Emits_Its_Lookaround()
    {
        var start = new TextPattern();
        Assert.AreEqual("(?=a)", start.TextAfterHereStartsWith("a").ToString());
        Assert.AreEqual("(?!a)", start.TextAfterHereDoesNotStartWith("a").ToString());
        Assert.AreEqual("(?<=a)", start.TextBeforeHereEndsWith("a").ToString());
        Assert.AreEqual("(?<!a)", start.TextBeforeHereDoesNotEndWith("a").ToString());
        Assert.AreEqual("(?=[0-9])", start.TextAfterHereStartsWith(Digit).ToString());
        Assert.AreEqual("(?![0-9])", start.TextAfterHereDoesNotStartWith(Digit).ToString());
        Assert.AreEqual("(?<=[0-9])", start.TextBeforeHereEndsWith(Digit).ToString());
        Assert.AreEqual("(?<![0-9])", start.TextBeforeHereDoesNotEndWith(Digit).ToString());
    }

    [TestMethod]
    public void Condition_Text_Is_Escaped()
    {
        var price = new TextPattern()
            .TextBeforeHereEndsWith("$")
            .Digit(Times.OneOrMore)
            .Pattern(new TextPattern().Text(".").Digit(Times.Exactly(2)), Times.Optional);

        Assert.AreEqual(@"(?<=\$)[0-9]+(?:\.[0-9]{2})?", price.ToString());
        Assert.AreEqual("42.50", price.FindFirst("cost $42.50")!.Text);
        Assert.AreEqual(@"(?=\(a\|b\)\*)", new TextPattern().TextAfterHereStartsWith("(a|b)*").ToString());
    }

    [TestMethod]
    public void Conditions_Add_Nothing_To_What_Is_Found()
    {
        var builder = new TextPattern().Text("X").TextAfterHereStartsWith("Y");
        Assert.AreEqual("X", builder.FindFirst("XY")!.Text);
    }

    [TestMethod]
    public void A_Password_Rule_From_Several_Conditions()
    {
        var aDigitSomewhereAhead = new TextPattern().AnyCharacter(Times.ZeroOrMore).Digit();
        var aLowercaseLetterSomewhereAhead = new TextPattern().AnyCharacter(Times.ZeroOrMore).AnyCharacterInRange('a', 'z');

        var password = new TextPattern()
            .TextAfterHereStartsWith(aDigitSomewhereAhead)
            .TextAfterHereStartsWith(aLowercaseLetterSomewhereAhead)
            .AnyCharacter(Times.AtLeast(8));

        Assert.AreEqual(@"(?=[\s\S]*[0-9])(?=[\s\S]*[a-z])[\s\S]{8,}", password.ToString());
        Assert.IsTrue(password.MatchesWholeText("passw0rd"));
        Assert.IsFalse(password.MatchesWholeText("password"));
        Assert.IsFalse(password.MatchesWholeText("PASSW0RD"));
        Assert.IsFalse(password.MatchesWholeText("abc1"));
    }

    [TestMethod]
    public void Empty_Conditions_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().TextAfterHereStartsWith(""));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().TextAfterHereStartsWith((string)null!));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().TextBeforeHereEndsWith(new TextPattern()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().TextBeforeHereDoesNotEndWith((TextPattern)null!));
    }
}
