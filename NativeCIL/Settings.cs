namespace NativeCIL;

public enum TargetArchitecture { Amd64, I386 }
public enum Format { Bin, Elf }
public enum ImageType { None, Iso }

public class Settings
{
    public readonly string InputFile, OutputFile;
    public readonly TargetArchitecture Architecture;
    public readonly Format Format;
    public readonly ImageType ImageType;

    public Settings(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg[0] != '-')
            {
                InputFile = arg;
                continue;
            }

            string param, argument;
            if (arg.StartsWith("--"))
            {
                param = arg[2..];
                argument = args[i++];
            }
            else
            {
                param = arg[1].ToString();
                argument = arg[2..];
            }

            switch (param)
            {
                case "output":
                case "o":
                    OutputFile = argument;
                    break;

                case "architecture":
                case "a":
                    Architecture = argument.ToLowerInvariant() switch
                    {
                        "amd64" => TargetArchitecture.Amd64,
                        "i386" => TargetArchitecture.I386,
                        _ => throw new Exception("Unsupported architecture!")
                    };
                    break;

                case "format":
                case "f":
                    Format = argument.ToLowerInvariant() switch
                    {
                        "bin" => Format.Bin,
                        "elf" => Format.Elf,
                        _ => throw new Exception("Unsupported format!")
                    };
                    break;

                case "image":
                case "t":
                    ImageType = argument.ToLowerInvariant() switch
                    {
                        "none" => ImageType.None,
                        "iso" => ImageType.Iso,
                        _ => throw new Exception("Unsupported image type!")
                    };
                    break;
            }
        }
    }
}