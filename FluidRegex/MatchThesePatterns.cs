using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluidRegex
{
    public class MatchThesePatterns : FluidRegexBuilderBase<MatchThesePatterns>
    {
        public MatchThesePatterns Group(FluidRegexGroupBuilder regexGroup, NumberOfTimes quantifierType = NumberOfTimes.Once)
        {
            return AddGroup(regexGroup.ToString(), quantifierType);
        }

        public MatchThesePatterns Group(string regexGroupString, NumberOfTimes quantifierType = NumberOfTimes.Once, bool escapeCharachters = true)
        {
            var updatedString = escapeCharachters ? EscapeSubstring(regexGroupString) : regexGroupString;
            return AddGroup(updatedString, quantifierType);
        }

        private MatchThesePatterns AddGroup(string regexGroupString, NumberOfTimes quantifierType = NumberOfTimes.Once) {
            CurrentRegexExpression = CurrentRegexExpression + "(" + regexGroupString + ")" + GetQuantifierStringFromQuantifierType(quantifierType);
            return this;
        }
    }
}
