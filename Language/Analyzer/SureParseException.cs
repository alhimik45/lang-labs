using System;
using Language.Scan;

namespace Language.Analyzer
{
    public class SureParseException : Exception
    {
        public SureParseException(ParseException e) :
            base("", e)
        {
        }
    }
}