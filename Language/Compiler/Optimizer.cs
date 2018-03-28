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
            Operation.Jz,
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

        private List<List<(Triad, int)>> SplitLinear()
        {
            var res = new List<List<(Triad, int)>>();
            var ll = new List<(Triad, int)>();
            var dests = ir.Where(tr => tr.Operation == Operation.Jmp)
                .Select(tr => (int)tr.Arg1)
                .Union(ir.Where(tr => tr.Operation == Operation.Jz).Select(tr => (int)tr.Arg2));
            for (var i = 0; i < ir.Count; i++)
            {
                var triad = ir[i];
                ll.Add((triad, i));
                if (splitOps.Contains(triad.Operation) || dests.Contains(i + 1))
                {
                    res.Add(ll);
                    ll = new List<(Triad, int)>();
                }
            }

            res.Add(ll);
            return res;
        }

        private List<Triad> ConstSubstitution(List<(Triad, int)> code)
        {
            var constValues = new Dictionary<IResult, ConstResult>();
            var res = new List<Triad>();
            var halfOk = new Dictionary<int,int>();
            for (var i = 0; i < code.Count; i++)
            {
                var triad = code[i].Item1;
                if (triad.Operation == Operation.Assign)
                {
                    if (constValues.ContainsKey(triad.Arg2))
                    {
                        triad.Arg2 = constValues[triad.Arg2];
                    }

                    if (triad.Arg2 is ConstResult)
                    {
                        constValues[triad.Arg1] = triad.Arg2;
                    }
                }

                if (triad.Operation == Operation.Cast)
                {
                    var tr = constValues.TryGetValue(triad.Arg1, out ConstResult r) ? r :
                        triad.Arg1 is ConstResult cr ? cr : null;
                    if (tr != null)
                    {
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

                        constValues[TriadResult.Of(code[i].Item2, triad.Arg2)] = ConstResult.Of(val);
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

                    if (triad.Arg2 != null && triad.Arg2 is IResult && constValues.ContainsKey(triad.Arg2))
                    {
                        triad.Arg2 = constValues[triad.Arg2];
                    }

                    if (triad.Arg1 is ConstResult cr1)
                    {
                        if (triad.Arg2 == null || triad.Arg2 is ConstResult)
                        {
                            var val = EvalConst(triad.Operation, cr1, triad.Arg2 is ConstResult cr3 ? cr3.Value : null);

                            if (val != null)
                            {
                                constValues[TriadResult.Of(code[i].Item2, val.Type)] = val;
                                triad.Operation = Operation.Nop;
                                triad.Arg1 = triad.Arg2 = null;
                            }
                        }
                    }

                    if (triad.Arg1 is ConstResult || triad.Arg2 is ConstResult)
                        halfOk[code[i].Item2] = i;

                    if (triad.Arg1 is TriadResult tr &&
                        triad.Arg2 is ConstResult crr1 &&
                        halfOk.ContainsKey(tr.Index) &&
                        triad.Operation == res[halfOk[tr.Index]].Operation)
                    {
                        var tr2 = res[halfOk[tr.Index]];
                        if (tr2.Arg1 is ConstResult crr2)
                        {
                            triad.Arg1 = tr2.Arg2;
                            triad.Arg2 = ConstResult.Of(EvalConst(triad.Operation, crr1, crr2.Value));
                            tr2.Operation = Operation.Nop;
                            tr2.Arg1 = tr2.Arg2 = null;
                        }
                        if (tr2.Arg2 is ConstResult crr3)
                        {
                            triad.Arg1 = tr2.Arg1;
                            triad.Arg2 = ConstResult.Of(EvalConst(triad.Operation, crr1, crr3.Value));
                            tr2.Operation = Operation.Nop;
                            tr2.Arg1 = tr2.Arg2 = null;
                        }
                    }
                    if (triad.Arg2 is TriadResult trr &&
                        triad.Arg1 is ConstResult crr4 &&
                        halfOk.ContainsKey(trr.Index) &&
                        triad.Operation == res[halfOk[trr.Index]].Operation)
                    {
                        var tr2 = res[halfOk[trr.Index]];
                        if (tr2.Arg1 is ConstResult crr2)
                        {
                            triad.Arg2 = tr2.Arg2;
                            triad.Arg1 = ConstResult.Of(EvalConst(triad.Operation, crr4, crr2.Value));
                            tr2.Operation = Operation.Nop;
                            tr2.Arg1 = tr2.Arg2 = null;
                        }
                        if (tr2.Arg2 is ConstResult crr3)
                        {
                            triad.Arg2 = tr2.Arg1;
                            triad.Arg1 = ConstResult.Of(EvalConst(triad.Operation, crr4, crr3.Value));
                            tr2.Operation = Operation.Nop;
                            tr2.Arg1 = tr2.Arg2 = null;
                        }
                    }
                }
                res.Add(triad);
            }

            return res;
        }

        private static dynamic EvalConst(Operation op, ConstResult cr1, dynamic v2)
        {
            ConstResult val = null;
            switch (op)
            {
                case Operation.Add:
                    val = ConstResult.Of(cr1.Value + v2);
                    break;
                case Operation.Sub:
                    val = ConstResult.Of(cr1.Value - v2);
                    break;
                case Operation.Mul:
                    val = ConstResult.Of(cr1.Value*v2);
                    break;
                case Operation.Div:
                    val = ConstResult.Of(cr1.Value/v2);
                    break;
                case Operation.Mod:
                    val = ConstResult.Of(cr1.Value%v2);
                    break;
                case Operation.And:
                    val = ConstResult.Of(cr1.Value & v2);
                    break;
                case Operation.Or:
                    val = ConstResult.Of(cr1.Value | v2);
                    break;
                case Operation.Xor:
                    val = ConstResult.Of(cr1.Value ^ v2);
                    break;
                case Operation.Lshift:
                    val = ConstResult.Of(cr1.Value << v2);
                    break;
                case Operation.Rshift:
                    val = ConstResult.Of(cr1.Value >> v2);
                    break;
                case Operation.Not:
                    val = ConstResult.Of(~cr1.Value);
                    break;
                case Operation.Push:
                case Operation.Jz:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op));
            }
            return val;
        }
    }
}