namespace NativeCIL.IR;

public static class IRFlag
{
    public static int Zero = 1 << 0;
    public static int NotZero = 1 << 1;
    public static int Equal = 1 << 2;
    public static int NotEqual = 1 << 3;
    public static int Less = 1 << 4;
    public static int GreaterOrEqual = 1 << 5;
    public static int Greater = 1 << 6;
    public static int LessOrEqual = 1 << 7;

    public static int Byte = 1 << 8;
    public static int Word = 1 << 9;
    public static int Dword = 1 << 10;
    public static int Qword = 1 << 11;

    public static int Immediate = 1 << 12;
    public static int Label = 1 << 13;
    public static int DestRegister = 1 << 14;
    public static int SrcRegister = 1 << 15;
    public static int DestPointer = 1 << 16;
    public static int SrcPointer = 1 << 17;

    public static int Unsigned = 1 << 18;
}