using System.Text.RegularExpressions;

namespace FluidRegex.Test;

[TestClass]
public class QuantifierBindingTests
{
    private static readonly TextPattern Start = new();

    [TestMethod]
    public void Single_Atoms_Take_The_Count_Directly()
    {
        Assert.AreEqual("s?", Start.Text("s", Times.Optional).ToString());
        Assert.AreEqual(@"\.+", Start.Text(".", Times.OneOrMore).ToString());
        Assert.AreEqual("[0-9]{3}", Start.Digit(Times.Exactly(3)).ToString());
        Assert.AreEqual("[-. ]?", Start.OneOfTheseCharacters("-. ", Times.Optional).ToString());
        Assert.AreEqual(@"\k<w>*", Start.SameTextAsSaved("w", Times.ZeroOrMore).ToString());
    }

    [TestMethod]
    public void Anything_Longer_Is_Wrapped()
    {
        Assert.AreEqual("(?:abc)+", Start.Text("abc", Times.OneOrMore).ToString());
        Assert.AreEqual(@"(?:\d{3})+", Start.RawRegex(@"\d{3}", Times.OneOrMore).ToString());
        Assert.AreEqual("(?:a[0-9])*", Start.Pattern(new TextPattern().Text("a").Digit(), Times.ZeroOrMore).ToString());
    }

    [TestMethod]
    public void A_Counted_Piece_Is_Never_Counted_Again_Without_A_Wrapper()
    {
        var digits = new TextPattern().Digit(Times.OneOrMore);
        Assert.AreEqual("(?:[0-9]+)*", Start.Pattern(digits, Times.ZeroOrMore).ToString());
        Assert.AreEqual("(?:[0-9]+)?", Start.Pattern(digits, Times.Optional).ToString(), "never [0-9]+? (lazy)");
        Assert.AreEqual("(?:a+?)?", Start.Pattern(new TextPattern().Text("a", Times.OneOrMore.AsFewAsPossible()), Times.Optional).ToString(), "never a+??");

        var optionalDigits = Start.Pattern(digits, Times.Optional);
        Assert.IsTrue(optionalDigits.MatchesWholeText(""));
        Assert.IsTrue(optionalDigits.MatchesWholeText("123"));
    }

    [TestMethod]
    public void A_Pattern_Of_One_Uncounted_Atom_Takes_The_Count_Directly()
    {
        Assert.AreEqual("[-. ]?", Start.Pattern(new TextPattern().OneOfTheseCharacters("-. "), Times.Optional).ToString());
        Assert.AreEqual("(?:cat|dog)+", Start.Pattern(new TextPattern().EitherOf("cat", "dog"), Times.OneOrMore).ToString());
    }

    [TestMethod]
    public void Every_Piece_With_Every_Count_Builds_A_Valid_Regex()
    {
        var counts = new[]
        {
            default, Times.Optional, Times.ZeroOrMore, Times.OneOrMore, Times.Exactly(3), Times.AtLeast(2),
            Times.Between(0, 4), Times.Between(2, 5),
            Times.Optional.AsFewAsPossible(), Times.ZeroOrMore.AsFewAsPossible(), Times.OneOrMore.AsFewAsPossible(),
            Times.AtLeast(2).AsFewAsPossible(), Times.Between(2, 5).AsFewAsPossible(),
        };
        var pieces = new Func<Times, TextPattern>[]
        {
            times => new TextPattern().Text("a", times),
            times => new TextPattern().Text("ab", times),
            times => new TextPattern().Digit(times),
            times => new TextPattern().LineBreak(times),
            times => new TextPattern().OneOfTheseCharacters("xy", times),
            times => new TextPattern().AnyCharacterExcept("xy", times),
            times => new TextPattern().RawRegex("a|b", times),
            times => new TextPattern().Pattern(new TextPattern().EitherOf("a", "b"), times),
            times => new TextPattern().Pattern(new TextPattern().Digit(Times.OneOrMore), times),
            times => new TextPattern().Pattern(new TextPattern().IgnoringCase().EitherOf("a", "b"), times),
        };

        foreach (var piece in pieces)
        {
            foreach (var inner in counts)
            {
                foreach (var outer in counts)
                {
                    var subPattern = piece(inner);
                    var builder = new TextPattern().Pattern(subPattern, outer).SaveTextAs("s", subPattern, outer);
                    try
                    {
                        _ = new Regex(builder.ToString());
                    }
                    catch (ArgumentException exception)
                    {
                        Assert.Fail($"{builder} is not valid: {exception.Message}");
                    }
                }
            }
        }
    }
}
