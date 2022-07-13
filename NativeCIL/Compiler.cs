using System.Text;
using NativeCIL.IR;

namespace NativeCIL;

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

    public abstract void AddHeader(bool bootableImage);
    public abstract void Compile();
    public abstract void Link();
}