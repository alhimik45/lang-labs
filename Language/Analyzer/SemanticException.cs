using System;
using Language.Scan;

namespace Language.Analyzer
{
    public class SemanticException : Exception
    {
        public SemanticException(string message, Lexema location, string reason = null) :
            base($"{message} at {location.Line}:{location.Symbol}" +
                 (reason != null ? $", {reason}" : ""))
        {
        }
    }
}