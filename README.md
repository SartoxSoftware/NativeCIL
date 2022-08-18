# Introduction
NativeCIL is, as its name suggests, a CIL to native x86 compiler. It's a [CS2ASM](https://github.com/nifanfa/CS2ASM) rewrite, which is meant to be faster and more efficient and stable.

# Dependencies
## External
- yasm (for assembling x86 code)

## .NET
- DiscUtils (for making the bootable image)
- dnlib (for reading the CIL instructions)

# Usage
``NativeCIL --architecture <amd64> --format <bin,elf> --image <none,iso> --output <output> <input>``

``NativeCIL -a<amd64> -f<bin,elf> -t<none,iso> -o<output> <input>``

For example:

``./NativeCIL -aamd64 -felf -tiso -ooutput.iso TestProject.dll``

You can now try to run on bare metal, or via QEMU:

``qemu-system-x86_64 -cdrom output.iso -cpu max -m 1G -enable-kvm -serial stdio``

**Be sure to remove the -enable-kvm argument if you're not on Linux!**