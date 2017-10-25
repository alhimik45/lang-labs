using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using Language.Scan;

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
            Many(() => Description());
        }

        private void Description()
        {
            Or(() => Data(), () => Func());
        }

        private void Many(Action comb)
        {
            while (sc.HasNext)
            {
                comb();
            }
        }

        private void Data(Lexema l)
        {
            if (l.Type == LexType.TlongIntType)
            {
                NextMust(LexType.TlongIntType);
                NextMust(LexType.TintType);
            }
            NextMust(LexType.Tident);
        }

        private void NextMust(LexType type)
        {
            var l = sc.Next();
            if (l.Type != type)
            {
                PrintError($"Wrong element: {l.Tok}, {Enum.GetName(typeof(LexType), type)} expected");
            }
        }

        private void PrintError(string err)
        {
            Console.WriteLine(err + $" at {sc.Current.Line}:{sc.Current.Symbol}");
            Environment.Exit(1);
        }
    }
}