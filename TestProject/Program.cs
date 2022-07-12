namespace TestProject;

class Program
{
    public static void Main(string[] _)
    {
        Setup.Initialize();
        Cycle('A');
        for (;;);
    }

    private static void Cycle(char c)
    {
        unsafe
        {
            // Cycle through all foreground colors, starting from the last one
            var start = (byte)c;
            var index = 0;
            for (var i = 0; i < 16; i++)
            {
                *(byte*)(Setup.VgaAddress + index++) = start++;
                *(byte*)(Setup.VgaAddress + index++) = Setup.DefaultColor++;
            }
        }
    }
}