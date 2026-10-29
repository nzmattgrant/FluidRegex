using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluidRegex.Test;

[TestClass]
public class ReuseAndImmutabilityTests
{
    [TestMethod]
    public void A_Shared_Start_Can_Branch()
    {
        var prefix = new TextPattern().StartOfText().Text("id-");
        var digits = prefix.Digit(Times.OneOrMore);
        var letters = prefix.Letter(Times.OneOrMore);

        Assert.AreEqual(@"\Aid-", prefix.ToString());
        Assert.AreEqual(@"\Aid-[0-9]+", digits.ToString());
        Assert.AreEqual(@"\Aid-\p{L}+", letters.ToString());
    }

    [TestMethod]
    public void A_Count_Can_Be_Used_For_Several_Pieces()
    {
        var start = new TextPattern().Text("a");
        var oneOrMore = Times.OneOrMore;
        var digits = start.Digit(oneOrMore);
        var letters = start.Letter(oneOrMore);

        Assert.AreEqual("a", start.ToString());
        Assert.AreEqual("a[0-9]+", digits.ToString());
        Assert.AreEqual(@"a\p{L}+", letters.ToString());
    }

    [TestMethod]
    public void One_Pattern_Can_Be_Used_Many_Times()
    {
        var word = new TextPattern().Letter(Times.OneOrMore);
        var builder = new TextPattern().Pattern(word).Text(".").Pattern(word).Text(".").Pattern(word);
        Assert.AreEqual(@"\p{L}+\.\p{L}+\.\p{L}+", builder.ToString());
    }

    [TestMethod]
    public void Extending_A_Pattern_After_Use_Does_Not_Change_Earlier_Uses()
    {
        var word = new TextPattern().Letter(Times.OneOrMore);
        var builder = new TextPattern().Pattern(word, Times.Optional);
        var longer = word.Text("!");

        Assert.AreEqual(@"(?:\p{L}+)?", builder.ToString());
        Assert.AreEqual(@"\p{L}+!", longer.ToString());
        Assert.AreEqual(@"\p{L}+", word.ToString());
    }

    [TestMethod]
    public void Settings_On_A_Derived_Pattern_Do_Not_Change_The_Original()
    {
        var original = new TextPattern().Text("a");
        var ignoringCase = original.IgnoringCase();
        Assert.AreEqual(RegexOptions.None, original.ToRegex().Options);
        Assert.AreEqual(RegexOptions.IgnoreCase, ignoringCase.ToRegex().Options);
    }

    [TestMethod]
    public void A_Shared_Pattern_Is_Safe_Across_Threads()
    {
        var shared = new TextPattern().Digit(Times.OneOrMore);
        Parallel.For(0, 1000, i =>
        {
            Assert.AreEqual("[0-9]+", shared.ToString());
            Assert.IsTrue(shared.MatchesWholeText(i.ToString()));
            Assert.IsTrue(shared.IsFoundIn(i.ToString()));
        });
    }
}
