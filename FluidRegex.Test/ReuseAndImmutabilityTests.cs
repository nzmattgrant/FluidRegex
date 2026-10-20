using System.Threading.Tasks;
using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class ReuseAndImmutabilityTests
{
    [TestMethod]
    public void A_Shared_Prefix_Can_Branch()
    {
        var prefix = new MatchThesePatterns().StartOfText().TheText("id-");
        var digits = prefix.AnyDigit(OneOrMore);
        var letters = prefix.AnyLetter(OneOrMore);

        Assert.AreEqual(@"\Aid-", prefix.ToString());
        Assert.AreEqual(@"\Aid-\d+", digits.ToString());
        Assert.AreEqual(@"\Aid-\p{L}+", letters.ToString());
    }

    [TestMethod]
    public void One_Sub_Pattern_Can_Be_Used_Many_Times()
    {
        var word = new SubPattern().AnyWordCharacter(OneOrMore);
        var builder = new MatchThesePatterns().Group(word).TheCharacter('.').Group(word).TheCharacter('.').Group(word);
        Assert.AreEqual(@"\w+\.\w+\.\w+", builder.ToString());
    }

    [TestMethod]
    public void Extending_A_Sub_Pattern_After_Use_Does_Not_Change_Earlier_Uses()
    {
        var word = new SubPattern().AnyWordCharacter(OneOrMore);
        var builder = new MatchThesePatterns().Group(word, Optional);
        var longer = word.TheCharacter('!');

        Assert.AreEqual(@"(?:\w+)?", builder.ToString());
        Assert.AreEqual(@"\w+!", longer.ToString());
        Assert.AreEqual(@"\w+", word.ToString());
    }

    [TestMethod]
    public void Options_On_A_Derived_Chain_Do_Not_Change_The_Original()
    {
        var original = new MatchThesePatterns().TheText("a");
        var ignoringCase = original.IgnoringCase();
        Assert.AreEqual(System.Text.RegularExpressions.RegexOptions.None, original.Options);
        Assert.AreEqual(System.Text.RegularExpressions.RegexOptions.IgnoreCase, ignoringCase.Options);
    }

    [TestMethod]
    public void A_Shared_Builder_Is_Safe_Across_Threads()
    {
        var shared = new MatchThesePatterns().StartOfText().AnyDigit(OneOrMore).EndOfText();
        Parallel.For(0, 1000, i =>
        {
            Assert.AreEqual(@"\A\d+\z", shared.ToString());
            Assert.IsTrue(shared.IsMatch(i.ToString()));
        });
    }
}
