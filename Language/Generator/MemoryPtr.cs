using Language.Analyzer;

namespace Language.Generator
{
    public class MemoryPtr : IPlace
    {
        public int Offset;
        public SemType Type;

        public override string ToString()
        {
            return $"qword ptr [TMP+{Offset}]";
        }
    }
}