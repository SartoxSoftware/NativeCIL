using System.Diagnostics;
using System.Text;
using static NativeCIL.Backend.IR.IROpCode;

namespace NativeCIL.Backend.IR.Amd64;

public class Amd64Compiler
{
    private readonly IRCompiler _compiler;
    private readonly StringBuilder _builder;

    public Amd64Compiler(ref IRCompiler compiler)
    {
        _compiler = compiler;
        _builder = new StringBuilder();
    }

    public void Compile()
    {
        _builder.AppendLine("[bits 32]");
        _builder.AppendLine("[global _start]");

        _builder.AppendLine("KERNEL_STACK equ 0x00200000");

        _builder.AppendLine("dd 0xE85250D6"); // Magic
        _builder.AppendLine("dd 0"); // Architecture
        _builder.AppendLine("dd 16"); // Header length
        _builder.AppendLine("dd -(0xE85250D6+16)"); // Checksum
        // Required tag
        _builder.AppendLine("dw 0");
        _builder.AppendLine("dw 0");
        _builder.AppendLine("dd 8");

        _builder.AppendLine("_start:");
        _builder.AppendLine("mov esp,KERNEL_STACK");
        _builder.AppendLine("push 0");
        _builder.AppendLine("popf");
        _builder.AppendLine("push eax");
        _builder.AppendLine("push 0");
        _builder.AppendLine("push ebx");
        _builder.AppendLine("push 0");
        _builder.AppendLine("call EnterLongMode");

        _builder.AppendLine("align 4");
        _builder.AppendLine("IDT:");
        _builder.AppendLine(".Length dw 0");
        _builder.AppendLine(".Base dd 0");

        _builder.AppendLine("EnterLongMode:");
        _builder.AppendLine("mov edi,p4_table");
        _builder.AppendLine("push di");
        _builder.AppendLine("mov eax,p3_table");
        _builder.AppendLine("or eax,0b11");
        _builder.AppendLine("mov [p4_table],eax");
        _builder.AppendLine("mov eax,p2_table");
        _builder.AppendLine("or eax,0b11");
        _builder.AppendLine("mov [p3_table],eax");
        _builder.AppendLine("mov ecx,0");

        _builder.AppendLine(".Map_P2_Table:");
        _builder.AppendLine("mov eax,0x200000");
        _builder.AppendLine("mul ecx");
        _builder.AppendLine("or eax,0b10000011");
        _builder.AppendLine("mov [p2_table+ecx*8],eax");
        _builder.AppendLine("inc ecx");
        _builder.AppendLine("cmp ecx,512");
        _builder.AppendLine("jne .Map_P2_Table");

        _builder.AppendLine("pop di");
        _builder.AppendLine("mov al,0xFF");
        _builder.AppendLine("out 0xA1,al");
        _builder.AppendLine("out 0x21,al");
        _builder.AppendLine("cli");
        _builder.AppendLine("nop");
        _builder.AppendLine("nop");
        _builder.AppendLine("lidt [IDT]");
        _builder.AppendLine("mov eax,10100000b");
        _builder.AppendLine("mov cr4,eax");
        _builder.AppendLine("mov edx,edi");
        _builder.AppendLine("mov cr3,edx");
        _builder.AppendLine("mov ecx,0xC0000080");
        _builder.AppendLine("rdmsr");
        _builder.AppendLine("or eax,0x00000100");
        _builder.AppendLine("wrmsr");
        _builder.AppendLine("mov ebx,cr0");
        _builder.AppendLine("or ebx,0x80000001");
        _builder.AppendLine("mov cr0,ebx");
        _builder.AppendLine("lgdt [GDT.Pointer]");
        _builder.AppendLine("sti");
        _builder.AppendLine("jmp 0x0008:Main");

        _builder.AppendLine("GDT:");
        _builder.AppendLine(".Null:");
        _builder.AppendLine("dq 0x0000000000000000");
        _builder.AppendLine(".Code:");
        _builder.AppendLine("dq 0x00209A0000000000");
        _builder.AppendLine("dq 0x0000920000000000");
        _builder.AppendLine("align 4");
        _builder.AppendLine("dw 0");
        _builder.AppendLine(".Pointer:");
        _builder.AppendLine("dw $-GDT-1");
        _builder.AppendLine("dd GDT");

        _builder.AppendLine("align 4096");
        _builder.AppendLine("p4_table:");
        _builder.AppendLine("resb 4096");
        _builder.AppendLine("p3_table:");
        _builder.AppendLine("resb 4096");
        _builder.AppendLine("p2_table:");
        _builder.AppendLine("resb 4096");

        _builder.AppendLine("[bits 64]");
        _builder.AppendLine("Main:");
        _builder.AppendLine("mov ax,0x0010");
        _builder.AppendLine("mov ds,ax");
        _builder.AppendLine("mov es,ax");
        _builder.AppendLine("mov fs,ax");
        _builder.AppendLine("mov gs,ax");
        _builder.AppendLine("mov ss,ax");
        _builder.AppendLine("pop rsi");
        _builder.AppendLine("pop rdx");
        _builder.AppendLine("mov rbp,KERNEL_STACK-1024");
        
        foreach (var inst in _compiler.Instructions)
        {
            switch (inst.OpCode)
            {
                case Comment: _builder.AppendLine(";" + inst.Operand1); break;
                case Label: _builder.AppendLine(inst.Operand1 + ":"); break;
                case Nop: break; // Common optimization
                case Add: _builder.AppendLine($"add {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Sub: _builder.AppendLine($"sub {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Or: _builder.AppendLine($"or {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Xor: _builder.AppendLine($"xor {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case And: _builder.AppendLine($"and {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Set: _builder.AppendLine($"{GetSetType(inst.Flags)} {MapObject(0, inst.Operand1, inst.Flags)}"); break;
                case Mov: _builder.AppendLine($"mov {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Cmp: _builder.AppendLine($"cmp {MapObject(0, inst.Operand1, inst.Flags)},{MapObject(1, inst.Operand2, inst.Flags)}"); break;
                case Jmp: _builder.AppendLine($"{GetJumpType(inst.Flags)} {inst.Operand1}"); break;
                case Call: _builder.AppendLine("call " + MapObject(0, inst.Operand1, inst.Flags)); break;
                case Ret: _builder.AppendLine("ret"); break;
            }
        }
        
        File.WriteAllText("kernel.asm", _builder.ToString());
        Process.Start("nasm", "-fbin kernel.asm -o kernel.bin").WaitForExit();
        Process.Start("objcopy", $"-Ibinary -Oelf64-x86-64 -Bi386 kernel.bin kernel.o");
        Process.Start("ld.lld", $"-melf_x86_64 -Tlinker.ld -okernel.elf kernel.o").WaitForExit();
    }

    // On x86 at least, equal and zero mean the same thing (ZF = 1)
    private static string GetJumpType(int flags)
    {
        if (HasFlag(ref flags, IRFlag.Zero) || HasFlag(ref flags, IRFlag.Equal))
            return "jz";

        if (HasFlag(ref flags, IRFlag.NotZero) || HasFlag(ref flags, IRFlag.NotEqual))
            return "jnz";

        return "jmp";
    }

    private static string GetSetType(int flags)
    {
        if (HasFlag(ref flags, IRFlag.Less))
            return "setl";

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
        }
        else str = operand.ToString();

        if (reg?.Offset > 0)
            str += "+" + reg.Offset;

        if ((op == 0 && dstPtr) || (op == 1 && srcPtr))
        {
            str = $"[{str}]";
            if (qword && !label)
                str = "qword " + str;
        }

        return str;
    }
    
    private static bool HasFlag(ref int flags, int flag) => (flags & flag) != 0;
}