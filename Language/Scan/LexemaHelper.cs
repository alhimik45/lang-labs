using System.Collections.Generic;
using System.Linq;

namespace Language.Scan
{
    public static class LexemaHelper
    {
        public static bool In(this Lexema l, IEnumerable<LexType> types)
        {
            return types.Contains(l.Type);
        }
    }
}