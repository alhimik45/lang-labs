using System;

namespace Language.Scan
{
    public class TokenException : Exception
    {
        public TokenException(Lexema l) : base($"Wront token `{l.Tok}` at {l.Line}:{l.Symbol}")
        {
        }
    }
}