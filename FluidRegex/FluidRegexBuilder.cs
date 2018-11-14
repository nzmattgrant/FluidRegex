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
        public FluidRegexBuilder MatchGroup(FluidRegexGroupBuilder regexGroup, RegexQuantifierType quantifierType = RegexQuantifierType.Once) {
            return MatchGroup(regexGroup.CreateInstanceAsString());
        }

        public FluidRegexBuilder MatchGroup(string regexGroupString, RegexQuantifierType quantifierType = RegexQuantifierType.Once) {
            CurrentRegexExpression = CurrentRegexExpression + "(" + regexGroupString + ")" + GetQuantifierStringFromQuantifierType(quantifierType);
            return this;
        }
    }
}
