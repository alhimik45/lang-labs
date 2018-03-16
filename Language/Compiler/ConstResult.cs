namespace Language.Compiler
{
    public class ConstResult : IResult
    {
        public dynamic Value { get; private set; }

        public static ConstResult Of(dynamic val)
        {
            return new ConstResult {Value = val};
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}