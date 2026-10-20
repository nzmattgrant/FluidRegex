using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

/// <summary>The original tests, moved to the new names. Each checks the emitted pattern and how it matches.</summary>
[TestClass]
public class MatchThesePatternsTests
{
    [TestMethod]
    public void Match_Everything_But_Whitespace_Wont_Match_WhiteSpace()
    {
        var builder = new MatchThesePatterns()
            .AnyNonWhitespace();
        Assert.AreEqual(@"\S", builder.ToString());
        Assert.IsFalse(builder.IsMatch(" "));
    }

    [TestMethod]
    public void Match_Everything_But_Whitespace_Matches_Non_White_Space()
    {
        var builder = new MatchThesePatterns()
            .AnyNonWhitespace();
        Assert.AreEqual(@"\S", builder.ToString());
        Assert.IsTrue(builder.IsMatch("hello"));
        Assert.IsTrue(builder.IsMatch("$$$fadg"));
        Assert.IsTrue(builder.IsMatch("^%$*& adg"));
        Assert.IsTrue(builder.IsMatch("$"));
    }

    [TestMethod]
    public void Match_The_Character_Matches_Character()
    {
        var builder = new MatchThesePatterns()
            .TheCharacter('@');
        Assert.AreEqual("@", builder.ToString());
        Assert.IsTrue(builder.IsMatch("a@a.a"));
        Assert.IsTrue(builder.IsMatch("a@a@a@"));
        Assert.IsTrue(builder.IsMatch("@@@aadsaf@"));
        Assert.IsTrue(builder.IsMatch("aadsaf@"));
    }

    [TestMethod]
    public void Match_The_Character_Does_Not_Match_Other_Characters()
    {
        var builder = new MatchThesePatterns()
            .TheCharacter('@');
        Assert.AreEqual("@", builder.ToString());
        Assert.IsFalse(builder.IsMatch("a"));
        Assert.IsFalse(builder.IsMatch("aaaaaa..dfalkj343"));
        Assert.IsFalse(builder.IsMatch("#^%&$"));
        Assert.IsFalse(builder.IsMatch(" "));
    }

    [TestMethod]
    public void Match_One_Of_These_Characters_Matches_Characters()
    {
        var builder = new MatchThesePatterns()
            .StartOfText()
            .OneOfTheseCharacters("sz");
        Assert.AreEqual(@"\A[sz]", builder.ToString());
        Assert.IsFalse(builder.IsMatch("astart"));
        Assert.IsFalse(builder.IsMatch(" start"));
        Assert.IsFalse(builder.IsMatch("aaaaaastart start"));
        Assert.IsFalse(builder.IsMatch(" "));
        Assert.IsFalse(builder.IsMatch("aaa"));
        Assert.IsTrue(builder.IsMatch("start of the string"));
        Assert.IsTrue(builder.IsMatch("ztart of the string"));
    }

    [TestMethod]
    public void Match_Start_String_Only_Matches_Start_Of_String()
    {
        var builder = new MatchThesePatterns()
            .StartOfText()
            .TheText("start");
        Assert.AreEqual(@"\Astart", builder.ToString());
        Assert.IsFalse(builder.IsMatch("astart"));
        Assert.IsFalse(builder.IsMatch(" start"));
        Assert.IsFalse(builder.IsMatch("aaaaaastart start"));
        Assert.IsFalse(builder.IsMatch(" "));
        Assert.IsFalse(builder.IsMatch("aaa"));
        Assert.IsTrue(builder.IsMatch("start of the string"));
    }

    [TestMethod]
    public void Match_One_Of_These_Characters_Matches_Right_Characters()
    {
        var builder = new MatchThesePatterns()
            .OneOfTheseCharacters("-+.'");
        Assert.AreEqual("[-+.']", builder.ToString());
        Assert.IsTrue(builder.IsMatch("-"));
        Assert.IsTrue(builder.IsMatch(" blah+blah"));
        Assert.IsTrue(builder.IsMatch("-.start"));
        Assert.IsTrue(builder.IsMatch("..."));
        Assert.IsTrue(builder.IsMatch("'ahhh'"));
        Assert.IsTrue(builder.IsMatch("start of the string..."));
    }

    [TestMethod]
    public void Match_One_Of_These_Characters_Fails_On_No_Match_Strings()
    {
        var builder = new MatchThesePatterns()
            .OneOfTheseCharacters("-+.'");
        Assert.AreEqual("[-+.']", builder.ToString());
        Assert.IsFalse(builder.IsMatch(" "));
        Assert.IsFalse(builder.IsMatch(" blah blah"));
        Assert.IsFalse(builder.IsMatch("start"));
        Assert.IsFalse(builder.IsMatch("///"));
        Assert.IsFalse(builder.IsMatch("ahhh"));
        Assert.IsFalse(builder.IsMatch("start of the string"));
        Assert.IsFalse(builder.IsMatch(","), "',' sits between '+' and '.', so it would match if [+-.] were a range");
    }

    [TestMethod]
    public void LookAhead()
    {
        var builder = new MatchThesePatterns()
            .TheText("X")
            .IfFollowedBy("Y");
        Assert.AreEqual("X(?=Y)", builder.ToString());
        Assert.IsTrue(builder.IsMatch("XYZ"));
        Assert.IsTrue(builder.IsMatch("XY"));
        Assert.IsFalse(builder.IsMatch("XZ"));
        Assert.AreEqual("X", builder.ToRegex().Match("XY").Value, "a lookahead consumes nothing");
    }

    [TestMethod]
    public void Test_Design_Example()
    {
        // was ^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$
        var wordsWithAllowedStartSymbols = new SubPattern()
            .OneOfTheseCharacters("-+.'")
            .AnyWordCharacter(OneOrMore);

        var wordsWithHyphenAndDots = new SubPattern()
            .OneOfTheseCharacters("-.")
            .AnyWordCharacter(OneOrMore);

        var email = new MatchThesePatterns()
            .StartOfText()
            .AnyWordCharacter(OneOrMore)
            .Group(wordsWithAllowedStartSymbols, ZeroOrMore)
            .TheCharacter('@')
            .AnyWordCharacter(OneOrMore)
            .Group(wordsWithHyphenAndDots, ZeroOrMore)
            .TheCharacter('.')
            .AnyWordCharacter(OneOrMore)
            .Group(wordsWithHyphenAndDots, ZeroOrMore)
            .EndOfText();

        Assert.AreEqual(@"\A\w+(?:[-+.']\w+)*@\w+(?:[-.]\w+)*\.\w+(?:[-.]\w+)*\z", email.ToString());
        Assert.IsTrue(email.IsMatch("john.smith@example.co.uk"));
        Assert.IsTrue(email.IsMatch("o'neil+tag@mail.co.uk"));
        Assert.IsFalse(email.IsMatch("john..smith@example.com"));
        Assert.IsFalse(email.IsMatch("john@example"));
        Assert.IsFalse(email.IsMatch("john@example.com\n"), "the old ^...$ pattern accepted a trailing newline");
    }

    [TestMethod]
    public void Test_Design_Example_With_Capturing_Groups()
    {
        var wordsWithAllowedStartSymbols = new SubPattern()
            .OneOfTheseCharacters("-+.'")
            .AnyWordCharacter(OneOrMore);

        var wordsWithHyphenAndDots = new SubPattern()
            .OneOfTheseCharacters("-.")
            .AnyWordCharacter(OneOrMore);

        var email = new MatchThesePatterns()
            .StartOfText()
            .AnyWordCharacter(OneOrMore)
            .Group(new SubPattern().Capture(wordsWithAllowedStartSymbols), ZeroOrMore)
            .TheCharacter('@')
            .AnyWordCharacter(OneOrMore)
            .Group(new SubPattern().Capture(wordsWithHyphenAndDots), ZeroOrMore)
            .TheCharacter('.')
            .AnyWordCharacter(OneOrMore)
            .Group(new SubPattern().Capture(wordsWithHyphenAndDots), ZeroOrMore)
            .EndOfText();

        Assert.AreEqual(@"\A\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*\z", email.ToString());
        Assert.IsTrue(email.IsMatch("john.smith@example.co.uk"));
    }

    [TestMethod]
    public void Test_URL_matcher()
    {
        var hostCharacters = new CharacterSet()
            .TheCharacter('-')
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').CharactersInRange('0', '9')
            .TheCharacters("@:%._+~#=");
        var pathCharacters = new CharacterSet()
            .TheCharacter('-')
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').CharactersInRange('0', '9')
            .TheCharacters("@:%_+.~#()?&//=");
        var path = new SubPattern().OneOfTheseCharacters(pathCharacters, ZeroOrMore);

        var url = new MatchThesePatterns()
            .TheText("http")
            .TheCharacter('s', Optional)
            .TheText("://")
            .Capture(new SubPattern().TheText("www."), Optional)
            .OneOfTheseCharacters(hostCharacters, Between(2, 256))
            .TheCharacter('.')
            .AnyCharacterInRange('a', 'z', Between(2, 6))
            .WordBoundary()
            .Capture(path);

        Assert.AreEqual(
            @"https?://(www\.)?[-a-zA-Z0-9@:%._+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_+.~#()?&/=]*)",
            url.ToString());

        var match = url.ToRegex().Match("https://www.example.com/path?q=1");
        Assert.IsTrue(match.Success);
        Assert.AreEqual("www.", match.Groups[1].Value);
        Assert.AreEqual("/path?q=1", match.Groups[2].Value);
        Assert.IsTrue(url.IsMatch("http://example.org"));
        Assert.IsFalse(url.IsMatch("ftp://example.com"));
        Assert.IsFalse(url.IsMatch("http//example.com"));
    }

    [TestMethod]
    public void LookBehind_Matches_Pattern_Preceded_By_Specific_String()
    {
        var builder = new MatchThesePatterns()
            .IfPrecededBy("X")
            .TheText("Y");
        Assert.AreEqual("(?<=X)Y", builder.ToString());
        Assert.IsTrue(builder.IsMatch("XY"));
        Assert.IsFalse(builder.IsMatch("Y"));
        Assert.IsFalse(builder.IsMatch("ZY"));
    }

    [TestMethod]
    public void NegativeLookAhead_Matches_Pattern_Not_Followed_By_Specific_String()
    {
        var builder = new MatchThesePatterns()
            .TheText("X")
            .IfNotFollowedBy("Y");
        Assert.AreEqual("X(?!Y)", builder.ToString());
        Assert.IsTrue(builder.IsMatch("X"));
        Assert.IsTrue(builder.IsMatch("XZ"));
        Assert.IsFalse(builder.IsMatch("XY"));
    }

    [TestMethod]
    public void NegativeLookBehind_Matches_Pattern_Not_Preceded_By_Specific_String()
    {
        var builder = new MatchThesePatterns()
            .IfNotPrecededBy("X")
            .TheText("Y");
        Assert.AreEqual("(?<!X)Y", builder.ToString());
        Assert.IsTrue(builder.IsMatch("Y"));
        Assert.IsTrue(builder.IsMatch("ZY"));
        Assert.IsFalse(builder.IsMatch("XY"));
    }

    [TestMethod]
    public void AnyWhitespace_Matches_Only_Whitespace()
    {
        // Replaces MatchWhitespace_Matches_Non_Word_Characters, which asserted the old \W bug.
        var builder = new MatchThesePatterns()
            .AnyWhitespace();
        Assert.AreEqual(@"\s", builder.ToString());
        Assert.IsTrue(builder.IsMatch(" "));
        Assert.IsTrue(builder.IsMatch("\t"));
        Assert.IsFalse(builder.IsMatch("@"));
        Assert.IsFalse(builder.IsMatch("a"));
    }

    [TestMethod]
    public void Capture_Wraps_Expression_In_Group()
    {
        // Replaces SetCurrentRegexAsGroup.
        var builder = new MatchThesePatterns()
            .Capture(new SubPattern().TheText("test"));
        Assert.AreEqual("(test)", builder.ToString());
        Assert.AreEqual("test", builder.ToRegex().Match("a test").Groups[1].Value);
    }

    [TestMethod]
    public void RawRegex_Matches_Provided_Pattern()
    {
        var builder = new MatchThesePatterns()
            .RawRegex(@"\d{3}");
        Assert.AreEqual(@"(?:\d{3})", builder.ToString());
        Assert.IsTrue(builder.IsMatch("123"));
        Assert.IsFalse(builder.IsMatch("12"));
    }

    [TestMethod]
    public void Match_String_End_Matches_End_Of_String()
    {
        var builder = new MatchThesePatterns()
            .TheText("end")
            .EndOfText();
        Assert.AreEqual(@"end\z", builder.ToString());
        Assert.IsTrue(builder.IsMatch("end"));
        Assert.IsFalse(builder.IsMatch("ending"));
        Assert.IsFalse(builder.IsMatch("end\n"));
    }

    [TestMethod]
    public void Match_One_Of_These_Characters_With_Min_Max_Quantifiers()
    {
        var builder = new MatchThesePatterns()
            .StartOfText()
            .OneOfTheseCharacters("abc", Between(2, 3))
            .EndOfText();
        Assert.AreEqual(@"\A[abc]{2,3}\z", builder.ToString());
        Assert.IsTrue(builder.IsMatch("ab"));
        Assert.IsTrue(builder.IsMatch("abc"));
        Assert.IsTrue(builder.IsMatch("cba"));
        Assert.IsFalse(builder.IsMatch("a"));
        Assert.IsFalse(builder.IsMatch("abcd"));
    }

    [TestMethod]
    public void TheText_OneOrMore_Repeats_The_Whole_Text()
    {
        // Replaces MatchTheSubstringOnceOrMore, which documented the abc+ bug.
        var builder = new MatchThesePatterns()
            .StartOfText()
            .TheText("abc", OneOrMore)
            .EndOfText();
        Assert.AreEqual(@"\A(?:abc)+\z", builder.ToString());
        Assert.IsTrue(builder.IsMatch("abc"));
        Assert.IsTrue(builder.IsMatch("abcabc"));
        Assert.IsFalse(builder.IsMatch("abcc"));
    }

    [TestMethod]
    public void WordBoundary_Matches_Boundary()
    {
        var builder = new MatchThesePatterns()
            .WordBoundary()
            .TheText("test")
            .WordBoundary();
        Assert.AreEqual(@"\btest\b", builder.ToString());
        Assert.IsTrue(builder.IsMatch("test"));
        Assert.IsTrue(builder.IsMatch("this is a test"));
        Assert.IsFalse(builder.IsMatch("testing"));
        Assert.IsFalse(builder.IsMatch("attest"));
    }
}
