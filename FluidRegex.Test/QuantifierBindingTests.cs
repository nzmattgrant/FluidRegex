using System.Text.RegularExpressions;

namespace FluidRegex.Test;

[TestClass]
public class QuantifierBindingTests
{
    private static readonly TextPattern Start = new();

    [TestMethod]
    public void Single_Atoms_Take_The_Count_Directly()
    {
        Assert.AreEqual("s?", Start.Optional().Text("s").ToString());
        Assert.AreEqual(@"\.+", Start.OneOrMore().Text(".").ToString());
        Assert.AreEqual("[0-9]{3}", Start.Exactly(3).Digit().ToString());
        Assert.AreEqual("[-. ]?", Start.Optional().OneOfTheseCharacters("-. ").ToString());
        Assert.AreEqual(@"\k<w>*", Start.ZeroOrMore().SameTextAsSaved("w").ToString());
    }

    [TestMethod]
    public void Anything_Longer_Is_Wrapped()
    {
        Assert.AreEqual("(?:abc)+", Start.OneOrMore().Text("abc").ToString());
        Assert.AreEqual(@"(?:\d{3})+", Start.OneOrMore().RawRegex(@"\d{3}").ToString());
        Assert.AreEqual("(?:a[0-9])*", Start.ZeroOrMore().Pattern(new TextPattern().Text("a").Digit()).ToString());
    }

    [TestMethod]
    public void A_Counted_Piece_Is_Never_Counted_Again_Without_A_Wrapper()
    {
        var digits = new TextPattern().OneOrMore().Digit();
        Assert.AreEqual("(?:[0-9]+)*", Start.ZeroOrMore().Pattern(digits).ToString());
        Assert.AreEqual("(?:[0-9]+)?", Start.Optional().Pattern(digits).ToString(), "never [0-9]+? (lazy)");
        Assert.AreEqual("(?:a+?)?", Start.Optional().Pattern(new TextPattern().OneOrMore().AsFewAsPossible().Text("a")).ToString(), "never a+??");

        var optionalDigits = Start.Optional().Pattern(digits);
        Assert.IsTrue(optionalDigits.MatchesWholeText(""));
        Assert.IsTrue(optionalDigits.MatchesWholeText("123"));
    }

    [TestMethod]
    public void A_Pattern_Of_One_Uncounted_Atom_Takes_The_Count_Directly()
    {
        Assert.AreEqual("[-. ]?", Start.Optional().Pattern(new TextPattern().OneOfTheseCharacters("-. ")).ToString());
        Assert.AreEqual("(?:cat|dog)+", Start.OneOrMore().Pattern(new TextPattern().EitherOf("cat", "dog")).ToString());
    }

    [TestMethod]
    public void Every_Piece_With_Every_Count_Builds_A_Valid_Regex()
    {
        var counts = new Func<TextPattern, PatternPieces>[]
        {
            p => p, p => p.Optional(), p => p.ZeroOrMore(), p => p.OneOrMore(), p => p.Exactly(3), p => p.AtLeast(2),
            p => p.AtMost(4), p => p.Between(2, 5),
            p => p.Optional().AsFewAsPossible(), p => p.ZeroOrMore().AsFewAsPossible(), p => p.OneOrMore().AsFewAsPossible(),
            p => p.AtLeast(2).AsFewAsPossible(), p => p.Between(2, 5).AsFewAsPossible(),
        };
        var pieces = new Func<PatternPieces, TextPattern>[]
        {
            p => p.Text("a"),
            p => p.Text("ab"),
            p => p.Digit(),
            p => p.LineBreak(),
            p => p.OneOfTheseCharacters("xy"),
            p => p.AnyCharacterExcept("xy"),
            p => p.RawRegex("a|b"),
            p => p.EitherOf("a", "b"),
            p => p.Pattern(new TextPattern().OneOrMore().Digit()),
            p => p.Pattern(new TextPattern().IgnoringCase().EitherOf("a", "b")),
        };

        foreach (var piece in pieces)
        {
            foreach (var inner in counts)
            {
                foreach (var outer in counts)
                {
                    var subPattern = piece(inner(new TextPattern()));
                    var builder = outer(outer(new TextPattern()).Pattern(subPattern)).SaveAs("s", subPattern);
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
