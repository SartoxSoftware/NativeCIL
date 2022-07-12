unsafe
{
    // Cycle through all foreground colors
    var start = (byte)'A';
    var index = 0;
    for (byte i = 1; i < 15; i++)
    {
        *(byte*)(0xB8000 + index++) = start++;
        *(byte*)(0xB8000 + index++) = i;
    }
}
for (;;);