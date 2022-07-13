using System.Text;
using static NativeCIL.Backend.IR.IROpCode;

namespace NativeCIL.Backend.IR.Amd64;

public class Amd64Compiler
{
    private readonly IRCompiler _compiler;
    private readonly StringBuilder _builder;

    public Amd64Compiler(IRCompiler compiler)
    {
        _compiler = compiler;
        _builder = new StringBuilder();
    }

    public void Compile()
    {
        foreach (var inst in _compiler.Instructions)
        {
            switch (inst.OpCode)
            {
                case Comment: break;
                case Label: _builder.AppendLine(inst.Operand1 + ":"); break;
                case Nop: break; // Common optimization
                case Add: _builder.AppendLine($"add {MapObject(inst.Operand1)},{MapObject(inst.Operand2)}"); break;
                case Sub: _builder.AppendLine($"sub {MapObject(inst.Operand1)},{MapObject(inst.Operand2)}"); break;
                case Or: _builder.AppendLine($"or {MapObject(inst.Operand1)},{MapObject(inst.Operand2)}"); break;
                case Xor: _builder.AppendLine($"xor {MapObject(inst.Operand1)},{MapObject(inst.Operand2)}"); break;
                case And: _builder.AppendLine($"and {MapObject(inst.Operand1)},{MapObject(inst.Operand2)}"); break;
                case Set: break;
                case Mov: break;
                case Cmp: _builder.AppendLine($"cmp {MapObject(inst.Operand1)},{MapObject(inst.Operand2)}"); break;
                case Jmp: _builder.AppendLine("jmp " + inst.Operand1); break;
                case Call: _builder.AppendLine("call " + MapObject(inst.Operand1)); break;
                case Ret: _builder.AppendLine("ret"); break;
            }
        }
    }

    private static string? MapObject(object? operand)
    {
        if (operand is not IRRegister reg)
            return operand?.ToString();

        var str = reg.Value switch
        {
            0 => "rbp",
            1 => "rax",
            2 => "rbx",
            3 => "r8",
            4 => "rdx",
            _ => string.Empty
        };

        return reg.Offset > 0 ? str + "+" + reg.Offset : str;
    }
}