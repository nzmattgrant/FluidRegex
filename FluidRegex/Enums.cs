using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidRegex
{
    public enum RegexNodeTypes
    {

    }

    public enum NumberOfTimes
    {
        /// <summary>
        /// No quantifier symbol, match once, non optional
        /// </summary>
        [Description("")]
        Once,
        /// <summary>
        /// +
        /// </summary>
        [Description("+")]
        OneOrMore,
        /// <summary>
        /// *
        /// </summary>
        [Description("*")]
        ZeroOrMore,
        /// <summary>
        /// ?
        /// </summary>
        [Description("?")]
        OnceOptional,
        /// <summary>
        /// +?
        /// </summary>
        [Description("+?")]
        OneOrMoreOptional,
        /// <summary>
        /// *?
        /// </summary>
        [Description("*?")]
        ZeroOrMoreOptional
    }
}
