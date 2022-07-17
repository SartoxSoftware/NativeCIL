namespace System;
 
public static unsafe class Console
{
    public static ConsoleColor ForeGround => ConsoleColor.White;
    public static ConsoleColor BackGround => ConsoleColor.Black;
    public static ushort* Buffer => (ushort*)0xB8000;
    public static int Height => 25;
    public static int Width => 80;
    public static int X { get; set; }
    public static int Y { get; set; }
    
    private static ushort Convert(char C)
    {
        return (ushort)(C | (ushort)(((byte)ForeGround | ((byte)BackGround << 4)) << 8));
    }

    public static void Write(char C)
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

        if (C == '\n')
        {
            Y++;
            return;
        }
        if (C == '\t')
        {
            X += 4;
        }
        
        Buffer[Y * Width + X] = Convert(C);
        X++;
    }
}