namespace Language.Compiler
{
    public class Triad
    {
        public Operation Operation { get; set; }
        public dynamic Arg1 { get; set; }
        public dynamic Arg2 { get; set; }

        public static Triad Of(Operation operation, dynamic arg1, dynamic arg2)
        {
            return new Triad
            {
                Operation = operation,
                Arg1 = arg1,
                Arg2 = arg2
            };
        }
    }
}