using System.Diagnostics;
using System.Text;
using DiscUtils.Iso9660;
using NativeCIL;
using NativeCIL.IR;
using NativeCIL.IR.Amd64;

var watch = new Stopwatch();
var settings = new Settings(args);
var ir = new IRCompiler(ref settings);
var compiler = new Amd64Compiler(ref ir);

watch.Start();
ir.Compile();

compiler.Initialize();
compiler.Compile();
if (settings.Format == Format.Elf)
    compiler.Link();

if (settings.ImageType == ImageType.Iso)
{
    if (settings.Format == Format.Bin)
        throw new Exception("Raw binaries cannot be used with Limine!");

    using var cd = File.OpenRead("Limine/limine-cd.bin");
    using var sys = File.OpenRead("Limine/limine.sys");
    using var kernel = File.OpenRead(compiler.OutputPath);

    var iso = new CDBuilder
    {
        UseJoliet = true,
        VolumeIdentifier = ir.AssemblyName,
        UpdateIsolinuxBootTable = true
    };
    iso.AddFile("limine.sys", sys);
    iso.AddFile("limine.cfg", Encoding.ASCII.GetBytes($"TIMEOUT=0\n:{ir.AssemblyName}\nPROTOCOL=multiboot2\nKERNEL_PATH=boot:///kernel.elf"));
    iso.AddFile("kernel.elf", kernel);
    iso.SetBootImage(cd, BootDeviceEmulation.NoEmulation, 0);
    iso.Build(settings.OutputFile);

    Process.Start("Limine/limine-deploy", "--force-mbr " + settings.OutputFile).WaitForExit();
}

watch.Stop();
Console.WriteLine($"Finished! Took {watch.ElapsedMilliseconds} ms.");