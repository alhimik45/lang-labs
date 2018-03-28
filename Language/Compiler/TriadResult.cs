using System;
using Language.Analyzer;

namespace Language.Compiler
{
    public class TriadResult : IResult, IEquatable<TriadResult>
    {
        public int Index { get; set; }
        public SemType Type { get; private set; }

        public static TriadResult Of(int index, SemType type)
        {
            return new TriadResult {Index = index, Type = type};
        }

        public override string ToString()
        {
            return $"({Index})";
        }
        
        public bool Equals(TriadResult other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Index == other.Index && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((TriadResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ (int) Type;
            }
        }
    }
}