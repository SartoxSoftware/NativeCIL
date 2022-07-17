using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static NativeCIL.IR.IROpCode;
using static NativeCIL.IR.IRRegister;

namespace NativeCIL.IR;

/// <summary>
/// Experimental CIL to IR compiler.
/// </summary>
public class IRCompilerCIL
{
    private readonly ModuleDefMD _module;
    private readonly int _bitnessFlag;
    private int _stackIndex;

    public string AssemblyName => _module.Assembly.Name;

    public readonly int PointerSize;
    public readonly Settings Settings;
    public readonly List<IRInstruction> Instructions;
    
    public IRCompilerCIL(ref Settings settings)
    {
        _module = ModuleDefMD.Load(settings.InputFile);
        _bitnessFlag = settings.Architecture == TargetArchitecture.Amd64 ? IRFlag.Qword : IRFlag.Dword;
        Settings = settings;
        PointerSize = settings.Architecture == TargetArchitecture.Amd64 ? 8 : 4;
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

                AddInstruction(Label, _bitnessFlag, GetSafeName(field.FullName), field.HasConstant ? field.Constant.Value : 0);
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
                    }
                }
            }
        }
    }

    private void AddInstruction(IROpCode opCode, int flags = 0, object? operand1 = null, object? operand2 = null) =>
        Instructions.Add(new IRInstruction(opCode, flags, operand1, operand2));

    private static string GetSafeName(string name) => Regex.Replace(name, @"[^0-9a-zA-Z]+", "_");

    private static string BrLabelName(Instruction ins, MethodDef def, bool create = false) =>
        $"LB_{def.GetHashCode():X4}{(create ? ins.Offset : ((Instruction)ins.Operand).Offset):X4}";

    private static IEnumerable<Instruction> GetAllBranches(MethodDef method)
    {
        foreach (var br in method.Body.Instructions)
            if (br.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget)
                yield return br;
    }
}