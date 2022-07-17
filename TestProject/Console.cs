namespace System;
 
public static unsafe class Console
{
    public static byte* Buffer = (byte*)0xB8000;

    public static void Write(char C)
    {
        *Buffer++ = 65;
        *Buffer++ = 15;
    }
}