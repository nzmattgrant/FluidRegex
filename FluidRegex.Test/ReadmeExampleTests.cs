namespace FluidRegex.Test;

/// <summary>Every snippet in README.md, exactly as written there. Add a test here with every new README snippet.</summary>
[TestClass]
public class ReadmeExampleTests
{
    [TestMethod]
    public void Opening_Iso_Date()
    {
        var isoDate = new TextPattern()
            .Digit(Times.Exactly(4)).Text("-")
            .Digit(Times.Exactly(2)).Text("-")
            .Digit(Times.Exactly(2));

        Assert.IsTrue(isoDate.MatchesWholeText("2026-10-12"));
        Assert.AreEqual("[0-9]{4}-[0-9]{2}-[0-9]{2}", isoDate.ToString());
    }

    [TestMethod]
    public void Email()
    {
        var word = new TextPattern()
            .OneOfTheseCharacters(new CharacterSet().Letters().Digits().Characters("_"), Times.OneOrMore);
        var symbolThenWord = new TextPattern().OneOfTheseCharacters("-+.'").Pattern(word);
        var dotOrHyphenThenWord = new TextPattern().OneOfTheseCharacters("-.").Pattern(word);

        var email = new TextPattern()
            .Pattern(word)
            .Pattern(symbolThenWord, Times.ZeroOrMore)
            .Text("@")
            .Pattern(word)
            .Pattern(dotOrHyphenThenWord, Times.ZeroOrMore)
            .Text(".")
            .Pattern(word)
            .Pattern(dotOrHyphenThenWord, Times.ZeroOrMore);

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
        Assert.AreEqual("[0-9]{4}", new TextPattern().Digit(Times.Exactly(4)).ToString());
        Assert.AreEqual("(?:abc)+", new TextPattern().Text("abc", Times.OneOrMore).ToString());
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
        Assert.AreEqual(@"(?:\z|\ )", start.EitherOf(start.EndOfText(), start.Text(" ")).ToString());
        Assert.AreEqual("a{0,3}", start.Text("a", Times.Between(0, 3)).ToString());
        Assert.AreEqual("(?:cat|dog)+", start.Pattern(start.EitherOf("cat", "dog"), Times.OneOrMore).ToString());
        Assert.AreEqual(@"\b(?=\w)", start.StartOfWord().ToString());
        Assert.AreEqual(@"\b(?<=\w)", start.EndOfWord().ToString());
        Assert.AreEqual(@"(?<=\$)", start.TextBeforeHereEndsWith("$").ToString());
        Assert.AreEqual(@"(?<!\$)", start.TextBeforeHereDoesNotEndWith("$").ToString());
        Assert.AreEqual(@"(?<=\$)[0-9]+", start.TextBeforeHereEndsWith("$").Digit(Times.OneOrMore).ToString());
        Assert.AreEqual("<a>", start.Text("<").AnyCharacter(Times.OneOrMore.AsFewAsPossible()).Text(">").FindFirst("<a><b>")!.Text);
        Assert.AreEqual("<a><b>", start.Text("<").AnyCharacter(Times.OneOrMore).Text(">").FindFirst("<a><b>")!.Text);
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
        var path = new TextPattern().OneOfTheseCharacters(pathCharacters, Times.ZeroOrMore);

        var url = new TextPattern()
            .Text("http")
            .Text("s", Times.Optional)
            .Text("://")
            .Text("www.", Times.Optional)
            .OneOfTheseCharacters(hostCharacters, Times.Between(2, 256))
            .Text(".")
            .AnyCharacterInRange('a', 'z', Times.Between(2, 6))
            .EndOfWord()
            .SaveTextAs("path", path);

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
        var fourDigits = new TextPattern().Digit(Times.Exactly(4));
        var twoDigits = new TextPattern().Digit(Times.Exactly(2));

        var isoDate = new TextPattern()
            .SaveTextAs("year", fourDigits)
            .Text("-")
            .SaveTextAs("month", twoDigits)
            .Text("-")
            .SaveTextAs("day", twoDigits);

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
            .Text("(", Times.Optional)
            .Digit(Times.Exactly(3))
            .Text(")", Times.Optional)
            .OneOfTheseCharacters("-. ", Times.Optional)
            .Digit(Times.Exactly(3))
            .OneOfTheseCharacters("-. ", Times.Optional)
            .Digit(Times.Exactly(4));

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
            .Text("s", Times.Optional)
            .EndOfWord();

        Assert.AreEqual(@"\b(?=\w)(?:cat|dog)s?\b(?<=\w)", catsOrDogs.ToString());
        Assert.IsTrue(catsOrDogs.IsFoundIn("I love Cats"));
        Assert.IsTrue(catsOrDogs.IsFoundIn("I have two DOGS"));
        Assert.IsFalse(catsOrDogs.IsFoundIn("concatenate"));
        Assert.IsFalse(catsOrDogs.IsFoundIn("hotdogs"));
    }

    [TestMethod]
    public void Either_Or_With_A_Position()
    {
        var theEndOfText = new TextPattern().EndOfText();
        var aSpace = new TextPattern().Text(" ");

        var abcThenX = new TextPattern()
            .OneOfTheseCharacters("abc")
            .Text("x")
            .EitherOf(theEndOfText, aSpace);

        Assert.AreEqual(@"[abc]x(?:\z|\ )", abcThenX.ToString());
        Assert.IsTrue(abcThenX.IsFoundIn("tax rebate"));
        Assert.IsFalse(abcThenX.IsFoundIn("taxi"));
        Assert.IsTrue(abcThenX.IsFoundIn("relax"));
        Assert.IsFalse(abcThenX.IsFoundIn("box "));
    }

    [TestMethod]
    public void Repeated_Words()
    {
        var aWord = new TextPattern().Letter(Times.OneOrMore);

        var repeatedWord = new TextPattern()
            .StartOfWord()
            .SaveTextAs("word", aWord)
            .Whitespace(Times.OneOrMore)
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
        var anythingThenADigit = new TextPattern().AnyCharacter(Times.ZeroOrMore).Digit();
        var anythingThenALowercaseLetter = new TextPattern().AnyCharacter(Times.ZeroOrMore).AnyCharacterInRange('a', 'z');

        var password = new TextPattern()
            .TextAfterHereStartsWith(anythingThenADigit)
            .TextAfterHereStartsWith(anythingThenALowercaseLetter)
            .AnyCharacter(Times.AtLeast(8));

        Assert.AreEqual(@"(?=[\s\S]*[0-9])(?=[\s\S]*[a-z])[\s\S]{8,}", password.ToString());
        Assert.IsTrue(password.MatchesWholeText("passw0rd"));
        Assert.IsFalse(password.MatchesWholeText("password"));
    }

    [TestMethod]
    public void Find_And_Replace()
    {
        var fourDigitsAndHyphen = new TextPattern().Digit(Times.Exactly(4)).Text("-");

        var cardNumber = new TextPattern()
            .Pattern(fourDigitsAndHyphen, Times.Exactly(3))
            .Digit(Times.Exactly(4));

        Assert.AreEqual("(?:[0-9]{4}-){3}[0-9]{4}", cardNumber.ToString());
        Assert.AreEqual("Card [hidden] was used.", cardNumber.ReplaceAll("Card 1234-5678-9012-3456 was used.", "[hidden]"));
    }
}
