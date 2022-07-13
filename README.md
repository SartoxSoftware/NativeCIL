# Introduction
NativeCIL is, as its name suggests, a CIL to native x86 compiler. It's a [CS2ASM](https://github.com/nifanfa/CS2ASM) rewrite, which is meant to be faster and more efficient and stable.

# Dependencies
- nasm
- objcopy
- lld

# Usage
``NativeCIL --architecture <amd64> --format <bin,elf> --image <none,iso> --output <output> <input>``

``NativeCIL -c<amd64> -f<bin,elf> -t<none,iso> -o<output> <input>``

For example:

``./NativeCIL -camd64 -felf -tiso -ooutput.iso TestProject.dll``

You can now try to run on bare metal, or via QEMU:

``qemu-system-x86_64 -cdrom output.iso -cpu max -m 1G -enable-kvm -serial stdio``

**You need to have LLVM on your PATH to execute the ELF linker, LLD!**

**Be sure to remove the -enable-kvm argument if you're not on Linux!**