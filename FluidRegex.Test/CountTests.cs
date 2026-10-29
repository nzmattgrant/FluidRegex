namespace FluidRegex.Test;

[TestClass]
public class CountTests
{
    private static readonly TextPattern Start = new();

    [TestMethod]
    public void Each_Count_Emits_Its_Suffix()
    {
        Assert.AreEqual("a", Start.Text("a").ToString());
        Assert.AreEqual("a?", Start.Text("a", Times.Optional).ToString());
        Assert.AreEqual("a*", Start.Text("a", Times.ZeroOrMore).ToString());
        Assert.AreEqual("a+", Start.Text("a", Times.OneOrMore).ToString());
        Assert.AreEqual("a", Start.Text("a", Times.Exactly(1)).ToString());
        Assert.AreEqual("a{4}", Start.Text("a", Times.Exactly(4)).ToString());
        Assert.AreEqual("a*", Start.Text("a", Times.AtLeast(0)).ToString());
        Assert.AreEqual("a+", Start.Text("a", Times.AtLeast(1)).ToString());
        Assert.AreEqual("a{8,}", Start.Text("a", Times.AtLeast(8)).ToString());
        Assert.AreEqual("a{0,5}", Start.Text("a", Times.Between(0, 5)).ToString(), ".NET treats {,5} as literal text");
        Assert.AreEqual("a?", Start.Text("a", Times.Between(0, 1)).ToString());
        Assert.AreEqual("a{3}", Start.Text("a", Times.Between(3, 3)).ToString());
        Assert.AreEqual("a{2,256}", Start.Text("a", Times.Between(2, 256)).ToString());
    }

    [TestMethod]
    public void AsFewAsPossible_Adds_The_Lazy_Marker()
    {
        Assert.AreEqual("a*?", Start.Text("a", Times.ZeroOrMore.AsFewAsPossible()).ToString());
        Assert.AreEqual("a+?", Start.Text("a", Times.OneOrMore.AsFewAsPossible()).ToString());
        Assert.AreEqual("a??", Start.Text("a", Times.Optional.AsFewAsPossible()).ToString());
        Assert.AreEqual("a{2,}?", Start.Text("a", Times.AtLeast(2).AsFewAsPossible()).ToString());
        Assert.AreEqual("a{2,5}?", Start.Text("a", Times.Between(2, 5).AsFewAsPossible()).ToString());
    }

    [TestMethod]
    public void AsFewAsPossible_Finds_The_Shortest_Text()
    {
        var shortestTag = Start.Text("<").AnyCharacter(Times.OneOrMore.AsFewAsPossible()).Text(">");
        var longestTag = Start.Text("<").AnyCharacter(Times.OneOrMore).Text(">");

        Assert.AreEqual("<a>", shortestTag.FindFirst("<a><b>")!.Text);
        Assert.AreEqual("<a><b>", longestTag.FindFirst("<a><b>")!.Text);
    }

    [TestMethod]
    public void AsFewAsPossible_Throws_On_Fixed_Counts()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Times.Exactly(1).AsFewAsPossible());
        Assert.ThrowsExactly<InvalidOperationException>(() => Times.Exactly(4).AsFewAsPossible());
        Assert.ThrowsExactly<InvalidOperationException>(() => Times.Between(3, 3).AsFewAsPossible());
    }

    [TestMethod]
    public void Counts_That_Could_Only_Match_Nothing_Throw()
    {
        Assert.AreEqual("count", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Times.Exactly(0)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Times.Between(0, 0)).ParamName);
    }

    [TestMethod]
    public void Invalid_Bounds_Throw()
    {
        Assert.AreEqual("count", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Times.Exactly(-1)).ParamName);
        Assert.AreEqual("minimum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Times.AtLeast(-1)).ParamName);
        Assert.AreEqual("minimum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Times.Between(-1, 2)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Times.Between(3, 2)).ParamName);
    }
}
