namespace NativeCIL.IR;

public enum IROpCode
{
    Comment, Label,
    Store,
    Nop,
    Add, Sub, Mul, Or, Xor, And, Shl, Shr,
    Set,
    Mov,
    Cmp, Jmp, Call, Ret
}