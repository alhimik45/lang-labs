using System;
using System.IO;
using Language.Analyzer;
using Language.Compiler;
using Language.Generator;
using Language.Scan;

namespace Language
{
    class Program
    {
        static void Main(string[] args)
        {
            var content = File.ReadAllText(args[0]);
            var sc = new Scanner(content);
            var a = new Ll1SyntaxAnalyzer(sc);
            try
            {
                a.Check();
            }
            catch (ParseException e)
            {
                Console.WriteLine($"Error analyzing program: {e.Message}");
                Environment.Exit(1);
            }
            catch (SureParseException e)
            {
                Console.WriteLine($"Error analyzing program: {e.InnerException.Message}");
                Environment.Exit(1);
            }
            catch (TokenException e)
            {
                Console.WriteLine($"Error parsing program: {e.Message}");
                Environment.Exit(1);
            }
            catch (SemanticException e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }

            Console.WriteLine("Program is correct!");
            File.WriteAllText("code.txt", a.Ir.ToListing());
            Console.WriteLine("Code writed!");
            File.WriteAllText("asm.txt", new AsmGenerator(a.Ir).Generate());
            Console.WriteLine("Asm generated!");
//            File.WriteAllText("optimized.txt", new Optimizer(a.Ir).Optimize().ToListing());
//            Console.WriteLine("Optimized code writed!");
        }
    }
}