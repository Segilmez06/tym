using CommandLine;

using SixLabors.ImageSharp.Processing.Processors.Transforms;

using System.Linq;
using System.Reflection;

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
        [Value(0, MetaName = "Path", Required = false, HelpText = "Path to the image file.")]
        public string? FilePath { get; set; }



        [Option('r', "resampler", Required = false, Default = "MitchellNetravali", HelpText = "Resampling algorithm for downsizing.")]
        public string? ResamplerName { get; set; }

        [Option('l', "list-resamplers", Required = false, Default = false, HelpText = "List available resampling algorithms.")]
        public bool ListResamplers { get; set; }



        [Option('x', "x-margin", Required = false, Default = 0, HelpText = "Left margin size. Shifts the output from cursor start to right by specified character count. 1 char width = 1 pixel.")]
        public int MarginX { get; set; }

        [Option('y', "y-margin", Required = false, Default = 0, HelpText = "Top margin size. Shifts the output from cursor start to bottom by specified character count. 1 char height = 2 pixels.")]
        public int MarginY { get; set; }
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
            if (CommandLineOptions.ListResamplers)
            {
                Console.WriteLine("\u001b[32mAvailable resampling algorithms:");
                Console.Write("\x1b[33m");
                typeof(KnownResamplers).GetProperties().ToList().ForEach(x => Console.WriteLine(x.Name));
                Console.WriteLine("\x1b[39m");

                Environment.Exit(0);
            }

            PropertyInfo? ResamplerProperty = typeof(KnownResamplers).GetProperty(CommandLineOptions.ResamplerName);
            if (ResamplerProperty == null)
            {
                Console.WriteLine("\x1b[31mError:\x1b[37m Invalid resampler specified.\x1b[39m");
                Environment.Exit(1);
            }
            IResampler Resampler = (IResampler)ResamplerProperty.GetValue(typeof(KnownResamplers));
            if (Resampler == null)
            {
                Console.WriteLine("\x1b[31mError:\x1b[37m Error occured while fetching resampler.\x1b[39m");
                Environment.Exit(1);
            }


            string? ImagePath = CommandLineOptions.FilePath;
            if (Path.Exists(ImagePath))
            {
                Size TermSize = new(Console.BufferWidth, Console.BufferHeight);
                Size TargetSize = new(TermSize.Width / 2, TermSize.Height);

                Image<Rgba32> Source = Image.Load<Rgba32>(ImagePath);
                Source.Mutate(x => x.Resize(new ResizeOptions()
                {
                    Size = TargetSize,
                    Mode = ResizeMode.Max,
                    Sampler = Resampler
                }));

                string Buffer = "";
                for (int y = 0; y < CommandLineOptions.MarginY; y++)
                {
                    Buffer += "\n";
                }
                for (int y = 0; y < Source.Height / 2; y++)
                {
                    string Line = "";
                    Line += $"\x1b[{CommandLineOptions.MarginX}C";
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
                    }
                    Buffer += Line;
                    Buffer += "\x1b[0m";
                    Buffer += "\n";
                }

                Console.Write(Buffer);
            }
            else
            {
                Console.WriteLine("\x1b[31mError:\x1b[37m Can't open file!");
                Environment.Exit(1);
            }
        }
    }
}
