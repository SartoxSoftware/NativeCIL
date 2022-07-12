unsafe
{
    byte color = 7;
    color += 8;
    
    *(byte*)0xB8000 = (byte)'H';
    *(byte*)0xB8001 = color--;
    *(byte*)0xB8002 = (byte)'i';
    *(byte*)0xB8003 = ++color;
}