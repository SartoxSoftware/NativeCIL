namespace NativeCIL.IR;

public enum IROpCode
{
    Comment, Label,
    Nop,
    Add, Sub, Or, Xor, And,
    Set,
    Mov,
    Cmp, Jmp, Call, Ret
}