namespace TestProject;

class Program
{
    public static void Main(string[] _)
    {
        Setup.Initialize();
        var idx = 16;
        idx |= 2;
        Cycle('A', 0xFF, idx - 18);
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