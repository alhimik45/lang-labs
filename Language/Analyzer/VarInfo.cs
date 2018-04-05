using System.Collections.Generic;
using Language.Scan;

namespace Language.Analyzer
{
    public class VarInfo
    {
        public SemType Type { get; }
        public Lexema Location { get; }
        public List<VarInfo> Params { get; }
        public string FullName { get; }
        public int Offset { get; }
        public static HashSet<VarInfo> globs;

        public VarInfo(SemType type, Lexema location, string scope, int offset)
        {
            Type = type;
            Location = location;
            Params = new List<VarInfo>();
            FullName = $"{scope}/{location.Tok}{{{offset}}}";
        }

        public static VarInfo Of(SemType type, Lexema location, string scope, int offset = 0)
        {
            return new VarInfo(type, location, scope, offset);
        }

        public void AddParam(VarInfo var)
        {
            Params.Add(var);
        }

        public string ReadableName => "v" + FullName.Replace('/', '_').Replace('{','_').Replace('}','_');

        private string nname => globs.Contains(this) ? ReadableName : $"rbp-{Offset}";
        
        public string Ptr => new Dictionary<SemType,string>{[SemType.Char]="byte", [SemType.Int]="dword", [SemType.LongLongInt]="qword"}[Type] + $" ptr [{nname}]"; 
    }
}