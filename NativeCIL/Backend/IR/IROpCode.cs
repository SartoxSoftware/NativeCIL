namespace NativeCIL.Backend.IR;

public enum IROpCode
{
    Comment, Label,
    Nop,
    Add, Sub, Mul, Div, Or, And, Not,
    Set,
    Mov,
    Jmp, Call, Ret
}