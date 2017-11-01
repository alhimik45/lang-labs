using System;

namespace Language.Analyzer
{
    public static class EnumExtensions
    {
        public static string ToStr(this object @enum)
        {
            return Enum.GetName(@enum.GetType(), @enum);
        }
    }
}