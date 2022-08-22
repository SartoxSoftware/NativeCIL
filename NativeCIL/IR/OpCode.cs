namespace NativeCIL.IR;

public enum OpCode
{
    Push, Pop, Dup, Mov, Popd,
    Add, And, Sub, Mul, Div, Or, Xor, Shl, Shr,
    Func, Label, Call, Ret,
    Memstore8, Memload8, Memstore16, Memload16, Memstore32, Memload32, Memstore64, Memload64,
    Iostore8, Ioload8, Iostore16, Ioload16, Iostore32, Ioload32, Iostore64, Ioload64,
    Jmp,
}