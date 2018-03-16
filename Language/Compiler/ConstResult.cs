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
    }
}