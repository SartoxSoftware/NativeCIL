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

        /*Setup.Initialize();

        long i8 = 5;
        i8 += 2;

        var idx = 16;
        idx |= 2;
        idx ^= 4;
        idx <<= 3;
        idx *= 2;
        var test = (ulong)idx;
        var finalIndex = (int)test;
        Cycle('A', 0xFF, finalIndex - 352);*/

        for (;;) ;
    }

    /*private static void Cycle(char c, byte end, int index)
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
    }*/
}