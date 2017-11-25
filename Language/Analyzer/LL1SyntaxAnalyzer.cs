using System.Collections.Generic;
using System.Linq;
using Language.Scan;

namespace Language.Analyzer
{
    public class Ll1SyntaxAnalyzer
    {
        private readonly Scanner sc;
        private readonly List<ITerm> magaz = new List<ITerm>();

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
                        LexType.TvoidType.Of(), LexType.Tident.Of(), LexType.Tlparen.Of(), "params".Of(),
                        LexType.Trparen.Of(), "block".Of(), "program".Of()
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
                    [LexType.TintType] = new[] {LexType.TintType.Of()},
                    [LexType.TcharType] = new[] {LexType.TcharType.Of()},
                    [LexType.TlongIntType] = new[]
                        {LexType.TlongIntType.Of(), LexType.TlongIntType.Of(), LexType.TintType.Of()},
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
                    [LexType.Tident] = new ITerm[] {LexType.Tident.Of(), "C".Of()},
                },
                ["C".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Teq] = new ITerm[] {LexType.Teq.Of(), "expr".Of()},
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
                    [LexType.TintType] = new ITerm[] {"type".Of(), LexType.Tident.Of()},
                    [LexType.TcharType] = new ITerm[] {"type".Of(), LexType.Tident.Of()},
                    [LexType.TlongIntType] = new ITerm[] {"type".Of(), LexType.Tident.Of()},
                },
                ["block".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tlbracket] = new ITerm[] {LexType.Tlbracket.Of(), "ops".Of(), LexType.Trbracket.Of()},
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
                    [LexType.Tfor] = new[] {"for".Of()},
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
                    [LexType.Tlparen] = new[] {"call".Of()},
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
                    [LexType.Tmul] = new ITerm[] {LexType.Tmul.Of(), "A8".Of(), "Q5".Of()},
                    [LexType.Tmod] = new ITerm[] {LexType.Tmod.Of(), "A8".Of(), "Q5".Of()},
                    [LexType.Tdiv] = new ITerm[] {LexType.Tdiv.Of(), "A8".Of(), "Q5".Of()},
                    [LexType.Tlshift] = new ITerm[] {LexType.Tlshift.Of(), "A6".Of(), "Q3".Of()},
                    [LexType.Trshift] = new ITerm[] {LexType.Trshift.Of(), "A6".Of(),"Q3".Of()},
                    [LexType.Tplus] = new ITerm[] {LexType.Tplus.Of(), "A7".Of(), "Q4".Of()},
                    [LexType.Tminus] = new ITerm[] {LexType.Tminus.Of(), "A7".Of(), "Q4".Of()},
                    [LexType.Txor] = new ITerm[] {LexType.Txor.Of(), "A4".Of(), "Q1".Of()},
                    [LexType.Tor] = new ITerm[] {LexType.Tor.Of(), "A3".Of(), "Q6".Of()},
                    [LexType.Tand] = new ITerm[] {LexType.Tand.Of(), "A5".Of(), "Q2".Of()},
                    [LexType.Teq] = new ITerm[] {LexType.Teq.Of(), "expr".Of()},
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
                        LexType.Tfor.Of(), LexType.Tlparen.Of(), "data".Of(), LexType.Tdelim.Of(), "R".Of(),
                        LexType.Tdelim.Of(), "R".Of(), LexType.Trparen.Of(), "op".Of()
                    },
                },
                ["call".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tlparen] = new ITerm[] {LexType.Tlparen.Of(), "cparams".Of(), LexType.Trparen.Of()},
                },
                ["cparams".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {"expr".Of(), "X".Of()},
                    [LexType.Tinth] = new[] {"expr".Of(), "X".Of()},
                    [LexType.Tinto] = new[] {"expr".Of(), "X".Of()},
                    [LexType.Tintd] = new[] {"expr".Of(), "X".Of()},
                    [LexType.Tchar] = new[] {"expr".Of(), "X".Of()},
                    [LexType.Tlparen] = new[] {"expr".Of(), "X".Of()},
                    [LexType.Tnot] = new[] {"expr".Of(), "X".Of()},
                    [LexType.Trparen] = new ITerm[] { },
                },
                ["X".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tcomma] = new ITerm[] {LexType.Tcomma.Of(), "cparams".Of()},
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
                    [LexType.Tor] = new ITerm[] {LexType.Tor.Of(), "A3".Of(), "Q6".Of()},
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
                    [LexType.Txor] = new ITerm[] {LexType.Txor.Of(), "A4".Of(), "Q1".Of()},
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
                    [LexType.Tnot] = new ITerm[] {LexType.Tnot.Of(), "A8".Of()},
                },
                ["A9".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tident] = new[] {LexType.Tident.Of()},
                    [LexType.Tinth] = new[] {"const".Of()},
                    [LexType.Tinto] = new[] {"const".Of()},
                    [LexType.Tintd] = new[] {"const".Of()},
                    [LexType.Tchar] = new[] {"const".Of()},
                    [LexType.Tlparen] = new ITerm[] {LexType.Tlparen.Of(), "expr".Of(), LexType.Trparen.Of()},
                },
                ["const".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tinth] = new[] {LexType.Tinth.Of()},
                    [LexType.Tinto] = new[] {LexType.Tinto.Of()},
                    [LexType.Tintd] = new[] {LexType.Tintd.Of()},
                    [LexType.Tchar] = new[] {LexType.Tchar.Of()},
                },
                ["Q2".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tand] = new ITerm[] {LexType.Tand.Of(), "A5".Of(), "Q2".Of()},
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
                    [LexType.Tlshift] = new ITerm[] {LexType.Tlshift.Of(), "A6".Of()},
                    [LexType.Trshift] = new ITerm[] {LexType.Trshift.Of(), "A6".Of()},
                },
                ["L3".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tplus] = new ITerm[] {LexType.Tplus.Of(), "A7".Of()},
                    [LexType.Tminus] = new ITerm[] {LexType.Tminus.Of(), "A7".Of()},
                },
                ["L4".Of()] = new Dictionary<LexType, IEnumerable<ITerm>>
                {
                    [LexType.Tmul] = new ITerm[] {LexType.Tmul.Of(), "A8".Of()},
                    [LexType.Tmod] = new ITerm[] {LexType.Tmod.Of(), "A8".Of()},
                    [LexType.Tdiv] = new ITerm[] {LexType.Tdiv.Of(), "A8".Of()},
                },
            };

        public Ll1SyntaxAnalyzer(Scanner sc)
        {
            this.sc = sc;
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
                if (term != null)
                {
                    var ll = sc.Next();
                    if (ll.Type != term.Type)
                    {
                        throw new ParseException(ll, term.Type);
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
            }
        }
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

    public static class Ext
    {
        public static Neterm Of(this string name) => new Neterm(name);
        public static Term Of(this LexType type) => new Term(type);
    }
}