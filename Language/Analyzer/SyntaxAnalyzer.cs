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

        public SyntaxAnalyzer(Scanner sc)
        {
            this.sc = sc;
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
            Or(() => L(LexType.Tident),
                () => L(LexType.Tmain));
            Sure(() =>
            {
                L(LexType.Tlparen);
                Maybe(Params);
                L(LexType.Trparen);
                Block();
            });
        }

        private void Block()
        {
            L(LexType.Tlbracket);
            Maybe(() => Many(Statement));
            Sure(() => L(LexType.Trbracket));
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
            L(LexType.Tident);
            Sure(() =>
            {
                L(LexType.Tlparen);

                Maybe(() =>
                {
                    Expr();
                    Maybe(() => Many(() =>
                    {
                        L(LexType.Tcomma);
                        Sure(Expr);
                    }));
                });
                L(LexType.Trparen);
                L(LexType.Tdelim);
            });
        }

        private void For()
        {
            L(LexType.Tfor);
            L(LexType.Tlparen);
            Sure(() =>
            {
                Data();
                Expr();
                L(LexType.Tdelim);
                Expr();
                L(LexType.Trparen);
                Statement();
            });
        }

        private void Params()
        {
            Param();
            Maybe(() => Many(() =>
            {
                L(LexType.Tcomma);
                Param();
            }));
        }

        private void Param()
        {
            Or(() => L(LexType.TintType),
                () =>
                {
                    L(LexType.TlongIntType);
                    L(LexType.TlongIntType);
                    L(LexType.TintType);
                },
                () => L(LexType.TcharType));
            L(LexType.Tident);
        }

        private void Data()
        {
            Or(() => L(LexType.TintType),
                () =>
                {
                    L(LexType.TlongIntType);
                    L(LexType.TlongIntType);
                    L(LexType.TintType);
                },
                () => L(LexType.TcharType));
            Def();
            Maybe(() => Many(() =>
            {
                L(LexType.Tcomma);
                Def();
            }));
            L(LexType.Tdelim);
        }

        private void Def()
        {
            L(LexType.Tident);
            Maybe(() =>
            {
                L(LexType.Teq);
                Expr();
            });
        }

        private void ExprPart(Action next, params LexType[] tokens)
        {
            next();
            Maybe(() => Many(() =>
            {
                Or(tokens.Select<LexType, Action>(t => () => L(t)).ToArray());
                next();
            }));
        }

        private void Expr()
        {
            Or(() =>
                {
                    L(LexType.Tident);
                    Maybe(() => Many(() =>
                    {
                        L(LexType.Teq);
                        Sure(A2);
                    }));
                },
                A2);
        }

        private void A2()
        {
            ExprPart(A3, LexType.Tor);
        }

        private void A3()
        {
            ExprPart(A4, LexType.Txor);
        }

        private void A4()
        {
            ExprPart(A5, LexType.Tand);
        }

        private void A5()
        {
            ExprPart(A6, LexType.Tlshift, LexType.Trshift);
        }

        private void A6()
        {
            ExprPart(A7, LexType.Tplus, LexType.Tminus);
        }

        private void A7()
        {
            ExprPart(A8, LexType.Tmul, LexType.Tdiv, LexType.Tmod);
        }

        private void A8()
        {
            Maybe(() => Many(() => L(LexType.Tnot)));
            A9();
        }

        private void A9()
        {
            Or(
                () => L(LexType.Tident),
                () =>
                {
                    L(LexType.Tlparen);
                    Expr();
                    L(LexType.Trparen);
                },
                () => L(LexType.Tintd),
                () => L(LexType.Tinth),
                () => L(LexType.Tinto),
                () => L(LexType.Tchar)
            );
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
        private void L(LexType type)
        {
            var l = sc.Next();
            if (l.Type != type)
            {
                throw new ParseException(l, type);
            }
        }

        private void Maybe(Action comb)
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
    }
}