namespace Language.Compiler
{
    public enum Operation
    {
        Undefined = 0,
        Proc,
        GlobVar,
        LocVar,
        Ret,
        Assign,
        Load,
        Add,
        Sub,
        Mul,
        Div,
        Mod,
        And,
        Or,
        Xor,
        Lshift,
        Rshift,
        Not,
        Jz,
        Jmp,
        Nop,
        Destroy,
        Call,
        Param,
        Push,
        Pop,
        Cast,
    }
}