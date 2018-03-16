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

        public VarInfo(SemType type, Lexema location, string scope)
        {
            Type = type;
            Location = location;
            Params = new List<VarInfo>();
            FullName = $"{scope}/{location.Tok}";
        }

        public static VarInfo Of(SemType type, Lexema location, string scope)
        {
            return new VarInfo(type, location, scope);
        }

        public void AddParam(VarInfo var)
        {
            Params.Add(var);
        }
    }
}