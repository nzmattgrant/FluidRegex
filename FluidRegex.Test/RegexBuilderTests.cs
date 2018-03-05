using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluidRegex.Test
{
    [TestClass]
    public class RegexBuilderTests
    {
        //TODO split this method out to multiple
        [TestMethod]
        public void MatchEverythingButWhitespaceWontMatchWhiteSpace()
        {
            var regex = @"\w";
            var builder = new FluidRegexBuilder()
                .MatchAnythingButWhiteSpace(RegexQuantifierType.Once);
            var builtRegex = builder.CreateInstance();
            var builtRegexString = builder
                .CreateInstanceAsString();
            Assert.AreEqual(regex, builtRegexString);
            Assert.IsTrue(builtRegex.IsMatch("hello"));
            Assert.IsFalse(builtRegex.IsMatch(" "));
        }
    }
}
