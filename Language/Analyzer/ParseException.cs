using System;
using System.Linq;
using Language.Scan;

namespace Language.Analyzer
{
    public class ParseException : Exception
    {
        public Lexema Lexema { get; }

        public ParseException(Lexema unexpected, params LexType[] expected) :
            base( //TODO
                $"Unexpected {Enum.GetName(typeof(LexType), unexpected.Type)} at {unexpected.Line}:{unexpected.Symbol}" +
                (expected.Any()
                    ? $", expected {string.Join(" or ", expected.Select(e => Enum.GetName(typeof(LexType), e)))}"
                    : ""))
        {
            Lexema = unexpected;
        }

        public ParseException(string unexpected, Lexema u) :
            base(unexpected + $" at {u.Line}:{u.Symbol}")
        {
        }
    }
}