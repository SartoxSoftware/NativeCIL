ENTRY(_start);

SECTIONS
{
    _start = 0;
    . = 0;
    .text : {
        *(.text)
    }
}