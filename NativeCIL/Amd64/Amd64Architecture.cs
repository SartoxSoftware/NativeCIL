using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace NativeCIL.Amd64;

public class Amd64Architecture : Architecture
{
    public Amd64Architecture(string path) : base(path) {}

    public override int PointerSize => 8;

    public override void Initialize()
    {
        Builder.AppendLine("[bits 32]");
        Builder.AppendLine("[global _start]");

        Builder.AppendLine("MULTIBOOT_ALIGN equ 1<<0");
        Builder.AppendLine("MULTIBOOT_MEMINFO equ 1<<1");
        Builder.AppendLine("MULTIBOOT_VBE_MODE equ 1<<2");
        Builder.AppendLine("MULTIBOOT_HEADER_MAGIC equ 0x1BADB002");
        Builder.AppendLine("MULTIBOOT_HEADER_FLAGS equ MULTIBOOT_ALIGN|MULTIBOOT_MEMINFO");
        Builder.AppendLine("CHECKSUM equ -(MULTIBOOT_HEADER_MAGIC+MULTIBOOT_HEADER_FLAGS)");
        Builder.AppendLine("KERNEL_STACK equ 0x00200000");

        Builder.AppendLine("_start:");
        Builder.AppendLine("xor eax,eax");
        Builder.AppendLine("xor ebx,ebx");
        Builder.AppendLine("jmp multiboot_entry");
        Builder.AppendLine("align 4");

        Builder.AppendLine("multiboot_header:");
        Builder.AppendLine("dd MULTIBOOT_HEADER_MAGIC");
        Builder.AppendLine("dd MULTIBOOT_HEADER_FLAGS");
        Builder.AppendLine("dd CHECKSUM");
        Builder.AppendLine("dd multiboot_header");
        Builder.AppendLine("dd _start");
        Builder.AppendLine("dd 00");
        Builder.AppendLine("dd 00");
        Builder.AppendLine("dd multiboot_entry");

        Builder.AppendLine("multiboot_entry:");
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
        Builder.AppendLine("or eax,0b11");
        Builder.AppendLine("mov [p4_table],eax");
        Builder.AppendLine("mov eax,p2_table");
        Builder.AppendLine("or eax,0b11");
        Builder.AppendLine("mov [p3_table],eax");
        Builder.AppendLine("mov ecx,0");

        Builder.AppendLine(".Map_P2_Table:");
        Builder.AppendLine("mov eax,0x200000");
        Builder.AppendLine("mul ecx");
        Builder.AppendLine("or eax,0b10000011");
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
        Builder.AppendLine("mov eax,10100000b");
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
        Builder.AppendLine("mov ax,0x0010");
        Builder.AppendLine("mov ds,ax");
        Builder.AppendLine("mov es,ax");
        Builder.AppendLine("mov fs,ax");
        Builder.AppendLine("mov gs,ax");
        Builder.AppendLine("mov ss,ax");
        Builder.AppendLine("pop rsi");
        Builder.AppendLine("pop rdi");
        Builder.AppendLine("mov rbp,KERNEL_STACK-1024");
    }

    public override void Compile()
    {
        foreach (var method in Module.EntryPoint.DeclaringType.Methods)
        {
            if (method.IsConstructor || method.IsStaticConstructor)
                continue;

            var branches = GetAllBranches(method).ToList();
            Builder.AppendLine(GetSafeName(method.FullName) + ":");

            if (method.Body.InitLocals)
                foreach (var local in method.Body.Variables)
                    PushVariable(local.Index, 0);

            foreach (var inst in method.Body.Instructions)
            {
                foreach (var branch in branches)
                    if (((Instruction)branch.Operand).Offset == inst.Offset)
                    {
                        Builder.AppendLine(BrLabelName(inst, method, true) + ":");
                        break;
                    }

                switch (inst.OpCode.Code)
                {
                    case Code.Nop: break;
                    case Code.Ret: Builder.AppendLine("ret"); break;

                    case Code.Ldc_I4_0: Push(0); break;
                    case Code.Ldc_I4_1: Push(1); break;
                    case Code.Ldc_I4_2: Push(2); break;
                    case Code.Ldc_I4_3: Push(3); break;
                    case Code.Ldc_I4_4: Push(4); break;
                    case Code.Ldc_I4_5: Push(5); break;
                    case Code.Ldc_I4_6: Push(6); break;
                    case Code.Ldc_I4_7: Push(7); break;
                    case Code.Ldc_I4_8: Push(8); break;
                    case Code.Ldc_I4_M1: Push(-1); break;

                    case Code.Ldc_I4_S:
                    case Code.Ldc_I4: Push(inst.Operand); break;

                    case Code.Conv_I4:
                    case Code.Conv_I:
                        Pop("rax");
                        Builder.AppendLine("and rax,0xFFFFFFFF");
                        Push("rax");
                        break;

                    case Code.Conv_U1:
                    case Code.Conv_I1:
                        Pop("rax");
                        Builder.AppendLine("and rax,0xFF");
                        Push("rax");
                        break;

                    case Code.Stind_I1:
                        Pop("rax"); // Value
                        Pop("rbx"); // Address
                        Builder.AppendLine("mov [rbx],al");
                        break;

                    case Code.Add:
                        Pop("rax");
                        Pop("rbx");
                        Builder.AppendLine("add rbx,rax");
                        Push("rbx");
                        break;

                    case Code.Sub:
                        Pop("rax");
                        Pop("rbx");
                        Builder.AppendLine("sub rbx,rax");
                        Push("rbx");
                        break;

                    case Code.Ldloc_0:
                        PopVariable(0, "rax");
                        Push("rax");
                        break;
                    case Code.Ldloc_1:
                        PopVariable(1, "rax");
                        Push("rax");
                        break;
                    case Code.Ldloc_2:
                        PopVariable(2, "rax");
                        Push("rax");
                        break;
                    case Code.Ldloc_3:
                        PopVariable(3, "rax");
                        Push("rax");
                        break;

                    case Code.Ldloc_S:
                    case Code.Ldloc:
                        PopVariable(inst.Operand is Local o ? o.Index : Convert.ToInt32(inst.Operand), "rax");
                        Push("rax");
                        break;

                    case Code.Stloc_0:
                        Pop("rax");
                        PushVariable(0, "rax");
                        break;
                    case Code.Stloc_1:
                        Pop("rax");
                        PushVariable(1, "rax");
                        break;
                    case Code.Stloc_2:
                        Pop("rax");
                        PushVariable(2, "rax");
                        break;
                    case Code.Stloc_3:
                        Pop("rax");
                        PushVariable(3, "rax");
                        break;

                    case Code.Stloc_S:
                    case Code.Stloc:
                        Pop("rax");
                        PushVariable(inst.Operand is Local u ? u.Index : Convert.ToInt32(inst.Operand), "rax");
                        break;

                    case Code.Dup:
                        Peek("rax");
                        Push("rax");
                        break;

                    case Code.Br_S:
                    case Code.Br:
                        Builder.AppendLine("jmp " + BrLabelName(inst, method));
                        break;

                    case Code.Brtrue_S:
                    case Code.Brtrue:
                        Pop("rax");
                        Builder.AppendLine("cmp rax,0");
                        Builder.AppendLine("jnz " + BrLabelName(inst, method));
                        break;

                    case Code.Brfalse_S:
                    case Code.Brfalse:
                        Pop("rax");
                        Builder.AppendLine("cmp rax,0");
                        Builder.AppendLine("jz " + BrLabelName(inst, method));
                        break;

                    case Code.Clt:
                        Pop("rax");
                        Pop("rbx");
                        Builder.AppendLine("cmp rbx,rax");
                        Builder.AppendLine("setl bl");
                        Push("rbx");
                        break;

                    case Code.Ceq:
                        Pop("rax");
                        Pop("rbx");
                        Builder.AppendLine("cmp rbx,rax");
                        Builder.AppendLine("sete bl");
                        Push("rbx");
                        break;
                    
                    case Code.Call:
                        Builder.AppendLine("call " + GetSafeName(((MethodDef)inst.Operand).FullName));
                        break;
                }
            }
        }
    }

    public override void Assemble()
    {
        Directory.CreateDirectory("Output");
        File.WriteAllText("Output/kernel.asm", Builder.ToString());
        Process.Start("nasm", "-fbin Output/kernel.asm -o Output/kernel.bin").WaitForExit();
    }

    public override void Link()
    {
        // TODO: Replace objcopy and lld with a C# linker
        Process.Start("objcopy", "-Ibinary -Oelf64-x86-64 -Bi386 Output/kernel.bin Output/kernel.o");
        Process.Start("ld.lld", "-melf_x86_64 -Tlinker.ld -oOutput/kernel.elf Output/kernel.o").WaitForExit();
        //Process.Start("ld.lld", "-Ttext=0x100000 -melf_x86_64 -o Output/kernel.elf Output/kernel.bin").WaitForExit();
    }

    public override void PushVariable(int index, object obj) => Builder.AppendLine($"mov qword [r8+{index * PointerSize}],{obj}");

    public override void PopVariable(int index, object obj) => Builder.AppendLine($"mov {obj},qword [r8+{index * 8}]");

    public override void Peek(object obj) => Builder.AppendLine($"mov {obj},qword [rbp+{StackIndex}]");

    public override void Push(object obj)
    {
        StackIndex += PointerSize;
        var index = StackIndex;
        Builder.AppendLine($"mov qword [rbp+{index}],{obj}");
    }

    public override void Pop(object obj)
    {
        var index = StackIndex;
        StackIndex -= PointerSize;
        Builder.AppendLine($"mov {obj},qword [rbp+{index}]");
    }
}