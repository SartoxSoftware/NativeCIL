namespace TestProject;

public static class Native
{
    public static extern byte In8(ushort port);
    public static extern ushort In16(ushort port);
    public static extern uint In32(ushort port);
    public static extern void Out8(ushort port, byte value);
    public static extern void Out16(ushort port, ushort value);
    public static extern void Out32(ushort port, uint value);
}