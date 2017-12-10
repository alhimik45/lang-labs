using System;
using System.Collections.Generic;
using System.Linq;
using Language.Scan;

namespace Language.Analyzer
{
    public static class EnumExtensions
    {
        public static string ToStr(this object @enum)
        {
            return Enum.GetName(@enum.GetType(), @enum);
        }

        public static List<Lexema> NN(this List<Lexema> l)
        {
            return l.Where(ll => ll.Type != LexType.Tneterm).ToList();
        }
    }
}