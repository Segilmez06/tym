using CommandLine;

using static TYM.BlockChars;

namespace TYM
{
    static class BlockChars
    {
        public static char TopBlockChar = '\u2580';
        public static char BottomBlockChar = '\u2584';
        public static char EmptyBlockChar = '\u0020';
    }

    public class Options
    {
        [Value(0, MetaName = "Path", Required = true, HelpText = "Path of image file.")]
        public string? FilePath { get; set; }
    }

    public class App
    {
        private readonly Parser CommandLineParser = Parser.Default;

        public App(string[] Arguments) 
        {
            CommandLineParser.ParseArguments<Options>(Arguments.Skip(1)).WithParsed(Run);
        }

        private void Run(Options CommandLineOptions)
        {
            string? ImagePath = CommandLineOptions.FilePath;
            if (Path.Exists(ImagePath))
            {
                Size TermSize = new(Console.BufferWidth, Console.BufferHeight);
                Size TargetSize = new(TermSize.Width / 2, TermSize.Height / 2);

                Image<Rgba32> Source = Image.Load<Rgba32>(ImagePath);
                Source.Mutate(x => x.Resize(new ResizeOptions()
                {
                    Size = TargetSize,
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.MitchellNetravali

                }));

                string Buffer = "";
                for (int y = 0; y < Source.Height / 2; y++)
                {
                    string Line = "";
                    for (int x = 0; x < Source.Width; x++)
                    {
                        Rgba32[] PixelColors = { Source[x, (y * 2)], Source[x, (y * 2) + 1] };

                        if (PixelColors.Any(x => x.A == 0))
                        {
                            int TransparencySide = PixelColors.All(x => x.A == 0) ? 2 : ((PixelColors[0].A == 0) ? 0 : 1);
                            Rgba32 ColoredPixel = PixelColors[(PixelColors[0].A == 0) ? 1 : 0];
                            Line += new List<int> { 0, 1 }.Any(x => TransparencySide == x) ? $"\x1b[38;2;{ColoredPixel.R};{ColoredPixel.G};{ColoredPixel.B}m" : "\x1b[39m";
                            Line += "\x1b[49m";
                            Line += PixelColors.All(x => x.A == 0) ? EmptyBlockChar : ((PixelColors[0].A == 0) ? BottomBlockChar : TopBlockChar);
                        }
                        else
                        {
                            Line += $"\x1b[38;2;{PixelColors[0].R};{PixelColors[0].G};{PixelColors[0].B}m";
                            Line += $"\x1b[48;2;{PixelColors[1].R};{PixelColors[1].G};{PixelColors[1].B}m";
                            Line += TopBlockChar;
                        }
                        Line += "\x1b[0m";
                    }
                    Buffer += Line;
                    Buffer += "\n";
                }
                Console.Write(Buffer);
            }
            else
            {
                Console.WriteLine($"\x1b[31mError:\x1b[37m Can't open file!");
            }
        }
    }
}
