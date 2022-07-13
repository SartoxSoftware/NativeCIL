namespace NativeCIL;

public enum TargetArchitecture { Amd64, I386 }
public enum Format { Bin, Elf }
public enum ImageType { None, Iso }

public class Settings
{
    public string InputFile, OutputFile;
    public TargetArchitecture Architecture;
    public Format Format;
    public ImageType ImageType;

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

            /*var param = arg[1..];
            string argument;
            if (param.StartsWith('-'))
            {
                param = param[1..];
                argument = args[i++];
            }
            else argument = param[1..];*/
            var param = arg[1].ToString();
            var argument = arg[2..];

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