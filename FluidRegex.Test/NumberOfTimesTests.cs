using static FluidRegex.NumberOfTimes;

namespace FluidRegex.Test;

[TestClass]
public class NumberOfTimesTests
{
    [TestMethod]
    public void Each_Count_Emits_Its_Suffix()
    {
        Assert.AreEqual("", Once.ToString());
        Assert.AreEqual("", default(NumberOfTimes).ToString());
        Assert.AreEqual("?", Optional.ToString());
        Assert.AreEqual("*", ZeroOrMore.ToString());
        Assert.AreEqual("+", OneOrMore.ToString());
        Assert.AreEqual("{0}", Exactly(0).ToString());
        Assert.AreEqual("", Exactly(1).ToString());
        Assert.AreEqual("{4}", Exactly(4).ToString());
        Assert.AreEqual("*", AtLeast(0).ToString());
        Assert.AreEqual("+", AtLeast(1).ToString());
        Assert.AreEqual("{8,}", AtLeast(8).ToString());
        Assert.AreEqual("{0}", AtMost(0).ToString());
        Assert.AreEqual("?", AtMost(1).ToString());
        Assert.AreEqual("{0,5}", AtMost(5).ToString(), ".NET treats {,5} as literal text");
        Assert.AreEqual("?", Between(0, 1).ToString());
        Assert.AreEqual("{3}", Between(3, 3).ToString());
        Assert.AreEqual("{2,256}", Between(2, 256).ToString());
    }

    [TestMethod]
    public void Equivalent_Counts_Are_Equal()
    {
        Assert.AreEqual(Once, default(NumberOfTimes));
        Assert.AreEqual(Once, Exactly(1));
        Assert.AreEqual(Once, Between(1, 1));
        Assert.AreEqual(OneOrMore, AtLeast(1));
        Assert.AreEqual(ZeroOrMore, AtLeast(0));
        Assert.AreEqual(Optional, Between(0, 1));
        Assert.AreEqual(Optional, AtMost(1));
        Assert.AreEqual(Exactly(0), AtMost(0));
        Assert.AreEqual(Exactly(3), Between(3, 3));
        Assert.IsTrue(Exactly(3) == Between(3, 3));
        Assert.IsTrue(OneOrMore != ZeroOrMore);
        Assert.AreNotEqual(OneOrMore, OneOrMore.AsFewAsPossible());
        Assert.AreEqual(AtLeast(1).GetHashCode(), OneOrMore.GetHashCode());
        Assert.AreEqual(Between(0, 1).GetHashCode(), Optional.GetHashCode());
    }

    [TestMethod]
    public void AsFewAsPossible_Adds_The_Lazy_Marker()
    {
        Assert.AreEqual("*?", ZeroOrMore.AsFewAsPossible().ToString());
        Assert.AreEqual("+?", OneOrMore.AsFewAsPossible().ToString());
        Assert.AreEqual("??", Optional.AsFewAsPossible().ToString());
        Assert.AreEqual("{2,}?", AtLeast(2).AsFewAsPossible().ToString());
        Assert.AreEqual("{2,5}?", Between(2, 5).AsFewAsPossible().ToString());
        Assert.IsTrue(OneOrMore.AsFewAsPossible().IsAsFewAsPossible);
        Assert.IsFalse(OneOrMore.IsAsFewAsPossible);
    }

    [TestMethod]
    public void AsFewAsPossible_Throws_On_Fixed_Counts()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Once.AsFewAsPossible());
        Assert.ThrowsExactly<InvalidOperationException>(() => Exactly(4).AsFewAsPossible());
        Assert.ThrowsExactly<InvalidOperationException>(() => Between(3, 3).AsFewAsPossible());
    }

    [TestMethod]
    public void Invalid_Bounds_Throw()
    {
        Assert.AreEqual("times", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Exactly(-1)).ParamName);
        Assert.AreEqual("minimum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AtLeast(-1)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AtMost(-1)).ParamName);
        Assert.AreEqual("minimum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Between(-1, 2)).ParamName);
        Assert.AreEqual("maximum", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Between(3, 2)).ParamName);
    }

    [TestMethod]
    public void Minimum_And_Maximum_Describe_The_Count()
    {
        Assert.AreEqual(1, Once.Minimum);
        Assert.AreEqual(1, Once.Maximum);
        Assert.AreEqual(0, ZeroOrMore.Minimum);
        Assert.IsNull(ZeroOrMore.Maximum);
        Assert.AreEqual(2, Between(2, 256).Minimum);
        Assert.AreEqual(256, Between(2, 256).Maximum);
    }
}
