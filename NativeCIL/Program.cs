using System.Diagnostics;
using System.Reflection;
using System.Text;
using DiscUtils.Iso9660;
using NativeCIL;
using NativeCIL.IR;
using NativeCIL.IR.Amd64;

var watch = new Stopwatch();
var settings = new Settings(args);
var ir = new IRCompiler(ref settings);

Compiler compiler = settings.Architecture switch
{
    TargetArchitecture.Amd64 => new Amd64Compiler(ref ir),
    _ => throw new Exception("i386 is not supported yet!")
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
watch.Restart();

if (settings.Format == Format.Elf)
    compiler.Link();

watch.Stop();
Console.WriteLine($"Finished linking! Took {watch.ElapsedMilliseconds} ms.");
watch.Restart();

if (settings.ImageType == ImageType.Iso)
{
    if (settings.Format == Format.Bin)
        throw new Exception("Raw binaries cannot be used with Limine!");

    var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    using var cd = File.OpenRead(Path.Combine(path, "Limine/limine-cd.bin"));
    using var sys = File.OpenRead(Path.Combine(path, "Limine/limine.sys"));
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

    Utils.StartSilent("Limine/limine-deploy", "--force-mbr " + settings.OutputFile);
}

watch.Stop();
Console.WriteLine($"Finished making the bootable image! Took {watch.ElapsedMilliseconds} ms.");