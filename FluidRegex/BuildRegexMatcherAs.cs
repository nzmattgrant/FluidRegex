using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluidRegex
{
    public class BuildRegexMatcherAs : FluidRegexBuilderBase<BuildRegexMatcherAs>
    {
        public BuildRegexMatcherAs Group(string regexGroupString, NumberOfTimes quantifierType = NumberOfTimes.Once)
        {
            return MatchGroup(EscapeSubstring(regexGroupString), quantifierType);
        }

        public BuildRegexMatcherAs Group(FluidRegexGroupBuilder regexGroup, NumberOfTimes quantifierType = NumberOfTimes.Once) {
            return MatchGroup(regexGroup.ToString(), quantifierType);
        }

        private BuildRegexMatcherAs Group(string regexGroupString, NumberOfTimes quantifierType = NumberOfTimes.Once) {
            CurrentRegexExpression = CurrentRegexExpression + "(" + regexGroupString + ")" + GetQuantifierStringFromQuantifierType(quantifierType);
            return this;
        }
    }
}
