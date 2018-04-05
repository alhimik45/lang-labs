namespace Language.Generator
{
    public class MemoryPtr : IPlace
    {
        public int Offset;

        public override string ToString()
        {
            return $"TMP+{Offset}";
        }
    }
}