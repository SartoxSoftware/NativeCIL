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
        foreach (var (opCode, op, src, condition) in IRCompiler.Builder.Instructions)
        {
            switch (opCode)
            {
                case Dup:
                    Builder.AppendLine("push dword [esp]");
                    break;
                case Label:
                    Builder.AppendLine(op + ":");
                    break;
                case Popd:
                    Builder.AppendLine("add esp,4");
                    break;
                
                case Jmp:
                    switch (condition)
                    {
                        case Condition.Zero:
                            Builder.AppendLine("pop ecx"); // Value
                            Builder.AppendLine("cmp ecx,0");
                            Builder.AppendLine("jz " + op);
                            break;
                        
                        case Condition.NotZero:
                            Builder.AppendLine("pop ecx"); // Value
                            Builder.AppendLine("cmp ecx,0");
                            Builder.AppendLine("jnz " + op);
                            break;
                        
                        case Condition.Less:
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            // Shouldn't this be jl?
                            Builder.AppendLine("jb " + op);
                            break;
                        
                        case Condition.NotEqual:
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            Builder.AppendLine("jne " + op);
                            break;
                        
                        case Condition.Equal:
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            Builder.AppendLine("je " + op);
                            break;
                        
                        default:
                            Builder.AppendLine("jmp " + op);
                            break;
                    }
                    break;

                case Push:
                    switch (condition)
                    {
                        case Condition.Zero:
                            Builder.AppendLine("xor eax,eax");
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            Builder.AppendLine("setz al");
                            Builder.AppendLine("push eax");
                            break;
                        
                        case Condition.NotZero:
                            Builder.AppendLine("xor eax,eax");
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            Builder.AppendLine("setnz al");
                            Builder.AppendLine("push eax");
                            break;
                        
                        case Condition.Less:
                            Builder.AppendLine("xor eax,eax");
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            Builder.AppendLine("setl al");
                            Builder.AppendLine("push eax");
                            break;
                        
                        case Condition.NotEqual:
                            Builder.AppendLine("xor eax,eax");
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            Builder.AppendLine("setne al");
                            Builder.AppendLine("push eax");
                            break;
                        
                        case Condition.Equal:
                            Builder.AppendLine("xor eax,eax");
                            Builder.AppendLine("pop ecx"); // Value 2
                            Builder.AppendLine("pop edx"); // Value 1
                            Builder.AppendLine("cmp edx,ecx");
                            Builder.AppendLine("sete al");
                            Builder.AppendLine("push eax");
                            break;
                        
                        default:
                            Builder.AppendLine("push " + (op is Register r ? $"dword [Register{r.Index}+{r.Value * 4}]" : op));
                            break;
                    }
                    break;

                case Pop:
                {
                    if (op is not Register r)
                        throw new Exception("What the fuck are you trying to do? Pop to an immediate value????");
                    
                    Builder.AppendLine($"pop dword [Register{r.Index}+{r.Value * 4}]");
                    break;
                }

                case Mov:
                {
                    if (op is not Register dest)
                        throw new Exception("What the fuck are you trying to do? Mov to an immediate value????");

                    Builder.AppendLine($"mov dword [Register{dest.Index}+{dest.Value * 4}],{(src is Register r ? $"dword [Register{r.Index}+{r.Value * 4}]" : src)}");
                    break;
                }

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
                    if (op is null && src is null)
                    {
                        Builder.AppendLine("pop ecx"); // Value 2
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("add edx,ecx");
                    }
                    else if (op is not null && src is null)
                    {
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("add edx," + op);
                    }
                    Builder.AppendLine("push edx");
                    break;

                case And:
                    if (op is null && src is null)
                    {
                        Builder.AppendLine("pop ecx"); // Value 2
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("and edx,ecx");
                    }
                    else if (op is not null && src is null)
                    {
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("and edx," + op);
                    }
                    Builder.AppendLine("push edx");
                    break;

                case Sub:
                    if (op is null && src is null)
                    {
                        Builder.AppendLine("pop ecx"); // Value 2
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("sub edx,ecx");
                    }
                    else if (op is not null && src is null)
                    {
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("sub edx," + op);
                    }
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
                    if (op is null && src is null)
                    {
                        Builder.AppendLine("pop ecx"); // Value 2
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("or edx,ecx");
                    }
                    else if (op is not null && src is null)
                    {
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("or edx," + op);
                    }
                    Builder.AppendLine("push edx");
                    break;

                case Xor:
                    if (op is null && src is null)
                    {
                        Builder.AppendLine("pop ecx"); // Value 2
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("xor edx,ecx");
                    }
                    else if (op is not null && src is null)
                    {
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("xor edx," + op);
                    }
                    Builder.AppendLine("push edx");
                    break;

                case Shl:
                    if (op is null && src is null)
                    {
                        Builder.AppendLine("pop ecx"); // Value 2
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("shl edx,cl");
                    }
                    else if (op is not null && src is null)
                    {
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("shl edx," + op);
                    }
                    Builder.AppendLine("push edx");
                    break;

                case Shr:
                    if (op is null && src is null)
                    {
                        Builder.AppendLine("pop ecx"); // Value 2
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("shr edx,cl");
                    }
                    else if (op is not null && src is null)
                    {
                        Builder.AppendLine("pop edx"); // Value 1
                        Builder.AppendLine("shr edx," + op);
                    }
                    Builder.AppendLine("push edx");
                    break;

                case Memstore8:
                    Builder.AppendLine("pop ecx"); // Value
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov byte [edx],cl");
                    break;

                case Memload8:
                    Builder.AppendLine("xor ecx,ecx");
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov cl,byte [edx]");
                    Builder.AppendLine("push ecx");
                    break;

                case Memstore16:
                    Builder.AppendLine("pop ecx"); // Value
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov word [edx],cx");
                    break;

                case Memload16:
                    Builder.AppendLine("xor ecx,ecx");
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov cx,word [edx]");
                    Builder.AppendLine("push ecx");
                    break;

                case Memstore32:
                    Builder.AppendLine("pop ecx"); // Value
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov dword [edx],ecx");
                    break;

                case Memload32:
                    Builder.AppendLine("pop edx"); // Address
                    Builder.AppendLine("mov ecx,dword [edx]");
                    Builder.AppendLine("push ecx");
                    break;
                
                case Memstore64: throw new Exception("Memstore64 is not supported on 32-bit targets!");
                case Memload64: throw new Exception("Memstore64 is not supported on 32-bit targets!");
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