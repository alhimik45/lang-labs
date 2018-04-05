using System;
using Language.Analyzer;

namespace Language.Compiler
{
    public class ConstResult : IResult
    {
        public dynamic Value { get; private set; }
        public SemType Type => Value is int ? SemType.Int : Value is long ? SemType.LongLongInt : SemType.Char;

        public static ConstResult Of(dynamic val)
        {
            return new ConstResult {Value = val};
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public dynamic Cast(SemType type)
        {
            switch (type)
            {
                case SemType.Int:
                    return (int) Value;
                case SemType.LongLongInt:
                    return(long) Value;
                case SemType.Char:
                    return (byte) Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public string Str => (Value is char ? (int) Value : Value).ToString();
    }
}