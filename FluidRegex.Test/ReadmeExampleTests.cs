using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

/// <summary>Every snippet in README.md, exactly as written there. Add a test here with every new README snippet.</summary>
[TestClass]
public class ReadmeExampleTests
{
    [TestMethod]
    public void Opening_Iso_Date()
    {
        var isoDate = new MatchThesePatterns()
            .StartOfText()
            .AnyDigit(Exactly(4)).TheCharacter('-')
            .AnyDigit(Exactly(2)).TheCharacter('-')
            .AnyDigit(Exactly(2))
            .EndOfText();

        Assert.IsTrue(isoDate.IsMatch("2026-10-12"));
        Assert.AreEqual(@"\A\d{4}-\d{2}-\d{2}\z", isoDate.ToString());
    }

    [TestMethod]
    public void Email()
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
        Assert.IsFalse(email.IsMatch("john@example.com\n"));
    }

    [TestMethod]
    public void Why_Bullets()
    {
        Assert.IsTrue(new MatchThesePatterns().TheText("a+b").IsMatch("a+b"));
        Assert.AreEqual("(?:abc)+", new MatchThesePatterns().TheText("abc", OneOrMore).ToString());
        Assert.ThrowsExactly<ArgumentException>(() => new MatchThesePatterns().OneOfTheseCharacters("a-z"));
    }

    [TestMethod]
    public void Url()
    {
        var hostCharacters = new CharacterSet()
            .TheCharacter('-')
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').CharactersInRange('0', '9')
            .TheCharacters("@:%._+~#=");
        var pathCharacters = new CharacterSet()
            .TheCharacter('-')
            .CharactersInRange('a', 'z').CharactersInRange('A', 'Z').CharactersInRange('0', '9')
            .TheCharacters("@:%_+.~#()?&/=");
        var path = new SubPattern().OneOfTheseCharacters(pathCharacters, ZeroOrMore);

        var url = new MatchThesePatterns()
            .TheText("http")
            .TheCharacter('s', Optional)
            .TheText("://")
            .TheText("www.", Optional)
            .OneOfTheseCharacters(hostCharacters, Between(2, 256))
            .TheCharacter('.')
            .AnyCharacterInRange('a', 'z', Between(2, 6))
            .WordBoundary()
            .Capture(path);

        Assert.AreEqual(
            @"https?://(?:www\.)?[-a-zA-Z0-9@:%._+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_+.~#()?&/=]*)",
            url.ToString());
        Assert.AreEqual("/path?q=1", url.ToRegex().Match("https://www.example.com/path?q=1").Groups[1].Value);
        Assert.IsTrue(url.IsMatch("http://example.org"));
        Assert.IsFalse(url.IsMatch("ftp://example.com"));
        Assert.IsFalse(url.IsMatch("http//example.com"));
    }

    [TestMethod]
    public void Iso_Date_With_Named_Captures()
    {
        var fourDigits = new SubPattern().AnyDigit(Exactly(4));
        var twoDigits = new SubPattern().AnyDigit(Exactly(2));

        var isoDate = new MatchThesePatterns()
            .StartOfText()
            .CaptureAs("year", fourDigits)
            .TheCharacter('-')
            .CaptureAs("month", twoDigits)
            .TheCharacter('-')
            .CaptureAs("day", twoDigits)
            .EndOfText();

        Assert.AreEqual(@"\A(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\z", isoDate.ToString());
        var match = isoDate.ToRegex().Match("2026-10-12");
        Assert.AreEqual("2026", match.Groups["year"].Value);
        Assert.AreEqual("10", match.Groups["month"].Value);
        Assert.AreEqual("12", match.Groups["day"].Value);
        Assert.IsFalse(isoDate.IsMatch("2026-1-12"));
        Assert.IsFalse(isoDate.IsMatch("2026-10-12\n"));
    }

    [TestMethod]
    public void Us_Phone_Number()
    {
        var usPhone = new MatchThesePatterns()
            .StartOfText()
            .TheCharacter('(', Optional)
            .AnyDigit(Exactly(3))
            .TheCharacter(')', Optional)
            .OneOfTheseCharacters("-. ", Optional)
            .AnyDigit(Exactly(3))
            .OneOfTheseCharacters("-. ", Optional)
            .AnyDigit(Exactly(4))
            .EndOfText();

        Assert.AreEqual(@"\A\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}\z", usPhone.ToString());
        Assert.IsTrue(usPhone.IsMatch("(555) 123-4567"));
        Assert.IsTrue(usPhone.IsMatch("555.123.4567"));
        Assert.IsTrue(usPhone.IsMatch("5551234567"));
        Assert.IsFalse(usPhone.IsMatch("555-1234"));
        Assert.IsFalse(usPhone.IsMatch("555_123_4567"));
    }

    [TestMethod]
    public void Either_Or_Ignoring_Case()
    {
        var catsOrDogs = new MatchThesePatterns()
            .IgnoringCase()
            .WordBoundary()
            .EitherOf("cat", "dog")
            .TheCharacter('s', Optional)
            .WordBoundary();

        Assert.AreEqual(@"\b(?:cat|dog)s?\b", catsOrDogs.ToString());
        Assert.IsTrue(catsOrDogs.IsMatch("I love Cats"));
        Assert.IsTrue(catsOrDogs.IsMatch("I have two DOGS"));
        Assert.IsFalse(catsOrDogs.IsMatch("concatenate"));
        Assert.IsFalse(catsOrDogs.IsMatch("hotdogs"));
    }

    [TestMethod]
    public void Repeated_Words()
    {
        var aWord = new SubPattern().AnyWordCharacter(OneOrMore);

        var repeatedWord = new MatchThesePatterns()
            .WordBoundary()
            .CaptureAs("word", aWord)
            .AnyWhitespace(OneOrMore)
            .TheSameTextAsCapture("word")
            .WordBoundary();

        Assert.AreEqual(@"\b(?<word>\w+)\s+\k<word>\b", repeatedWord.ToString());
        Assert.AreEqual("the", repeatedWord.ToRegex().Match("Paris in the the spring").Groups["word"].Value);
        Assert.IsFalse(repeatedWord.IsMatch("the theory"));
        Assert.IsFalse(repeatedWord.IsMatch("is island"));
    }

    [TestMethod]
    public void Password_Rule()
    {
        var aDigitSomewhereAhead = new SubPattern().AnyCharacter(ZeroOrMore).AnyDigit();
        var aLowercaseLetterSomewhereAhead = new SubPattern().AnyCharacter(ZeroOrMore).AnyCharacterInRange('a', 'z');

        var password = new MatchThesePatterns()
            .StartOfText()
            .IfFollowedBy(aDigitSomewhereAhead)
            .IfFollowedBy(aLowercaseLetterSomewhereAhead)
            .AnyCharacter(AtLeast(8))
            .EndOfText();

        Assert.AreEqual(@"\A(?=.*\d)(?=.*[a-z]).{8,}\z", password.ToString());
        Assert.IsTrue(password.IsMatch("passw0rd"));
        Assert.IsFalse(password.IsMatch("password"));
    }

    [TestMethod]
    public void Cheat_Sheet_Rows()
    {
        var start = new MatchThesePatterns();
        Assert.AreEqual(@"(?:www\.)?", start.TheText("www.", Optional).ToString());
        Assert.AreEqual(".{8,}", start.AnyCharacter(AtLeast(8)).ToString());
        Assert.AreEqual("[^<>]+", start.AnyCharacterExcept("<>", OneOrMore).ToString());
        Assert.AreEqual("[a-z]", start.AnyCharacterInRange('a', 'z').ToString());
        Assert.AreEqual(@"(?<=\$)", start.IfPrecededBy("$").ToString());
        Assert.AreEqual(@"(?<!\$)", start.IfNotPrecededBy("$").ToString());
        Assert.AreEqual(@"(?:\p{IsGreek})", start.RawRegex(@"\p{IsGreek}").ToString());
        Assert.AreEqual("+?", OneOrMore.AsFewAsPossible().ToString());
    }
}
