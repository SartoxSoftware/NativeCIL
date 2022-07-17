using System;

namespace TestProject;

public static class Program
{
    public static void Main(string[] _)
    {
        Console.Write('H');
        Console.Write('e');
        Console.Write('l');
        Console.Write('l');
        Console.Write('o');
        Console.Write(' ');
        Console.Write('W');
        Console.Write('o');
        Console.Write('r');
        Console.Write('l');
        Console.Write('d');
        Console.Write('!');
        Console.Write('\r');
        Console.Write('\n');

        long i8 = 5;
        i8 += 2;

        var idx = 16;
        idx |= 2;
        idx ^= 4;
        idx <<= 3;
        idx *= 2;
        var test = (ulong)idx;
        var finalIndex = (int)test;
        Cycle('A', 0xFF, finalIndex - 352);

        for (;;) ;
    }

    /// <summary>
    /// Cycle through all characters and colors
    /// </summary>
    /// <param name="c">Start character</param>
    /// <param name="end">End character</param>
    /// <param name="index">Start index</param>
    private static void Cycle(char c, byte end, int index)
    {
        var start = (byte)c;
        var fore = true;
        var back = false;
        for (var i = index; i < end; i++)
        {
            if (fore)
                Console.ForeGround++;
            if (back)
                Console.BackGround++;
            if (Console.ForeGround == ConsoleColor.White)
            {
                Console.ForeGround = 0;
                fore = false;
                back = true;
            }
            Console.Write((char)start++);
        }
    }
}