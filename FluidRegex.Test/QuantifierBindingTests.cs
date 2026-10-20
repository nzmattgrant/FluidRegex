using System.Text.RegularExpressions;
using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class QuantifierBindingTests
{
    [TestMethod]
    public void Single_Atoms_Take_The_Count_Directly()
    {
        Assert.AreEqual("s?", new MatchThesePatterns().TheCharacter('s', Optional).ToString());
        Assert.AreEqual(@"\.+", new MatchThesePatterns().TheText(".", OneOrMore).ToString());
        Assert.AreEqual(@"\d{3}", new MatchThesePatterns().AnyDigit(Exactly(3)).ToString());
        Assert.AreEqual("[-. ]?", new MatchThesePatterns().OneOfTheseCharacters("-. ", Optional).ToString());
        Assert.AreEqual(@"\k<w>*", new MatchThesePatterns().TheSameTextAsCapture("w", ZeroOrMore).ToString());
    }

    [TestMethod]
    public void Anything_Longer_Is_Wrapped()
    {
        Assert.AreEqual("(?:abc)+", new MatchThesePatterns().TheText("abc", OneOrMore).ToString());
        Assert.AreEqual(@"(?:\d{3})+", new MatchThesePatterns().RawRegex(@"\d{3}", OneOrMore).ToString());
        Assert.AreEqual(@"(?:a\d)*", new MatchThesePatterns().Group(new SubPattern().TheCharacter('a').AnyDigit(), ZeroOrMore).ToString());
    }

    [TestMethod]
    public void A_Counted_Piece_Is_Never_Counted_Again_Without_A_Wrapper()
    {
        var digits = new SubPattern().AnyDigit(OneOrMore);
        Assert.AreEqual(@"(?:\d+)*", new MatchThesePatterns().Group(digits, ZeroOrMore).ToString());
        Assert.AreEqual(@"(?:\d+)?", new MatchThesePatterns().Group(digits, Optional).ToString(), "never \\d+? (lazy)");
        Assert.AreEqual(@"(?:a+?)?", new MatchThesePatterns().Group(new SubPattern().TheCharacter('a', OneOrMore.AsFewAsPossible()), Optional).ToString(), "never a+??");

        var optionalDigits = new MatchThesePatterns().StartOfText().Group(digits, Optional).EndOfText();
        Assert.IsTrue(optionalDigits.IsMatch(""));
        Assert.IsTrue(optionalDigits.IsMatch("123"));
    }

    [TestMethod]
    public void A_Group_Of_One_Uncounted_Atom_Takes_The_Count_Directly()
    {
        Assert.AreEqual("[-. ]?", new MatchThesePatterns().Group(new SubPattern().OneOfTheseCharacters("-. "), Optional).ToString());
        Assert.AreEqual("(?:cat|dog)+", new MatchThesePatterns().Group(new SubPattern().EitherOf("cat", "dog"), OneOrMore).ToString());
    }

    [TestMethod]
    public void Every_Piece_With_Every_Count_Builds_A_Valid_Regex()
    {
        var counts = new[]
        {
            Once, Optional, ZeroOrMore, OneOrMore, Exactly(0), Exactly(3), AtLeast(2), AtMost(4), Between(2, 5),
            Optional.AsFewAsPossible(), ZeroOrMore.AsFewAsPossible(), OneOrMore.AsFewAsPossible(),
            AtLeast(2).AsFewAsPossible(), Between(2, 5).AsFewAsPossible(),
        };
        var pieces = new Func<SubPattern, NumberOfTimes, SubPattern>[]
        {
            (p, t) => p.TheCharacter('a', t),
            (p, t) => p.TheText("ab", t),
            (p, t) => p.AnyDigit(t),
            (p, t) => p.AnyLineBreak(t),
            (p, t) => p.OneOfTheseCharacters("xy", t),
            (p, t) => p.AnyCharacterExcept("xy", t),
            (p, t) => p.RawRegex("a|b", t),
            (p, t) => p.Group(new SubPattern().AnyDigit(OneOrMore), t),
            (p, t) => p.Group(new SubPattern().EitherOf("a", "b"), t),
        };

        foreach (var piece in pieces)
        {
            foreach (var inner in counts)
            {
                foreach (var outer in counts)
                {
                    var subPattern = piece(new SubPattern(), inner);
                    var builder = new MatchThesePatterns().Group(subPattern, outer).Group(new SubPattern().Capture(subPattern, outer.Maximum == 0 ? Once : outer));
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
