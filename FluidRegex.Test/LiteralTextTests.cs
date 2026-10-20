using System.Text.RegularExpressions;
using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class LiteralTextTests
{
    private const string SpecialCharacters = "\\*+?|{}[]()^$.# \t\n";

    [TestMethod]
    public void TheCharacter_Matches_Every_Special_Character_Literally()
    {
        foreach (var character in SpecialCharacters)
        {
            var builder = new MatchThesePatterns().StartOfText().TheCharacter(character).EndOfText();
            Assert.IsTrue(builder.IsMatch(character.ToString()), $"'{character}' should match itself ({builder})");
            Assert.IsFalse(builder.IsMatch("z"), $"'{character}' should not match 'z' ({builder})");
        }
    }

    [TestMethod]
    public void TheText_Matches_Every_Special_Character_Literally()
    {
        foreach (var character in SpecialCharacters)
        {
            var text = character + "z";
            var builder = new MatchThesePatterns().StartOfText().TheText(text).EndOfText();
            Assert.IsTrue(builder.IsMatch(text), $"\"{text}\" should match itself ({builder})");
            Assert.IsFalse(builder.IsMatch("az"), builder.ToString());
        }
    }

    [TestMethod]
    public void Literals_Stay_Literal_Under_IgnorePatternWhitespace()
    {
        var builder = new MatchThesePatterns()
            .WithOptions(RegexOptions.IgnorePatternWhitespace)
            .StartOfText().TheText("a b#c").EndOfText();
        Assert.IsTrue(builder.IsMatch("a b#c"));
        Assert.IsFalse(builder.IsMatch("ab"));
    }

    [TestMethod]
    public void Characters_That_Need_No_Escape_Are_Left_Alone()
    {
        Assert.AreEqual("://", new MatchThesePatterns().TheText("://").ToString());
        Assert.AreEqual("@-]}", new MatchThesePatterns().TheText("@-]}").ToString());
    }

    [TestMethod]
    public void Counted_Text_Is_Wrapped_Only_When_Longer_Than_One_Character()
    {
        Assert.AreEqual(@"www\.", new MatchThesePatterns().TheText("www.").ToString());
        Assert.AreEqual(@"(?:www\.)?", new MatchThesePatterns().TheText("www.", Optional).ToString());
        Assert.AreEqual("s?", new MatchThesePatterns().TheText("s", Optional).ToString());
        Assert.AreEqual(@"\.+", new MatchThesePatterns().TheCharacter('.', OneOrMore).ToString());
        Assert.AreEqual(@"\(?", new MatchThesePatterns().TheCharacter('(', Optional).ToString());
    }

    [TestMethod]
    public void A_Surrogate_Pair_Repeats_As_One_Unit()
    {
        const string emoji = "\U0001F600";
        var builder = new MatchThesePatterns().StartOfText().TheText(emoji, OneOrMore).EndOfText();
        Assert.IsTrue(builder.IsMatch(emoji + emoji));
    }

    [TestMethod]
    public void Null_Or_Empty_Text_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new MatchThesePatterns().TheText(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().TheText(""));
    }
}
