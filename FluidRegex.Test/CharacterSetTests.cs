using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class CharacterSetTests
{
    private static MatchThesePatterns OneOf(string characters) =>
        new MatchThesePatterns().StartOfText().OneOfTheseCharacters(characters).EndOfText();

    private static MatchThesePatterns OneOf(CharacterSet characters) =>
        new MatchThesePatterns().StartOfText().OneOfTheseCharacters(characters).EndOfText();

    [TestMethod]
    public void Characters_That_Are_Special_Inside_A_Set_Are_Escaped()
    {
        var set = OneOf("]^\\[");
        Assert.AreEqual(@"\A[\]\^\\\[]\z", set.ToString());
        foreach (var character in "]^\\[")
        {
            Assert.IsTrue(set.IsMatch(character.ToString()), character.ToString());
        }

        Assert.IsFalse(set.IsMatch("a"));
    }

    [TestMethod]
    public void A_Hyphen_Is_Always_Written_First()
    {
        Assert.AreEqual("[-a]", new MatchThesePatterns().OneOfTheseCharacters("a-").ToString());
        Assert.AreEqual("[-+.]", new MatchThesePatterns().OneOfTheseCharacters("+-.").ToString());
        Assert.IsFalse(OneOf("+-.").IsMatch(","), "',' lies between '+' and '.', so it would match if this were a range");
        Assert.IsTrue(OneOf("+-.").IsMatch("-"));
        Assert.AreEqual("[-az]", new MatchThesePatterns().OneOfTheseCharacters("-az").ToString());
    }

    [TestMethod]
    public void Duplicates_Are_Dropped()
    {
        Assert.AreEqual("[/=]", new MatchThesePatterns().OneOfTheseCharacters("//=").ToString());
        Assert.AreEqual(@"[a-z\d]", new MatchThesePatterns()
            .OneOfTheseCharacters(new CharacterSet().CharactersInRange('a', 'z').Digits().CharactersInRange('a', 'z').Digits())
            .ToString());
    }

    [TestMethod]
    public void Control_Characters_Are_Written_As_Escapes()
    {
        Assert.AreEqual(@"[\t\n\r\f\v]", new MatchThesePatterns().OneOfTheseCharacters("\t\n\r\f\v").ToString());
        Assert.AreEqual(@"[\u0007\u007F]", new MatchThesePatterns().OneOfTheseCharacters("\a\u007F").ToString());
        Assert.IsTrue(OneOf("\t").IsMatch("\t"));
        Assert.IsTrue(OneOf("\a\u007F").IsMatch("\u007F"));
    }

    [TestMethod]
    public void Space_And_Hash_Stay_Literal_Under_IgnorePatternWhitespace()
    {
        var set = new MatchThesePatterns()
            .WithOptions(System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace)
            .StartOfText().OneOfTheseCharacters(" #").EndOfText();
        Assert.IsTrue(set.IsMatch(" "));
        Assert.IsTrue(set.IsMatch("#"));
    }

    [TestMethod]
    public void Ranges_With_Punctuation_Endpoints_Are_Correct()
    {
        var bangToHyphen = OneOf(new CharacterSet().CharactersInRange('!', '-').TheCharacter('/'));
        Assert.IsTrue(bangToHyphen.IsMatch("#"));
        Assert.IsTrue(bangToHyphen.IsMatch(","));
        Assert.IsTrue(bangToHyphen.IsMatch("/"));
        Assert.IsFalse(bangToHyphen.IsMatch("."));

        var aThenHyphenToSlash = OneOf(new CharacterSet().TheCharacter('a').CharactersInRange('-', '/'));
        Assert.IsTrue(aThenHyphenToSlash.IsMatch("."));
        Assert.IsTrue(aThenHyphenToSlash.IsMatch("a"));
        Assert.IsFalse(aThenHyphenToSlash.IsMatch("#"));
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
            var exception = Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().OneOfTheseCharacters(lookAlike));
            Assert.AreEqual("characters", exception.ParamName);
            StringAssert.Contains(exception.Message, "AnyCharacterInRange");
        }

        Assert.ThrowsExactly<ArgumentException>(() => new CharacterSet().TheCharacters("a-z"));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().AnyCharacterExcept("0-9"));
    }

    [TestMethod]
    public void Empty_Sets_And_Reversed_Ranges_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().OneOfTheseCharacters(""));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().OneOfTheseCharacters(new CharacterSet()));
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().AnyCharacterExcept(new CharacterSet()));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterSet().CharactersInRange('z', 'a'));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MatchThesePatterns().AnyCharacterInRange('9', '0'));
    }

    [TestMethod]
    public void AnyCharacterExcept_Emits_A_Negated_Set()
    {
        Assert.AreEqual("[^<>]+", new MatchThesePatterns().AnyCharacterExcept("<>", OneOrMore).ToString());
        Assert.AreEqual(@"[^-\^]", new MatchThesePatterns().AnyCharacterExcept("^-").ToString());
        Assert.AreEqual("[^\"]*", new MatchThesePatterns().AnyCharacterExcept("\"", ZeroOrMore).ToString());

        var notHyphenOrCaret = new MatchThesePatterns().StartOfText().AnyCharacterExcept("^-").EndOfText();
        Assert.IsTrue(notHyphenOrCaret.IsMatch("a"));
        Assert.IsFalse(notHyphenOrCaret.IsMatch("-"));
        Assert.IsFalse(notHyphenOrCaret.IsMatch("^"));
    }

    [TestMethod]
    public void Sets_Can_Hold_Character_Classes()
    {
        var notWhitespaceOrAt = new CharacterSet().Whitespace().TheCharacter('@');
        Assert.AreEqual(@"[\s@]", new MatchThesePatterns().OneOfTheseCharacters(notWhitespaceOrAt).ToString());
        Assert.AreEqual(@"[^\s@]+", new MatchThesePatterns().AnyCharacterExcept(notWhitespaceOrAt, OneOrMore).ToString());

        var classes = new CharacterSet().Letters().UppercaseLetters().LowercaseLetters().Digits().WordCharacters().Whitespace();
        Assert.AreEqual(@"[\p{L}\p{Lu}\p{Ll}\d\w\s]", classes.ToString());
        Assert.IsTrue(OneOf(new CharacterSet().WordCharacters().TheCharacters("-.")).IsMatch("é"));
    }

    [TestMethod]
    public void AnyCharacterInRange_Emits_A_Range()
    {
        Assert.AreEqual("[a-z]{2,6}", new MatchThesePatterns().AnyCharacterInRange('a', 'z', Between(2, 6)).ToString());
        var digits = new MatchThesePatterns().StartOfText().AnyCharacterInRange('0', '9', OneOrMore).EndOfText();
        Assert.IsTrue(digits.IsMatch("0123456789"));
        Assert.IsFalse(digits.IsMatch("\u0663"), "unlike AnyDigit, the range is ASCII only");
    }

    [TestMethod]
    public void Sets_Are_Immutable()
    {
        var common = new CharacterSet().CharactersInRange('a', 'z');
        var withDigits = common.Digits();
        var withDot = common.TheCharacter('.');

        Assert.AreEqual("[a-z]", common.ToString());
        Assert.AreEqual(@"[a-z\d]", withDigits.ToString());
        Assert.AreEqual(@"[a-z.]", withDot.ToString());
    }
}
