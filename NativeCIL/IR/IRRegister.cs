namespace NativeCIL.IR;

public record IRRegister(int Value, int Offset = 0, int ExplicitType = 0)
{
    public static readonly IRRegister R0 = new(0);
    public static readonly IRRegister R1 = new(1);
    public static readonly IRRegister R2 = new(2);
    public static readonly IRRegister R3 = new(3);
    public static readonly IRRegister R4 = new(4);

    public IRRegister Byte => this with { ExplicitType = 1 };
    public IRRegister Word => this with { ExplicitType = 2 };
    public IRRegister Dword => this with { ExplicitType = 3 };
    public IRRegister Qword => this with { ExplicitType = 4 };

    public static IRRegister operator +(IRRegister reg, int offset)
    {
        return reg with { Offset = offset };
    }

    public override string ToString()
    {
        return "r" + Value;
    }
}