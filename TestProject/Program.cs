unsafe
{
    byte color = 0;
    for (var i = 1; i < 15; i++)
        color++;

    if (color != 15)
        color = 15;

    if (color == 15)
    {
        *(byte*)0xB8004 = (byte)'a';
        *(byte*)0xB8005 = 12;
    }
    
    *(byte*)0xB8000 = (byte)'H';
    *(byte*)0xB8001 = color--;
    *(byte*)0xB8002 = (byte)'i';
    *(byte*)0xB8003 = ++color;
}