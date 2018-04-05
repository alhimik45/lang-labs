namespace Language.Generator
{
    public class Register : IPlace
    {
        public string B64 { get; }
        public string B32 { get; }
        public string B16 { get; }
        public string B8 { get; }

        public static readonly Register Rax = new Register("rax", "eax", "ax", "al");
        public static readonly Register Rbx = new Register("rbx", "ebx", "bx", "bl");
        public static readonly Register Rcx = new Register("rcx", "ecx", "cx", "cl");
        public static readonly Register Rdx = new Register("rdx", "edx", "dx", "dl");
        public static readonly Register Rsi = new Register("rsi", "esi", "si", "sil");
        public static readonly Register Rdi = new Register("rdi", "edi", "di", "dil");
        public static readonly Register R8 = new Register("r8", "r8d", "r8w", "r8b");
        public static readonly Register R9 = new Register("r9", "r9d", "r9w", "r9b");
        public static readonly Register R10 = new Register("r10", "r10d", "r10w", "r10b");
        public static readonly Register R11 = new Register("r11", "r11d", "r11w", "r11b");
        public static readonly Register R12 = new Register("r12", "r12d", "r12w", "r12b");
        public static readonly Register R13 = new Register("r13", "r13d", "r13w", "r13b");
        public static readonly Register R14 = new Register("r14", "r14d", "r14w", "r14b");
        public static readonly Register R15 = new Register("r15", "r15d", "r15w", "r15b");

        public static readonly Register[] Registers =
        {
            Rax, Rbx, Rcx, Rdx, Rsi, Rdi, R8, R9, R10, R11, R12, R13, R14, R15
        };

        public Register(string b64, string b32, string b16, string b8)
        {
            B64 = b64;
            B32 = b32;
            B16 = b16;
            B8 = b8;
        }
    }
}