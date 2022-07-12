namespace TestProject;

public static class Setup
{
    public static byte DefaultColor;
    public static int VgaAddress;

    public static void Initialize()
    {
        DefaultColor = 0;
        VgaAddress = 0xB8000;
    }
}