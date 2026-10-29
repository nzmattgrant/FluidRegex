namespace FluidRegex.Test;

[TestClass]
public class CharacterClassTests
{
    private static readonly TextPattern Start = new();

    [TestMethod]
    public void Each_Kind_Emits_Its_Token()
    {
        Assert.AreEqual(@"[\s\S]", Start.AnyCharacter().ToString());
        Assert.AreEqual(@"[^\r\n]", Start.AnyCharacterExceptLineBreak().ToString());
        Assert.AreEqual("[0-9]", Start.Digit().ToString());
        Assert.AreEqual("[^0-9]", Start.AnyCharacterExceptDigit().ToString());
        Assert.AreEqual(@"\p{L}", Start.Letter().ToString());
        Assert.AreEqual(@"\P{L}", Start.AnyCharacterExceptLetter().ToString());
        Assert.AreEqual(@"\p{Lu}", Start.UppercaseLetter().ToString());
        Assert.AreEqual(@"\p{Ll}", Start.LowercaseLetter().ToString());
        Assert.AreEqual(@"[\p{L}0-9]", Start.LetterOrDigit().ToString());
        Assert.AreEqual(@"\s", Start.Whitespace().ToString());
        Assert.AreEqual(@"\S", Start.AnyCharacterExceptWhitespace().ToString());
        Assert.AreEqual(@"(?:\r\n|\r|\n)", Start.LineBreak().ToString());
    }

    [TestMethod]
    public void Each_Kind_Takes_A_Count()
    {
        Assert.AreEqual("[0-9]{4}", Start.Digit(Times.Exactly(4)).ToString());
        Assert.AreEqual(@"[\p{L}0-9]+", Start.LetterOrDigit(Times.OneOrMore).ToString());
        Assert.AreEqual(@"[\s\S]{8,}", Start.AnyCharacter(Times.AtLeast(8)).ToString());
        Assert.AreEqual(@"(?:\r\n|\r|\n)+", Start.LineBreak(Times.OneOrMore).ToString());
    }

    [TestMethod]
    public void Letters_Follow_Unicode_But_Digits_Are_Only_0_To_9()
    {
        Assert.IsTrue(Start.Digit(Times.OneOrMore).MatchesWholeText("0123456789"));
        Assert.IsFalse(Start.Digit().MatchesWholeText("٣"), "Arabic-Indic digit three");
        Assert.IsTrue(Start.AnyCharacterExceptDigit().MatchesWholeText("٣"));
        Assert.IsTrue(Start.Letter().MatchesWholeText("é"));
        Assert.IsFalse(Start.Letter().MatchesWholeText("_"));
        Assert.IsTrue(Start.AnyCharacterExceptLetter().MatchesWholeText("_"));
        Assert.IsTrue(Start.LetterOrDigit().MatchesWholeText("é"));
        Assert.IsTrue(Start.LetterOrDigit().MatchesWholeText("7"));
        Assert.IsFalse(Start.LetterOrDigit().MatchesWholeText("_"), "no hidden underscore");
        Assert.IsTrue(Start.UppercaseLetter().MatchesWholeText("É"));
        Assert.IsFalse(Start.UppercaseLetter().MatchesWholeText("é"));
        Assert.IsTrue(Start.LowercaseLetter().MatchesWholeText("é"));
    }

    [TestMethod]
    public void Uppercase_Letter_Also_Matches_Lowercase_When_Ignoring_Case()
    {
        Assert.IsTrue(new TextPattern().IgnoringCase().UppercaseLetter().MatchesWholeText("a"));
    }

    [TestMethod]
    public void AnyCharacter_Includes_Line_Breaks()
    {
        Assert.IsTrue(Start.AnyCharacter().MatchesWholeText("\n"));
        Assert.IsTrue(Start.AnyCharacter().MatchesWholeText("\r"));
        Assert.IsFalse(Start.AnyCharacterExceptLineBreak().MatchesWholeText("\n"));
        Assert.IsFalse(Start.AnyCharacterExceptLineBreak().MatchesWholeText("\r"));
        Assert.IsTrue(Start.AnyCharacterExceptLineBreak().MatchesWholeText("a"));
    }

    [TestMethod]
    public void LineBreak_Treats_Crlf_As_One_Break()
    {
        var oneBreak = Start.LineBreak();
        Assert.IsTrue(oneBreak.MatchesWholeText("\r\n"));
        Assert.IsTrue(oneBreak.MatchesWholeText("\r"));
        Assert.IsTrue(oneBreak.MatchesWholeText("\n"));
        Assert.IsFalse(oneBreak.MatchesWholeText("\n\r"));
    }

    [TestMethod]
    public void Whitespace_Kinds()
    {
        Assert.IsTrue(Start.Whitespace(Times.OneOrMore).MatchesWholeText(" \t\r\n"));
        Assert.IsFalse(Start.AnyCharacterExceptWhitespace().MatchesWholeText(" "));
        Assert.IsTrue(Start.AnyCharacterExceptWhitespace().MatchesWholeText("@"));
    }
}
