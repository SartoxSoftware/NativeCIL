using System.Runtime.InteropServices;

namespace NativeCIL.Linker;

public static unsafe class ELF
{
    private struct Header64
    {
        public fixed byte Identification[16];
        public ushort Type;
        public ushort Architecture;
        public uint Version;
        public ulong EntryPoint;
        public ulong ProgramHeaderOffset;
        public ulong SectionHeaderOffset;
        public uint Flags;
        public ushort HeaderSize;
        public ushort ProgramHeaderEntrySize;
        public ushort ProgramHeaderEntries;
        public ushort SectionHeaderEntrySize;
        public ushort SectionHeaderEntries;
        public ushort SectionHeaderStringTableIndex;
    }

    private struct ProgramHeader64
    {
        public uint Type;
        public uint Flags;
        public ulong Offset;
        public ulong VirtualAddress;
        public ulong PhysicalAddress;
        public ulong FileSize;
        public ulong MemorySize;
        public ulong Alignment;
    }

    public static MemoryStream Link64(string inputPath, string outputPath)
    {
        var code = File.ReadAllBytes(inputPath);
        var length = (ulong)code.Length;
        var stream = new MemoryStream();
        var header = new Header64();
        var programHeader = new ProgramHeader64();
        var size = sizeof(Header64);
        
        header.Identification[0] = 0x7F;
        header.Identification[1] = 0x45; // E
        header.Identification[2] = 0x4C; // L
        header.Identification[3] = 0x46; // F
        header.Identification[4] = 0x02; // File class, 64-bit
        header.Identification[5] = 0x01; // Data encoding, little-endian (LSB)
        header.Identification[6] = 0x01; // File version, current
        header.Identification[7] = 0x00; // OS ABI, none
        header.Identification[8] = 0x00; // ABI version, none
        header.Type = 0x02; // Executable file
        header.Architecture = 0x3E; // AMD64
        header.Version = 0x01; // Current
        header.EntryPoint = 0x00; // None
        header.ProgramHeaderOffset = (ulong)size; // Right after the header
        header.SectionHeaderOffset = 0x00; // None
        header.Flags = 0x00; // None
        header.HeaderSize = (ushort)size;
        header.ProgramHeaderEntrySize = 0x38;
        header.ProgramHeaderEntries = 0x01;
        header.SectionHeaderEntrySize = 0; // None
        header.SectionHeaderEntries = 0; // None
        header.SectionHeaderStringTableIndex = 0; // None

        stream.Write(FromStruct(header));

        programHeader.Type = 0x01; // Load
        programHeader.Flags = 0x00; // None
        programHeader.Offset = (ulong)(size + sizeof(ProgramHeader64)); // Right after the program header
        programHeader.VirtualAddress = 0x00; // None
        programHeader.PhysicalAddress = 0x00; // None
        programHeader.FileSize = length;
        programHeader.MemorySize = length;
        programHeader.Alignment = 0x00; // None

        stream.Write(FromStruct(programHeader));
        stream.Write(code);

        return stream;
    }

    //https://stackoverflow.com/questions/3278827/how-to-convert-a-structure-to-a-byte-array-in-c#3278956
    private static byte[] FromStruct<T>(T str)
    {
        var size = Marshal.SizeOf(str);
        var arr = new byte[size];
        var ptr = IntPtr.Zero;

        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return arr;
    }
}