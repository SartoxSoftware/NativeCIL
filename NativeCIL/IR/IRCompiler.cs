using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static NativeCIL.IR.OpCode;
using OperandType = dnlib.DotNet.Emit.OperandType;

namespace NativeCIL.IR;

public class IRCompiler
{
    private int _curField;
    
    private readonly ModuleDefMD _module;
    private readonly List<FieldDef> _fields;

    public readonly Builder Builder;
    public readonly Settings Settings;
    public readonly string AssemblyName;
    
    public IRCompiler(ref Settings settings)
    {
        _module = ModuleDefMD.Load(settings.InputFile);
        _fields = new();

        Settings = settings;
        Builder = new();
        AssemblyName = GetSafeName(_module.Assembly.Name);
    }

    public void Compile()
    {
        foreach (var type in _module.Types)
        {
            foreach (var field in type.Fields)
                if (field.IsStatic)
                {
                    if (field.HasConstant)
                        Builder.Inst(Mov, new Register(_curField, 2), field.Constant.Value);
                    _curField++;
                    _fields.Add(field);
                }

            foreach (var method in type.Methods)
                if (method.IsConstructor || method.IsStaticConstructor)
                {
                    Builder.Inst(Mov, new Register(0, 1), 0);
                    Builder.Inst(Call, GetSafeName(method.FullName));
                }
        }

        Builder.Inst(Call, GetSafeName(_module.EntryPoint.FullName));

        foreach (var type in _module.Types)
        {
            foreach (var method in type.Methods)
            {
                Builder.Inst(Func, GetSafeName(method.FullName));
                
                var branches = GetAllBranches(method).ToList();
                foreach (var inst in method.Body.Instructions)
                {
                    foreach (var branch in branches)
                        if (((dnlib.DotNet.Emit.Instruction)branch.Operand).Offset == inst.Offset)
                        {
                            Builder.Inst(Label, BrLabelName(inst, method, true));
                            break;
                        }

                    switch (inst.OpCode.Code)
                    {
                        case Code.Nop: break;
                        case Code.Ret: Builder.Inst(Ret); break;
                        case Code.Dup: Builder.Inst(Dup); break;
                        case Code.Pop: Builder.Inst(Popd); break;

                        case Code.Add: Builder.Inst(Add); break;
                        case Code.And: Builder.Inst(And); break;
                        case Code.Sub: Builder.Inst(Sub); break;
                        case Code.Mul: Builder.Inst(Mul); break;
                        case Code.Div: Builder.Inst(Div); break;
                        case Code.Or: Builder.Inst(Or); break;
                        case Code.Xor: Builder.Inst(Xor); break;
                        case Code.Shl: Builder.Inst(Shl); break;
                        case Code.Shr: Builder.Inst(Shr); break;

                        case Code.Br_S:
                        case Code.Br:
                            Builder.Inst(Jmp, BrLabelName(inst, method));
                            break;
                        
                        case Code.Brfalse_S:
                        case Code.Brfalse:
                            Builder.Inst(Jz, BrLabelName(inst, method));
                            break;
                        
                        case Code.Brtrue_S:
                        case Code.Brtrue:
                            Builder.Inst(Jnz, BrLabelName(inst, method));
                            break;
                        
                        case Code.Blt_Un_S:
                        case Code.Blt_Un:
                        case Code.Blt_S:
                        case Code.Blt:
                            Builder.Inst(Jb, BrLabelName(inst, method));
                            break;
                        
                        case Code.Bne_Un_S:
                        case Code.Bne_Un:
                            Builder.Inst(Jne, BrLabelName(inst, method));
                            break;
                        
                        case Code.Clt_Un:
                        case Code.Clt:
                            Builder.Inst(Pushl);
                            break;
                        
                        case Code.Ceq:
                            Builder.Inst(Pushe);
                            break;

                        case Code.Stloc_0: Builder.Inst(Pop, new Register(0)); break;
                        case Code.Stloc_1: Builder.Inst(Pop, new Register(1)); break;
                        case Code.Stloc_2: Builder.Inst(Pop, new Register(2)); break;
                        case Code.Stloc_3: Builder.Inst(Pop, new Register(3)); break;

                        case Code.Stloc_S:
                        case Code.Stloc:
                        {
                            Builder.Inst(Pop, new Register(inst.Operand is Local l ? l.Index : Convert.ToUInt16(inst.Operand)));
                            break;
                        }

                        case Code.Ldloc_0: Builder.Inst(Push, new Register(0)); break;
                        case Code.Ldloc_1: Builder.Inst(Push, new Register(1)); break;
                        case Code.Ldloc_2: Builder.Inst(Push, new Register(2)); break;
                        case Code.Ldloc_3: Builder.Inst(Push, new Register(3)); break;

                        case Code.Ldloc_S:
                        case Code.Ldloc:
                        {
                            Builder.Inst(Push, new Register(inst.Operand is Local l ? l.Index : Convert.ToUInt16(inst.Operand)));
                            break;
                        }
                        
                        case Code.Ldc_I4_0: Builder.Inst(Push, 0); break;
                        case Code.Ldc_I4_1: Builder.Inst(Push, 1); break;
                        case Code.Ldc_I4_2: Builder.Inst(Push, 2); break;
                        case Code.Ldc_I4_3: Builder.Inst(Push, 3); break;
                        case Code.Ldc_I4_4: Builder.Inst(Push, 4); break;
                        case Code.Ldc_I4_5: Builder.Inst(Push, 5); break;
                        case Code.Ldc_I4_6: Builder.Inst(Push, 6); break;
                        case Code.Ldc_I4_7: Builder.Inst(Push, 7); break;
                        case Code.Ldc_I4_8: Builder.Inst(Push, 8); break;
                        case Code.Ldc_I4_M1: Builder.Inst(Push, -1); break;
                        
                        case Code.Ldc_I4_S:
                        case Code.Ldc_I4:
                            Builder.Inst(Push, Convert.ToInt32(inst.Operand));
                            break;
                        
                        case Code.Ldarg_0: Builder.Inst(Push, new Register(0, 1)); break;
                        case Code.Ldarg_1: Builder.Inst(Push, new Register(1, 1)); break;
                        case Code.Ldarg_2: Builder.Inst(Push, new Register(2, 1)); break;
                        case Code.Ldarg_3: Builder.Inst(Push, new Register(3, 1)); break;
                        
                        case Code.Ldarg_S:
                        case Code.Ldarg:
                            Builder.Inst(Push, new Register(Convert.ToInt32(inst.Operand), 1));
                            break;
                        
                        case Code.Conv_I4:
                        case Code.Conv_I:
                        case Code.Conv_U4:
                        case Code.Conv_U:
                            Builder.Inst(And, 0xFFFFFFFF);
                            break;

                        case Code.Conv_I1:
                        case Code.Conv_U1:
                            Builder.Inst(And, 0xFF);
                            break;

                        case Code.Conv_I2:
                        case Code.Conv_U2:
                            Builder.Inst(And, 0xFFFF);
                            break;

                        case Code.Conv_I8:
                        case Code.Conv_U8:
                            Builder.Inst(And, 0xFFFFFFFFFFFFFFF);
                            break;

                        case Code.Stind_I1:
                        case Code.Stind_I2:
                        case Code.Stind_I4:
                        case Code.Stind_I8:
                            Builder.Inst(Memstore);
                            break;

                        case Code.Ldind_I1:
                        case Code.Ldind_U1:
                        case Code.Ldind_I2:
                        case Code.Ldind_U2:
                        case Code.Ldind_I4:
                        case Code.Ldind_U4:
                            Builder.Inst(Memload);
                            goto case Code.Conv_I4;

                        case Code.Ldind_I8:
                            Builder.Inst(Memload);
                            goto case Code.Conv_I8;
                            
                        case Code.Ldsfld:
                            Builder.Inst(Push, new Register(_fields.IndexOf((FieldDef)inst.Operand), 2));
                            break;
                        
                        case Code.Stsfld:
                            Builder.Inst(Pop, new Register(_fields.IndexOf((FieldDef)inst.Operand), 2));
                            break;

                        case Code.Call:
                            var meth = (MethodDef)inst.Operand;
                            for (var i = meth.Parameters.Count; i > 0; i--)
                                Builder.Inst(Pop, new Register(i - 1, 1));
                            Builder.Inst(Call, GetSafeName(meth.FullName));
                            break;
                        
                        default: Console.WriteLine("Unimplemented opcode: " + inst.OpCode); break;
                    }
                }
            }}
    }
    
    private static string BrLabelName(dnlib.DotNet.Emit.Instruction ins, MethodDef def, bool create = false) =>
        $"LB_{def.GetHashCode():X4}{(create ? ins.Offset : ((dnlib.DotNet.Emit.Instruction)ins.Operand).Offset):X4}";

    private static IEnumerable<dnlib.DotNet.Emit.Instruction> GetAllBranches(MethodDef method)
    {
        foreach (var br in method.Body.Instructions)
            if (br.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget)
                yield return br;
    }

    private static string GetSafeName(string name) => Regex.Replace(name, @"[^0-9a-zA-Z]+", "_");
}