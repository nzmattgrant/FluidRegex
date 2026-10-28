namespace FluidRegex.Test;

/// <summary>Every snippet in README.md, exactly as written there. Add a test here with every new README snippet.</summary>
[TestClass]
public class ReadmeExampleTests
{
    [TestMethod]
    public void Opening_Iso_Date()
    {
        var isoDate = new TextPattern()
            .Exactly(4).Digit().Text("-")
            .Exactly(2).Digit().Text("-")
            .Exactly(2).Digit();

        Assert.IsTrue(isoDate.MatchesWholeText("2026-10-12"));
        Assert.AreEqual("[0-9]{4}-[0-9]{2}-[0-9]{2}", isoDate.ToString());
    }

    [TestMethod]
    public void Email()
    {
        var word = new TextPattern()
            .OneOrMore().OneOfTheseCharacters(new CharacterSet().Letters().Digits().Characters("_"));
        var symbolThenWord = new TextPattern().OneOfTheseCharacters("-+.'").Pattern(word);
        var dotOrHyphenThenWord = new TextPattern().OneOfTheseCharacters("-.").Pattern(word);

        var email = new TextPattern()
            .Pattern(word)
            .ZeroOrMore().Pattern(symbolThenWord)
            .Text("@")
            .Pattern(word)
            .ZeroOrMore().Pattern(dotOrHyphenThenWord)
            .Text(".")
            .Pattern(word)
            .ZeroOrMore().Pattern(dotOrHyphenThenWord);

        Assert.IsTrue(email.MatchesWholeText("john.smith@example.co.uk"));
        Assert.IsFalse(email.MatchesWholeText("junk john@example.com"));
        Assert.IsTrue(email.MatchesWholeText("o'neil+tag@mail.co.uk"));
        Assert.IsFalse(email.MatchesWholeText("john..smith@example.com"));
        Assert.IsFalse(email.MatchesWholeText("john@example"));
        Assert.IsFalse(email.MatchesWholeText("john@example.com\n"));
    }

    [TestMethod]
    public void Why_Bullets()
    {
        Assert.IsTrue(new TextPattern().Text("a+b").IsFoundIn("a+b"));
        Assert.AreEqual("(?:abc)+", new TextPattern().OneOrMore().Text("abc").ToString());
        Assert.IsTrue(new TextPattern().Digit().IsFoundIn("7"));
        Assert.IsFalse(new TextPattern().Digit().IsFoundIn("٣"));
        Assert.IsTrue(new TextPattern().AnyCharacter().IsFoundIn("\n"));
        var exception = Assert.ThrowsExactly<ArgumentException>(() => new TextPattern().OneOfTheseCharacters("a-z"));
        StringAssert.Contains(exception.Message, "AnyCharacterInRange('a', 'z')");
    }

    [TestMethod]
    public void Cheat_Sheet_Rows()
    {
        var start = new TextPattern();
        Assert.AreEqual(@"www\.", start.Text("www.").ToString());
        Assert.AreEqual("[^<>]", start.AnyCharacterExcept("<>").ToString());
        Assert.AreEqual("[a-z]", start.AnyCharacterInRange('a', 'z').ToString());
        Assert.AreEqual(@"(?:\p{IsGreek})", start.RawRegex(@"\p{IsGreek}").ToString());
        Assert.AreEqual("a{0,3}", start.AtMost(3).Text("a").ToString());
        Assert.AreEqual(@"\b(?=\w)", start.StartOfWord().ToString());
        Assert.AreEqual(@"\b(?<=\w)", start.EndOfWord().ToString());
        Assert.AreEqual(@"(?<=\$)", start.IfPrecededBy("$").ToString());
        Assert.AreEqual(@"(?<!\$)", start.IfNotPrecededBy("$").ToString());
        Assert.AreEqual("<a>", start.Text("<").OneOrMore().AsFewAsPossible().AnyCharacter().Text(">").FindFirst("<a><b>")!.Text);
        Assert.AreEqual("<a><b>", start.Text("<").OneOrMore().AnyCharacter().Text(">").FindFirst("<a><b>")!.Text);
    }

    [TestMethod]
    public void Url()
    {
        var hostCharacters = new CharacterSet()
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').Digits()
            .Characters("-@:%._+~#=");
        var pathCharacters = new CharacterSet()
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').Digits()
            .Characters("-@:%_+.~#()?&/=");
        var path = new TextPattern().ZeroOrMore().OneOfTheseCharacters(pathCharacters);

        var url = new TextPattern()
            .Text("http")
            .Optional().Text("s")
            .Text("://")
            .Optional().Text("www.")
            .Between(2, 256).OneOfTheseCharacters(hostCharacters)
            .Text(".")
            .Between(2, 6).AnyCharacterInRange('a', 'z')
            .EndOfWord()
            .SaveAs("path", path);

        Assert.AreEqual(
            @"https?://(?:www\.)?[-a-zA-Z0-9@:%._+~#=]{2,256}\.[a-z]{2,6}\b(?<=\w)(?<path>[-a-zA-Z0-9@:%_+.~#()?&/=]*)",
            url.ToString());
        Assert.AreEqual("/path?q=1", url.FindFirst("Go to https://www.example.com/path?q=1")?.Saved("path"));
        Assert.IsTrue(url.IsFoundIn("http://example.org"));
        Assert.IsFalse(url.IsFoundIn("ftp://example.com"));
        Assert.IsFalse(url.IsFoundIn("http//example.com"));
    }

    [TestMethod]
    public void Iso_Date_With_Saved_Parts()
    {
        var fourDigits = new TextPattern().Exactly(4).Digit();
        var twoDigits = new TextPattern().Exactly(2).Digit();

        var isoDate = new TextPattern()
            .SaveAs("year", fourDigits)
            .Text("-")
            .SaveAs("month", twoDigits)
            .Text("-")
            .SaveAs("day", twoDigits);

        Assert.AreEqual("(?<year>[0-9]{4})-(?<month>[0-9]{2})-(?<day>[0-9]{2})", isoDate.ToString());
        var found = isoDate.FindFirst("Due on 2026-10-12.");
        Assert.AreEqual("2026", found?.Saved("year"));
        Assert.AreEqual("10", found?.Saved("month"));
        Assert.AreEqual("12", found?.Saved("day"));
        Assert.IsFalse(isoDate.MatchesWholeText("2026-1-12"));
        Assert.IsFalse(isoDate.MatchesWholeText("2026-10-12\n"));
    }

    [TestMethod]
    public void Us_Phone_Number()
    {
        var usPhone = new TextPattern()
            .Optional().Text("(")
            .Exactly(3).Digit()
            .Optional().Text(")")
            .Optional().OneOfTheseCharacters("-. ")
            .Exactly(3).Digit()
            .Optional().OneOfTheseCharacters("-. ")
            .Exactly(4).Digit();

        Assert.AreEqual(@"\(?[0-9]{3}\)?[-. ]?[0-9]{3}[-. ]?[0-9]{4}", usPhone.ToString());
        Assert.IsTrue(usPhone.MatchesWholeText("(555) 123-4567"));
        Assert.IsTrue(usPhone.MatchesWholeText("555.123.4567"));
        Assert.IsTrue(usPhone.MatchesWholeText("5551234567"));
        Assert.IsFalse(usPhone.MatchesWholeText("555-1234"));
        Assert.IsFalse(usPhone.MatchesWholeText("555_123_4567"));
    }

    [TestMethod]
    public void Either_Or_Ignoring_Case()
    {
        var catsOrDogs = new TextPattern()
            .IgnoringCase()
            .StartOfWord()
            .EitherOf("cat", "dog")
            .Optional().Text("s")
            .EndOfWord();

        Assert.AreEqual(@"\b(?=\w)(?:cat|dog)s?\b(?<=\w)", catsOrDogs.ToString());
        Assert.IsTrue(catsOrDogs.IsFoundIn("I love Cats"));
        Assert.IsTrue(catsOrDogs.IsFoundIn("I have two DOGS"));
        Assert.IsFalse(catsOrDogs.IsFoundIn("concatenate"));
        Assert.IsFalse(catsOrDogs.IsFoundIn("hotdogs"));
    }

    [TestMethod]
    public void Repeated_Words()
    {
        var aWord = new TextPattern().OneOrMore().Letter();

        var repeatedWord = new TextPattern()
            .StartOfWord()
            .SaveAs("word", aWord)
            .OneOrMore().Whitespace()
            .SameTextAsSaved("word")
            .EndOfWord();

        Assert.AreEqual(@"\b(?=\w)(?<word>\p{L}+)\s+\k<word>\b(?<=\w)", repeatedWord.ToString());
        Assert.AreEqual("the", repeatedWord.FindFirst("Paris in the the spring")?.Saved("word"));
        Assert.IsFalse(repeatedWord.IsFoundIn("the theory"));
        Assert.IsFalse(repeatedWord.IsFoundIn("is island"));
    }

    [TestMethod]
    public void Password_Rule()
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
    }

    [TestMethod]
    public void Find_And_Replace()
    {
        var fourDigitsAndHyphen = new TextPattern().Exactly(4).Digit().Text("-");

        var cardNumber = new TextPattern()
            .Exactly(3).Pattern(fourDigitsAndHyphen)
            .Exactly(4).Digit();

        Assert.AreEqual("(?:[0-9]{4}-){3}[0-9]{4}", cardNumber.ToString());
        Assert.AreEqual("Card [hidden] was used.", cardNumber.ReplaceAll("Card 1234-5678-9012-3456 was used.", "[hidden]"));
    }
}
