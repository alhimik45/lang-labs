using System;
using System.IO;
using Language.Analyzer;
using Language.Scan;

namespace Language
{
    class Program
    {
        static void Main(string[] args)
        {
            var content = File.ReadAllText(args[0]);
            var sc = new Scanner(content);
            var a = new SyntaxAnalyzer(sc);
            a.Check();
        }
    }
}