using System;
using System.Collections.Generic;
using System.Linq;
using Language.Analyzer;

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
            Operation.Lshift,
            Operation.Rshift,
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
                        var tr = constValues[triad.Arg1];
                        dynamic val;
                        switch (triad.Arg2)
                        {
                            case SemType.Int:
                                val = (int)tr.Value;
                                break;
                            case SemType.LongLongInt:
                                val = (long)tr.Value;
                                break;
                            case SemType.Char:
                                val = (char)tr.Value;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(tr.Value));
                        }
                        constValues[TriadResult.Of(i, triad.Arg2)] = ConstResult.Of(val);
                        triad.Operation = Operation.Nop;
                        triad.Arg1 = triad.Arg2 = null;
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

                    if (triad.Arg1 is ConstResult cr1)
                    {
                        if (triad.Arg2 == null || triad.Arg2 is ConstResult)
                        {
                            var val2 = triad.Arg2 is ConstResult cr2 ? cr2.Value : null;
                            switch (triad.Operation)
                            {
                                case Operation.Add:
                                    var val = ConstResult.Of(cr1.Value + val2);
                                    constValues[TriadResult.Of(i, val.Type)] = val;
                                    break;
                                case Operation.Sub:
                                    break;
                                case Operation.Mul:
                                    break;
                                case Operation.Div:
                                    break;
                                case Operation.Mod:
                                    break;
                                case Operation.And:
                                    break;
                                case Operation.Or:
                                    break;
                                case Operation.Xor:
                                    break;
                                case Operation.Lshift:
                                    break;
                                case Operation.Rshift:
                                    break;
                                case Operation.Not:
                                    break;
                                case Operation.Push:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(triad.Operation));
                            }
                            triad.Operation = Operation.Nop;
                            triad.Arg1 = triad.Arg2 = null;
                        }
                    }
                }
            }

            return code;
        }
    }
}