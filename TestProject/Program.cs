namespace TestProject;

class Program
{
    public static void Main(string[] _)
    {
        Setup.Initialize();
        Cycle('A', 0xFF, 0);
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