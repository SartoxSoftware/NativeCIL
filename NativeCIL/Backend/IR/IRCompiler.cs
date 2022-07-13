using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static NativeCIL.Backend.IR.IROpCode;
using static NativeCIL.Backend.IR.IRRegister;

namespace NativeCIL.Backend.IR;

public class IRCompiler
{
    private ModuleDefMD _module;
    private int _stackIndex;

    public readonly int PointerSize;
    public readonly List<IRInstruction> Instructions;

    public IRCompiler(string path, int pointerSize)
    {
        _module = ModuleDefMD.Load(path);
        _stackIndex = 0;
        PointerSize = pointerSize;
        Instructions = new();
    }

    public void Compile()
    {
        foreach (var type in _module.Types)
        {
            // Initialize static fields
            foreach (var field in type.Fields)
            {
                if (!field.IsStatic)
                    continue;

                AddInstruction(Label, IRFlag.Qword, GetSafeName(field.Name), field.HasConstant ? field.Constant.Value : 0);
            }

            // Compile methods
            foreach (var method in type.Methods)
            {
                if (method.IsConstructor)
                    continue;

                if (method.IsStaticConstructor)
                {
                    AddInstruction(Call, IRFlag.Label, GetSafeName(method.FullName));
                    continue;
                }

                var branches = GetAllBranches(method).ToList();
                AddInstruction(Label, -1, GetSafeName(method.FullName));

                foreach (var inst in method.Body.Instructions)
                {
                    foreach (var branch in branches)
                        if (((Instruction)branch.Operand).Offset == inst.Offset)
                        {
                            AddInstruction(Label, -1, BrLabelName(inst, method, true));
                            break;
                        }

                    AddInstruction(Comment, -1, inst.OpCode);
                    switch (inst.OpCode.Code)
                    {
                        case Code.Nop: AddInstruction(Nop); break;
                        case Code.Ret: AddInstruction(Ret); break;

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
                            Pop(R1);
                            AddInstruction(And, IRFlag.Dword | IRFlag.DestRegister | IRFlag.Immediate, R1, 0xFFFFFFFF);
                            Push(R1);
                            break;

                        case Code.Conv_U1:
                        case Code.Conv_I1:
                            Pop(R1);
                            AddInstruction(And, IRFlag.Byte | IRFlag.DestRegister | IRFlag.Immediate, R1, 0xFF);
                            Push(R1);
                            break;

                        case Code.Stind_I1:
                            Pop(R1); // Value
                            Pop(R2); // Address
                            AddInstruction(Mov, IRFlag.DestRegister | IRFlag.DestPointer, R2, R1);
                            //Builder.AppendLine("mov [rbx],al");
                            break;

                        case Code.Add:
                            Pop(R1);
                            Pop(R2);
                            AddInstruction(Add, IRFlag.DestRegister | IRFlag.SrcRegister, R2, R1);
                            Push(R2);
                            break;

                        case Code.Sub:
                            Pop(R1);
                            Pop(R2);
                            AddInstruction(Sub, IRFlag.DestRegister | IRFlag.SrcRegister, R2, R1);
                            Push(R2);
                            break;

                        case Code.Or:
                            Pop(R1);
                            Pop(R2);
                            AddInstruction(Or, IRFlag.DestRegister | IRFlag.SrcRegister, R2, R1);
                            Push(R2);
                            break;

                        case Code.Xor:
                            Pop(R1);
                            Pop(R2);
                            AddInstruction(Xor, IRFlag.DestRegister | IRFlag.SrcRegister, R2, R1);
                            Push(R2);
                            break;

                        case Code.Ldloc_0:
                            PopIndex(0, R1, R3);
                            Push(R1);
                            break;
                        case Code.Ldloc_1:
                            PopIndex(1, R1, R3);
                            Push(R1);
                            break;
                        case Code.Ldloc_2:
                            PopIndex(2, R1, R3);
                            Push(R1);
                            break;
                        case Code.Ldloc_3:
                            PopIndex(3, R1, R3);
                            Push(R1);
                            break;

                        case Code.Ldloc_S:
                        case Code.Ldloc:
                            PopIndex(inst.Operand is Local o ? o.Index : Convert.ToInt32(inst.Operand), R1, R3);
                            Push(R1);
                            break;

                        case Code.Stloc_0:
                            Pop(R1);
                            PushIndex(0, R1, R3);
                            break;
                        case Code.Stloc_1:
                            Pop(R1);
                            PushIndex(1, R1, R3);
                            break;
                        case Code.Stloc_2:
                            Pop(R1);
                            PushIndex(2, R1, R3);
                            break;
                        case Code.Stloc_3:
                            Pop(R1);
                            PushIndex(3, R1, R3);
                            break;

                        case Code.Stloc_S:
                        case Code.Stloc:
                            Pop(R1);
                            PushIndex(inst.Operand is Local u ? u.Index : Convert.ToInt32(inst.Operand), R1, R3);
                            break;

                        case Code.Dup:
                            Peek(R1);
                            Push(R1);
                            break;

                        case Code.Br_S:
                        case Code.Br:
                            AddInstruction(Jmp, IRFlag.Label, BrLabelName(inst, method));
                            break;

                        case Code.Brtrue_S:
                        case Code.Brtrue:
                            Pop(R1);
                            AddInstruction(Cmp, IRFlag.DestRegister | IRFlag.Immediate, R1, 0);
                            AddInstruction(Jmp, IRFlag.Label | IRFlag.NotZero, BrLabelName(inst, method));
                            break;

                        case Code.Brfalse_S:
                        case Code.Brfalse:
                            Pop(R1);
                            AddInstruction(Cmp, IRFlag.DestRegister | IRFlag.Immediate, R1, 0);
                            AddInstruction(Jmp, IRFlag.Label | IRFlag.Zero, BrLabelName(inst, method));
                            break;

                        case Code.Clt:
                            Pop(R1);
                            Pop(R2);
                            AddInstruction(Cmp, IRFlag.DestRegister | IRFlag.SrcRegister, R2, R1);
                            AddInstruction(Set, IRFlag.DestRegister | IRFlag.Less, R2);
                            //Builder.AppendLine("setl bl");
                            Push(R2);
                            break;

                        case Code.Ceq:
                            Pop(R1);
                            Pop(R2);
                            AddInstruction(Cmp, IRFlag.DestRegister | IRFlag.SrcRegister, R2, R1);
                            AddInstruction(Set, IRFlag.DestRegister | IRFlag.Equal, R2);
                            //Builder.AppendLine("sete bl");
                            Push(R2);
                            break;

                        case Code.Call:
                            var meth = (MethodDef)inst.Operand;
                            for (var i = meth.Parameters.Count; i > 0; i--)
                            {
                                Pop(R1);
                                PushIndex(i - 1, R1, R4);
                            }
                            AddInstruction(Call, IRFlag.Label, GetSafeName(meth.FullName));
                            break;

                        case Code.Ldarg_S:
                        case Code.Ldarg:
                            PopIndex(Convert.ToInt32(inst.Operand), R1, R4);
                            Push(R1);
                            break;

                        case Code.Ldarg_0:
                            PopIndex(0, R1, R4);
                            Push(R1);
                            break;
                        case Code.Ldarg_1:
                            PopIndex(1, R1, R4);
                            Push(R1);
                            break;
                        case Code.Ldarg_2:
                            PopIndex(2, R1, R4);
                            Push(R1);
                            break;
                        case Code.Ldarg_3:
                            PopIndex(3, R1, R4);
                            Push(R1);
                            break;

                        case Code.Ldsfld:
                            PopString(GetSafeName(((FieldDef)inst.Operand).Name), R1);
                            Push(R1);
                            break;

                        case Code.Stsfld:
                            Pop(R1);
                            PushString(GetSafeName(((FieldDef)inst.Operand).Name), R1);
                            break;

                        default:
                            Console.WriteLine("Unimplemented opcode: " + inst.OpCode);
                            break;
                    }
                }
            }
        }
    }

    private static string GetSafeName(string name) => Regex.Replace(name, @"[^0-9a-zA-Z]+", "_");
    private static string BrLabelName(Instruction ins, MethodDef def, bool create = false) =>
        $"LB_{def.GetHashCode():X4}{(create ? ins.Offset : ((Instruction)ins.Operand).Offset):X4}";
    private static IEnumerable<Instruction> GetAllBranches(MethodDef method)
    {
        foreach (var br in method.Body.Instructions)
            if (br.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget)
                yield return br;
    }

    private void AddInstruction(IROpCode opCode, int flags = 0, object? operand1 = null, object? operand2 = null) =>
        Instructions.Add(new IRInstruction(opCode, flags, operand1, operand2));

    private void PushIndex(int index, object obj, IRRegister reg) =>
        AddInstruction(Mov, IRFlag.DestRegister | IRFlag.DestPointer | (PointerSize == 8 ? IRFlag.Qword :
            IRFlag.Dword), reg + index * PointerSize, obj);

    private void PopIndex(int index, IRRegister dst, IRRegister src) =>
        AddInstruction(Mov,
            IRFlag.DestRegister | IRFlag.SrcRegister | IRFlag.SrcPointer |
            (PointerSize == 8 ? IRFlag.Qword : IRFlag.Dword), dst, src + index * PointerSize);

    private void PushString(string str, IRRegister reg) =>
        AddInstruction(Mov, IRFlag.DestPointer | IRFlag.Label | IRFlag.SrcRegister, str, reg);

    private void PopString(string str, IRRegister reg) =>
        AddInstruction(Mov, IRFlag.SrcPointer | IRFlag.Label | IRFlag.DestRegister, reg, str);

    private void Peek(IRRegister reg)
        => AddInstruction(Mov, IRFlag.DestRegister | IRFlag.SrcRegister | IRFlag.SrcPointer | (PointerSize ==
            8 ? IRFlag.Qword : IRFlag.Dword), reg, R0 + _stackIndex);

    private void Push(object imm)
    {
        _stackIndex += PointerSize;
        var index = _stackIndex;
        AddInstruction(Mov, IRFlag.DestRegister | IRFlag.DestPointer | (PointerSize == 8 ? IRFlag.Qword :
            IRFlag.Dword) | IRFlag.Immediate, R0 + index, imm);
    }

    private void Push(IRRegister reg)
    {
        _stackIndex += PointerSize;
        var index = _stackIndex;
        AddInstruction(Mov, IRFlag.DestRegister | IRFlag.DestPointer | IRFlag.SrcRegister | (PointerSize == 8
            ? IRFlag.Qword : IRFlag.Dword), R0 + index, reg);
    }

    private void Pop(IRRegister reg)
    {
        var index = _stackIndex;
        _stackIndex -= PointerSize;
        AddInstruction(Mov, IRFlag.DestRegister | IRFlag.SrcRegister | IRFlag.SrcPointer | (PointerSize == 8
            ? IRFlag.Qword : IRFlag.Dword), reg, R0 + index);
    }
}