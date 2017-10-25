using System;
using Language.Scan;

namespace Language.Analyzer
{
    public class ParseException : Exception
    {
        public Lexema Lexema { get; }

        public ParseException(Lexema unexpected, LexType? expected) :
            base(
                $"Unexpected {Enum.GetName(typeof(LexType), unexpected.Type)} at {unexpected.Line}:{unexpected.Symbol}" +
                (expected != null
                    ? $", expected {Enum.GetName(typeof(LexType), expected.Value)}"
                    : ""))
        {
            Lexema = unexpected;
        }
    }
}