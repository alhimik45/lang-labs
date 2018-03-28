using System;
using Language.Analyzer;

namespace Language.Compiler
{
    public class VariableResult : IResult, IEquatable<VariableResult>
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

        public bool Equals(VariableResult other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return ReferenceEquals(this, other) || Equals(Var, other.Var);
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

            return obj.GetType() == GetType() && Equals((VariableResult) obj);
        }

        public override int GetHashCode()
        {
            return Var != null ? Var.GetHashCode() : 0;
        }
    }
}