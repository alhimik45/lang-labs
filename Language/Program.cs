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
            try
            {
                a.Check();
            }
            catch (ParseException e)
            {
                Console.WriteLine($"Error analyzing program: {e.Message}");
                Environment.Exit(1);
            }
            catch (TokenException e)
            {
                Console.WriteLine($"Error parsing program: {e.Message}");
                Environment.Exit(1);
            }
            Console.WriteLine("Program is correct!");
        }
    }
}