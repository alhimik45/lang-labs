using Language.Analyzer;

namespace Language.Compiler
{
    public class TriadResult : IResult
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
    }
}