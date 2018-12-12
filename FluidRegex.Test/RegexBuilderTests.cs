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
                .MatchAnythingButWhiteSpace(NumberOfTimes.Once);
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
                .MatchAnythingButWhiteSpace(NumberOfTimes.Once);
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


        [TestMethod]
        public void Match_One_Of_These_Charachters_Matches_Charachters() {
            var regex = "^[sz]";
            var builder = new FluidRegexBuilder()
                .MatchStringStart()
                .MatchOneOfTheseCharachters(NumberOfTimes.Once, "s", "z");
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch("astart"));
            Assert.IsFalse(builtRegex.IsMatch(" start"));
            Assert.IsFalse(builtRegex.IsMatch("aaaaaastart start"));
            Assert.IsFalse(builtRegex.IsMatch(" "));
            Assert.IsFalse(builtRegex.IsMatch("aaa"));
            Assert.IsTrue(builtRegex.IsMatch("start of the string"));
            Assert.IsTrue(builtRegex.IsMatch("ztart of the string"));
        }

        [TestMethod]
        public void Match_Start_String_Only_Matches_Start_Of_String() {
            var regex = "^start";
            var builder = new FluidRegexBuilder()
                .MatchStringStart()
                .MatchTheCharachter("start");
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch("astart"));
            Assert.IsFalse(builtRegex.IsMatch(" start"));
            Assert.IsFalse(builtRegex.IsMatch("aaaaaastart start"));
            Assert.IsFalse(builtRegex.IsMatch(" "));
            Assert.IsFalse(builtRegex.IsMatch("aaa"));
            Assert.IsTrue(builtRegex.IsMatch("start of the string"));

        }

        [TestMethod]
        public void Match_One_Of_These_Charachters_Matches_Right_Charatchers() {
            var regex = "[-+.']";
            var builder = new FluidRegexBuilder()
                .MatchOneOfTheseCharachters(NumberOfTimes.Once, "-", "+", ".", "'");
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("-"));
            Assert.IsTrue(builtRegex.IsMatch(" blah+blah"));
            Assert.IsTrue(builtRegex.IsMatch("-.start"));
            Assert.IsTrue(builtRegex.IsMatch("..."));
            Assert.IsTrue(builtRegex.IsMatch("'ahhh'"));
            Assert.IsTrue(builtRegex.IsMatch("start of the string..."));

        }

        [TestMethod]
        public void Match_One_Of_These_Charachters_Fails_On_No_Match_Strings() {
            var regex = "[-+.']";
            var builder = new FluidRegexBuilder()
                .MatchOneOfTheseCharachters(NumberOfTimes.Once, "-", "+", ".", "'");
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch(" "));
            Assert.IsFalse(builtRegex.IsMatch(" blah blah"));
            Assert.IsFalse(builtRegex.IsMatch("start"));
            Assert.IsFalse(builtRegex.IsMatch("///"));
            Assert.IsFalse(builtRegex.IsMatch("ahhh"));
            Assert.IsFalse(builtRegex.IsMatch("start of the string"));

        }

        public void Test_Design_Example()
        {
            //match this bad boy
            //^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$

            var charachterGroup = new FluidRegexGroupBuilder()
                .MatchOneOfTheseCharachters(NumberOfTimes.Once, "-", "+", ".", "'")
                .MatchAnyWords(NumberOfTimes.OneOrMore);

            //Idea about a refactor (think about this later)
            //new RegexMatcher().Match().WhiteSpace().ThenGroup().ThenTheCharchater

            var builder = new FluidRegexBuilder()
                .MatchStringStart()
                .MatchAnyWords(NumberOfTimes.OneOrMore)
                .MatchGroup(
                    charachterGroup, NumberOfTimes.ZeroOrMore
                )
                .MatchOneOfTheseCharachters(NumberOfTimes.Once, "@");
            //.matchTheCharachter("@")
            //.matchAnythingButWhitespace
            //.matchGroup(DefineRegexGroupAs()
            //        .matchOneOfTheseCharachters("-", ".")
            //        .matchAnythingButWhitespace(),
            //    RegexQuantifierType.MatchManyTimes
            //)
        }
    }
}
