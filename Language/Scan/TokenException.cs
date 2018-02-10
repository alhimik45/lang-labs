using System;

namespace Language.Scan
{
    public class TokenException : Exception
    {
        public TokenException(Lexema l) : base($"Wrong token `{l.Tok}` at {l.Line}:{l.Symbol}")
        {
        }
    }
}