using System.Text;
using NativeCIL.IR;

namespace NativeCIL.Base;

public abstract class Compiler
{
    protected readonly IRCompiler IRCompiler;
    protected readonly StringBuilder Builder;

    public string OutputPath;

    public Compiler(ref IRCompiler compiler)
    {
        IRCompiler = compiler;
        Builder = new StringBuilder();
    }

    public abstract void Initialize();
    public abstract void Compile();
    public abstract void Link();

    protected static bool HasFlag(ref int flags, int flag) => (flags & flag) != 0;
}