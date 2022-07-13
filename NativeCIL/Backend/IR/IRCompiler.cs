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
                AddInstruction(Label, 0, GetSafeName(method.FullName));

                foreach (var inst in method.Body.Instructions)
                {
                    foreach (var branch in branches)
                        if (((Instruction)branch.Operand).Offset == inst.Offset)
                        {
                            AddInstruction(Label, 0, BrLabelName(inst, method, true));
                            break;
                        }

                    AddInstruction(Comment, 0, inst.OpCode);
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
                    }
                }
            }
        }
    }

    private static string GetSafeName(string name) => Regex.Replace(name, @"[^0-9a-zA-Z]+", "_");
    private static string BrLabelName(Instruction ins, MethodDef def, bool create = false) => $"LB_{def.GetHashCode():X4}{(create ? ins.Offset : ((Instruction)ins.Operand).Offset):X4}";
    private static IEnumerable<Instruction> GetAllBranches(MethodDef method)
    {
        foreach (var br in method.Body.Instructions)
            if (br.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget)
                yield return br;
    }

    private void AddInstruction(IROpCode opCode, int flags = 0, object? operand1 = null, object? operand2 = null) =>
        Instructions.Add(new IRInstruction(opCode, flags, operand1, operand2));

    private void Peek(IRRegister reg)
        => AddInstruction(Mov, IRFlag.DestRegister | IRFlag.SrcRegister | IRFlag.SrcPointer | (PointerSize == 8 ? IRFlag.Qword : IRFlag.Dword), reg, R0 + _stackIndex);

    private void Push(object imm)
    {
        _stackIndex += PointerSize;
        var index = _stackIndex;
        AddInstruction(Mov, IRFlag.DestRegister | IRFlag.DestPointer | (PointerSize == 8 ? IRFlag.Qword : IRFlag.Dword) | IRFlag.Immediate, R0 + index, imm);
    }

    private void Push(IRRegister reg)
    {
        _stackIndex += PointerSize;
        var index = _stackIndex;
        AddInstruction(Mov, IRFlag.DestRegister | IRFlag.DestPointer | IRFlag.SrcRegister | (PointerSize == 8 ? IRFlag.Qword : IRFlag.Dword), R0 + index, reg);
    }

    private void Pop(IRRegister reg)
    {
        var index = _stackIndex;
        _stackIndex -= PointerSize;
        AddInstruction(Mov, IRFlag.DestRegister | IRFlag.SrcRegister | IRFlag.SrcPointer | (PointerSize == 8 ? IRFlag.Qword : IRFlag.Dword), reg, R0 + index);
    }
}