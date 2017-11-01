using System.Collections.Generic;
using Language.Scan;

namespace Language.Analyzer
{
    public class VarInfo
    {
        public SemType Type { get; }
        public Lexema Location { get; }
        public List<SemType> Params { get; }

        public VarInfo(SemType type, Lexema location)
        {
            Type = type;
            Location = location;
            Params = new List<SemType>();
        }

        public static VarInfo Of(SemType type, Lexema location)
        {
            return new VarInfo(type, location);
        }

        public void AddParam(SemType type)
        {
            Params.Add(type);
        }
    }
}