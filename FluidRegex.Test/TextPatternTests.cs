namespace FluidRegex.Test;

/// <summary>The original tests, moved to the new names. Each checks the emitted pattern and how it matches.</summary>
[TestClass]
public class TextPatternTests
{
    [TestMethod]
    public void AnyCharacterExceptWhitespace_Wont_Match_Whitespace()
    {
        var builder = new TextPattern()
            .AnyCharacterExceptWhitespace();
        Assert.AreEqual(@"\S", builder.ToString());
        Assert.IsFalse(builder.IsFoundIn(" "));
    }

    [TestMethod]
    public void AnyCharacterExceptWhitespace_Matches_Everything_Else()
    {
        var builder = new TextPattern()
            .AnyCharacterExceptWhitespace();
        Assert.AreEqual(@"\S", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("hello"));
        Assert.IsTrue(builder.IsFoundIn("$$$fadg"));
        Assert.IsTrue(builder.IsFoundIn("^%$*& adg"));
        Assert.IsTrue(builder.IsFoundIn("$"));
    }

    [TestMethod]
    public void A_Character_Matches_That_Character()
    {
        var builder = new TextPattern()
            .Text("@");
        Assert.AreEqual("@", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("a@a.a"));
        Assert.IsTrue(builder.IsFoundIn("a@a@a@"));
        Assert.IsTrue(builder.IsFoundIn("@@@aadsaf@"));
        Assert.IsTrue(builder.IsFoundIn("aadsaf@"));
    }

    [TestMethod]
    public void A_Character_Does_Not_Match_Other_Characters()
    {
        var builder = new TextPattern()
            .Text("@");
        Assert.AreEqual("@", builder.ToString());
        Assert.IsFalse(builder.IsFoundIn("a"));
        Assert.IsFalse(builder.IsFoundIn("aaaaaa..dfalkj343"));
        Assert.IsFalse(builder.IsFoundIn("#^%&$"));
        Assert.IsFalse(builder.IsFoundIn(" "));
    }

    [TestMethod]
    public void OneOfTheseCharacters_At_The_Start()
    {
        var builder = new TextPattern()
            .StartOfText()
            .OneOfTheseCharacters("sz");
        Assert.AreEqual(@"\A[sz]", builder.ToString());
        Assert.IsFalse(builder.IsFoundIn("astart"));
        Assert.IsFalse(builder.IsFoundIn(" start"));
        Assert.IsFalse(builder.IsFoundIn("aaaaaastart start"));
        Assert.IsFalse(builder.IsFoundIn(" "));
        Assert.IsFalse(builder.IsFoundIn("aaa"));
        Assert.IsTrue(builder.IsFoundIn("start of the string"));
        Assert.IsTrue(builder.IsFoundIn("ztart of the string"));
    }

    [TestMethod]
    public void StartOfText_Only_Matches_The_Start()
    {
        var builder = new TextPattern()
            .StartOfText()
            .Text("start");
        Assert.AreEqual(@"\Astart", builder.ToString());
        Assert.IsFalse(builder.IsFoundIn("astart"));
        Assert.IsFalse(builder.IsFoundIn(" start"));
        Assert.IsFalse(builder.IsFoundIn("aaaaaastart start"));
        Assert.IsFalse(builder.IsFoundIn(" "));
        Assert.IsFalse(builder.IsFoundIn("aaa"));
        Assert.IsTrue(builder.IsFoundIn("start of the string"));
    }

    [TestMethod]
    public void OneOfTheseCharacters_Matches_The_Right_Characters()
    {
        var builder = new TextPattern()
            .OneOfTheseCharacters("-+.'");
        Assert.AreEqual("[-+.']", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("-"));
        Assert.IsTrue(builder.IsFoundIn(" blah+blah"));
        Assert.IsTrue(builder.IsFoundIn("-.start"));
        Assert.IsTrue(builder.IsFoundIn("..."));
        Assert.IsTrue(builder.IsFoundIn("'ahhh'"));
        Assert.IsTrue(builder.IsFoundIn("start of the string..."));
    }

    [TestMethod]
    public void OneOfTheseCharacters_Fails_When_None_Are_There()
    {
        var builder = new TextPattern()
            .OneOfTheseCharacters("-+.'");
        Assert.AreEqual("[-+.']", builder.ToString());
        Assert.IsFalse(builder.IsFoundIn(" "));
        Assert.IsFalse(builder.IsFoundIn(" blah blah"));
        Assert.IsFalse(builder.IsFoundIn("start"));
        Assert.IsFalse(builder.IsFoundIn("///"));
        Assert.IsFalse(builder.IsFoundIn("ahhh"));
        Assert.IsFalse(builder.IsFoundIn("start of the string"));
        Assert.IsFalse(builder.IsFoundIn(","), "',' sits between '+' and '.', so it would match if [+-.] were a range");
    }

    [TestMethod]
    public void TextAfterHereStartsWith()
    {
        var builder = new TextPattern()
            .Text("X")
            .TextAfterHereStartsWith("Y");
        Assert.AreEqual("X(?=Y)", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("XYZ"));
        Assert.IsTrue(builder.IsFoundIn("XY"));
        Assert.IsFalse(builder.IsFoundIn("XZ"));
        Assert.AreEqual("X", builder.FindFirst("XY")!.Text, "a condition adds nothing to what is found");
    }

    [TestMethod]
    public void Test_Design_Example()
    {
        // was ^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$
        var word = new TextPattern().OneOfTheseCharacters(new CharacterSet().Letters().Digits().Characters("_"), Times.OneOrMore);
        var symbolThenWord = new TextPattern()
            .OneOfTheseCharacters("-+.'")
            .Pattern(word);
        var dotOrHyphenThenWord = new TextPattern()
            .OneOfTheseCharacters("-.")
            .Pattern(word);

        var email = new TextPattern()
            .StartOfText()
            .Pattern(word)
            .Pattern(symbolThenWord, Times.ZeroOrMore)
            .Text("@")
            .Pattern(word)
            .Pattern(dotOrHyphenThenWord, Times.ZeroOrMore)
            .Text(".")
            .Pattern(word)
            .Pattern(dotOrHyphenThenWord, Times.ZeroOrMore)
            .EndOfText();

        Assert.AreEqual(
            @"\A[\p{L}0-9_]+(?:[-+.'][\p{L}0-9_]+)*@[\p{L}0-9_]+(?:[-.][\p{L}0-9_]+)*\.[\p{L}0-9_]+(?:[-.][\p{L}0-9_]+)*\z",
            email.ToString());
        Assert.IsTrue(email.IsFoundIn("john.smith@example.co.uk"));
        Assert.IsTrue(email.IsFoundIn("o'neil+tag@mail.co.uk"));
        Assert.IsTrue(email.IsFoundIn("john_smith@example.com"));
        Assert.IsFalse(email.IsFoundIn("john..smith@example.com"));
        Assert.IsFalse(email.IsFoundIn("john@example"));
        Assert.IsFalse(email.IsFoundIn("john@example.com\n"), "the old ^...$ pattern accepted a trailing newline");
    }

    [TestMethod]
    public void Test_Design_Example_With_Saved_Parts()
    {
        var word = new TextPattern().LetterOrDigit(Times.OneOrMore);
        var dotOrHyphenThenWord = new TextPattern()
            .OneOfTheseCharacters("-.")
            .Pattern(word);

        var email = new TextPattern()
            .SaveTextAs("user", word)
            .Text("@")
            .SaveTextAs("domain", new TextPattern().Pattern(word).Pattern(dotOrHyphenThenWord, Times.ZeroOrMore))
            .Text(".")
            .SaveTextAs("ending", word);

        Assert.AreEqual(@"(?<user>[\p{L}0-9]+)@(?<domain>[\p{L}0-9]+(?:[-.][\p{L}0-9]+)*)\.(?<ending>[\p{L}0-9]+)", email.ToString());
        var found = email.FindFirst("john@mail.example.uk")!;
        Assert.AreEqual("john", found.Saved("user"));
        Assert.AreEqual("mail.example", found.Saved("domain"));
        Assert.AreEqual("uk", found.Saved("ending"));
    }

    [TestMethod]
    public void Test_URL_matcher()
    {
        var hostCharacters = new CharacterSet()
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').Digits()
            .Characters("-@:%._+~#=");
        var pathCharacters = new CharacterSet()
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').Digits()
            .Characters("-@:%_+.~#()?&//=");
        var path = new TextPattern().OneOfTheseCharacters(pathCharacters, Times.ZeroOrMore);

        var url = new TextPattern()
            .Text("http")
            .Text("s", Times.Optional)
            .Text("://")
            .SaveTextAs("www", new TextPattern().Text("www."), Times.Optional)
            .OneOfTheseCharacters(hostCharacters, Times.Between(2, 256))
            .Text(".")
            .AnyCharacterInRange('a', 'z', Times.Between(2, 6))
            .EndOfWord()
            .SaveTextAs("path", path);

        Assert.AreEqual(
            @"https?://(?<www>www\.)?[-a-zA-Z0-9@:%._+~#=]{2,256}\.[a-z]{2,6}\b(?<=\w)(?<path>[-a-zA-Z0-9@:%_+.~#()?&/=]*)",
            url.ToString());

        var found = url.FindFirst("https://www.example.com/path?q=1");
        Assert.IsNotNull(found);
        Assert.AreEqual("www.", found.Saved("www"));
        Assert.AreEqual("/path?q=1", found.Saved("path"));
        Assert.IsTrue(url.IsFoundIn("http://example.org"));
        Assert.IsFalse(url.IsFoundIn("ftp://example.com"));
        Assert.IsFalse(url.IsFoundIn("http//example.com"));
    }

    [TestMethod]
    public void TextBeforeHereEndsWith_Matches_Only_After_The_Text()
    {
        var builder = new TextPattern()
            .TextBeforeHereEndsWith("X")
            .Text("Y");
        Assert.AreEqual("(?<=X)Y", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("XY"));
        Assert.IsFalse(builder.IsFoundIn("Y"));
        Assert.IsFalse(builder.IsFoundIn("ZY"));
    }

    [TestMethod]
    public void TextAfterHereDoesNotStartWith_Matches_Only_When_The_Text_Is_Not_Next()
    {
        var builder = new TextPattern()
            .Text("X")
            .TextAfterHereDoesNotStartWith("Y");
        Assert.AreEqual("X(?!Y)", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("X"));
        Assert.IsTrue(builder.IsFoundIn("XZ"));
        Assert.IsFalse(builder.IsFoundIn("XY"));
    }

    [TestMethod]
    public void TextBeforeHereDoesNotEndWith_Matches_Only_When_The_Text_Is_Not_Before()
    {
        var builder = new TextPattern()
            .TextBeforeHereDoesNotEndWith("X")
            .Text("Y");
        Assert.AreEqual("(?<!X)Y", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("Y"));
        Assert.IsTrue(builder.IsFoundIn("ZY"));
        Assert.IsFalse(builder.IsFoundIn("XY"));
    }

    [TestMethod]
    public void Whitespace_Matches_Only_Whitespace()
    {
        var builder = new TextPattern()
            .Whitespace();
        Assert.AreEqual(@"\s", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn(" "));
        Assert.IsTrue(builder.IsFoundIn("\t"));
        Assert.IsFalse(builder.IsFoundIn("@"));
        Assert.IsFalse(builder.IsFoundIn("a"));
    }

    [TestMethod]
    public void SaveTextAs_Wraps_The_Pattern_In_A_Named_Group()
    {
        var builder = new TextPattern()
            .SaveTextAs("word", new TextPattern().Text("test"));
        Assert.AreEqual("(?<word>test)", builder.ToString());
        Assert.AreEqual("test", builder.FindFirst("a test")!.Saved("word"));
    }

    [TestMethod]
    public void RawRegex_Matches_Provided_Pattern()
    {
        var builder = new TextPattern()
            .RawRegex(@"\d{3}");
        Assert.AreEqual(@"(?:\d{3})", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("123"));
        Assert.IsFalse(builder.IsFoundIn("12"));
    }

    [TestMethod]
    public void EndOfText_Matches_Only_The_End()
    {
        var builder = new TextPattern()
            .Text("end")
            .EndOfText();
        Assert.AreEqual(@"end\z", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("end"));
        Assert.IsFalse(builder.IsFoundIn("ending"));
        Assert.IsFalse(builder.IsFoundIn("end\n"));
    }

    [TestMethod]
    public void OneOfTheseCharacters_Between_Two_And_Three_Times()
    {
        var builder = new TextPattern()
            .StartOfText()
            .OneOfTheseCharacters("abc", Times.Between(2, 3))
            .EndOfText();
        Assert.AreEqual(@"\A[abc]{2,3}\z", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("ab"));
        Assert.IsTrue(builder.IsFoundIn("abc"));
        Assert.IsTrue(builder.IsFoundIn("cba"));
        Assert.IsFalse(builder.IsFoundIn("a"));
        Assert.IsFalse(builder.IsFoundIn("abcd"));
    }

    [TestMethod]
    public void OneOrMore_Text_Repeats_The_Whole_Text()
    {
        var builder = new TextPattern()
            .StartOfText()
            .Text("abc", Times.OneOrMore)
            .EndOfText();
        Assert.AreEqual(@"\A(?:abc)+\z", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("abc"));
        Assert.IsTrue(builder.IsFoundIn("abcabc"));
        Assert.IsFalse(builder.IsFoundIn("abcc"));
    }

    [TestMethod]
    public void StartOfWord_And_EndOfWord_Match_A_Whole_Word()
    {
        var builder = new TextPattern()
            .StartOfWord()
            .Text("test")
            .EndOfWord();
        Assert.AreEqual(@"\b(?=\w)test\b(?<=\w)", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("test"));
        Assert.IsTrue(builder.IsFoundIn("this is a test"));
        Assert.IsFalse(builder.IsFoundIn("testing"));
        Assert.IsFalse(builder.IsFoundIn("attest"));
    }
}
