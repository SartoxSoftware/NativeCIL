namespace NativeCIL.Backend.IR;

public record IRRegister(int Value, int Offset)
{
    public static readonly IRRegister R0 = new(0);
    public static readonly IRRegister R1 = new(1);
    public static readonly IRRegister R2 = new(2);
    public static readonly IRRegister R3 = new(3);
    public static readonly IRRegister R4 = new(4);

    public IRRegister(int value) : this(value, 0) {}

    public static IRRegister operator +(IRRegister reg, int offset)
    {
        return reg with { Offset = offset };
    }

    public override string ToString()
    {
        return "r" + Value;
    }
}