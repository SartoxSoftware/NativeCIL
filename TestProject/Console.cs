namespace System;
 
public static unsafe class Console
{
    public static ConsoleColor ForeGround { get; set; } = ConsoleColor.White;
    public static ConsoleColor BackGround { get; set; } = ConsoleColor.Black;
    public static ushort* Buffer => (ushort*)0xB8000;
    public static int Height => 25;
    public static int Width => 80;
    public static int X { get; set; }
    public static int Y { get; set; }

    public static void Write(char c)
    {
        if (X >= Width)
        {
            X -= Width;
            Y++;
        }
        if (Y >= Height)
        {
            Y--;
            for (int I = Width; I < Width * (Height - 1); I++)
            {
                Buffer[I - Width] = Buffer[I];
            }
        }

        if (c == '\r')
        {
            X = 0;
            return;
        }
        if (c == '\n')
        {
            Y++;
            return;
        }
        if (c == '\t')
        {
            X += 4;
        }
        
        Buffer[Y * Width + X] = (ushort)(c | (ushort)(((byte)ForeGround | ((byte)BackGround << 4)) << 8));
        X++;
    }
}