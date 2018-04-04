using System.Collections.Generic;
using System.Linq;
using Language.Analyzer;
using Language.Compiler;

namespace Language.Generator
{
    public class AsmGenerator
    {
        private const string Header =
            @".extern printf 			# экспорт функции печати
.intel_syntax noprefix	# используем синтаксис интел вместо AT&T
";

        private const string Footer = @"
mov rdi, offset TOINTFMT
mov rsi, v_1_r_0_
mov rax,0
call printf
mov rax, 60
mov rdi, 0
syscall
";

        private readonly List<Triad> ir;

        private readonly List<VarInfo> bssVars;
        private readonly List<(VarInfo var, ConstResult val)> dataVars;

        public AsmGenerator(List<Triad> ir)
        {
            this.ir = ir;
            bssVars = new List<VarInfo>();
            dataVars = new List<(VarInfo, ConstResult)>();
        }

        public string GenBss()
        {
            return ".bss\n" + string.Join("\n",
                       bssVars.Select(v => $"\t.lcomm {v.ReadableName}, {Ll1SyntaxAnalyzer.GetSize(v.Type)}")) + "\n";
        }

        public string GenData()
        {
            return ".data\n\tTOINTFMT:  .asciz \"Вывод: %d\\n\"\n" + string.Join("\n",
                       dataVars.Select(v =>
                           $"\t{v.var.ReadableName}: {new Dictionary<SemType, string> {[SemType.Char] = ".byte", [SemType.Int] = ".int", [SemType.LongLongInt] = ".long"}[v.var.Type]} {v.val.Cast(v.var.Type)}")) +
                   @"
.text
    .global _start
_start:
";
        }

        public string Generate()
        {
            for (var i = 0; i < ir.Count; i++)
            {
                var triad = ir[i];
                if (triad.Operation == Operation.GlobVar)
                {
                    if (i + 1 < ir.Count && ir[i + 1].Operation == Operation.Assign)
                    {
                        dataVars.Add((ir[i + 1].Arg1.Var, ir[i + 1].Arg2));
                    }
                    else
                    {
                        bssVars.Add(triad.Arg1.Var);
                    }
                }
            }

            return Header + GenBss() + GenData() + GenMain() + Footer;
        }

        private string GenMain()
        {
            var s = "";
            var globs = new HashSet<VarInfo>();
            var places = new Dictionary<VarInfo, string>();
            for (var i = 0; i < ir.Count; i++)
            {
                var triad = ir[i];
                switch (triad.Operation)
                {
                    case Operation.GlobVar:
                        globs.Add(triad.Arg1.Var);
                        break;
                    case Operation.Assign:
                        break;
                    case Operation.Add:
                        break;
                    case Operation.Sub:
                        break;
                    case Operation.Cast:
                        break;
                    case Operation.Alloc:
                        s += $@"push rbp
mov rbp, rsp
sub rsp, {triad.Arg1}
";
                        break;
                    case Operation.Free:
                        s += $@"add rsp, {triad.Arg1}
pop rbp
";
                        break;
                }
            }

            return s;
        }
    }
}