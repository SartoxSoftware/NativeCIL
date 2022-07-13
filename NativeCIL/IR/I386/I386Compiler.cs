using System.Diagnostics;
using static NativeCIL.IR.IROpCode;

namespace NativeCIL.IR.I386;

public class I386Compiler : Compiler
{
    private readonly string _asmPath, _binPath, _objPath;

    public I386Compiler(ref IRCompiler compiler) : base(ref compiler)
    {
        _asmPath = Path.ChangeExtension(compiler.Settings.OutputFile, "asm");
        _binPath = Path.ChangeExtension(compiler.Settings.OutputFile, "bin");
        _objPath = Path.ChangeExtension(compiler.Settings.OutputFile, "o");
        OutputPath = Path.ChangeExtension(compiler.Settings.OutputFile, "elf");
    }

    public override void Initialize()
    {
        Builder.AppendLine("[bits 32]");
        Builder.AppendLine("[global _start]");

        Builder.AppendLine("KERNEL_STACK equ 0x00200000");

        if (IRCompiler.Settings.ImageType != ImageType.None)
        {
            Builder.AppendLine("dd 0xE85250D6"); // Magic
            Builder.AppendLine("dd 0"); // Architecture
            Builder.AppendLine("dd 16"); // Header length
            Builder.AppendLine("dd -(0xE85250D6+16)"); // Checksum
            // Required tag
            Builder.AppendLine("dw 0");
            Builder.AppendLine("dw 0");
            Builder.AppendLine("dd 8");
        }

        Builder.AppendLine("_start:");
        Builder.AppendLine("mov esp,KERNEL_STACK");
        Builder.AppendLine("mov ebp,esp");
    }

    public override void Compile()
    {
        foreach (var inst in IRCompiler.Instructions)
        {
            switch (inst.OpCode)
            {
                case Comment: Builder.AppendLine(";" + inst.Operand1); break;
                case Label:
                    Builder.AppendLine(inst.Operand1 + ":");
                    if (inst.Operand2 != null)
                        Builder.AppendLine("dq " + inst.Operand2);
                    break;
                case Nop: break; // Common optimization
                case Add: Builder.AppendLine($"add {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Sub: Builder.AppendLine($"sub {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Or: Builder.AppendLine($"or {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Xor: Builder.AppendLine($"xor {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case And: Builder.AppendLine($"and {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Set: Builder.AppendLine($"{GetSetType(inst.Flags)} {MapObject(0, inst.Operand1, inst.Flags)}"); break;
                case Mov: Builder.AppendLine($"mov {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Cmp: Builder.AppendLine($"cmp {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Jmp: Builder.AppendLine($"{GetJumpType(inst.Flags)} {inst.Operand1}"); break;
                case Call: Builder.AppendLine("call " + MapObject(0, inst.Operand1, inst.Flags)); break;
                case Ret: Builder.AppendLine("ret"); break;
            }
        }
        
        File.WriteAllText(_asmPath, Builder.ToString());
        Process.Start("nasm", $"-fbin {_asmPath} -o {_binPath}").WaitForExit();
    }

    public override void Link()
    {
        // TODO: Replace objcopy and lld with a C# linker
        Process.Start("objcopy", $"-Ibinary -Oelf32-i386 -Bi386 {_binPath} {_objPath}");
        Process.Start("ld.lld", $"-melf_i386 -Tlinker.ld -o{OutputPath} {_objPath}").WaitForExit();
    }
    
    // On x86 at least, equal and zero mean the same thing (ZF = 1)
    private static string GetJumpType(int flags)
    {
        if (HasFlag(ref flags, IRFlag.Zero) || HasFlag(ref flags, IRFlag.Equal))
            return "jz";

        if (HasFlag(ref flags, IRFlag.NotZero) || HasFlag(ref flags, IRFlag.NotEqual))
            return "jnz";

        if (HasFlag(ref flags, IRFlag.Less))
            return "jb";

        if (HasFlag(ref flags, IRFlag.GreaterOrEqual))
            return "jae";

        if (HasFlag(ref flags, IRFlag.Greater))
            return "ja";
        
        if (HasFlag(ref flags, IRFlag.LessOrEqual))
            return "jle";

        return "jmp";
    }

    private static string GetSetType(int flags)
    {
        if (HasFlag(ref flags, IRFlag.Less))
            return "setl";

        if (HasFlag(ref flags, IRFlag.Greater))
            return "setg";

        if (HasFlag(ref flags, IRFlag.Zero) || HasFlag(ref flags, IRFlag.Equal))
            return "setz";

        if (HasFlag(ref flags, IRFlag.NotZero) || HasFlag(ref flags, IRFlag.NotEqual))
            return "setnz";

        throw new Exception("Unknown or unsupported set type found!");
    }

    // I know this code is terrible, please be kind :D
    private static string? MapObject(byte op, object? operand, int flags)
    {
        var dstReg = HasFlag(ref flags, IRFlag.DestRegister);
        var srcReg = HasFlag(ref flags, IRFlag.SrcRegister);
        var imm = HasFlag(ref flags, IRFlag.Immediate);
        var label = HasFlag(ref flags, IRFlag.Label);
        var dstPtr = HasFlag(ref flags, IRFlag.DestPointer);
        var srcPtr = HasFlag(ref flags, IRFlag.SrcPointer);

        if ((!dstReg && !srcReg) || (op == 1 && imm))
            return operand?.ToString();

        var isByte = HasFlag(ref flags, IRFlag.Byte);
        var word = HasFlag(ref flags, IRFlag.Word);
        var dword = HasFlag(ref flags, IRFlag.Dword);

        var reg = operand as IRRegister;
        string? str;

        if (operand is not string)
        {
            str = reg?.Value switch
            {
                0 => "bp",
                1 => "ax",
                2 => "bx",
                3 => "di",
                4 => "dx",
                _ => string.Empty
            };

            if (isByte || reg?.ExplicitType == 1)
                str = str[0] + "l";
            else if (dword || reg?.ExplicitType == 3)
                str = "e" + str;
        }
        else str = operand.ToString();

        if (reg?.Offset > 0)
            str += "+" + reg.Offset;

        if ((op == 0 && dstPtr) || (op == 1 && srcPtr))
        {
            str = $"[{str}]";
            if (dword && !label)
                str = "dword " + str;
        }

        return str;
    }
}