namespace Language.Compiler
{
    public class Triad
    {
        public Operation Operation { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }

        public static Triad Of(Operation operation, string arg1, string arg2)
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