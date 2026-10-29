using System.Text.RegularExpressions;

namespace FluidRegex.Test;

[TestClass]
public class CharacterSetTests
{
    private static TextPattern OneOf(string characters) => new TextPattern().OneOfTheseCharacters(characters);

    private static TextPattern OneOf(CharacterSet characters) => new TextPattern().OneOfTheseCharacters(characters);

    [TestMethod]
    public void Characters_That_Are_Special_Inside_A_Set_Are_Escaped()
    {
        var set = OneOf("]^\\[");
        Assert.AreEqual(@"[\]\^\\\[]", set.ToString());
        foreach (var character in "]^\\[")
        {
            Assert.IsTrue(set.MatchesWholeText(character.ToString()), character.ToString());
        }

        Assert.IsFalse(set.MatchesWholeText("a"));
    }

    [TestMethod]
    public void A_Hyphen_Is_Always_Written_First()
    {
        Assert.AreEqual("[-a]", OneOf("a-").ToString());
        Assert.AreEqual("[-+.]", OneOf("+-.").ToString());
        Assert.IsFalse(OneOf("+-.").MatchesWholeText(","), "',' lies between '+' and '.', so it would match if this were a range");
        Assert.IsTrue(OneOf("+-.").MatchesWholeText("-"));
        Assert.AreEqual("[-az]", OneOf("-az").ToString());
    }

    [TestMethod]
    public void Duplicates_Are_Dropped()
    {
        Assert.AreEqual("[/=]", OneOf("//=").ToString());
        Assert.AreEqual("[a-z0-9]", OneOf(new CharacterSet().CharactersInRange('a', 'z').Digits().CharactersInRange('a', 'z').Digits()).ToString());
        Assert.AreEqual("[0-9]", new CharacterSet().Digits().CharactersInRange('0', '9').ToString());
    }

    [TestMethod]
    public void Control_Characters_Are_Written_As_Escapes()
    {
        Assert.AreEqual(@"[\t\n\r\f\v]", OneOf("\t\n\r\f\v").ToString());
        Assert.AreEqual(@"[\u0007\u007F]", OneOf("\a\u007F").ToString());
        Assert.IsTrue(OneOf("\t").MatchesWholeText("\t"));
        Assert.IsTrue(OneOf("\a\u007F").MatchesWholeText("\u007F"));
    }

    [TestMethod]
    public void Space_And_Hash_Stay_Literal_Under_IgnorePatternWhitespace()
    {
        var set = new Regex(@"\A" + OneOf(" #") + @"\z", RegexOptions.IgnorePatternWhitespace);
        Assert.IsTrue(set.IsMatch(" "));
        Assert.IsTrue(set.IsMatch("#"));
    }

    [TestMethod]
    public void Ranges_With_Punctuation_Endpoints_Are_Correct()
    {
        var bangToHyphen = OneOf(new CharacterSet().CharactersInRange('!', '-').Characters("/"));
        Assert.IsTrue(bangToHyphen.MatchesWholeText("#"));
        Assert.IsTrue(bangToHyphen.MatchesWholeText(","));
        Assert.IsTrue(bangToHyphen.MatchesWholeText("/"));
        Assert.IsFalse(bangToHyphen.MatchesWholeText("."));

        var aThenHyphenToSlash = OneOf(new CharacterSet().Characters("a").CharactersInRange('-', '/'));
        Assert.IsTrue(aThenHyphenToSlash.MatchesWholeText("."));
        Assert.IsTrue(aThenHyphenToSlash.MatchesWholeText("a"));
        Assert.IsFalse(aThenHyphenToSlash.MatchesWholeText("#"));
    }

    [TestMethod]
    public void A_Range_Of_One_Character_Is_That_Character()
    {
        Assert.AreEqual("[-]", new CharacterSet().CharactersInRange('-', '-').ToString());
        Assert.AreEqual("[a]", new CharacterSet().CharactersInRange('a', 'a').ToString());
    }

    [TestMethod]
    public void Range_Look_Alikes_Throw_With_A_Hint()
    {
        foreach (var lookAlike in new[] { "a-z", "0-9", "x-1", "@A-Z" })
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().OneOfTheseCharacters(lookAlike));
            Assert.AreEqual("characters", exception.ParamName);
            StringAssert.Contains(exception.Message, "AnyCharacterInRange");
        }

        Assert.ThrowsExactly<ArgumentException>(() => new CharacterSet().Characters("a-z"));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().AnyCharacterExcept("0-9"));
    }

    [TestMethod]
    public void Empty_Sets_And_Reversed_Ranges_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().OneOfTheseCharacters(""));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().OneOfTheseCharacters(new CharacterSet()));
        Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().AnyCharacterExcept(new CharacterSet()));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterSet().CharactersInRange('z', 'a'));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TextPattern().AnyCharacterInRange('9', '0'));
    }

    [TestMethod]
    public void AnyCharacterExcept_Emits_A_Negated_Set()
    {
        Assert.AreEqual("[^<>]+", new TextPattern().AnyCharacterExcept("<>", Times.OneOrMore).ToString());
        Assert.AreEqual(@"[^-\^]", new TextPattern().AnyCharacterExcept("^-").ToString());
        Assert.AreEqual("[^\"]*", new TextPattern().AnyCharacterExcept("\"", Times.ZeroOrMore).ToString());

        var notHyphenOrCaret = new TextPattern().AnyCharacterExcept("^-");
        Assert.IsTrue(notHyphenOrCaret.MatchesWholeText("a"));
        Assert.IsFalse(notHyphenOrCaret.MatchesWholeText("-"));
        Assert.IsFalse(notHyphenOrCaret.MatchesWholeText("^"));
    }

    [TestMethod]
    public void Sets_Can_Hold_Kinds_Of_Character()
    {
        var whitespaceOrAt = new CharacterSet().Whitespace().Characters("@");
        Assert.AreEqual(@"[\s@]", OneOf(whitespaceOrAt).ToString());
        Assert.AreEqual(@"[^\s@]+", new TextPattern().AnyCharacterExcept(whitespaceOrAt, Times.OneOrMore).ToString());

        var kinds = new CharacterSet().Letters().UppercaseLetters().LowercaseLetters().Digits().Whitespace();
        Assert.AreEqual(@"[\p{L}\p{Lu}\p{Ll}0-9\s]", kinds.ToString());
        Assert.IsTrue(OneOf(new CharacterSet().Letters().Characters("-.")).MatchesWholeText("é"));
        Assert.IsFalse(OneOf(new CharacterSet().Digits()).MatchesWholeText("٣"), "only 0 to 9");
    }

    [TestMethod]
    public void AnyCharacterInRange_Emits_A_Range()
    {
        Assert.AreEqual("[a-z]{2,6}", new TextPattern().AnyCharacterInRange('a', 'z', Times.Between(2, 6)).ToString());
        var digits = new TextPattern().AnyCharacterInRange('0', '9', Times.OneOrMore);
        Assert.IsTrue(digits.MatchesWholeText("0123456789"));
        Assert.IsFalse(digits.MatchesWholeText("٣"));
    }

    [TestMethod]
    public void Sets_Are_Immutable()
    {
        var common = new CharacterSet().CharactersInRange('a', 'z');
        var withDigits = common.Digits();
        var withDot = common.Characters(".");

        Assert.AreEqual("[a-z]", common.ToString());
        Assert.AreEqual("[a-z0-9]", withDigits.ToString());
        Assert.AreEqual("[a-z.]", withDot.ToString());
    }
}
