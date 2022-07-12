unsafe
{
    /*byte color = 0;
    for (var i = 0; i < 16; i++)
        color++;

    if (color != 15)
        color = 15;*/
    byte color = 15;
    
    *(byte*)0xB8000 = (byte)'H';
    *(byte*)0xB8001 = color--;
    *(byte*)0xB8002 = (byte)'i';
    *(byte*)0xB8003 = ++color;
}