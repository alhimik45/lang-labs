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
        Call,
        Param,
        Push,
        Cast,
        Alloc,
        Free,
    }
}