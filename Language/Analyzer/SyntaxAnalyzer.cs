using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Language.Scan;
using MoreLinq;

namespace Language.Analyzer
{
    public class SyntaxAnalyzer
    {
        private readonly Scanner sc;
        private ParseException lastEx;
        private readonly List<Dictionary<string, VarInfo>> environment;
        private readonly List<string> envowners;
        private string currentFunction;

        private Dictionary<LexType, Func<object, object, object>> binops;

        private bool interpret = true;
        private bool ret = false;

        private static long GetLong(object val)
        {
            return val is int i ? i :
                val is char c ? c : (long) val;
        }

        public SyntaxAnalyzer(Scanner sc)
        {
            this.sc = sc;
            environment = new List<Dictionary<string, VarInfo>>
            {
                new Dictionary<string, VarInfo>
                {
                    ["print"] = new VarInfo(SemType.Function, null, "print")
                }
            };
            currentFunction = null;
            envowners = new List<string> {currentFunction};
            binops =
                new Dictionary<LexType, Func<object, object, object>>
                {
                    [LexType.Tand] = (a, b) => GetLong(a) & GetLong(b),
                    [LexType.Tor] = (a, b) => GetLong(a) | GetLong(b),
                    [LexType.Txor] = (a, b) => GetLong(a) ^ GetLong(b),
                    [LexType.Tdiv] = (a, b) =>
                    {
                        if (GetLong(b) == 0)
                        {
                            throw new SemanticException("Divizion by zero", sc.Current);
                        }

                        return GetLong(a) / GetLong(b);
                    },
                    [LexType.Tmul] = (a, b) => GetLong(a) * GetLong(b),
                    [LexType.Tmod] = (a, b) => GetLong(a) % GetLong(b),
                    [LexType.Tminus] = (a, b) => GetLong(a) - GetLong(b),
                    [LexType.Tlshift] = (a, b) => GetLong(a) << (int) GetLong(b),
                    [LexType.Trshift] = (a, b) => GetLong(a) >> (int) GetLong(b),
                    [LexType.Tplus] = (a, b) => GetLong(a) + GetLong(b),
                    [LexType.Tless] = (a, b) => GetLong(a) < GetLong(b) ? 1 : 0,
                    [LexType.Tlesseq] = (a, b) => GetLong(a) <= GetLong(b) ? 1 : 0
                };
            NewEnv();
        }

        public void Check()
        {
            Many(Description);
            if (sc.HasNext)
            {
                if (lastEx != null)
                {
                    throw lastEx;
                }

                throw new ParseException(sc.Next(), LexType.Tend);
            }
        }

        private void Description()
        {
            Or(Data, Function);
        }

        private void Function()
        {
            L(LexType.TvoidType);
            Lexema nameL = null;
            Or(() => { nameL = L(LexType.Tident); },
                () => { nameL = L(LexType.Tmain); });
            var fn = AddVar(nameL, SemType.Function);
            environment.Last()[nameL.Tok].Name = nameL.Tok;
            Sure(() =>
            {
                var prevFn = currentFunction;
                currentFunction = nameL.Tok;
                using (NewEnv())
                {
                    L(LexType.Tlparen);
                    Maybe(() => Params(fn));
                    L(LexType.Trparen);
                    fn.SetPos(sc.Pos);
                    interpret = nameL.Type == LexType.Tmain;
                    Block();
                    interpret = true;
                }

                currentFunction = prevFn;
            });
        }

        private void Block()
        {
            using (NewEnv())
            {
                L(LexType.Tlbracket);
                Maybe(() => Many(Statement));
                Sure(() => L(LexType.Trbracket));
            }
        }

        private void Statement()
        {
            Or(
                () =>
                {
                    Expr();
                    L(LexType.Tdelim);
                },
                For,
                Funcall,
                () => L(LexType.Tdelim),
                () =>
                {
                    L(LexType.Treturn);
                    if (interpret)
                    {
                        ret = true;
                        interpret = false;
                    }
                },
                Block,
                Data
            );
        }

        private void Funcall()
        {
            var l = L(LexType.Tident);
            var fn = FindVar(l);
            Sure(() =>
            {
                L(LexType.Tlparen);
                if (fn.Type != SemType.Function)
                {
                    throw new SemanticException("Cannot call non-function", l);
                }

                var count = 0;
                var pars = new List<object>();
                Maybe(() =>
                {
                    var ll = GetNextLex();
                    var (eType, val) = Expr();
                    pars.Add(val);
                    ++count;
                    if (fn.Params?.Count < count)
                    {
                        throw new SemanticException("Extra function parameter", ll);
                    }

                    Mismatch(fn.Params?[count - 1].type, eType, ll);
                    Maybe(() => Many(() =>
                    {
                        L(LexType.Tcomma);
                        Sure(() =>
                        {
                            var lll = GetNextLex();
                            var (eeType, v) = Expr();
                            pars.Add(v);
                            ++count;
                            if (fn.Params?.Count < count)
                            {
                                throw new SemanticException("Extra function parameter", ll);
                            }

                            Mismatch(fn.Params?[count - 1].type, eeType, lll);
                        });
                    }));
                }, () => count = 0);
                var llll = L(LexType.Trparen);
                if (fn.Params != null && fn.Params.Count != count)
                {
                    throw new SemanticException("Wrong number of function parameters", llll);
                }

                L(LexType.Tdelim);
                Call(fn, pars);
            });
        }

        private void For()
        {
            using (NewEnv())
            {
                L(LexType.Tfor);
                var posU = -1;
                var posC = -1;
                int posS;
                var lflInt = false;
                var cond = interpret;
                Sure(() =>
                {
                    L(LexType.Tlparen);
                    Maybe(() =>
                    {
                        Maybe(Data);
                        Maybe(() => L(LexType.Tdelim));
                        posU = sc.Pos;
                        Sure(() =>
                        {
                            Maybe(() =>
                            {
                                var ll = GetNextLex();
                                var (eType, val) = Expr();
                                cond = false;
                                if (eType != SemType.LongLongInt && eType != SemType.Int)
                                {
                                    throw new SemanticException("Invalid condition type", ll);
                                }

                                if (interpret)
                                {
                                    cond = GetLong(val) != 0;
                                }
                            });
                            L(LexType.Tdelim);
                            posC = sc.Pos;
                            lflInt = interpret;
                            interpret = false;
                            Maybe(() => Expr());
                        });
                    });
                    L(LexType.Trparen);
                    posS = sc.Pos;
                    if (lflInt)
                    {
                        interpret = cond;
                    }

                    Statement();
                    while (!ret && cond)
                    {
                        sc.Pos = posC;
                        Maybe(() => Expr());
                        sc.Pos = posU;
                        Maybe(() =>
                        {
                            var (_, val) = Expr();
                            if (interpret)
                            {
                                cond = GetLong(val) != 0;
                            }
                        });
                        interpret = cond;
                        sc.Pos = posS;
                        Statement();
                    }

                    interpret = lflInt;
                    if (ret)
                    {
                        interpret = false;
                    }
                });
            }
        }

        private void Params(VarInfo fn)
        {
            Param(fn);
            Maybe(() => Many(() =>
            {
                L(LexType.Tcomma);
                Param(fn);
            }));
        }

        private void Param(VarInfo fn)
        {
            var type = Type();
            var id = L(LexType.Tident);
            AddVar(id, type);
            fn.AddParam(id.Tok, type);
        }

        private void Data()
        {
            var type = Type();
            Def(type);
            Maybe(() => Many(() =>
            {
                L(LexType.Tcomma);
                Def(type);
            }));
            L(LexType.Tdelim);
        }

        private SemType Type()
        {
            var type = default(SemType);
            Or(() =>
                {
                    L(LexType.TintType);
                    type = SemType.Int;
                },
                () =>
                {
                    L(LexType.TlongIntType);
                    Sure(() =>
                    {
                        L(LexType.TlongIntType);
                        L(LexType.TintType);
                    });
                    type = SemType.LongLongInt;
                },
                () =>
                {
                    L(LexType.TcharType);
                    type = SemType.Char;
                });
            return type;
        }

        private void Def(SemType type)
        {
            Sure(() =>
            {
                var id = L(LexType.Tident);
                var var = AddVar(id, type);
                Maybe(() =>
                {
                    L(LexType.Teq);
                    var l = GetNextLex();
                    var (eType, val) = Expr();
                    Mismatch(type, eType, l);
                    SetVar(var, val);
                });
            });
        }

        private (SemType, object) ExprPart(Func<(SemType, object)> next, params LexType[] tokens)
        {
            var (p1Type, v1) = next();
            Maybe(() => Many(() =>
            {
                Lexema lex = null;
                Or(tokens.Select<LexType, Action>(t => () => lex = L(t)).ToArray());
                var l = sc.Current;
                var (p2Type, v2) = next();
                Mismatch(p1Type, p2Type, l);
                if (interpret)
                {
                    v1 = binops[lex.Type](v1, v2);
                }
            }));
            return (p1Type, v1);
        }

        private (SemType, object) Expr()
        {
            var type = default(SemType);
            object val = null;
            Or(() =>
                {
                    var id = L(LexType.Tident);
                    var var = FindVar(id);
                    type = var.Type;
                    L(LexType.Teq);
                    var l = GetNextLex();
                    Sure(() =>
                    {
                        var (eType, v) = Expr();
                        val = v;
                        Mismatch(type, eType, l);
                        SetVar(var, val);
                    });
                },
                () =>
                {
                    var (eType, v) = A2();
                    type = eType;
                    val = v;
                });
            return (type, val);
        }

        private Lexema GetNextLex()
        {
            sc.PushState();
            var l = sc.Next();
            sc.PopState();
            return l;
        }

        private static void Mismatch(SemType? type, SemType eType, Lexema l)
        {
            if (type == null) return;
            var t = type == SemType.LongLongInt ? SemType.Int : type;
            var tt = eType == SemType.LongLongInt ? SemType.Int : eType;
//            if (t != tt)
//            {
//                throw new SemanticException("Expression type mismatch", l,
//                    $"cannot assign {eType.ToStr()} to {type.ToStr()}");
//            }
        }

        private (SemType, object) A2()
        {
            return ExprPart(A3, LexType.Tor);
        }

        private (SemType, object) A3()
        {
            return ExprPart(A4, LexType.Txor);
        }

        private (SemType, object) A4()
        {
            return ExprPart(Yoy, LexType.Tand);
        }

        private (SemType, object) Yoy()
        {
            return ExprPart(A5, LexType.Tless, LexType.Tlesseq);
        }

        private (SemType, object) A5()
        {
            return ExprPart(A6, LexType.Tlshift, LexType.Trshift);
        }

        private (SemType, object) A6()
        {
            return ExprPart(A7, LexType.Tplus, LexType.Tminus);
        }

        private (SemType, object) A7()
        {
            return ExprPart(A8, LexType.Tmul, LexType.Tdiv, LexType.Tmod);
        }

        private (SemType, object) A8()
        {
            var count = 0;
            Maybe(() => Many(() =>
            {
                L(LexType.Tnot);
                ++count;
            }));
            var (t, v) = A9();
            if (count % 2 == 1)
            {
                v = ~GetLong(v);
            }

            return (t, v);
        }

        private (SemType, object) A9()
        {
            var type = default(SemType);
            object val = null;
            Or(
                () =>
                {
                    var id = L(LexType.Tident);
                    var var = FindVar(id);
                    type = var.Type;
                    val = var.Value;
                },
                () =>
                {
                    L(LexType.Tlparen);
                    var (eType, v) = Expr();
                    type = eType;
                    val = v;
                    L(LexType.Trparen);
                },
                () =>
                {
                    var l = L(LexType.Tintd);
                    type = SemType.Int;
                    val = int.Parse(l.Tok);
                },
                () =>
                {
                    var l = L(LexType.Tinth);
                    type = SemType.Int;
                    val = int.Parse(l.Tok.Substring(2), NumberStyles.HexNumber);
                },
                () =>
                {
                    var l = L(LexType.Tinto);
                    type = SemType.Int;
                    val = Convert.ToInt64(l.Tok, 8);
                },
                () =>
                {
                    var l = L(LexType.Tchar);
                    type = SemType.Char;
                    val = l.Tok[1];
                });
            return (type, val);
        }

        private void Many(Action comb)
        {
            comb();
            try
            {
                while (sc.HasNext)
                {
                    sc.PushState();
                    comb();
                    sc.DropState();
                }
            }
            catch (ParseException e)
            {
                lastEx = e;
                //Console.WriteLine("many " + e.Message);
                sc.PopState();
            }
        }

        private void Or(params Action[] combinators)
        {
            var errors = new List<ParseException>();
            foreach (var comb in combinators)
            {
                try
                {
                    sc.PushState();
                    comb();
                    sc.DropState();
                    return;
                }
                catch (ParseException e)
                {
                    sc.PopState();
                    errors.Add(e);
                }
            }

            throw errors.MaxBy(e => e.Lexema);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private Lexema L(LexType type)
        {
            var l = sc.Next();
            if (l.Type != type)
            {
                throw new ParseException(l, type);
            }

            return l;
        }

        private void Maybe(Action comb, Action failedCb = null)
        {
            try
            {
                sc.PushState();
                comb();
                sc.DropState();
            }
            catch (ParseException e)
            {
                lastEx = e;
                // Console.WriteLine("may  " + e.Message);
                sc.PopState();
                failedCb?.Invoke();
            }
        }

        private void Sure(Action comb)
        {
            try
            {
                comb();
            }
            catch (ParseException e)
            {
                throw new SureParseException(e);
            }
        }

        private Env NewEnv()
        {
            environment.Add(new Dictionary<string, VarInfo>());
            envowners.Add(currentFunction);
            return new Env(environment, envowners);
        }

        private VarInfo AddVar(Lexema var, SemType type)
        {
            var name = var.Tok;
            var currentFrame = environment.Last();
            if (currentFrame.ContainsKey(name))
            {
                var prev = currentFrame[name];
                throw new SemanticException($"Cannot redefine variable: `{name}`", var,
                    $"previous declaration at {prev.Location.Line}:{prev.Location.Symbol}");
            }

            currentFrame[name] = VarInfo.Of(type, var);
            currentFrame[name].Name = name;
            return currentFrame[name];
        }

        private void SetVar(VarInfo varInfo, object value)
        {
            if (!interpret)
            {
                return;
            }

            Cast(varInfo.Type, ref value);
            Console.WriteLine($"{varInfo.Name} = {VarValue(value)}");
            varInfo.Value = value;
        }

        private void Cast(SemType type, ref object value)
        {
            var v = GetLong(value);
            switch (type)
            {
                case SemType.Int:
                    value = (int) v;
                    break;
                case SemType.LongLongInt:
                    value = v;
                    break;
                case SemType.Char:
                    if (v < 0 || v > 65535)
                    {
                        throw new SemanticException($"Wrong char code: {v}", sc.Current);
                    }

                    value = (char) v;
                    break;
            }
        }

        private VarInfo TryFindVar(string name)
        {
            var origFrame = environment.Zip(envowners, (env, owner) => new {env, owner}).Reverse()
                .FirstOrDefault(frame =>
                    frame.env.ContainsKey(name) && (frame.owner == null || frame.owner == currentFunction));

            return origFrame?.env?[name];
        }

        private VarInfo TryFindVar(Lexema lex)
        {
            return TryFindVar(lex.Tok);
        }

        private VarInfo FindVar(Lexema lex)
        {
            var res = TryFindVar(lex);
            if (res == null)
            {
                throw new SemanticException($"Undefined variable: {lex.Tok}", lex);
            }

            return res;
        }

        private void Call(VarInfo fn, List<object> pars)
        {
            if (!interpret)
            {
                return;
            }

            if (fn.Name == "print")
            {
                Console.WriteLine(string.Join("", pars.Select(VarValue)));
                return;
            }

            var prevPos = sc.Pos;
            sc.Pos = fn.Pos;
            var prevFn = currentFunction;
            currentFunction = fn.Name;
            using (NewEnv())
            {
                var currentFrame = environment.Last();
                var i = 0;
                foreach (var param in fn.Params)
                {
                    var val = pars[i++];
                    currentFrame[param.name] = VarInfo.Of(param.type, null);
                    Cast(param.type, ref val);
                    currentFrame[param.name].Value = val;
                }

                Block();
                interpret = true;
                ret = false;
            }

            currentFunction = prevFn;
            sc.Pos = prevPos;
        }

        private static object VarValue(object o)
        {
            return o is char c ? $"'{c}'" : o;
        }
    }
}