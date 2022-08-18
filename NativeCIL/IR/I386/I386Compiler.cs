using NativeCIL.Base;
using NativeCIL.Linker;
using static NativeCIL.IR.OpCode;

namespace NativeCIL.IR.I386;

public class I386Compiler : Compiler
{
    private readonly string _asmPath, _binPath;

    public I386Compiler(ref IRCompiler compiler) : base(ref compiler)
    {
        _asmPath = Path.ChangeExtension(compiler.Settings.OutputFile, "asm");
        _binPath = Path.ChangeExtension(compiler.Settings.OutputFile, "bin");
        OutputPath = Path.ChangeExtension(compiler.Settings.OutputFile, "elf");
    }

    public override void Initialize()
    {
        Builder.AppendLine("[bits 32]");

        if (IRCompiler.Settings.ImageType == ImageType.None)
            return;

        // Thanks https://os.phil-opp.com/multiboot-kernel!
        Builder.AppendLine("dd 0xE85250D6"); // Magic
        Builder.AppendLine("dd 0"); // Architecture
        Builder.AppendLine("dd 16"); // Header length
        Builder.AppendLine("dd 0x100000000-(0xE85250D6+16)"); // Checksum
        // Required tag
        Builder.AppendLine("dw 0");
        Builder.AppendLine("dw 0");
        Builder.AppendLine("dd 8");
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
                    Builder.AppendLine("push dword [esp]");
                    break;
                case Label:
                    Builder.AppendLine(op + ":");
                    break;

                case Jz:
                    Builder.AppendLine("pop ecx"); // Value
                    Builder.AppendLine("cmp ecx,0");
                    Builder.AppendLine("jz " + op);
                    break;

                case Jnz:
                    Builder.AppendLine("pop ecx"); // Value
                    Builder.AppendLine("cmp ecx,0");
                    Builder.AppendLine("jnz " + op);
                    break;

                case Jb:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("cmp edx,ecx");
                    Builder.AppendLine("jb " + op);
                    break;

                case Jne:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("cmp edx,ecx");
                    Builder.AppendLine("jne " + op);
                    break;

                case Push:
                {
                    Builder.AppendLine("push " + (op is Register r ? $"dword [Register{r.Index}+{r.Value * 4}]" : op));
                    break;
                }

                case Pop:
                {
                    Builder.AppendLine("pop " + (op is Register r ? $"dword [Register{r.Index}+{r.Value * 4}]" : op));
                    break;
                }

                case Mov:
                {
                    var dest = op as Register;
                    Builder.AppendLine($"mov dword [Register{dest.Index}+{dest.Value * 4}],{(src is Register r ? $"dword [Register{r.Index}+{r.Value * 4}]" : src)}");
                    break;
                }

                case Pushl:
                    Builder.AppendLine("xor eax,eax");
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("cmp edx,ecx");
                    Builder.AppendLine("setl al");
                    Builder.AppendLine("push eax");
                    break;

                case Pushe:
                    Builder.AppendLine("xor eax,eax");
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("cmp edx,ecx");
                    Builder.AppendLine("sete al");
                    Builder.AppendLine("push eax");
                    break;

                case Func:
                    calls++;
                    Builder.AppendLine(op + ":");
                    Builder.AppendLine($"pop dword [Register3+{calls * 4}]");
                    break;

                case Ret:
                    Builder.AppendLine($"push dword [Register3+{calls * 4}]");
                    Builder.AppendLine("ret");
                    break;

                case Call:
                    Builder.AppendLine("call " + op);
                    break;

                case Add:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("add edx,ecx");
                    Builder.AppendLine("push edx");
                    break;

                case And:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("and edx,ecx");
                    Builder.AppendLine("push edx");
                    break;

                case Sub:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("sub edx,ecx");
                    Builder.AppendLine("push edx");
                    break;

                case Mul:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("imul edx,ecx");
                    Builder.AppendLine("push edx");
                    break;

                case Div:
                    Builder.AppendLine("xor edx,edx");
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop eax"); // Value 1
                    Builder.AppendLine("idiv ecx");
                    Builder.AppendLine("push eax");
                    break;

                case Or:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("or edx,ecx");
                    Builder.AppendLine("push edx");
                    break;

                case Xor:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("xor edx,ecx");
                    Builder.AppendLine("push edx");
                    break;

                case Shl:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("shl edx,cl");
                    Builder.AppendLine("push edx");
                    break;

                case Shr:
                    Builder.AppendLine("pop ecx"); // Value 2
                    Builder.AppendLine("pop edx"); // Value 1
                    Builder.AppendLine("shr edx,cl");
                    Builder.AppendLine("push edx");
                    break;

                case Memstore:
                    Builder.AppendLine("pop ecx"); // Value
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov dword [edx],ecx");
                    break;

                case Memload:
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov ecx,dword [edx]");
                    Builder.AppendLine("push ecx");
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