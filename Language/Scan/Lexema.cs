using System;

namespace Language.Scan
{
    public class Lexema 
    {
        public LexType Type = LexType.Terr;
        public string Tok = "";
        public int Line { get; set; }
        public int Symbol { get; set; }

        public override string ToString()
        {
            return $"{Tok} : {Enum.GetName(typeof(LexType), Type)}";
        }

        public static bool operator <(Lexema me, Lexema other)
        {
            return me.Line < other.Line || me.Line == other.Line && me.Symbol < other.Symbol;
        }

        public static bool operator >(Lexema me, Lexema other)
        {
            return me.Line > other.Line || me.Line == other.Line && me.Symbol > other.Symbol;
        }
    }
}