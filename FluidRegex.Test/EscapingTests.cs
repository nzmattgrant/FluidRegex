using System.Text;
using System.Text.RegularExpressions;
using FluidRegex.Internal;

namespace FluidRegex.Test;

[TestClass]
public class EscapingTests
{
    [TestMethod]
    public void Outside_A_Set_Escaping_Is_Regex_Escape()
    {
        foreach (var text in new[] { "a.b", "\\*+?|{[()^$#", " \t\n", "://@-]}", "plain" })
        {
            Assert.AreEqual(Regex.Escape(text), Escaping.OutsideSet(text));
        }
    }

    [TestMethod]
    public void Inside_A_Set_Only_The_Set_Specials_Are_Escaped()
    {
        Assert.AreEqual(@"\\", InsideSet('\\'));
        Assert.AreEqual(@"\]", InsideSet(']'));
        Assert.AreEqual(@"\[", InsideSet('['));
        Assert.AreEqual(@"\^", InsideSet('^'));
        Assert.AreEqual(@"\t", InsideSet('\t'));
        Assert.AreEqual(@"\n", InsideSet('\n'));
        Assert.AreEqual(@"\u0007", InsideSet('\a'));
        Assert.AreEqual(@"\u007F", InsideSet('\u007F'));
        Assert.AreEqual(" ", InsideSet(' '));
        Assert.AreEqual("#", InsideSet('#'));
        Assert.AreEqual(".", InsideSet('.'));
        Assert.AreEqual("é", InsideSet('é'));
    }

    [TestMethod]
    public void Range_Endpoints_That_Are_Not_Letters_Or_Digits_Are_Unicode_Escapes()
    {
        Assert.AreEqual("a", RangeEndpoint('a'));
        Assert.AreEqual("Z", RangeEndpoint('Z'));
        Assert.AreEqual("0", RangeEndpoint('0'));
        Assert.AreEqual(@"\u002D", RangeEndpoint('-'));
        Assert.AreEqual(@"\u0021", RangeEndpoint('!'));
        Assert.AreEqual(@"\u00E9", RangeEndpoint('\u00E9'));
    }

    [TestMethod]
    public void Range_Look_Alikes_Are_Found()
    {
        Assert.IsTrue(Escaping.TryFindRangeLookAlike("a-z", out var lookAlike));
        Assert.AreEqual("a-z", lookAlike);
        Assert.IsTrue(Escaping.TryFindRangeLookAlike("@:0-9", out lookAlike));
        Assert.AreEqual("0-9", lookAlike);
        Assert.IsFalse(Escaping.TryFindRangeLookAlike("-az", out _));
        Assert.IsFalse(Escaping.TryFindRangeLookAlike("+-.", out _));
        Assert.IsFalse(Escaping.TryFindRangeLookAlike("a-", out _));
        Assert.IsFalse(Escaping.TryFindRangeLookAlike("é-z", out _));
    }

    private static string InsideSet(char character)
    {
        var builder = new StringBuilder();
        Escaping.AppendInsideSet(builder, character);
        return builder.ToString();
    }

    private static string RangeEndpoint(char character)
    {
        var builder = new StringBuilder();
        Escaping.AppendRangeEndpoint(builder, character);
        return builder.ToString();
    }
}
