using System;
using System.Collections.Generic;
using Language.Scan;

namespace Language.Analyzer
{
    public class VarInfo
    {
        public SemType Type { get; }
        public Lexema Location { get; }
        public List<(string name, SemType type)> Params { get; }
        public int Pos { get; private set; }
        public object Value { get; set; }
        public string Name { get; set; }

        public VarInfo(SemType type, Lexema location, string name = "")
        {
            Name = name;
            Type = type;
            Location = location;
            Params = location == null ? null :new List<(string, SemType)>();
            switch (type)
            {
                case SemType.Int:
                    Value = 0;
                    break;
                case SemType.LongLongInt:
                    Value = 0L;
                    break;
                case SemType.Char:
                    Value = '\0';
                    break;
                case SemType.Function:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static VarInfo Of(SemType type, Lexema location)
        {
            return new VarInfo(type, location);
        }

        public void AddParam(string name, SemType type)
        {
            Params.Add((name, type));
        }

        public void SetPos(int pos)
        {
            Pos = pos;
        }
    }
}