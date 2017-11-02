using System;
using System.Collections.Generic;
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


        public SyntaxAnalyzer(Scanner sc)
        {
            this.sc = sc;
            environment = new List<Dictionary<string, VarInfo>>();
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
            Sure(() =>
            {
                using (NewEnv())
                {
                    L(LexType.Tlparen);
                    Maybe(() => Params(fn));
                    L(LexType.Trparen);
                    Block();
                }
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
                var count = 0;
                Maybe(() =>
                {
                    var ll = GetNextLex();
                    var eType = Expr();
                    ++count;
                    if (fn.Params.Count < count)
                    {
                        throw new SemanticException("Extra function parameter", ll);
                    }
                    Mismatch(fn.Params[count - 1], eType, ll);
                    Maybe(() => Many(() =>
                    {
                        L(LexType.Tcomma);
                        Sure(() =>
                        {
                            var lll = GetNextLex();
                            var eeType = Expr();
                            ++count;
                            if (fn.Params.Count < count)
                            {
                                throw new SemanticException("Extra function parameter", ll);
                            }
                            Mismatch(fn.Params[count - 1], eeType, lll);
                        });
                    }));
                }, () => count = 0);
                var llll = L(LexType.Trparen);
                if (fn.Params.Count != count)
                {
                    throw new SemanticException("Wrong number of function parameters", llll);
                }
                L(LexType.Tdelim);
            });
        }

        private void For()
        {
            using (NewEnv())
            {
                L(LexType.Tfor);
                Sure(() =>
                {
                    L(LexType.Tlparen);
                    Maybe(() =>
                    {
                        Data();
                        Sure(() =>
                        {
                            Maybe(() => Expr());
                            L(LexType.Tdelim);
                            Maybe(() => Expr());
                        });
                    });
                    L(LexType.Trparen);
                    Statement();
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
            fn.AddParam(type);
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
                AddVar(id, type);
                Maybe(() =>
                {
                    L(LexType.Teq);
                    var l = GetNextLex();
                    var eType = Expr();
                    Mismatch(type, eType, l);
                });
            });
        }

        private SemType ExprPart(Func<SemType> next, params LexType[] tokens)
        {
            var type = next();
            Maybe(() => Many(() =>
            {
                Or(tokens.Select<LexType, Action>(t => () => L(t)).ToArray());
                var l = sc.Current;
                var nextType = next();
                Mismatch(type, nextType, l);
            }));
            return type;
        }

        private SemType Expr()
        {
            var type = default(SemType);
            Or(() =>
                {
                    var var = L(LexType.Tident);
                    type = FindVar(var).Type;
                    Maybe(() => Many(() =>
                    {
                        L(LexType.Teq);
                        var l = GetNextLex();
                        Sure(() =>
                        {
                            var eType = A2();
                            Mismatch(type, eType, l);
                        });
                    }));
                },
                () => { type = A2(); });
            return type;
        }

        private Lexema GetNextLex()
        {
            sc.PushState();
            var l = sc.Next();
            sc.PopState();
            return l;
        }

        private static void Mismatch(SemType type, SemType eType, Lexema l)
        {
            var t = type == SemType.LongLongInt ? SemType.Int : type;
            var tt = eType == SemType.LongLongInt ? SemType.Int : eType;
            if (t != tt)
            {
                throw new SemanticException("Expression type mismatch", l,
                    $"cannot assign {eType.ToStr()} to {type.ToStr()}");
            }
        }

        private SemType A2()
        {
            return ExprPart(A3, LexType.Tor);
        }

        private SemType A3()
        {
            return ExprPart(A4, LexType.Txor);
        }

        private SemType A4()
        {
            return ExprPart(A5, LexType.Tand);
        }

        private SemType A5()
        {
            return ExprPart(A6, LexType.Tlshift, LexType.Trshift);
        }

        private SemType A6()
        {
            return ExprPart(A7, LexType.Tplus, LexType.Tminus);
        }

        private SemType A7()
        {
            return ExprPart(A8, LexType.Tmul, LexType.Tdiv, LexType.Tmod);
        }

        private SemType A8()
        {
            Maybe(() => Many(() => L(LexType.Tnot)));
            return A9();
        }

        private SemType A9()
        {
            var type = default(SemType);
            Or(
                () =>
                {
                    var id = L(LexType.Tident);
                    type = FindVar(id).Type;
                },
                () =>
                {
                    L(LexType.Tlparen);
                    type = Expr();
                    L(LexType.Trparen);
                },
                () =>
                {
                    L(LexType.Tintd);
                    type = SemType.Int;
                },
                () =>
                {
                    L(LexType.Tinth);
                    type = SemType.Int;
                },
                () =>
                {
                    L(LexType.Tinto);
                    type = SemType.Int;
                },
                () =>
                {
                    L(LexType.Tchar);
                    type = SemType.Char;
                });
            return type;
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
            return new Env(environment);
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
            return currentFrame[name] = VarInfo.Of(type, var);
        }

        private VarInfo TryFindVar(string name)
        {
            var origFrame = ((IEnumerable<Dictionary<string, VarInfo>>) environment)
                .Reverse().FirstOrDefault(frame => frame.ContainsKey(name));
            return origFrame?[name];
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
    }
}