using System.Linq;
using System.Text.RegularExpressions;

namespace FluidRegex.Test;

[TestClass]
public class AnchorTests
{
    [TestMethod]
    public void StartOfText_And_EndOfText_Emit_Absolute_Anchors()
    {
        var builder = new TextPattern().StartOfText().Text("abc").EndOfText();
        Assert.AreEqual(@"\Aabc\z", builder.ToString());
        Assert.IsTrue(builder.IsFoundIn("abc"));
        Assert.IsFalse(builder.IsFoundIn("abc\n"), "$ would accept a trailing newline; \\z does not");
        Assert.IsFalse(builder.IsFoundIn("xabc"));
    }

    [TestMethod]
    public void Line_Anchors_Recognise_Every_Line_Break_Style()
    {
        const string text = "x\r\ny\rz\nw";
        var starts = new TextPattern().StartOfLine().FindAll(text).Select(found => found.Position).ToArray();
        var ends = new TextPattern().EndOfLine().FindAll(text).Select(found => found.Position).ToArray();

        CollectionAssert.AreEqual(new[] { 0, 3, 5, 7 }, starts);
        CollectionAssert.AreEqual(new[] { 1, 4, 6, 8 }, ends, "never between \\r and \\n");
    }

    [TestMethod]
    public void A_Whole_Line_Does_Not_Keep_The_Carriage_Return()
    {
        var line = new TextPattern().StartOfLine().ZeroOrMore().AnyCharacterExceptLineBreak().EndOfLine();
        var lines = line.FindAll("abc\r\nx").Select(found => found.Text).Where(text => text.Length > 0).ToArray();
        CollectionAssert.AreEqual(new[] { "abc", "x" }, lines);
    }

    [TestMethod]
    public void Line_Anchors_Find_Lines_In_Mixed_Text()
    {
        var todo = new TextPattern().StartOfLine().Text("TODO").ZeroOrMore().AnyCharacterExceptLineBreak().EndOfLine();
        var found = todo.FindAll("a\r\nTODO one\nb\rTODO two").Select(f => f.Text).ToArray();
        CollectionAssert.AreEqual(new[] { "TODO one", "TODO two" }, found);
    }

    [TestMethod]
    public void Line_Anchors_Match_An_Empty_Line()
    {
        var emptyLine = new TextPattern().StartOfLine().EndOfLine();
        Assert.IsTrue(emptyLine.IsFoundIn("a\n\nb"));
        Assert.IsTrue(emptyLine.IsFoundIn(""));
    }

    [TestMethod]
    public void Line_Anchors_Work_Right_To_Left()
    {
        var line = new TextPattern().StartOfLine().Text("b").EndOfLine();
        Assert.IsTrue(new Regex(line.ToString(), RegexOptions.RightToLeft).IsMatch("a\r\nb\r\nc"));
        Assert.IsFalse(new Regex(line.ToString(), RegexOptions.RightToLeft).IsMatch("ab\r\nc"));
    }

    [TestMethod]
    public void StartOfWord_And_EndOfWord_Find_Whole_Words()
    {
        var wholeX = new TextPattern().StartOfWord().Text("x").EndOfWord();

        Assert.AreEqual(@"\b(?=\w)x\b(?<=\w)", wholeX.ToString());
        Assert.AreEqual(4, wholeX.FindFirst("axb x")!.Position);
        Assert.IsTrue(wholeX.MatchesWholeText("x"));
        Assert.IsFalse(new TextPattern().Text("caf").EndOfWord().IsFoundIn("café"), "é is a letter");
    }

    [TestMethod]
    public void StartOfWord_Is_Never_Before_A_Space_And_EndOfWord_Is_Never_After_One()
    {
        Assert.IsFalse(new TextPattern().StartOfWord().Text(" ").IsFoundIn("a b"));
        Assert.IsFalse(new TextPattern().Text(" ").EndOfWord().IsFoundIn("a b"));
    }
}
