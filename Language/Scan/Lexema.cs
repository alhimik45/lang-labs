using System;

namespace Language.Scan
{
    public class Lexema : IComparable<Lexema>, IComparable
    {
        public LexType Type = LexType.Terr;
        public string Tok = "";
        public string TTok = "";
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

        public int CompareTo(Lexema other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var lineComparison = Line.CompareTo(other.Line);
            if (lineComparison != 0) return lineComparison;
            return Symbol.CompareTo(other.Symbol);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is Lexema)) throw new ArgumentException($"Object must be of type {nameof(Lexema)}");
            return CompareTo((Lexema) obj);
        }
    }
}