Cycle('A');
for (;;);

void Cycle(char c)
{
    unsafe
    {
        // Cycle through all foreground colors
        var start = (byte)c;
        var index = 0;
        var address = 0xB8000;
        for (byte i = 1; i < 15; i++)
        {
            *(byte*)(address + index++) = start++;
            *(byte*)(address + index++) = i;
        }
    }
}