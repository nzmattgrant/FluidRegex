using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluidRegex
{
    public class FluidRegexBuilder : FluidRegexBuilderBase<FluidRegexBuilder>
    {
        public FluidRegexBuilder MatchSubstringGroup(string regexGroupString, NumberOfTimes quantifierType = NumberOfTimes.Once)
        {
            return MatchGroup(EscapeSubstring(regexGroupString), quantifierType);
        }

        public FluidRegexBuilder MatchGroup(FluidRegexGroupBuilder regexGroup, NumberOfTimes quantifierType = NumberOfTimes.Once) {
            return MatchGroup(regexGroup.ToString(), quantifierType);
        }

        private FluidRegexBuilder MatchGroup(string regexGroupString, NumberOfTimes quantifierType = NumberOfTimes.Once) {
            CurrentRegexExpression = CurrentRegexExpression + "(" + regexGroupString + ")" + GetQuantifierStringFromQuantifierType(quantifierType);
            return this;
        }
    }
}
