using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluidRegex.Test
{
    [TestClass]
    public class BuildRegexMatcherAsTests
    {
        [TestMethod]
        public void Match_Everything_But_Whitespace_Wont_Match_WhiteSpace()
        {
            var regex = @"\w";
            var builder = new BuildRegexMatcherAs()
                .MatchAnythingButWhiteSpace(NumberOfTimes.Once);
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch(" "));
        }

        [TestMethod]
        public void Match_Everything_But_Whitespace_Matches_Non_White_Space() {
            var regex = @"\w";
            var builder = new BuildRegexMatcherAs()
                .MatchAnythingButWhiteSpace(NumberOfTimes.Once);
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("hello"));
            Assert.IsTrue(builtRegex.IsMatch("$$$fadg"));
            Assert.IsTrue(builtRegex.IsMatch("^%$*& adg"));

        }

        [TestMethod]
        public void Match_The_Character_Matches_Character()
        {
            var regex = '@';
            var builder = new BuildRegexMatcherAs()
                .MatchTheCharacters(NumberOfTimes.Once, regex);
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex.ToString(), builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("a@a.a"));
            Assert.IsTrue(builtRegex.IsMatch("a@a@a@"));
            Assert.IsTrue(builtRegex.IsMatch("@@@aadsaf@"));
            Assert.IsTrue(builtRegex.IsMatch("aadsaf@"));
        }


        [TestMethod]
        public void Match_The_Character_Does_Not_Match_Other_Characters() {
            var regex = '@';
            var builder = new BuildRegexMatcherAs()
                .MatchTheCharacters(NumberOfTimes.Once, regex);
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex.ToString(), builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch("a"));
            Assert.IsFalse(builtRegex.IsMatch("aaaaaa..dfalkj343"));
            Assert.IsFalse(builtRegex.IsMatch("#^%&$"));
            Assert.IsFalse(builtRegex.IsMatch(" "));
        }


        [TestMethod]
        public void Match_One_Of_These_Characters_Matches_Characters() {
            var regex = "^[sz]";
            var builder = new BuildRegexMatcherAs()
                .StringStart()
                .OneOfTheseCharacters(NumberOfTimes.Once, "s", "z");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
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
            var builder = new BuildRegexMatcherAs()
                .StringStart()
                .MatchString("start");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch("astart"));
            Assert.IsFalse(builtRegex.IsMatch(" start"));
            Assert.IsFalse(builtRegex.IsMatch("aaaaaastart start"));
            Assert.IsFalse(builtRegex.IsMatch(" "));
            Assert.IsFalse(builtRegex.IsMatch("aaa"));
            Assert.IsTrue(builtRegex.IsMatch("start of the string"));

        }

        [TestMethod]
        public void Match_One_Of_These_Characters_Matches_Right_Characters() {
            var regex = "[-+.']";
            var builder = new BuildRegexMatcherAs()
                .OneOfTheseCharacters(NumberOfTimes.Once, "-", "+", ".", "'");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("-"));
            Assert.IsTrue(builtRegex.IsMatch(" blah+blah"));
            Assert.IsTrue(builtRegex.IsMatch("-.start"));
            Assert.IsTrue(builtRegex.IsMatch("..."));
            Assert.IsTrue(builtRegex.IsMatch("'ahhh'"));
            Assert.IsTrue(builtRegex.IsMatch("start of the string..."));

        }

        [TestMethod]
        public void Match_One_Of_These_Characters_Fails_On_No_Match_Strings() {
            var regex = "[-+.']";
            var builder = new BuildRegexMatcherAs()
                .OneOfTheseCharacters(NumberOfTimes.Once, "-", "+", ".", "'");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsFalse(builtRegex.IsMatch(" "));
            Assert.IsFalse(builtRegex.IsMatch(" blah blah"));
            Assert.IsFalse(builtRegex.IsMatch("start"));
            Assert.IsFalse(builtRegex.IsMatch("///"));
            Assert.IsFalse(builtRegex.IsMatch("ahhh"));
            Assert.IsFalse(builtRegex.IsMatch("start of the string"));

        }

        [TestMethod]
        public void Test_Design_Example()
        {
            //match this bad boy
            //^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$

            var regexToTest = "^\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$";

            var wordsWithAllowedStartSymbols = new FluidRegexGroupBuilder()
                .OneOfTheseCharacters(NumberOfTimes.Once, "-", "+", ".", "'")
                .AnyLettersOrNumbers(NumberOfTimes.OneOrMore);

            //Idea about a refactor (think about this later)
            //new RegexMatcher().MatchString().WhiteSpace().ThenGroup().ThenTheCharchater

            var wordsWithHyphenAndDots = new FluidRegexGroupBuilder()
                .OneOfTheseCharacters(NumberOfTimes.Once, "-", ".")
                .AnyLettersOrNumbers(NumberOfTimes.OneOrMore);

            var builder = new BuildRegexMatcherAs()
                .StringStart()
                .AnyLettersOrNumbers(NumberOfTimes.OneOrMore)
                .Group(wordsWithAllowedStartSymbols, NumberOfTimes.ZeroOrMore)
                .TheCharacter('@')
                .AnyLettersOrNumbers(NumberOfTimes.OneOrMore)
                .Group(wordsWithHyphenAndDots, NumberOfTimes.ZeroOrMore)
                .TheCharacter('.')
                .AnyLettersOrNumbers(NumberOfTimes.OneOrMore)
                .Group(wordsWithHyphenAndDots,NumberOfTimes.ZeroOrMore)
                .StringEnd();

            var endResultString = builder.ToString();
            Console.WriteLine(endResultString);
            Assert.AreEqual(regexToTest, endResultString);
        }

        [TestMethod]
        public void Test_URL_matcher()
        {
            var regexToTest = "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{2256}\\.[a-z]{2,6}\\b([-a-zA-Z0-9@:%_\\+.~#()?&//=]*)";

            //todo combine the characters for matching
            var builder = new BuildRegexMatcherAs()
                .Once("http")
                .OnceOrNone("s")
                .Once("://") //todo be smart about it and match as a group if it's not in list of escaped characters and is over
                .Group("www.", NumberOfTimes.OnceOrNone)
                .OneOfTheseCharacters(2256, "-", "a-z", "A-Z", "0-9", "@", ":", "%", ".", "_", "\\+", "~", "#", "=") //todo, update the escaping logic
                .Once(".")
                .OneOfTheseCharacters(2, 6, "a-z")
                .WordBoundary()
                .Group(
                    new FluidRegexGroupBuilder()
                        .OneOfTheseCharacters(NumberOfTimes.ZeroOrMore, "-", "a-z", "A-Z", "0-9", "@", ":", "%", "_",
                            "\\+", ".", "~", "#", "(", ")", "?", "&", "/", "/", "=")
                );

            var endResultString = builder.ToString();
            Console.WriteLine(endResultString);
            Assert.AreEqual(regexToTest, endResultString);
        }
    }
}
