# Introduction
NativeCIL is a CIL bytecode to native code compiler, written in C#. It originated as a better rewrite of [CS2ASM](https://github.com/nifanfa/CS2ASM).
Unlike most other compilers, especially [IL2CPU](https://github.com/CosmosOS/IL2CPU), NativeCIL implements its own IR which will allow for IR optimizations and also having the bytecode compiled to other architectures as well, with not much effort.

# Dependencies
## External
- yasm (for assembling x86 code)

## .NET
- DiscUtils (for making the bootable image)
- dnlib (for reading the CIL bytecode instructions)

# Usage
``NativeCIL --architecture <i386,amd64> --format <bin,elf> --image <none,iso> --output <output> <input>``

``NativeCIL -a<i386,amd64> -f<bin,elf> -t<none,iso> -o<output> <input>``

## Example

``./NativeCIL -aamd64 -felf -tiso -ooutput.iso TestProject.dll``

# Try it out!
After you've compiled your program as a bootable image file, you may want to try it on bare metal now. If that isn't possible, you can try it out in a VM. Here's an example with QEMU:

``qemu-system-x86_64 -cdrom output.iso``
