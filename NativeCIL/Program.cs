using System.Diagnostics;
using System.Reflection;
using System.Text;
using DiscUtils.Iso9660;
using NativeCIL;
using NativeCIL.Base;
using NativeCIL.IR;
using NativeCIL.IR.Amd64;
using NativeCIL.IR.I386;

var watch = new Stopwatch();
var settings = new Settings(args);
var ir = new IRCompiler(ref settings);

Compiler compiler = settings.Architecture switch
{
    TargetArchitecture.Amd64 => new Amd64Compiler(ref ir),
    TargetArchitecture.I386 => new I386Compiler(ref ir),
    _ => throw new Exception("Unsupported architecture!")
};

watch.Start();

ir.Compile();

watch.Stop();
Console.WriteLine($"Finished compiling to IR! Took {watch.ElapsedMilliseconds} ms.");
watch.Restart();

compiler.Initialize();
compiler.Compile();

watch.Stop();
Console.WriteLine($"Finished compiling to native code! Took {watch.ElapsedMilliseconds} ms.");

if (settings.Format == Format.Elf)
{
    watch.Restart();
    compiler.Link();
    watch.Stop();
    Console.WriteLine($"Finished linking! Took {watch.ElapsedMilliseconds} ms.");
}

switch (settings.ImageType)
{
    case ImageType.None:
        File.WriteAllBytes(compiler.OutputPath, compiler.OutputStream.ToArray());
        compiler.OutputStream.Close();
        return;

    case ImageType.Iso when settings.Format == Format.Bin:
        throw new Exception("Raw binaries cannot be used with Limine!");

    case ImageType.Iso:
    {
        watch.Restart();

        var assembly = Assembly.GetExecutingAssembly();
        using var cd = assembly.GetManifestResourceStream("NativeCIL.limine-cd.bin");
        using var sys = assembly.GetManifestResourceStream("NativeCIL.limine.sys");

        var iso = new CDBuilder
        {
            UseJoliet = true,
            VolumeIdentifier = ir.AssemblyName,
            UpdateIsolinuxBootTable = true
        };
        iso.AddFile("limine.sys", sys);
        iso.AddFile("limine.cfg", Encoding.ASCII.GetBytes($"TIMEOUT=0\n:{ir.AssemblyName}\nPROTOCOL=multiboot2\nKERNEL_PATH=boot:///kernel.elf"));
        iso.AddFile("kernel.elf", compiler.OutputStream);
        iso.SetBootImage(cd, BootDeviceEmulation.NoEmulation, 0);
        iso.Build(settings.OutputFile);

        Utils.StartSilent("limine-deploy", "--force-mbr " + settings.OutputFile);

        watch.Stop();
        Console.WriteLine($"Finished making the bootable image! Took {watch.ElapsedMilliseconds} ms.");
        break;
    }
}

compiler.OutputStream.Close();