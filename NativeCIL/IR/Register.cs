namespace NativeCIL.IR;

public class Register
{
    public readonly int Index, Value;
    
    public Register(int value, int index = 0)
    {
        Index = index;
        Value = value;
    }

    public override string ToString()
    {
        return $"%{Index}({Value})";
    }
}