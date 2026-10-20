using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class CharacterClassTests
{
    private static MatchThesePatterns Whole(Func<MatchThesePatterns, MatchThesePatterns> piece) =>
        piece(new MatchThesePatterns().StartOfText()).EndOfText();

    [TestMethod]
    public void Each_Kind_Emits_Its_Token()
    {
        var start = new MatchThesePatterns();
        Assert.AreEqual(".", start.AnyCharacter().ToString());
        Assert.AreEqual(@"[\s\S]", start.AnyCharacterIncludingNewline().ToString());
        Assert.AreEqual(@"\d", start.AnyDigit().ToString());
        Assert.AreEqual(@"\D", start.AnyNonDigit().ToString());
        Assert.AreEqual(@"\p{L}", start.AnyLetter().ToString());
        Assert.AreEqual(@"\P{L}", start.AnyNonLetter().ToString());
        Assert.AreEqual(@"\p{Lu}", start.AnyUppercaseLetter().ToString());
        Assert.AreEqual(@"\p{Ll}", start.AnyLowercaseLetter().ToString());
        Assert.AreEqual(@"\w", start.AnyWordCharacter().ToString());
        Assert.AreEqual(@"\W", start.AnyNonWordCharacter().ToString());
        Assert.AreEqual(@"\s", start.AnyWhitespace().ToString());
        Assert.AreEqual(@"\S", start.AnyNonWhitespace().ToString());
        Assert.AreEqual(@"(?:\r\n|\r|\n)", start.AnyLineBreak().ToString());
    }

    [TestMethod]
    public void Each_Kind_Takes_A_Count()
    {
        Assert.AreEqual(@"\d{4}", new MatchThesePatterns().AnyDigit(Exactly(4)).ToString());
        Assert.AreEqual(@"\w+", new MatchThesePatterns().AnyWordCharacter(OneOrMore).ToString());
        Assert.AreEqual(".{8,}", new MatchThesePatterns().AnyCharacter(AtLeast(8)).ToString());
        Assert.AreEqual(@"(?:\r\n|\r|\n)+", new MatchThesePatterns().AnyLineBreak(OneOrMore).ToString());
    }

    [TestMethod]
    public void Letters_And_Digits_Follow_Unicode()
    {
        Assert.IsTrue(Whole(p => p.AnyLetter()).IsMatch("é"));
        Assert.IsTrue(Whole(p => p.AnyWordCharacter()).IsMatch("é"));
        Assert.IsTrue(Whole(p => p.AnyDigit()).IsMatch("\u0663"), "Arabic-Indic digit three");
        Assert.IsTrue(Whole(p => p.AnyWordCharacter()).IsMatch("_"));
        Assert.IsFalse(Whole(p => p.AnyLetter()).IsMatch("_"));
        Assert.IsTrue(Whole(p => p.AnyNonLetter()).IsMatch("_"));
        Assert.IsTrue(Whole(p => p.AnyUppercaseLetter()).IsMatch("É"));
        Assert.IsFalse(Whole(p => p.AnyUppercaseLetter()).IsMatch("é"));
        Assert.IsTrue(Whole(p => p.AnyLowercaseLetter()).IsMatch("é"));
    }

    [TestMethod]
    public void Uppercase_Letter_Also_Matches_Lowercase_When_Ignoring_Case()
    {
        var upper = new MatchThesePatterns().IgnoringCase().StartOfText().AnyUppercaseLetter().EndOfText();
        Assert.IsTrue(upper.IsMatch("a"));
    }

    [TestMethod]
    public void AnyCharacter_Excludes_Only_Newline()
    {
        Assert.IsFalse(Whole(p => p.AnyCharacter()).IsMatch("\n"));
        Assert.IsTrue(Whole(p => p.AnyCharacter()).IsMatch("\r"));
        Assert.IsTrue(Whole(p => p.AnyCharacterIncludingNewline()).IsMatch("\n"));
    }

    [TestMethod]
    public void AnyLineBreak_Treats_Crlf_As_One_Break()
    {
        var oneBreak = Whole(p => p.AnyLineBreak());
        Assert.IsTrue(oneBreak.IsMatch("\r\n"));
        Assert.IsTrue(oneBreak.IsMatch("\r"));
        Assert.IsTrue(oneBreak.IsMatch("\n"));
        Assert.IsFalse(oneBreak.IsMatch("\n\r"));
    }

    [TestMethod]
    public void Whitespace_Classes()
    {
        Assert.IsTrue(Whole(p => p.AnyWhitespace(OneOrMore)).IsMatch(" \t\r\n"));
        Assert.IsFalse(Whole(p => p.AnyNonWhitespace()).IsMatch(" "));
        Assert.IsTrue(Whole(p => p.AnyNonWordCharacter()).IsMatch("@"));
        Assert.IsTrue(Whole(p => p.AnyNonDigit()).IsMatch("a"));
    }
}
