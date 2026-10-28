using System.Text.RegularExpressions;

namespace FluidRegex.Test;

[TestClass]
public class LiteralTextTests
{
    private const string SpecialCharacters = "\\*+?|{}[]()^$.# \t\n";

    [TestMethod]
    public void A_Single_Character_Is_Matched_As_Written()
    {
        foreach (var character in SpecialCharacters)
        {
            var builder = new TextPattern().Text(character.ToString());
            Assert.IsTrue(builder.MatchesWholeText(character.ToString()), $"'{character}' should match itself ({builder})");
            Assert.IsFalse(builder.MatchesWholeText("z"), $"'{character}' should not match 'z' ({builder})");
        }
    }

    [TestMethod]
    public void Longer_Text_Is_Matched_As_Written()
    {
        foreach (var character in SpecialCharacters)
        {
            var text = character + "z";
            var builder = new TextPattern().Text(text);
            Assert.IsTrue(builder.MatchesWholeText(text), $"\"{text}\" should match itself ({builder})");
            Assert.IsFalse(builder.MatchesWholeText("az"), builder.ToString());
        }
    }

    [TestMethod]
    public void Text_Stays_Literal_Under_IgnorePatternWhitespace()
    {
        var builder = new Regex(@"\A" + new TextPattern().Text("a b#c") + @"\z", RegexOptions.IgnorePatternWhitespace);
        Assert.IsTrue(builder.IsMatch("a b#c"));
        Assert.IsFalse(builder.IsMatch("ab"));
    }

    [TestMethod]
    public void Characters_That_Need_No_Escape_Are_Left_Alone()
    {
        Assert.AreEqual("://", new TextPattern().Text("://").ToString());
        Assert.AreEqual("@-]}", new TextPattern().Text("@-]}").ToString());
    }

    [TestMethod]
    public void Counted_Text_Is_Wrapped_Only_When_Longer_Than_One_Character()
    {
        Assert.AreEqual(@"www\.", new TextPattern().Text("www.").ToString());
        Assert.AreEqual(@"(?:www\.)?", new TextPattern().Optional().Text("www.").ToString());
        Assert.AreEqual("s?", new TextPattern().Optional().Text("s").ToString());
        Assert.AreEqual(@"\.+", new TextPattern().OneOrMore().Text(".").ToString());
        Assert.AreEqual(@"\(?", new TextPattern().Optional().Text("(").ToString());
    }

    [TestMethod]
    public void A_Surrogate_Pair_Repeats_As_One_Unit()
    {
        const string emoji = "\U0001F600";
        Assert.IsTrue(new TextPattern().OneOrMore().Text(emoji).MatchesWholeText(emoji + emoji));
    }

    [TestMethod]
    public void Null_Or_Empty_Text_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new TextPattern().Text(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().Text(""));
    }
}
