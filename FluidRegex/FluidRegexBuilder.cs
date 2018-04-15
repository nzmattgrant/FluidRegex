﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluidRegex
{
    public class FluidRegexBuilder
    {
        //Notes this is my attempt to learn .NET regular expressions by creating a readable constructor using a fluid builder class
        //Maybe have this in some kind of array so that we can have stand alone components look ahead etc
        //Things that could be done to track the grouping of the expression
        //Build out some kind of expression tree to track things
        //Questions
        //How can we gracefully end a group?
        //TODO Add some example regular expressions so you can add all the features
        public string CurrentRegexExpression { get; set; }

        public FluidRegexBuilder DefineRegexAs()
        {
            return this;
        }

        public FluidRegexBuilder MatchStringStart() {
            CurrentRegexExpression += "^";
            return this;
        }

        public FluidRegexBuilder MatchDigits(RegexQuantifierType quantifierType)
        {
            CurrentRegexExpression += "+d";
            return this;
        }

        public FluidRegexBuilder MatchAnythingButWhiteSpace(RegexQuantifierType? quantifierType)
        {
            CurrentRegexExpression += @"\w";
            return this;
        }

        public FluidRegexBuilder MatchTheCharachter(string charachter, RegexQuantifierType? quantifierType = RegexQuantifierType.Once)
        {
            //Add the checks for escape chars
            CurrentRegexExpression += charachter;
            return this;
        }

        public FluidRegexBuilder MatchOneOfTheseCharachters(RegexQuantifierType quantifierType = RegexQuantifierType.Once, params string[] stringsToMatch) {
            //Add the checks for escape chars
            CurrentRegexExpression += "[" + string.Join("", stringsToMatch) + "]" + getQuantifierStringFromQuantifierType(quantifierType);
            return this;
        }
        

        public FluidRegexBuilder MatchCustom(string customMatch)
        {
            //check for characters that need to be escaped
            CurrentRegexExpression += customMatch;
            return this;
        }

        public FluidRegexBuilder SetAsGroup()
        {
            CurrentRegexExpression = "(" + CurrentRegexExpression + ")";
            return this;
        }

        public string CreateInstanceAsString()
        {
            return CurrentRegexExpression;
        }

        public Regex CreateInstance()
        {
            return new Regex(CurrentRegexExpression);
        }

        private string getQuantifierStringFromQuantifierType(RegexQuantifierType regexQuantifierType)
        {
            switch (regexQuantifierType)
            {
                case RegexQuantifierType.Once:
                    return "";
                case RegexQuantifierType.OnceOrNone:
                    return "?";
                case RegexQuantifierType.OneOrMore:
                    return "+";
                case RegexQuantifierType.ZeroOrMore:
                    return "*";
                default:
                    return null;
            }
        }
    }
}
