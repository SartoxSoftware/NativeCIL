using System.Text;
using System.Text.RegularExpressions;
using dnlib.DotNet;

namespace NativeCIL;

public abstract class Architecture
{
    protected StringBuilder Builder;
    protected ModuleDefMD Module;
    protected int StackIndex;

    public string AssemblyName => Module.Assembly.Name;

    public abstract int PointerSize { get; }

    public Architecture(string path)
    {
        Module = ModuleDefMD.Load(path);
        Builder = new();
    }

    public string GetSafeName(string name)
    {
        return Regex.Replace(name, @"[^0-9a-zA-Z]+", "_");
    }

    public abstract void Initialize();
    public abstract void Compile();
    public abstract void Assemble();
    public abstract void Link();

    public abstract void PushVariable(int index, object obj);
    public abstract void PopVariable(int index, object obj);

    public abstract void Peek(object obj);
    public abstract void Push(object obj);
    public abstract void Pop(object obj);
}