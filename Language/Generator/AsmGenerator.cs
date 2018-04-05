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
        private readonly HashSet<VarInfo> globVars = new HashSet<VarInfo>();
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
                        globVars.Add(ir[i + 1].Arg1.Var);
                        dataVars.Add((ir[i + 1].Arg1.Var, ir[i + 1].Arg2));
                    }
                    else
                    {
                        globVars.Add(triad.Arg1.Var);
                        bssVars.Add(triad.Arg1.Var);
                    }
                }
            }

            return Header + GenBss() + GenData() + GenMain() + Footer;
        }

        private int memOffset = 0;
        private HashSet<Register> registers = new HashSet<Register>(Register.Registers);
        private HashSet<MemoryPtr> mems = new HashSet<MemoryPtr>();

        //varInfo, triadResult
        Dictionary<dynamic, IPlace> places = new Dictionary<dynamic, IPlace>();

        private string GenMain()
        {
            var s = "";
            var globs = new HashSet<VarInfo>();
            for (var i = 0; i < ir.Count; i++)
            {
                var triad = ir[i];
                switch (triad.Operation)
                {
                    case Operation.GlobVar:
                        globs.Add(triad.Arg1.Var);
                        break;
                    case Operation.Assign:
                        if(globVars.Contains(triad.Arg1.Var))
                            break;
                        switch (triad.Arg2)
                        {
                            case ConstResult cr:
                                System.Diagnostics.Debugger.Break();
                                s += $"mov {triad.Arg1.Var.Ptr}, " + (cr.Value is char ? (int) cr.Value: cr.Value) + "\n";
                                break;
                        }
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

        private string GetRegister(out Register reg)
        {
            if (registers.Any())
            {
                reg = registers.First();
                registers.Remove(reg);
            }
            else
            {
                var regKey = FindRegister();
                reg = places[regKey];
                GetMem(out var mem);
                places[regKey] = mem;
                return $"mov {mem}, {reg.B64}\n";
            }

            return "";
        }

        private dynamic FindRegister()
        {
            foreach (var kv in places)
            {
                if (kv.Value is Register)
                {
                    return kv.Key;
                }
            }

            return null;
        }

        private void GetMem(out MemoryPtr mem)
        {
            if (mems.Any())
            {
                mem = mems.First();
                mems.Remove(mem);
            }
            else
            {
                mem = new MemoryPtr {Offset = memOffset};
                memOffset += 8;
            }
        }

        private void GetPlace(out IPlace place)
        {
            if (registers.Any())
            {
                GetRegister(out var reg);
                place = reg;
            }
            else
            {
                GetMem(out var mem);
                place = mem;
            }
        }
    }
}