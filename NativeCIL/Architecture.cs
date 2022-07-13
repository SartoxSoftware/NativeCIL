using System.Text;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace NativeCIL;

public abstract class Architecture
{
    protected StringBuilder Builder;
    protected ModuleDefMD Module;
    protected int StackIndex;

    public string OutputPath;

    public string AssemblyName => Module.Assembly.Name;

    public abstract int PointerSize { get; }

    protected Architecture(string path)
    {
        Module = ModuleDefMD.Load(path);
        Builder = new();
    }

    protected static string GetSafeName(string name) => Regex.Replace(name, @"[^0-9a-zA-Z]+", "_");
    protected static string BrLabelName(Instruction ins, MethodDef def, bool create = false) => $"LB_{def.GetHashCode():X4}{(create ? ins.Offset : ((Instruction)ins.Operand).Offset):X4}";
    protected static IEnumerable<Instruction> GetAllBranches(MethodDef method)
    {
        foreach (var br in method.Body.Instructions)
            if (br.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget)
                yield return br;
    }

    public abstract void Initialize();
    public abstract void Compile();
    public abstract void Assemble();
    public abstract void Link();

    public abstract void PushIndex(int index, object obj, string reg);
    public abstract void PopIndex(int index, object obj, string reg);

    public abstract void PushString(string str, object obj);
    public abstract void PopString(string str, object obj);

    public abstract void Peek(object obj);
    public abstract void Push(object obj);
    public abstract void Pop(object obj);
}