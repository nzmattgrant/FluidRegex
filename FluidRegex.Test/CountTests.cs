namespace FluidRegex.Test;

[TestClass]
public class CountTests
{
    private static readonly TextPattern Start = new();

    [TestMethod]
    public void Each_Count_Emits_Its_Suffix()
    {
        Assert.AreEqual("a", Start.Text("a").ToString());
        Assert.AreEqual("a?", Start.Optional().Text("a").ToString());
        Assert.AreEqual("a*", Start.ZeroOrMore().Text("a").ToString());
        Assert.AreEqual("a+", Start.OneOrMore().Text("a").ToString());
        Assert.AreEqual("a", Start.Exactly(1).Text("a").ToString());
        Assert.AreEqual("a{4}", Start.Exactly(4).Text("a").ToString());
        Assert.AreEqual("a*", Start.AtLeast(0).Text("a").ToString());
        Assert.AreEqual("a+", Start.AtLeast(1).Text("a").ToString());
        Assert.AreEqual("a{8,}", Start.AtLeast(8).Text("a").ToString());
        Assert.AreEqual("a?", Start.AtMost(1).Text("a").ToString());
        Assert.AreEqual("a{0,5}", Start.AtMost(5).Text("a").ToString(), ".NET treats {,5} as literal text");
        Assert.AreEqual("a?", Start.Between(0, 1).Text("a").ToString());
        Assert.AreEqual("a{3}", Start.Between(3, 3).Text("a").ToString());
        Assert.AreEqual("a{2,256}", Start.Between(2, 256).Text("a").ToString());
    }

    [TestMethod]
    public void AsFewAsPossible_Adds_The_Lazy_Marker()
    {
        Assert.AreEqual("a*?", Start.ZeroOrMore().AsFewAsPossible().Text("a").ToString());
        Assert.AreEqual("a+?", Start.OneOrMore().AsFewAsPossible().Text("a").ToString());
        Assert.AreEqual("a??", Start.Optional().AsFewAsPossible().Text("a").ToString());
        Assert.AreEqual("a{2,}?", Start.AtLeast(2).AsFewAsPossible().Text("a").ToString());
        Assert.AreEqual("a{2,5}?", Start.Between(2, 5).AsFewAsPossible().Text("a").ToString());
    }

    [TestMethod]
    public void AsFewAsPossible_Finds_The_Shortest_Text()
    {
        var shortestTag = Start.Text("<").OneOrMore().AsFewAsPossible().AnyCharacter().Text(">");
        var longestTag = Start.Text("<").OneOrMore().AnyCharacter().Text(">");

        Assert.AreEqual("<a>", shortestTag.FindFirst("<a><b>")!.Text);
        Assert.AreEqual("<a><b>", longestTag.FindFirst("<a><b>")!.Text);
    }

    [TestMethod]
    public void AsFewAsPossible_Throws_On_Fixed_Counts()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Start.Exactly(1).AsFewAsPossible());
        Assert.ThrowsExactly<InvalidOperationException>(() => Start.Exactly(4).AsFewAsPossible());
        Assert.ThrowsExactly<InvalidOperationException>(() => Start.Between(3, 3).AsFewAsPossible());
    }

    [TestMethod]
    public void Counts_That_Could_Only_Match_Nothing_Throw()
    {
        Assert.AreEqual("count", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.Exactly(0)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.AtMost(0)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.Between(0, 0)).ParamName);
    }

    [TestMethod]
    public void Invalid_Bounds_Throw()
    {
        Assert.AreEqual("count", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.Exactly(-1)).ParamName);
        Assert.AreEqual("minimum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.AtLeast(-1)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.AtMost(-1)).ParamName);
        Assert.AreEqual("minimum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.Between(-1, 2)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Start.Between(3, 2)).ParamName);
    }
}
