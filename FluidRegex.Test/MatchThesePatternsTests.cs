using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluidRegex.Test
{
    [TestClass]
    public class MatchThesePatternsTests
    {
        [TestMethod]
        public void Match_Everything_But_Whitespace_Wont_Match_WhiteSpace()
        {
            var regex = @"\w";
            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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
        public void LookAhead()
        {
            var regex = "X(?=Y)";
            var builder = new MatchThesePatterns()
                .Once("X")
                .Before("Y");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder
                .ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("XYZ"));
            Assert.IsTrue(builtRegex.IsMatch("XY"));
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

            var builder = new MatchThesePatterns()
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
            var builder = new MatchThesePatterns()
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

        [TestMethod]
        public void LookBehind_Matches_Pattern_Preceded_By_Specific_String()
        {
            var regex = "(?<=X)Y";
            var builder = new MatchThesePatterns()
                .LookBehind("X")
                .Once("Y");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("XY"));
            Assert.IsFalse(builtRegex.IsMatch("Y"));
            Assert.IsFalse(builtRegex.IsMatch("ZY"));
        }

        [TestMethod]
        public void NegativeLookAhead_Matches_Pattern_Not_Followed_By_Specific_String()
        {
            var regex = "X(?!Y)";
            var builder = new MatchThesePatterns()
                .Once("X")
                .NegativeLookAhead("Y");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("X"));
            Assert.IsTrue(builtRegex.IsMatch("XZ"));
            Assert.IsFalse(builtRegex.IsMatch("XY"));
        }

        [TestMethod]
        public void NegativeLookBehind_Matches_Pattern_Not_Preceded_By_Specific_String()
        {
            var regex = "(?<!X)Y";
            var builder = new MatchThesePatterns()
                .NegativeLookBehind("X")
                .Once("Y");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("Y"));
            Assert.IsTrue(builtRegex.IsMatch("ZY"));
            Assert.IsFalse(builtRegex.IsMatch("XY"));
        }

        [TestMethod]
        public void MatchWhitespace_Matches_Non_Word_Characters()
        {
            // Note: MatchWhitespace implementation currently uses \W which matches any non-word character (not just whitespace)
            var regex = "\\W";
            var builder = new MatchThesePatterns()
                .MatchWhitespace(NumberOfTimes.Once);
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch(" "));
            Assert.IsTrue(builtRegex.IsMatch("@"));
            Assert.IsFalse(builtRegex.IsMatch("a"));
        }

        [TestMethod]
        public void SetCurrentRegexAsGroup_Wraps_Expression_In_Group()
        {
            var regex = "(test)";
            var builder = new MatchThesePatterns()
                .MatchString("test")
                .SetCurrentRegexAsGroup();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builder.GetRegex().IsMatch("test"));
        }

        [TestMethod]
        public void Match_Custom_Matches_Provided_Pattern()
        {
            var regex = "\\d{3}";
            var builder = new MatchThesePatterns()
                .MatchCustom("\\d{3}");
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("123"));
            Assert.IsFalse(builtRegex.IsMatch("12"));
        }

        [TestMethod]
        public void Match_String_End_Matches_End_Of_String()
        {
            var regex = "end$";
            var builder = new MatchThesePatterns()
                .MatchString("end")
                .StringEnd();
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("end"));
            Assert.IsFalse(builtRegex.IsMatch("ending"));
        }

        [TestMethod]
        public void Match_One_Of_These_Characters_With_Min_Max_Quantifiers()
        {
            var regex = "^[abc]{2,3}$";
            var builder = new MatchThesePatterns()
                .StringStart()
                .OneOfTheseCharacters(2, 3, "a", "b", "c")
                .StringEnd();
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("ab"));
            Assert.IsTrue(builtRegex.IsMatch("abc"));
            Assert.IsTrue(builtRegex.IsMatch("cba"));
            Assert.IsFalse(builtRegex.IsMatch("a"));
            Assert.IsFalse(builtRegex.IsMatch("abcd"));
        }

        [TestMethod]
        public void MatchTheSubstringOnceOrMore_Matches_Repeating_Substring()
        {
            var regex = "(abc)+";
            // MatchTheSubstringOnceOrMore escapes characters, but doesn't wrap in group automatically unless we do it?
            // Wait, EscapeSpecialCharacters just escapes. + applies to last character if not grouped.
            // "abc+" matches "abcc", not "abcabc".
            // Let's check implementation of MatchTheSubstringOnceOrMore
            // It calls EscapeSpecialCharacters(substring) + GetQuantifierStringFromQuantifierType(NumberOfTimes.OneOrMore)
            // If substring is "abc", result is "abc+". This means "ab" followed by one or more "c".
            
            var builder = new MatchThesePatterns().MatchTheSubstringOnceOrMore("abc");
            var builtRegex = builder.GetRegex();
            Assert.IsTrue(builtRegex.IsMatch("abcc"));
            Assert.IsTrue(builtRegex.IsMatch("abc"));
            // This confirms the behavior of the method, even if the name suggests matching the whole substring multiple times
        }

        [TestMethod]
        public void WordBoundary_Matches_Boundary()
        {
            var regex = "\\btest\\b";
            var builder = new MatchThesePatterns()
                .WordBoundary()
                .MatchString("test")
                .WordBoundary();
            var builtRegex = builder.GetRegex();
            var builtRegexString = builder.ToString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("test"));
            Assert.IsTrue(builtRegex.IsMatch("this is a test"));
            Assert.IsFalse(builtRegex.IsMatch("testing"));
            Assert.IsFalse(builtRegex.IsMatch("attest"));
        }
    }
}
