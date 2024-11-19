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

        public T MatchStringEnd()
        {
            CurrentRegexExpression += "$";
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

        public T MatchAnythingButWhiteSpace(NumberOfTimes quantifierType = NumberOfTimes.Once) {
            CurrentRegexExpression += @"\w" + GetQuantifierStringFromQuantifierType(quantifierType);
            return GetThisAsOriginalType();
        }

        public T MatchAnyLettersOrNumbers(NumberOfTimes quantifierType = NumberOfTimes.Once) {
            CurrentRegexExpression += @"\w" + GetQuantifierStringFromQuantifierType(quantifierType);
            return GetThisAsOriginalType();
        }

        public T MatchTheCharacter(char charachter, NumberOfTimes quantifierType = NumberOfTimes.Once)
        {
            CurrentRegexExpression += EscapeCharacters(new []{ charachter })[0] + GetQuantifierStringFromQuantifierType(quantifierType);
            return GetThisAsOriginalType();
        }

        public T MatchTheCharachters(NumberOfTimes quantifierType = NumberOfTimes.Once, params char[] charachters) {
            CurrentRegexExpression += string.Join("", EscapeCharacters(charachters) + GetQuantifierStringFromQuantifierType(quantifierType));
            return GetThisAsOriginalType();
        }

        public T MatchTheSubstring(string substring, NumberOfTimes quantifierType = NumberOfTimes.Once)
        {
            CurrentRegexExpression += EscapeSpecialCharacters(substring) + GetQuantifierStringFromQuantifierType(quantifierType);
            return GetThisAsOriginalType();
        }

        private string EscapeSpecialCharacters(string stringToEscape)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var t in stringToEscape)
            {
                sb.Append(EscapeCharacters(new[] {t}));
            }
            return sb.ToString();
        }

        public T MatchOneOfTheseCharacters(NumberOfTimes quantifierType = NumberOfTimes.Once, params string[] stringsToMatch) {
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

        public override string ToString() {
            return CurrentRegexExpression;
        }

        public Regex GetRegex() {
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

        public string[] EscapeCharacters(char[] characters)
        {
            string[] escapedCharacters = new string[characters.Length];
            for (var i = 0; i < characters.Length; i++)
            {
                if (characters[i] == '.')
                {
                    escapedCharacters[i] = $"\\.";
                    continue;
                }
                if (characters[i] == '/')
                {
                    escapedCharacters[i] = $"\\/";
                    continue;
                }

                escapedCharacters[i] = characters[i].ToString();
            }
            return escapedCharacters;

        }
    }
}
