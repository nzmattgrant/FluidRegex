using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluidRegex {
    public class FluidRegexBuilderBase<T> where T : FluidRegexBuilderBase<T> {

        protected T GetThisAsOriginalType()
        {
            return (T) this;
        }
        public string CurrentRegexExpression { get; set; }

        public T DefineRegexAs() {
            return GetThisAsOriginalType();
        }

        public T MatchStringStart() {
            CurrentRegexExpression += "^";
            return GetThisAsOriginalType();
        }

        public T MatchWhitespace(NumberOfTimes quantifierType) {
            CurrentRegexExpression += "\\W" + GetQuantifierStringFromQuantifierType(quantifierType); ;
            return GetThisAsOriginalType();
        }

        public T MatchDigits(NumberOfTimes quantifierType) {
            CurrentRegexExpression += "+d";
            return GetThisAsOriginalType();
        }

        public T MatchAnythingButWhiteSpace(NumberOfTimes? quantifierType) {
            CurrentRegexExpression += @"\w";
            return GetThisAsOriginalType();
        }

        public T MatchAnyWords(NumberOfTimes? quantifierType) {
            CurrentRegexExpression += @"\w";
            return GetThisAsOriginalType();
        }

        public T MatchTheCharachter(string charachter, NumberOfTimes? quantifierType = NumberOfTimes.Once) {
            //Add the checks for escape chars
            CurrentRegexExpression += charachter;
            return GetThisAsOriginalType();
        }

        public T MatchOneOfTheseCharachters(NumberOfTimes quantifierType = NumberOfTimes.Once, params string[] stringsToMatch) {
            //Add the checks for escape chars
            CurrentRegexExpression += "[" + string.Join("", stringsToMatch) + "]" + GetQuantifierStringFromQuantifierType(quantifierType);
            return GetThisAsOriginalType();
        }


        public T MatchCustom(string customMatch) {
            //check for characters that need to be escaped
            CurrentRegexExpression += customMatch;
            return GetThisAsOriginalType();
        }

        public T SetCurrentRegexAsGroup() {
            CurrentRegexExpression = "(" + CurrentRegexExpression + ")";
            return GetThisAsOriginalType();
        }

        public string CreateInstanceAsString() {
            return CurrentRegexExpression;
        }

        public Regex CreateInstance() {
            return new Regex(CurrentRegexExpression);
        }

        protected string GetQuantifierStringFromQuantifierType(NumberOfTimes regexQuantifierType) {
            switch (regexQuantifierType) {
                case NumberOfTimes.Once:
                    return "";
                case NumberOfTimes.OnceOrNone:
                    return "?";
                case NumberOfTimes.OneOrMore:
                    return "+";
                case NumberOfTimes.ZeroOrMore:
                    return "*";
                default:
                    return null;
            }
        }
    }
}
