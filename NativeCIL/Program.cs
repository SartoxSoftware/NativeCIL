using System.Diagnostics;
using System.Text;
using DiscUtils.Iso9660;
using NativeCIL.Amd64;

var arch = new Amd64Architecture(args[0]);
arch.Initialize();
arch.Compile();
arch.Assemble();
arch.Link();

using var cd = File.OpenRead("limine/limine-cd.bin");
using var sys = File.OpenRead("limine/limine.sys");
using var kernel = File.OpenRead("Output/kernel.elf");

var iso = new CDBuilder
{
    UseJoliet = true,
    VolumeIdentifier = arch.AssemblyName,
    UpdateIsolinuxBootTable = true
};
iso.AddFile("limine.sys", sys);
iso.AddFile("limine.cfg", Encoding.ASCII.GetBytes($"TIMEOUT=0\n:{arch.AssemblyName}\nPROTOCOL=multiboot1\nKERNEL_PATH=boot:///kernel.elf"));
iso.AddFile("kernel.elf", kernel);
iso.SetBootImage(cd, BootDeviceEmulation.NoEmulation, 0);
iso.Build("Output/output.iso");

Process.Start("limine/limine-deploy", "--force-mbr Output/output.iso").WaitForExit();
Process.Start("qemu-system-x86_64", "-m 1G -cpu max -cdrom Output/output.iso -enable-kvm");