using static NativeCIL.IR.IROpCode;

namespace NativeCIL.IR.Amd64;

public class Amd64Compiler : Compiler
{
    private readonly string _asmPath, _binPath, _objPath;

    public Amd64Compiler(ref IRCompiler compiler) : base(ref compiler)
    {
        _asmPath = Path.ChangeExtension(compiler.Settings.OutputFile, "asm");
        _binPath = Path.ChangeExtension(compiler.Settings.OutputFile, "bin");
        _objPath = Path.ChangeExtension(compiler.Settings.OutputFile, "o");
        OutputPath = Path.ChangeExtension(compiler.Settings.OutputFile, "elf");
    }

    public override void Initialize()
    {
        if (IRCompiler.Settings.ImageType != ImageType.None)
        {
            Builder.AppendLine("[bits 32]");

            Builder.AppendLine("KERNEL_STACK equ 0x00200000");

            Builder.AppendLine("dd 0xE85250D6"); // Magic
            Builder.AppendLine("dd 0"); // Architecture
            Builder.AppendLine("dd 16"); // Header length
            Builder.AppendLine("dd 0x100000000-(0xE85250D6+16)"); // Checksum
            // Required tag
            Builder.AppendLine("dw 0");
            Builder.AppendLine("dw 0");
            Builder.AppendLine("dd 8");

            Builder.AppendLine("_start:");
            Builder.AppendLine("mov esp,KERNEL_STACK");
            Builder.AppendLine("push 0");
            Builder.AppendLine("popf");
            Builder.AppendLine("push eax");
            Builder.AppendLine("push 0");
            Builder.AppendLine("push ebx");
            Builder.AppendLine("push 0");
            Builder.AppendLine("call EnterLongMode");

            Builder.AppendLine("align 4");
            Builder.AppendLine("IDT:");
            Builder.AppendLine(".Length dw 0");
            Builder.AppendLine(".Base dd 0");

            Builder.AppendLine("EnterLongMode:");
            Builder.AppendLine("mov edi,p4_table");
            Builder.AppendLine("push di");
            Builder.AppendLine("mov eax,p3_table");
            Builder.AppendLine("or eax,3");
            Builder.AppendLine("mov [p4_table],eax");
            Builder.AppendLine("mov eax,p2_table");
            Builder.AppendLine("or eax,3");
            Builder.AppendLine("mov [p3_table],eax");
            Builder.AppendLine("mov ecx,0");

            Builder.AppendLine(".Map_P2_Table:");
            Builder.AppendLine("mov eax,0x200000");
            Builder.AppendLine("mul ecx");
            Builder.AppendLine("or eax,131");
            Builder.AppendLine("mov [p2_table+ecx*8],eax");
            Builder.AppendLine("inc ecx");
            Builder.AppendLine("cmp ecx,512");
            Builder.AppendLine("jne .Map_P2_Table");

            Builder.AppendLine("pop di");
            Builder.AppendLine("mov al,0xFF");
            Builder.AppendLine("out 0xA1,al");
            Builder.AppendLine("out 0x21,al");
            Builder.AppendLine("cli");
            Builder.AppendLine("nop");
            Builder.AppendLine("nop");
            Builder.AppendLine("lidt [IDT]");
            Builder.AppendLine("mov eax,160");
            Builder.AppendLine("mov cr4,eax");
            Builder.AppendLine("mov edx,edi");
            Builder.AppendLine("mov cr3,edx");
            Builder.AppendLine("mov ecx,0xC0000080");
            Builder.AppendLine("rdmsr");
            Builder.AppendLine("or eax,0x00000100");
            Builder.AppendLine("wrmsr");
            Builder.AppendLine("mov ebx,cr0");
            Builder.AppendLine("or ebx,0x80000001");
            Builder.AppendLine("mov cr0,ebx");
            Builder.AppendLine("lgdt [GDT.Pointer]");
            Builder.AppendLine("sti");
            Builder.AppendLine("mov ax,0x0010");
            Builder.AppendLine("mov ds,ax");
            Builder.AppendLine("mov es,ax");
            Builder.AppendLine("mov fs,ax");
            Builder.AppendLine("mov gs,ax");
            Builder.AppendLine("mov ss,ax");
            Builder.AppendLine("jmp 0x0008:Main");

            Builder.AppendLine("GDT:");
            Builder.AppendLine(".Null:");
            Builder.AppendLine("dq 0x0000000000000000");
            Builder.AppendLine(".Code:");
            Builder.AppendLine("dq 0x00209A0000000000");
            Builder.AppendLine("dq 0x0000920000000000");
            Builder.AppendLine("align 4");
            Builder.AppendLine("dw 0");
            Builder.AppendLine(".Pointer:");
            Builder.AppendLine("dw $-GDT-1");
            Builder.AppendLine("dd GDT");

            Builder.AppendLine("align 4096");
            Builder.AppendLine("p4_table:");
            Builder.AppendLine("resb 4096");
            Builder.AppendLine("p3_table:");
            Builder.AppendLine("resb 4096");
            Builder.AppendLine("p2_table:");
            Builder.AppendLine("resb 4096");

            Builder.AppendLine("[bits 64]");
            Builder.AppendLine("Main:");
            Builder.AppendLine("pop rsi");
            Builder.AppendLine("pop rdx");
            Builder.AppendLine("mov rbp,KERNEL_STACK-1024");

            return;
        }

        Builder.AppendLine("[bits 64]");
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
        Utils.StartSilent("yasm", $"-fbin {_asmPath} -o {_binPath}");
    }

    public override void Link()
    {
        // TODO: Replace objcopy and lld with a C# linker
        Utils.StartSilent("objcopy", $"-Ibinary -Oelf64-x86-64 -Bi386 {_binPath} {_objPath}");
        Utils.StartSilent("ld.lld", $"-melf_x86_64 -Tlinker.ld -o{OutputPath} {_objPath}");
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
        var qword = HasFlag(ref flags, IRFlag.Qword);

        var reg = operand as IRRegister;
        string? str;

        if (operand is not string)
        {
            str = reg?.Value switch
            {
                0 => "bp",
                1 => "ax",
                2 => "bx",
                3 => "8",
                4 => "dx",
                _ => string.Empty
            };

            if (isByte || reg?.ExplicitType == 1)
                str = str[0] + "l";
            else if (dword || reg?.ExplicitType == 3)
                str = "e" + str;
            else if (qword || reg?.ExplicitType == 4)
                str = "r" + str;

            // TODO: Improve code generation by using the high registers of AMD64
            /*str = "r" + (reg?.Value + 8);

            if (isByte || reg?.ExplicitType == 1)
                str = reg?.Value switch
                {
                    0 => "ah",
                    1 => "al",
                    2 => "cl",
                    3 => "dl",
                    4 => "bl",
                    _ => string.Empty
                };
            else if (word || reg?.ExplicitType == 2)
                str = reg?.Value switch
                {
                    0 => "bp",
                    1 => "ax",
                    2 => "cx",
                    3 => "dx",
                    4 => "bx",
                    _ => string.Empty
                };
            else if (dword || reg?.ExplicitType == 3)
                str = reg?.Value switch
                {
                    0 => "ebp",
                    1 => "eax",
                    2 => "ecx",
                    3 => "edx",
                    4 => "ebx",
                    _ => string.Empty
                };
            else if (reg?.ExplicitType == 4)
                str = reg?.Value switch
                {
                    0 => "rbp",
                    1 => "rax",
                    2 => "rcx",
                    3 => "rdx",
                    4 => "rbx",
                    _ => string.Empty
                };*/
        }
        else str = operand.ToString();

        if (reg?.Offset > 0)
            str += "+" + reg.Offset;

        if ((op != 0 || !dstPtr) && (op != 1 || !srcPtr))
            return str;

        str = $"[{str}]";
        if (qword && !label)
            str = "qword " + str;

        return str;
    }
}