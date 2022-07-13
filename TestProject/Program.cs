namespace TestProject;

public static class Program
{
    public static void Main(string[] _)
    {
        Setup.Initialize();

        long i8 = 5;
        i8 += 2;

        var idx = 16;
        idx |= 2;
        idx ^= 4;
        var test = (ulong)idx;
        var finalIndex = (int)test;
        Cycle('A', 0xFF, finalIndex - 22);
        for (;;);
    }

    private static void Cycle(char c, byte end, int index)
    {
        unsafe
        {
            // Cycle through all characters and colors
            var start = (byte)c;
            for (var i = index; i < end; i++)
            {
                *(byte*)Setup.VgaAddress++ = start++;
                *(byte*)Setup.VgaAddress++ = Setup.DefaultColor++;
            }
        }
    }
}