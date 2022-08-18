namespace NativeCIL.IR;

public enum OpCode
{
    Push, Pop, Dup, Mov, Popd,
    Add, And, Sub, Mul, Div, Or, Xor, Shl, Shr,
    Func, Label, Call, Ret,
    Memstore, Memload, Iostore, Ioload,
    Jmp, Jz, Jnz, Jb, Jne,
    Pushl, Pushe,
}