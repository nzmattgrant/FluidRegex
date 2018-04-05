using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluidRegex.Test
{
    [TestClass]
    public class RegexBuilderTests
    {
        [TestMethod]
        public void Match_Everything_But_Whitespace_Wont_Match_WhiteSpace()
        {
            var regex = @"\w";
            var builder = new FluidRegexBuilder()
                .MatchAnythingButWhiteSpace(RegexQuantifierType.Once);
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch(" "));
        }

        [TestMethod]
        public void Match_Everything_But_Whitespace_Matches_Non_White_Space() {
            var regex = @"\w";
            var builder = new FluidRegexBuilder()
                .MatchAnythingButWhiteSpace(RegexQuantifierType.Once);
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("hello"));
            Assert.IsTrue(builtRegex.IsMatch("$$$fadg"));
            Assert.IsTrue(builtRegex.IsMatch("^%$*& adg"));

        }

        [TestMethod]
        public void Match_The_Charachter_Matches_Charatcher() {
            var regex = "@";
            var builder = new FluidRegexBuilder()
                .MatchTheCharachter(regex);
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("a@a.a"));
            Assert.IsTrue(builtRegex.IsMatch("a@a@a@"));
            Assert.IsTrue(builtRegex.IsMatch("@@@aadsaf@"));
            Assert.IsTrue(builtRegex.IsMatch("aadsaf@"));
        }


        [TestMethod]
        public void Match_The_Charachter_Does_Not_Match_Other_Charatchers() {
            var regex = "@";
            var builder = new FluidRegexBuilder()
                .MatchTheCharachter(regex);
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch("a"));
            Assert.IsFalse(builtRegex.IsMatch("aaaaaa..dfalkj343"));
            Assert.IsFalse(builtRegex.IsMatch("#^%&$"));
            Assert.IsFalse(builtRegex.IsMatch(" "));

        }
    }
}
