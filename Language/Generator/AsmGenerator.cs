using System;
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
                       bssVars.Select(v => $"\t.lcomm {v.ReadableName}, {Ll1SyntaxAnalyzer.GetSize(v.Type)}")) +
                   $"\n\tTMP: .byte {memOffset}\n";
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

            VarInfo.globs = globVars;
            var main = GenMain();
            return Header + GenBss() + GenData() + main + Footer;
        }

        private int memOffset;
        private HashSet<Register> registers = new HashSet<Register>(Register.Registers);
        private HashSet<MemoryPtr> mems = new HashSet<MemoryPtr>();

        //varInfo, triadResult
        Dictionary<dynamic, IPlace> places = new Dictionary<dynamic, IPlace>();

        private string GenMain()
        {
            var s = "";
            var globs = new HashSet<VarInfo>();
            var inProc = false;
            for (var i = 0; i < ir.Count; i++)
            {
                var triad = ir[i];
                if (triad.Operation == Operation.Proc)
                {
                    inProc = true;
                }

                switch (triad.Operation)
                {
                    case Operation.GlobVar:
                        globs.Add(triad.Arg1.Var);
                        break;
                    case Operation.Assign:
                        if (!inProc)
                        {
                            break;
                        }

                        switch (triad.Arg2)
                        {
                            case ConstResult cr:
                                s += $"mov {triad.Arg1.Var.Ptr}, {cr.Str}\n";
                                break;
                            case TriadResult tr:
                                var place = places[tr];
                                Register r;
                                if (place is MemoryPtr mem)
                                {
                                    s += GetRegister(out var regg);
                                    r = regg;
                                    s += $"mov {r.B64}, {mem}\n";
                                }
                                else
                                {
                                    r = (Register) place;
                                }

                                s += $"mov {triad.Arg1.Var.Ptr}, {r.OfType(triad.Arg1.Type)}\n";
                                break;
                            case VariableResult vr:
                                s += GetRegister(out var rreg);
                                s += $"{rreg.MovType(vr.Type)}, {vr.Var.Ptr}\n";
                                s += $"mov {triad.Arg1.Var.Ptr}, {rreg.OfType(triad.Arg1.Type)}\n";
                                break;
                        }

                        break;
                    case Operation.Add:
                        s += OpGen(triad, i, "add");
                        break;
                    case Operation.Sub:
                        s += OpGen(triad, i, "sub");
                        break;
                    case Operation.Cast:
                        s += GetRegister(out var reg);
                        s += $"{reg.MovType(triad.Arg1.Type)}, {triad.Arg1.Var.Ptr}\n";
                        places[TriadResult.Of(i, triad.Arg2)] = reg;
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

        private string OpGen(Triad triad, int i, string op)
        {
            string s = "";
            dynamic a1 = triad.Arg1, a2 = triad.Arg2;
            string v1 = DetType(triad.Arg1);
            string v2 = DetType(triad.Arg2);
            var changed = false;

            string OpOrder(params string[] args) =>
                string.Join(", ", !changed || op != "sub" ? args : args.Reverse());

            var dict = new Dictionary<(string, string), Action>
            {
                [("const", "const")] = () =>
                {
                    s += GetRegister(out var reg);
                    s += $"mov {reg.B64}, {a1.Str}\n";
                    s += $"{op} {OpOrder(reg.OfType(a2.Type), a2.Str)}\n";
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                },
                [("treg", "const")] = () =>
                {
                    (a2, a1) = (a1, a2);
                    var reg = (Register) places[a2];
                    s += $"{op} {OpOrder(reg.OfType(a1.Type), a1.Str)}\n";
                    places.Remove(a2);
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                },
                [("tmem", "const")] = () =>
                {
                    (a2, a1) = (a1, a2);
                    s += GetRegister(out var reg);
                    var mem = (MemoryPtr) places[a2];
                    s += $"mov {reg.B64}, {a1.Str}\n";
                    s += $"{op} {OpOrder(reg.B64, mem.ToString())}\n";
                    places.Remove(a2);
                    mems.Add(mem);
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                },
                [("vmem", "const")] = () =>
                {
                    (a2, a1) = (a1, a2);
                    s += GetRegister(out var reg);
                    s += $"mov {reg.B64}, {a1.Str}\n";
                    s += $"{op} {OpOrder(reg.OfType(a2.Type), a2.Var.Ptr)}\n";
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                },
                [("treg", "treg")] = () =>
                {
                    var reg1 = (Register) places[a1];
                    var reg2 = (Register) places[a2];
                    s += $"{op} {OpOrder(reg1.B64, reg2.B64)}\n";
                    registers.Add(reg2);
                    places.Remove(a2);
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg1;
                },
                [("treg", "tmem")] = () =>
                {
                    var reg1 = (Register) places[a1];
                    var mem = (MemoryPtr) places[a2];
                    s += $"{op} {OpOrder(reg1.B64, mem.ToString())}\n";
                    mems.Add(mem);
                    places.Remove(a2);
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg1;
                },
                [("treg", "vmem")] = () =>
                {
                    var reg = (Register) places[a1];
                    s += $"{op} {OpOrder(reg.OfType(a2.Type), a2.Var.Ptr)}\n";
                    places.Remove(a1);
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                },
                [("tmem", "tmem")] = () =>
                {
                    s += GetRegister(out var reg);
                    var mem1 = (MemoryPtr) places[a1];
                    var mem2 = (MemoryPtr) places[a2];
                    s += $"{reg.MovType(mem1.Type)}, {mem1}\n";
                    s += $"{op} {OpOrder(reg.B64, mem2.ToString())}\n";
                    mems.Add(mem1);
                    mems.Add(mem2);
                    places.Remove(a1);
                    places.Remove(a2);
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                },
                [("tmem", "vmem")] = () =>
                {
                    s += GetRegister(out var reg);
                    var mem1 = (MemoryPtr) places[a1];
                    s += $"{reg.MovType(mem1.Type)}, {mem1}\n";
                    s += $"{op} {OpOrder(reg.OfType(a2.Type), a2.Var.Ptr)}\n";
                    mems.Add(mem1);
                    places.Remove(a1);
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                },
                [("vmem", "vmem")] = () =>
                {
                    s += GetRegister(out var reg);
                    s += $"{reg.MovType(a1.Type)}, {a1.Var.Ptr}\n";
                    s += $"{op} {OpOrder(reg.OfType(a2.Type), a2.Var.Ptr)}\n";
                    places[TriadResult.Of(i, (SemType) Math.Max((int) a1.Type, (int) a2.Type))] = reg;
                }
            };
            if (dict.ContainsKey((v1, v2)))
            {
                dict[(v1, v2)]();
            }
            else
            {
                changed = true;
                (a2, a1) = (a1, a2);
                dict[(v2, v1)]();
            }

            return s;
        }

        private string DetType(dynamic arg)
        {
            if (arg is ConstResult)
            {
                return "const";
            }

            if (!(arg is TriadResult trr))
            {
                return "vmem";
            }

            return places[trr] is Register ? "treg" : "tmem";
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
    }
}