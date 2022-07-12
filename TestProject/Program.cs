Test();
for (;;);

void Test()
{
    unsafe
    {
        // Cycle through all foreground colors
        var start = (byte)'A';
        var index = 0;
        var address = 0xB8000;
        for (byte i = 1; i < 15; i++)
        {
            *(byte*)(address + index++) = start++;
            *(byte*)(address + index++) = i;
        }
    }
}