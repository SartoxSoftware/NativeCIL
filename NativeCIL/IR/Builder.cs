using System.Text;

namespace NativeCIL.IR;

public class Builder
{
    public readonly List<Instruction> Instructions;

    public Builder()
    {
        Instructions = new();
    }

    public void Inst(OpCode opcode, object? operand1 = null, object? operand2 = null)
    {
        Instructions.Add(new Instruction(opcode, operand1, operand2));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var inst in Instructions)
        {
            sb.Append(inst.OpCode.ToString().ToLowerInvariant());

            if (inst.Operand1 != null)
                sb.Append(' ').Append(inst.Operand1);
            
            if (inst.Operand2 != null)
                sb.Append(", ").Append(inst.Operand2);

            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}