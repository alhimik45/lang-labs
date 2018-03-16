using Language.Analyzer;

namespace Language.Compiler
{
    public interface IResult
    {
        SemType Type { get; }
    }
}