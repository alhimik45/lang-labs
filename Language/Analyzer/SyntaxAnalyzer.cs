using System;
using System.Collections.Generic;
using Language.Scan;
using MoreLinq;

namespace Language.Analyzer
{
    public class SyntaxAnalyzer
    {
        private readonly Scanner sc;

        public SyntaxAnalyzer(Scanner sc)
        {
            this.sc = sc;
        }

        public void Check()
        {
            Many(Description);
        }

        private void Description()
        {
            Or(Data, Function);
        }

        private void Function()
        {
            L(LexType.Tchar);
        }

        private void Data()
        {
            Or(() => L(LexType.TintType),
                () => Seq(
                    () => L(LexType.TlongIntType),
                    () => L(LexType.TlongIntType),
                    () => L(LexType.TintType)),
                () => L(LexType.TcharType));
            Def();
            Maybe(() => Many(() => Seq(() => L(LexType.Tcomma), Def)));
            L(LexType.Tdelim);
        }

        private void Def()
        {
            Seq(() => L(LexType.Tident),
                () => Maybe(() => Seq(() => L(LexType.Teq), Expr)));
        }

        private void Expr()
        {
            L(LexType.Tand);
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
                    //  Console.WriteLine("or   " + e.Message);
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
                // Console.WriteLine("may  " + e.Message);
                sc.PopState();
            }
        }

        private void Seq(params Action[] combinators)
        {
            combinators.ForEach(comb => comb());
        }
    }
}