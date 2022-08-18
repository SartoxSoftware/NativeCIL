using NativeCIL.Base;
using NativeCIL.Linker;
using static NativeCIL.IR.OpCode;

namespace NativeCIL.IR.Amd64;

public class Amd64Compiler : Compiler
{
    private readonly string _asmPath, _binPath;

    public Amd64Compiler(ref IRCompiler compiler) : base(ref compiler)
    {
        _asmPath = Path.ChangeExtension(compiler.Settings.OutputFile, "asm");
        _binPath = Path.ChangeExtension(compiler.Settings.OutputFile, "bin");
        OutputPath = Path.ChangeExtension(compiler.Settings.OutputFile, "elf");
    }

    public override void Initialize()
    {
        if (IRCompiler.Settings.ImageType != ImageType.None)
        {
            Builder.AppendLine("[bits 32]");

            Builder.AppendLine("KERNEL_STACK equ 0x00200000");

            // Thanks https://os.phil-opp.com/multiboot-kernel!
            Builder.AppendLine("dd 0xE85250D6"); // Magic
            Builder.AppendLine("dd 0"); // Architecture
            Builder.AppendLine("dd 16"); // Header length
            Builder.AppendLine("dd 0x100000000-(0xE85250D6+16)"); // Checksum
            // Required tag
            Builder.AppendLine("dw 0");
            Builder.AppendLine("dw 0");
            Builder.AppendLine("dd 8");

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
        // Create virtual registers
        for (var i = 0; i < 4; i++)
        {
            Builder.Append($"Register{i}:dq 0");
            for (var j = 0; j < 29; j++)
                Builder.Append(",0");
            Builder.AppendLine();
        }

        var calls = 0;
        foreach (var (opCode, op, src) in IRCompiler.Builder.Instructions)
        {
            switch (opCode)
            {
                case Jmp:
                    Builder.AppendLine("jmp " + op);
                    break;
                case Dup:
                    Builder.AppendLine("push qword [rsp]");
                    break;
                case Label:
                    Builder.AppendLine(op + ":");
                    break;
                case Popd:
                    Builder.AppendLine("add rsp,4");
                    break;

                case Jz:
                    Builder.AppendLine("pop rcx"); // Value
                    Builder.AppendLine("cmp rcx,0");
                    Builder.AppendLine("jz " + op);
                    break;

                case Jnz:
                    Builder.AppendLine("pop rcx"); // Value
                    Builder.AppendLine("cmp rcx,0");
                    Builder.AppendLine("jnz " + op);
                    break;

                case Jb:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("cmp rdx,rcx");
                    Builder.AppendLine("jb " + op);
                    break;

                case Jne:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("cmp rdx,rcx");
                    Builder.AppendLine("jne " + op);
                    break;

                case Push:
                {
                    Builder.AppendLine("push " + (op is Register r ? $"qword [Register{r.Index}+{r.Value * 8}]" : op));
                    break;
                }

                case Pop:
                {
                    Builder.AppendLine("pop " + (op is Register r ? $"qword [Register{r.Index}+{r.Value * 8}]" : op));
                    break;
                }

                case Mov:
                {
                    var dest = op as Register;
                    Builder.AppendLine($"mov qword [Register{dest.Index}+{dest.Value * 8}],{(src is Register r ? $"qword [Register{r.Index}+{r.Value * 8}]" : src)}");
                    break;
                }

                case Pushl:
                    Builder.AppendLine("xor rax,rax");
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("cmp rdx,rcx");
                    Builder.AppendLine("setl al");
                    Builder.AppendLine("push rax");
                    break;

                case Pushe:
                    Builder.AppendLine("xor rax,rax");
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("cmp rdx,rcx");
                    Builder.AppendLine("sete al");
                    Builder.AppendLine("push rax");
                    break;

                case Func:
                    calls++;
                    Builder.AppendLine(op + ":");
                    Builder.AppendLine($"pop qword [Register3+{calls * 8}]");
                    break;

                case Ret:
                    Builder.AppendLine($"push qword [Register3+{calls * 8}]");
                    Builder.AppendLine("ret");
                    break;

                case Call:
                    Builder.AppendLine("call " + op);
                    break;

                case Add:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("add rdx,rcx");
                    Builder.AppendLine("push rdx");
                    break;

                case And:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("and rdx,rcx");
                    Builder.AppendLine("push rdx");
                    break;

                case Sub:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("sub rdx,rcx");
                    Builder.AppendLine("push rdx");
                    break;

                case Mul:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("imul rdx,rcx");
                    Builder.AppendLine("push rdx");
                    break;

                case Div:
                    Builder.AppendLine("xor rdx,rdx");
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rax"); // Value 1
                    Builder.AppendLine("idiv rcx");
                    Builder.AppendLine("push rax");
                    break;

                case Or:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("or rdx,rcx");
                    Builder.AppendLine("push rdx");
                    break;

                case Xor:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("xor rdx,rcx");
                    Builder.AppendLine("push rdx");
                    break;

                case Shl:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("shl rdx,cl");
                    Builder.AppendLine("push rdx");
                    break;

                case Shr:
                    Builder.AppendLine("pop rcx"); // Value 2
                    Builder.AppendLine("pop rdx"); // Value 1
                    Builder.AppendLine("shr rdx,cl");
                    Builder.AppendLine("push rdx");
                    break;

                case Memstore:
                    Builder.AppendLine("pop rcx"); // Value
                    Builder.AppendLine("pop rdx"); // Address
                    Builder.AppendLine("mov qword [rdx],rcx");
                    break;

                case Memload:
                    Builder.AppendLine("pop rdx"); // Address
                    Builder.AppendLine("mov rcx,qword [rdx]");
                    Builder.AppendLine("push rcx");
                    break;
            }
        }

        File.WriteAllText(_asmPath, Builder.ToString());
        Utils.StartSilent("yasm", $"-fbin {_asmPath} -o {_binPath}");
    }

    public override void Link()
    {
        OutputStream = ELF.Link32(_binPath);
    }
}