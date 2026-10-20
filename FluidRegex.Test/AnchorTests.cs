using System.Linq;
using System.Text.RegularExpressions;
using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class AnchorTests
{
    [TestMethod]
    public void StartOfText_And_EndOfText_Emit_Absolute_Anchors()
    {
        var builder = new MatchThesePatterns().StartOfText().TheText("abc").EndOfText();
        Assert.AreEqual(@"\Aabc\z", builder.ToString());
        Assert.IsTrue(builder.IsMatch("abc"));
        Assert.IsFalse(builder.IsMatch("abc\n"), "$ would accept a trailing newline; \\z does not");
        Assert.IsFalse(builder.IsMatch("xabc"));
    }

    [TestMethod]
    public void StartOfText_Is_Not_Changed_By_Multiline()
    {
        var builder = new MatchThesePatterns().WithOptions(RegexOptions.Multiline).StartOfText().TheText("b");
        Assert.IsFalse(builder.IsMatch("a\nb"));
        Assert.IsTrue(builder.IsMatch("b\na"));
    }

    [TestMethod]
    public void Line_Anchors_Recognise_Every_Line_Break_Style()
    {
        const string text = "x\r\ny\rz\nw";
        var starts = new MatchThesePatterns().StartOfLine().ToRegex().Matches(text).Cast<Match>().Select(m => m.Index).ToArray();
        var ends = new MatchThesePatterns().EndOfLine().ToRegex().Matches(text).Cast<Match>().Select(m => m.Index).ToArray();

        CollectionAssert.AreEqual(new[] { 0, 3, 5, 7 }, starts);
        CollectionAssert.AreEqual(new[] { 1, 4, 6, 8 }, ends, "never between \\r and \\n");
    }

    [TestMethod]
    public void A_Whole_Line_Does_Not_Keep_The_Carriage_Return()
    {
        var line = new MatchThesePatterns().StartOfLine().AnyCharacter(ZeroOrMore).EndOfLine();
        var lines = line.ToRegex().Matches("abc\r\nx").Cast<Match>().Select(m => m.Value).Where(v => v.Length > 0).ToArray();
        CollectionAssert.AreEqual(new[] { "abc", "x" }, lines);
    }

    [TestMethod]
    public void Line_Anchors_Find_Lines_In_Mixed_Text()
    {
        var todo = new MatchThesePatterns().StartOfLine().TheText("TODO").AnyCharacter(ZeroOrMore).EndOfLine();
        var found = todo.ToRegex().Matches("a\r\nTODO one\nb\rTODO two").Cast<Match>().Select(m => m.Value).ToArray();
        CollectionAssert.AreEqual(new[] { "TODO one", "TODO two" }, found);
    }

    [TestMethod]
    public void Line_Anchors_Match_An_Empty_Line()
    {
        var emptyLine = new MatchThesePatterns().StartOfLine().EndOfLine();
        Assert.IsTrue(emptyLine.IsMatch("a\n\nb"));
        Assert.IsTrue(emptyLine.IsMatch(""));
    }

    [TestMethod]
    public void Line_Anchors_Work_Right_To_Left()
    {
        var line = new MatchThesePatterns().WithOptions(RegexOptions.RightToLeft).StartOfLine().TheText("b").EndOfLine();
        Assert.IsTrue(line.IsMatch("a\r\nb\r\nc"));
        Assert.IsFalse(line.IsMatch("ab\r\nc"));
    }

    [TestMethod]
    public void WordBoundary_And_NotAWordBoundary()
    {
        var wholeX = new MatchThesePatterns().WordBoundary().TheCharacter('x').WordBoundary();
        var insideX = new MatchThesePatterns().NotAWordBoundary().TheCharacter('x').NotAWordBoundary();

        Assert.AreEqual(@"\bx\b", wholeX.ToString());
        Assert.AreEqual(@"\Bx\B", insideX.ToString());
        Assert.AreEqual(4, wholeX.ToRegex().Match("axb x").Index);
        Assert.AreEqual(1, insideX.ToRegex().Match("axb x").Index);
        Assert.IsTrue(new MatchThesePatterns().TheText("caf").NotAWordBoundary().IsMatch("café"), "é is a word character");
    }
}
