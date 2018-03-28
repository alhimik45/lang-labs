using System.Collections.Generic;
using System.Linq;

namespace Language.Compiler
{
    public class Optimizer
    {
        private readonly List<Triad> ir;

        private readonly Operation[] replaceOps =
        {
            Operation.Add,
            Operation.Sub,
            Operation.Mul,
            Operation.Div,
            Operation.Mod,
            Operation.Xor,
            Operation.Or,
            Operation.And,
            Operation.Not,
            Operation.Push,
        };

        private readonly Operation[] splitOps =
        {
            Operation.Call,
            Operation.Jmp,
            Operation.Jz,
        };

        public Optimizer(List<Triad> ir)
        {
            this.ir = ir;
        }

        public List<Triad> Optimize()
        {
            var splitted = SplitLinear();
            return splitted.SelectMany(ConstSubstitution).ToList();
        }

        private List<List<Triad>> SplitLinear()
        {
            var res = new List<List<Triad>>();
            var ll = new List<Triad>();
            foreach (var triad in ir)
            {
                ll.Add(triad);
                if (splitOps.Contains(triad.Operation))
                {
                    res.Add(ll);
                    ll = new List<Triad>();
                }
            }

            res.Add(ll);
            return res;
        }

        private List<Triad> ConstSubstitution(List<Triad> code)
        {
            var constValues = new Dictionary<IResult, ConstResult>();
            for (var i = 0; i < code.Count; i++)
            {
                var triad = code[i];
                if (triad.Operation == Operation.Assign)
                {
                    if (triad.Arg2 is ConstResult)
                    {
                        constValues[triad.Arg1] = triad.Arg2;
                    }
                }

                if (triad.Operation == Operation.Cast)
                {
                    if (triad.Arg1 != null && constValues.ContainsKey(triad.Arg1))
                    {
                        triad.Arg1 = constValues[triad.Arg1];
                    }
                }

                if (replaceOps.Contains(triad.Operation))
                {
                    if (triad.Arg1 != null && constValues.ContainsKey(triad.Arg1))
                    {
                        triad.Arg1 = constValues[triad.Arg1];
                    }

                    if (triad.Arg2 != null && constValues.ContainsKey(triad.Arg2))
                    {
                        triad.Arg2 = constValues[triad.Arg2];
                    }
                }

                ++i;
            }

            return code;
        }
    }
}