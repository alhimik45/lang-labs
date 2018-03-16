using Language.Analyzer;

namespace Language.Compiler
{
    public class VariableResult : IResult
    {
        public VarInfo Var { get; set; }
        public SemType Type => Var.Type;

        public static VariableResult Of(VarInfo var)
        {
            return new VariableResult {Var = var};
        }

        public override string ToString()
        {
            return $"{Var.FullName}";
        }
    }
}