using System;
using System.Collections.Generic;
using System.Linq;
using Language.Compiler;
using Language.Scan;

namespace Language.Analyzer
{
    public class Ll1SyntaxAnalyzer
    {
        private readonly Scanner sc;
        private readonly List<ITerm> magaz = new List<ITerm>();
        private int counter;
        private readonly List<string> scopes = new List<string> {""};
        private string Scope => string.Join('/', scopes);
        private readonly List<Dictionary<string, VarInfo>> environment = new List<Dictionary<string, VarInfo>>();
        private readonly Stack<Env> envs = new Stack<Env>();
        private readonly Stack<IResult> r = new Stack<IResult>();
        private readonly Stack<Lexema> ops = new Stack<Lexema>();
        private readonly Stack<List<Triad>> saves = new Stack<List<Triad>>();
        private readonly Stack<int> addresses = new Stack<int>();
        private readonly Stack<int> jumps = new Stack<int>();
        private Lexema lastId;
        private SemType ttype;
        private SemType lastType;
        private dynamic lastConst;

        private List<Triad> realIr;
        public List<Triad> Ir = new List<Triad>();

        private readonly Dictionary<Neterm, Dictionary<LexType, IEnumerable<ITerm>>> table = new
            Dictionary<Neterm, Dictionary<LexType, IEnumerable<ITerm>>>
            {
                ["program".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.TintType] = new ITerm[] {"data".Of(), LexType.Tdelim.Of(), "program".Of()},
                    [LexType.TcharType] = new ITerm[] {"data".Of(), LexType.Tdelim.Of(), "program".Of()},
                    [LexType.TlongIntType] = new ITerm[] {"data".Of(), LexType.Tdelim.Of(), "program".Of()},
                    [LexType.TvoidType] = new ITerm[]
                    {
                        LexType.TvoidType.Of(), LexType.Tident.Of(), NewFunc.Of(), Begin.Of(), LexType.Tlparen.Of(),
                        "params".Of(),
                        LexType.Trparen.Of(), GenFunctionProlog.Of(), "block".Of(), GenFunctionEpilog.Of(),
                        "program".Of()
                    },
                    [LexType.Trparen] = new ITerm[] { },
                    [LexType.Tend] = new ITerm[] { },
                },
                ["data".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.TintType] = new[] {"type".Of(), "defs".Of()},
                    [LexType.TcharType] = new[] {"type".Of(), "defs".Of()},
                    [LexType.TlongIntType] = new[] {"type".Of(), "defs".Of()},
                },
                ["type".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.TintType] = new ITerm[] {LexType.TintType.Of(), SaveType.Of()},
                    [LexType.TcharType] = new ITerm[] {LexType.TcharType.Of(), SaveType.Of()},
                    [LexType.TlongIntType] = new ITerm[]
                        {LexType.TlongIntType.Of(), LexType.TlongIntType.Of(), SaveType.Of(), LexType.TintType.Of()},
                },
                ["defs".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"def".Of(), "B".Of()},
                },
                ["B".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tcomma] = new ITerm[] {LexType.Tcomma.Of(), "defs".Of()},
                    [LexType.Tdelim] = new ITerm[] { },
                },
                ["def".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new ITerm[] {LexType.Tident.Of(), NewVar.Of(), "C".Of()},
                },
                ["C".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Teq] = new ITerm[] {LexType.Teq.Of(), "expr".Of(), GenAssign.Of()},
                    [LexType.Tcomma] = new ITerm[] { },
                    [LexType.Tdelim] = new ITerm[] { },
                },
                ["params".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.TintType] = new[] {"param".Of(), "D".Of()},
                    [LexType.TcharType] = new[] {"param".Of(), "D".Of()},
                    [LexType.TlongIntType] = new[] {"param".Of(), "D".Of()},
                    [LexType.Trparen] = new ITerm[] { },
                },

                ["D".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tcomma] = new ITerm[] {LexType.Tcomma.Of(), "params".Of()},
                    [LexType.Trparen] = new ITerm[] { },
                },
                ["param".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.TintType] = new ITerm[] {"type".Of(), LexType.Tident.Of(), AddParam.Of()},
                    [LexType.TcharType] = new ITerm[] {"type".Of(), LexType.Tident.Of(), AddParam.Of()},
                    [LexType.TlongIntType] = new ITerm[] {"type".Of(), LexType.Tident.Of(), AddParam.Of()},
                },
                ["block".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tlbracket] = new ITerm[]
                        {LexType.Tlbracket.Of(), Begin.Of(), "ops".Of(), End.Of(), LexType.Trbracket.Of()},
                },
                ["ops".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.TintType] = new ITerm[] {"data".Of(), LexType.Tdelim.Of(), "ops".Of()},
                    [LexType.TcharType] = new ITerm[] {"data".Of(), LexType.Tdelim.Of(), "ops".Of()},
                    [LexType.TlongIntType] = new ITerm[] {"data".Of(), LexType.Tdelim.Of(), "ops".Of()},
                    [LexType.Tident] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tfor] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tinth] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tinto] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tintd] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tchar] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tlparen] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tdelim] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tlbracket] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Tnot] = new[] {"op".Of(), "ops".Of()},
                    [LexType.Trbracket] = new ITerm[] { },
                },
                ["op".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new ITerm[] {LexType.Tident.Of(), "M".Of()},
                    [LexType.Tfor] = new ITerm[] {"for".Of()},
                    [LexType.Tinth] = new[] {"P".Of()},
                    [LexType.Tinto] = new[] {"P".Of()},
                    [LexType.Tintd] = new[] {"P".Of()},
                    [LexType.Tchar] = new[] {"P".Of()},
                    [LexType.Tlparen] = new[] {"P".Of()},
                    [LexType.Tdelim] = new[] {LexType.Tdelim.Of()},
                    [LexType.Tlbracket] = new[] {"block".Of()},
                    [LexType.Tnot] = new[] {"P".Of()},
                },
                ["P".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tinth] = new ITerm[] {"const".Of(), LexType.Tdelim.Of()},
                    [LexType.Tinto] = new ITerm[] {"const".Of(), LexType.Tdelim.Of()},
                    [LexType.Tintd] = new ITerm[] {"const".Of(), LexType.Tdelim.Of()},
                    [LexType.Tchar] = new ITerm[] {"const".Of(), LexType.Tdelim.Of()},
                    [LexType.Tlparen] = new ITerm[]
                        {LexType.Tlparen.Of(), "expr".Of(), LexType.Trparen.Of(), LexType.Tdelim.Of()},
                    [LexType.Tnot] = new ITerm[] {LexType.Tnot.Of(), "expr".Of()},
                },
                ["M".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tlparen] = new ITerm[] {FunName.Of(), "call".Of()},
                    [LexType.Tmul] = new[] {"N".Of()},
                    [LexType.Tmod] = new[] {"N".Of()},
                    [LexType.Tdiv] = new[] {"N".Of()},
                    [LexType.Tlshift] = new[] {"N".Of()},
                    [LexType.Trshift] = new[] {"N".Of()},
                    [LexType.Tplus] = new[] {"N".Of()},
                    [LexType.Tminus] = new[] {"N".Of()},
                    [LexType.Txor] = new[] {"N".Of()},
                    [LexType.Tor] = new[] {"N".Of()},
                    [LexType.Tand] = new[] {"N".Of()},
                    [LexType.Teq] = new[] {"N".Of()},
                },
                ["N".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tmul] = new ITerm[]
                        {IdentToR.Of(), LexType.Tmul.Of(), "A8".Of(), GenBinary.Of(), "Q5".Of()},
                    [LexType.Tmod] = new ITerm[]
                        {IdentToR.Of(), LexType.Tmod.Of(), "A8".Of(), GenBinary.Of(), "Q5".Of()},
                    [LexType.Tdiv] = new ITerm[]
                        {IdentToR.Of(), LexType.Tdiv.Of(), "A8".Of(), GenBinary.Of(), "Q5".Of()},
                    [LexType.Tlshift] = new ITerm[]
                        {IdentToR.Of(), LexType.Tlshift.Of(), "A6".Of(), GenBinary.Of(), "Q3".Of()},
                    [LexType.Trshift] = new ITerm[]
                        {IdentToR.Of(), LexType.Trshift.Of(), "A6".Of(), GenBinary.Of(), "Q3".Of()},
                    [LexType.Tplus] = new ITerm[]
                        {IdentToR.Of(), LexType.Tplus.Of(), "A7".Of(), GenBinary.Of(), "Q4".Of()},
                    [LexType.Tminus] = new ITerm[]
                        {IdentToR.Of(), LexType.Tminus.Of(), "A7".Of(), GenBinary.Of(), "Q4".Of()},
                    [LexType.Txor] = new ITerm[]
                        {IdentToR.Of(), LexType.Txor.Of(), "A4".Of(), GenBinary.Of(), "Q1".Of()},
                    [LexType.Tor] = new ITerm[] {IdentToR.Of(), LexType.Tor.Of(), "A3".Of(), GenBinary.Of(), "Q6".Of()},
                    [LexType.Tand] = new ITerm[]
                        {IdentToR.Of(), LexType.Tand.Of(), "A5".Of(), GenBinary.Of(), "Q2".Of()},
                    [LexType.Teq] = new ITerm[] {LexType.Teq.Of(), "expr".Of(), GenAssign.Of()},
                },
                ["R".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new ITerm[] {LexType.Tident.Of(), "N".Of()},
                    [LexType.Tinth] = new[] {"expr".Of()},
                    [LexType.Tinto] = new[] {"expr".Of()},
                    [LexType.Tintd] = new[] {"expr".Of()},
                    [LexType.Tchar] = new[] {"expr".Of()},
                    [LexType.Tlparen] = new[] {"expr".Of()},
                    [LexType.Tnot] = new[] {"expr".Of()},
                },
                ["for".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tfor] = new ITerm[]
                    {
                        LexType.Tfor.Of(), Begin.Of(), LexType.Tlparen.Of(), "data".Of(), LexType.Tdelim.Of(),
                        StartSave.Of(), "R".Of(), EndSave.Of(),
                        LexType.Tdelim.Of(), StartSave.Of(), "R".Of(), EndSave.Of(), LexType.Trparen.Of(),
                        SaveAddress.Of(), PasteSave.Of(), GenJump.Of(), "op".Of(), PasteSave.Of(), JumpTo.Of(),
                        EndFor.Of(), End.Of()
                    },
                },
                ["call".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tlparen] = new ITerm[]
                        {LexType.Tlparen.Of(), "cparams".Of(), LexType.Trparen.Of(), GenCall.Of()},
                },
                ["cparams".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new ITerm[] {"expr".Of(), PushParam.Of(), "X".Of()},
                    [LexType.Tinth] = new ITerm[] {"expr".Of(), PushParam.Of(), "X".Of()},
                    [LexType.Tinto] = new ITerm[] {"expr".Of(), PushParam.Of(), "X".Of()},
                    [LexType.Tintd] = new ITerm[] {"expr".Of(), PushParam.Of(), "X".Of()},
                    [LexType.Tchar] = new ITerm[] {"expr".Of(), PushParam.Of(), "X".Of()},
                    [LexType.Tlparen] = new ITerm[] {"expr".Of(), PushParam.Of(), "X".Of()},
                    [LexType.Tnot] = new ITerm[] {"expr".Of(), PushParam.Of(), "X".Of()},
                    [LexType.Trparen] = new ITerm[] { },
                },
                ["X".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tcomma] = new ITerm[] {LexType.Tcomma.Of(), "cparams".Of()},
                    [LexType.Trparen] = new ITerm[] { },
                },
                ["expr".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"A3".Of(), "Q6".Of()},
                    [LexType.Tinth] = new[] {"A3".Of(), "Q6".Of()},
                    [LexType.Tinto] = new[] {"A3".Of(), "Q6".Of()},
                    [LexType.Tintd] = new[] {"A3".Of(), "Q6".Of()},
                    [LexType.Tchar] = new[] {"A3".Of(), "Q6".Of()},
                    [LexType.Tlparen] = new[] {"A3".Of(), "Q6".Of()},
                    [LexType.Tnot] = new[] {"A3".Of(), "Q6".Of()},
                },
                ["Q6".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tor] = new ITerm[] {LexType.Tor.Of(), "A3".Of(), GenBinary.Of(), "Q6".Of()},
                    [LexType.Trparen] = new ITerm[] { },
                    [LexType.Tdelim] = new ITerm[] { },
                    [LexType.Tcomma] = new ITerm[] { },
                },
                ["A3".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"A4".Of(), "Q1".Of()},
                    [LexType.Tinth] = new[] {"A4".Of(), "Q1".Of()},
                    [LexType.Tinto] = new[] {"A4".Of(), "Q1".Of()},
                    [LexType.Tintd] = new[] {"A4".Of(), "Q1".Of()},
                    [LexType.Tchar] = new[] {"A4".Of(), "Q1".Of()},
                    [LexType.Tlparen] = new[] {"A4".Of(), "Q1".Of()},
                    [LexType.Tnot] = new[] {"A4".Of(), "Q1".Of()},
                },
                ["Q1".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Txor] = new ITerm[] {LexType.Txor.Of(), "A4".Of(), GenBinary.Of(), "Q1".Of()},
                    [LexType.Trparen] = new ITerm[] { },
                    [LexType.Tdelim] = new ITerm[] { },
                    [LexType.Tcomma] = new ITerm[] { },
                },
                ["A4".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"A5".Of(), "Q2".Of()},
                    [LexType.Tinth] = new[] {"A5".Of(), "Q2".Of()},
                    [LexType.Tinto] = new[] {"A5".Of(), "Q2".Of()},
                    [LexType.Tintd] = new[] {"A5".Of(), "Q2".Of()},
                    [LexType.Tchar] = new[] {"A5".Of(), "Q2".Of()},
                    [LexType.Tlparen] = new[] {"A5".Of(), "Q2".Of()},
                    [LexType.Tnot] = new[] {"A5".Of(), "Q2".Of()},
                },
                ["A5".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"A6".Of(), "Q3".Of()},
                    [LexType.Tinth] = new[] {"A6".Of(), "Q3".Of()},
                    [LexType.Tinto] = new[] {"A6".Of(), "Q3".Of()},
                    [LexType.Tintd] = new[] {"A6".Of(), "Q3".Of()},
                    [LexType.Tchar] = new[] {"A6".Of(), "Q3".Of()},
                    [LexType.Tlparen] = new[] {"A6".Of(), "Q3".Of()},
                    [LexType.Tnot] = new[] {"A6".Of(), "Q3".Of()},
                },
                ["A6".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"A7".Of(), "Q4".Of()},
                    [LexType.Tinth] = new[] {"A7".Of(), "Q4".Of()},
                    [LexType.Tinto] = new[] {"A7".Of(), "Q4".Of()},
                    [LexType.Tintd] = new[] {"A7".Of(), "Q4".Of()},
                    [LexType.Tchar] = new[] {"A7".Of(), "Q4".Of()},
                    [LexType.Tlparen] = new[] {"A7".Of(), "Q4".Of()},
                    [LexType.Tnot] = new[] {"A7".Of(), "Q4".Of()},
                },
                ["A7".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"A8".Of(), "Q5".Of()},
                    [LexType.Tinth] = new[] {"A8".Of(), "Q5".Of()},
                    [LexType.Tinto] = new[] {"A8".Of(), "Q5".Of()},
                    [LexType.Tintd] = new[] {"A8".Of(), "Q5".Of()},
                    [LexType.Tchar] = new[] {"A8".Of(), "Q5".Of()},
                    [LexType.Tlparen] = new[] {"A8".Of(), "Q5".Of()},
                    [LexType.Tnot] = new[] {"A8".Of(), "Q5".Of()},
                },
                ["A8".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"A9".Of()},
                    [LexType.Tinth] = new[] {"A9".Of()},
                    [LexType.Tinto] = new[] {"A9".Of()},
                    [LexType.Tintd] = new[] {"A9".Of()},
                    [LexType.Tchar] = new[] {"A9".Of()},
                    [LexType.Tlparen] = new[] {"A9".Of()},
                    [LexType.Tnot] = new ITerm[] {LexType.Tnot.Of(), "A8".Of(), GenNot.Of()},
                },
                ["A9".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new ITerm[] {LexType.Tident.Of(), IdentToR.Of()},
                    [LexType.Tinth] = new[] {"const".Of()},
                    [LexType.Tinto] = new[] {"const".Of()},
                    [LexType.Tintd] = new[] {"const".Of()},
                    [LexType.Tchar] = new[] {"const".Of()},
                    [LexType.Tlparen] = new ITerm[] {LexType.Tlparen.Of(), "expr".Of(), LexType.Trparen.Of()},
                },
                ["const".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tinth] = new ITerm[] {LexType.Tinth.Of(), ConstToR.Of()},
                    [LexType.Tinto] = new ITerm[] {LexType.Tinto.Of(), ConstToR.Of()},
                    [LexType.Tintd] = new ITerm[] {LexType.Tintd.Of(), ConstToR.Of()},
                    [LexType.Tchar] = new ITerm[] {LexType.Tchar.Of(), ConstToR.Of()},
                },
                ["Q2".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tand] = new ITerm[] {LexType.Tand.Of(), "A5".Of(), GenBinary.Of(), "Q2".Of()},
                    [LexType.Txor] = new ITerm[] { },
                    [LexType.Trparen] = new ITerm[] { },
                    [LexType.Tdelim] = new ITerm[] { },
                    [LexType.Tcomma] = new ITerm[] { },
                },
                ["Q3".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tlshift] = new[] {"L2".Of(), "Q3".Of()},
                    [LexType.Trshift] = new[] {"L2".Of(), "Q3".Of()},
                    [LexType.Tand] = new ITerm[] { },
                    [LexType.Txor] = new ITerm[] { },
                    [LexType.Trparen] = new ITerm[] { },
                    [LexType.Tdelim] = new ITerm[] { },
                    [LexType.Tcomma] = new ITerm[] { },
                },
                ["Q4".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tplus] = new[] {"L3".Of(), "Q4".Of()},
                    [LexType.Tminus] = new[] {"L3".Of(), "Q4".Of()},
                    [LexType.Tlshift] = new ITerm[] { },
                    [LexType.Trshift] = new ITerm[] { },
                    [LexType.Tand] = new ITerm[] { },
                    [LexType.Txor] = new ITerm[] { },
                    [LexType.Trparen] = new ITerm[] { },
                    [LexType.Tdelim] = new ITerm[] { },
                    [LexType.Tcomma] = new ITerm[] { },
                },
                ["Q5".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tmul] = new[] {"L4".Of(), "Q5".Of()},
                    [LexType.Tmod] = new[] {"L4".Of(), "Q5".Of()},
                    [LexType.Tdiv] = new[] {"L4".Of(), "Q5".Of()},
                    [LexType.Tplus] = new ITerm[] { },
                    [LexType.Tminus] = new ITerm[] { },
                    [LexType.Tlshift] = new ITerm[] { },
                    [LexType.Trshift] = new ITerm[] { },
                    [LexType.Tand] = new ITerm[] { },
                    [LexType.Txor] = new ITerm[] { },
                    [LexType.Trparen] = new ITerm[] { },
                    [LexType.Tdelim] = new ITerm[] { },
                    [LexType.Tcomma] = new ITerm[] { },
                },
                ["L2".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tlshift] = new ITerm[] {LexType.Tlshift.Of(), "A6".Of(), GenBinary.Of()},
                    [LexType.Trshift] = new ITerm[] {LexType.Trshift.Of(), "A6".Of(), GenBinary.Of()},
                },
                ["L3".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tplus] = new ITerm[] {LexType.Tplus.Of(), "A7".Of(), GenBinary.Of()},
                    [LexType.Tminus] = new ITerm[] {LexType.Tminus.Of(), "A7".Of(), GenBinary.Of()},
                },
                ["L4".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tmul] = new ITerm[] {LexType.Tmul.Of(), "A8".Of(), GenBinary.Of()},
                    [LexType.Tmod] = new ITerm[] {LexType.Tmod.Of(), "A8".Of(), GenBinary.Of()},
                    [LexType.Tdiv] = new ITerm[] {LexType.Tdiv.Of(), "A8".Of(), GenBinary.Of()},
                },
            };

        public Ll1SyntaxAnalyzer(Scanner sc)
        {
            this.sc = sc;
            NewEnv();
        }

        public void Check()
        {
            sc.PushState();
            var l = sc.Next();
            sc.PopState();

            var inps = table["program".Of()];
            if (!inps.ContainsKey(l.Type))
            {
                throw new ParseException(l, inps.Keys.ToArray());
            }

            magaz.AddRange(inps[l.Type].Reverse());
            while (magaz.Count > 0)
            {
                var term = magaz.Last() as Term;
                var neterm = magaz.Last() as Neterm;
                var delta = magaz.Last() as Delta;
                if (term != null)
                {
                    var ll = sc.Next();
                    if (ll.Type != term.Type)
                    {
                        throw new ParseException(ll, term.Type);
                    }

                    switch (ll.Type)
                    {
                        case LexType.Tident:
                            lastId = ll;
                            break;
                        case LexType.TlongIntType:
                        case LexType.TintType:
                        case LexType.TcharType:
                            ttype = new Dictionary<LexType, SemType>
                            {
                                [LexType.TlongIntType] = SemType.LongLongInt,
                                [LexType.TintType] = SemType.Int,
                                [LexType.TcharType] = SemType.Char
                            }[ll.Type];
                            break;
                        case LexType.Tintd:
                        case LexType.Tinth:
                        case LexType.Tinto:
                            lastConst = ll.IntValue;
                            break;
                        case LexType.Tchar:
                            lastConst = ll.Tok[1];
                            break;
                        case LexType.Tand:
                        case LexType.Tor:
                        case LexType.Tdiv:
                        case LexType.Tmod:
                        case LexType.Tmul:
                        case LexType.Tplus:
                        case LexType.Tminus:
                        case LexType.Tlshift:
                        case LexType.Trshift:
                        case LexType.Txor:
                        case LexType.Tnot:
                            ops.Push(ll);
                            break;
                    }

                    magaz.RemoveAt(magaz.Count - 1);
                }
                else if (neterm != null)
                {
                    sc.PushState();
                    var ll = sc.Next();
                    sc.PopState();
                    var inpss = table[neterm];
                    if (!inpss.ContainsKey(ll.Type))
                    {
                        throw new ParseException(ll, inpss.Keys.ToArray());
                    }

                    magaz.RemoveAt(magaz.Count - 1);
                    magaz.AddRange(inpss[ll.Type].Reverse());
                }
                else
                {
                    delta?.Action(this);
                    magaz.RemoveAt(magaz.Count - 1);
                }
            }
        }

        private void Gen(Operation operation, dynamic arg1 = null, dynamic arg2 = null)
        {
            Ir.Add(Triad.Of(operation, arg1, arg2));
        }

        private void NewEnv()
        {
            scopes.Add((++counter).ToString());
            environment.Add(new Dictionary<string, VarInfo>());
            envs.Push(new Env(environment, scopes));
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

            return currentFrame[name] = VarInfo.Of(type, var, Scope);
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

        private static int GetSize(params SemType[] types)
        {
            return types.Select(t => new Dictionary<SemType, int>
            {
                [SemType.Char] = 1,
                [SemType.Int] = 4,
                [SemType.LongLongInt] = 8
            }[t]).Sum();
        }

        private string fnName;

        private static readonly Action<Ll1SyntaxAnalyzer> NewFunc = a =>
        {
            a.Gen(Operation.Proc, a.fnName = a.lastId.Tok);
            a.AddVar(a.lastId, SemType.Function);
        };

        private static readonly Action<Ll1SyntaxAnalyzer> AddParam = a =>
        {
            var v = a.AddVar(a.lastId, a.lastType);
            var fn = a.TryFindVar(a.fnName);
            fn.AddParam(v);
        };

        private static readonly Action<Ll1SyntaxAnalyzer> GenFunctionProlog = a =>
        {
            var fn = a.TryFindVar(a.fnName);
            foreach (var param in fn.Params)
            {
                a.Gen(Operation.Param, param.FullName, GetSize(param.Type));
            }
        };

        private static readonly Action<Ll1SyntaxAnalyzer> GenFunctionEpilog = a =>
        {
            var fn = a.TryFindVar(a.fnName);
            foreach (var param in fn.Params)
            {
                a.Gen(Operation.Pop, param.FullName);
            }

            a.envs.Pop().Dispose();
            a.Gen(Operation.Ret);
        };

        private static readonly Action<Ll1SyntaxAnalyzer> SaveType = a => { a.lastType = a.ttype; };

        private VarInfo currVar;

        private static readonly Action<Ll1SyntaxAnalyzer> NewVar = a =>
        {
            var v = a.AddVar(a.lastId, a.lastType);
            a.currVar = v;
            var size = GetSize(a.lastType);
            a.Gen(a.scopes.Count <= 2 ? Operation.GlobVar : Operation.LocVar, v.FullName, size);
        };

        private static readonly Action<Ll1SyntaxAnalyzer> Begin = a => { a.NewEnv(); };

        private static readonly Action<Ll1SyntaxAnalyzer> End = a =>
        {
            foreach (var kv in a.environment.Last())
            {
                a.Gen(Operation.Destroy, kv.Value.FullName);
            }

            a.envs.Pop().Dispose();
        };

        private static readonly Action<Ll1SyntaxAnalyzer> GenAssign = a =>
        {
            a.Gen(Operation.Assign, a.currVar.FullName, a.r.Pop());
        };

        private static readonly Action<Ll1SyntaxAnalyzer> ConstToR = a => { a.r.Push(ConstResult.Of(a.lastConst)); };

        private static readonly Action<Ll1SyntaxAnalyzer> IdentToR = a =>
        {
            var v = a.FindVar(a.lastId);
            a.Gen(Operation.Load, v.FullName);
            a.r.Push(TriadResult.Of(a.Ir.Count - 1, v.Type));
        };

        private static readonly Action<Ll1SyntaxAnalyzer> GenBinary = a =>
        {
            var op = a.ops.Pop();
            var o = Operation.Undefined;
            switch (op.Type)
            {
                case LexType.Tand:
                    o = Operation.And;
                    break;
                case LexType.Tor:
                    o = Operation.Or;
                    break;
                case LexType.Tdiv:
                    o = Operation.Div;
                    break;
                case LexType.Tmod:
                    o = Operation.Mod;
                    break;
                case LexType.Tmul:
                    o = Operation.Mul;
                    break;
                case LexType.Tplus:
                    o = Operation.Add;
                    break;
                case LexType.Tminus:
                    o = Operation.Sub;
                    break;
                case LexType.Tlshift:
                    o = Operation.Lshift;
                    break;
                case LexType.Trshift:
                    o = Operation.Rshift;
                    break;
                case LexType.Txor:
                    o = Operation.Xor;
                    break;
            }


            var o2 = a.r.Pop();
            var o1 = a.r.Pop();
            var resType = (SemType) Math.Max((int) o1.Type, (int) o2.Type);
            if (resType != o1.Type)
            {
                a.Gen(Operation.Cast, o1, GetSize(resType));
                o1 = TriadResult.Of(a.Ir.Count - 1, resType);
            }

            if (resType != o2.Type)
            {
                a.Gen(Operation.Cast, o2, GetSize(resType));
                o2 = TriadResult.Of(a.Ir.Count - 1, resType);
            }

            a.Gen(o, o1, o2);
            a.r.Push(TriadResult.Of(a.Ir.Count - 1, resType));
        };

        private static readonly Action<Ll1SyntaxAnalyzer> GenNot = a =>
        {
            a.ops.Pop();
            var r = a.r.Pop();
            a.Gen(Operation.Not, r);
            a.r.Push(TriadResult.Of(a.Ir.Count - 1, r.Type));
        };

        private static readonly Action<Ll1SyntaxAnalyzer> StartSave = a =>
        {
            a.realIr = a.Ir;
            a.Ir = new List<Triad>();
        };

        private static readonly Action<Ll1SyntaxAnalyzer> EndSave = a =>
        {
            a.saves.Push(a.Ir);
            a.Ir = a.realIr;
        };

        private static readonly Action<Ll1SyntaxAnalyzer> SaveAddress = a =>
        {
            var a1 = a.saves.Pop();
            var a2 = a.saves.Pop();
            a.saves.Push(a1);
            a.saves.Push(a2);
            a.addresses.Push(a.Ir.Count);
        };

        private static readonly Action<Ll1SyntaxAnalyzer> PasteSave = a =>
        {
            var @base = a.Ir.Count;
            foreach (var triad in a.saves.Pop())
            {
                if (triad.Arg1 is TriadResult tr1)
                {
                    tr1.Index += @base;
                }

                if (triad.Arg2 is TriadResult tr2)
                {
                    tr2.Index += @base;
                }

                a.Ir.Add(triad);
            }
        };

        private static readonly Action<Ll1SyntaxAnalyzer> GenJump = a =>
        {
            var o = a.r.Pop();
            if (SemType.Int != o.Type)
            {
                a.Gen(Operation.Cast, o, GetSize(SemType.Int));
                o = TriadResult.Of(a.Ir.Count - 1, SemType.Int);
            }

            a.Gen(Operation.Jz, o);
            a.jumps.Push(a.Ir.Count - 1);
        };

        private static readonly Action<Ll1SyntaxAnalyzer> JumpTo = a => { a.Gen(Operation.Jmp, a.addresses.Pop()); };

        private static readonly Action<Ll1SyntaxAnalyzer> EndFor = a =>
        {
            var ja = a.jumps.Pop();
            a.Gen(Operation.Nop);
            a.Ir[ja].Arg2 = a.Ir.Count - 1;
        };

        private static readonly Action<Ll1SyntaxAnalyzer> GenCall = a =>
        {
            a.Gen(Operation.Call, a.FindVar(a.lastFn).FullName);
        };

        private Lexema lastFn;
        private static readonly Action<Ll1SyntaxAnalyzer> FunName = a => { a.lastFn = a.lastId; };
        private static readonly Action<Ll1SyntaxAnalyzer> PushParam = a => { a.Gen(Operation.Push, a.r.Pop()); };
        private static readonly Action<Ll1SyntaxAnalyzer> aa = a => { };
    }

    public interface ITerm
    {
    }

    public class Neterm : ITerm
    {
        private string Name { get; }

        public Neterm(string name)
        {
            Name = name;
        }

        private bool Equals(Neterm other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((Neterm) obj);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }
    }

    public class Term : ITerm
    {
        public LexType Type { get; }

        public Term(LexType type)
        {
            Type = type;
        }

        private bool Equals(Term other)
        {
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((Term) obj);
        }

        public override int GetHashCode()
        {
            return (int) Type;
        }
    }

    public class Delta : ITerm
    {
        public Action<Ll1SyntaxAnalyzer> Action { get; }

        public Delta(Action<Ll1SyntaxAnalyzer> action)
        {
            Action = action;
        }

        private bool Equals(Delta other)
        {
            return Equals(Action, other.Action);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((Delta) obj);
        }

        public override int GetHashCode()
        {
            return Action != null ? Action.GetHashCode() : 0;
        }
    }

    public static class Ext
    {
        public static Neterm Of(this string name) => new Neterm(name);
        public static Term Of(this LexType type) => new Term(type);
        public static Delta Of(this Action<Ll1SyntaxAnalyzer> action) => new Delta(action);

        public static string ToListing(this IEnumerable<Triad> program)
        {
            var s = "";
            var i = 0;
            foreach (var triad in program)
            {
                s += $"{i}) {triad}\n";
                ++i;
            }

            return s;
        }
    }
}