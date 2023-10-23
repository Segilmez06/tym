using CommandLine;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

using System.Net.Http.Headers;
using System.Reflection;

using TYM.Properties;
using static TYM.BlockChars;
using static TYM.Logger;

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
        [Value(0, MetaName = "Path", Required = false, HelpText = "Path to the image file. Also supports web links. (DANGER: USE WEB LINKS AT YOUR OWN RISK!)")]
        public string? FilePath { get; set; }



        [Option('r', "resampler", Required = false, Default = "MitchellNetravali", HelpText = "Resampling algorithm for downsizing.")]
        public string? ResamplerName { get; set; }

        [Option('l', "list-resamplers", Required = false, Default = false, HelpText = "List available resampling algorithms.")]
        public bool ListResamplers { get; set; }



        [Option('x', "x-margin", Required = false, Default = 0, HelpText = "Left margin size as characters. Shifts the output from cursor start to right by specified character count. 1 char width = 1 pixel.")]
        public int MarginX { get; set; }

        [Option('y', "y-margin", Required = false, Default = 0, HelpText = "Top margin size as characters. Shifts the output from cursor start to bottom by specified character count. 1 char height = 2 pixels.")]
        public int MarginY { get; set; }



        [Option('w', "width", Required = false, Default = 0, HelpText = "Output width as pixels. 1 char width = 1 pixel. Set to 0 for auto size.")]
        public int Width { get; set; }

        [Option('h', "height", Required = false, Default = 0, HelpText = "Output height as pixels. 1 char height = 2 pixels. Set to 0 for auto size.")]
        public int Height { get; set; }



        [Option('m', "resize-method", Required = false, Default = "Contain", HelpText = "Resizing mode. Available options: Contain, Cover (Crop), Stretch")]
        public string? ResizeMethod { get; set; }




        [Option('f', "fullscreen", Required = false, Default = false, HelpText = "Use fullscreen. This overrides margin and size arguments!")]
        public bool UseFullscreen { get; set; }



        [Option('c', "clear", Required = false, Default = false, HelpText = "Clear downloaded cache folder.")]
        public bool ClearCache { get; set; }
    }

    public static class Logger
    {
        public enum LogLevel
        {
            Verbose,
            Info,
            Warning,
            Error
        }

        private static readonly Dictionary<LogLevel, int> LevelColors = new()
        {
            { LogLevel.Verbose, 7 }, // White
            { LogLevel.Info, 4 },    // Blue
            { LogLevel.Warning, 3 },    // Yellow
            { LogLevel.Error, 1 }    // Red
        };

        private static readonly Dictionary<LogLevel, string> LevelPrefix= new()
        {
            { LogLevel.Verbose, Messages.LogCategory_Verbose },
            { LogLevel.Info, Messages.LogCategory_Info },
            { LogLevel.Warning, Messages.LogCategory_Warning },
            { LogLevel.Error, Messages.LogCategory_Error }
        };

        private static readonly Dictionary<LogLevel, int> LevelExitCodes = new()
        {
            { LogLevel.Verbose, 0 },
            { LogLevel.Info, 0 },
            { LogLevel.Warning, 0 },
            { LogLevel.Error, 1 }
        };

        public static void LogMsg(LogLevel Level, string Message)
        {
            Console.WriteLine($"\u001b[3{LevelColors[Level]}m{LevelPrefix[Level]}{((LevelPrefix[Level].Length > 0) ? ": " : "")}\u001b[37m{Message}\u001b[39m");
        }

        public static void LogExit(LogLevel Level, string Message)
        {
            LogMsg(Level, Message);
            Environment.Exit(LevelExitCodes[Level]);
        }

        public static void LogExit(LogLevel Level, string Message, string Argument)
        {
            LogMsg(Level, Message.Replace("%s", Argument));
            Environment.Exit(LevelExitCodes[Level]);
        }

        public static void LogExit(LogLevel Level, string Message, int Argument)
        {
            LogMsg(Level, Message.Replace("%s", Argument.ToString()));
            Environment.Exit(LevelExitCodes[Level]);
        }
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
                LogMsg(LogLevel.Info, Messages.Message_AvailableResamplers);
                typeof(KnownResamplers).GetProperties().ToList().ForEach(x => LogMsg(LogLevel.Verbose, x.Name));
                Environment.Exit(0);
            }

            string DownloadDirectory = Path.Combine(Path.GetTempPath(), Settings.tempDirectoryName);
            if (CommandLineOptions.ClearCache)
            {
                Directory.Delete(DownloadDirectory, true);
            }

            string? ImagePath = CommandLineOptions.FilePath;
            if (Path.Exists(ImagePath))
            {
                ProcessImageFile(CommandLineOptions, ImagePath);
            }
            else if (Uri.IsWellFormedUriString(ImagePath, UriKind.Absolute))
            {
                if (Uri.TryCreate(ImagePath, UriKind.Absolute, out Uri? WebURI))
                {
                    if (WebURI.Scheme == Uri.UriSchemeHttps)
                    {
                        HttpClient Client = new();

                        List<string> SupportedMimeTypes = Settings.supportedImageFormats.Split(",").Select(x => x = $"image/{x}").ToList();
                        SupportedMimeTypes.ForEach(x => Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(x)));

                        Task<HttpResponseMessage> GetTask = Client.GetAsync(WebURI);
                        GetTask.Wait();
                        HttpResponseMessage Response = GetTask.Result;

                        if (Response.IsSuccessStatusCode)
                        {
                            string ResponseMimeType = Response.Content.Headers.ContentType.MediaType;

                            if (SupportedMimeTypes.Any(x => x == ResponseMimeType))
                            {
                                Directory.CreateDirectory(DownloadDirectory);

                                string TempFile = Path.Combine(DownloadDirectory, Guid.NewGuid().ToString());

                                Task<byte[]> ReadTask = Response.Content.ReadAsByteArrayAsync();
                                ReadTask.Wait();
                                byte[] Data = ReadTask.Result;

                                File.WriteAllBytes(TempFile, Data);
                                ProcessImageFile(CommandLineOptions, TempFile);
                            }
                            else
                            {
                                LogExit(LogLevel.Error, Messages.Error_InvalidMimeType, ResponseMimeType);
                            }
                        }
                        else
                        {
                            LogExit(LogLevel.Error, Messages.Error_ResponseCode, (int)Response.StatusCode);
                        }
                    }
                    else
                    {
                        LogExit(LogLevel.Error, Messages.Error_UnsupportedProtocol);
                    }
                }
                else
                {
                    LogExit(LogLevel.Error, Messages.Error_URLParseError);
                }
            }
            else
            {
                LogExit(LogLevel.Error, Messages.Error_FileError);
            }
        }

        private static void ProcessImageFile(Options CommandLineOptions, string ImagePath)
        {
            PropertyInfo? ResamplerProperty = typeof(KnownResamplers).GetProperty(CommandLineOptions.ResamplerName);
            if (ResamplerProperty == null)
            {
                LogExit(LogLevel.Error, Messages.Error_InvalidResampler);
            }
            IResampler? Resampler = (IResampler)ResamplerProperty.GetValue(typeof(KnownResamplers));
            if (Resampler == null)
            {
                LogExit(LogLevel.Error, Messages.Error_ResamplerError);
            }

            Dictionary<string, ResizeMode> AvailableResizeModes = new(){
                {"Contain", ResizeMode.Max},
                {"Cover", ResizeMode.Crop},
                {"Crop", ResizeMode.Crop},
                {"Stretch", ResizeMode.Stretch},
                {"Center", ResizeMode.Pad}
            };
            if (!AvailableResizeModes.ContainsKey(CommandLineOptions.ResizeMethod))
            {
                LogExit(LogLevel.Error, Messages.Error_InvalidResizeMode);
            }
            ResizeMode SelectedResizeMode = AvailableResizeModes.GetValueOrDefault(CommandLineOptions.ResizeMethod);

            Size TermSize = new(Console.BufferWidth, Console.BufferHeight);
            Size TargetSize = CommandLineOptions.UseFullscreen 
                ? new(
                    TermSize.Width,
                    TermSize.Height * 2
                    )
                : new(
                    CommandLineOptions.Width < 1 ? TermSize.Width / 2 : CommandLineOptions.Width,
                    CommandLineOptions.Height < 1 ? TermSize.Height : CommandLineOptions.Height
                );

            Image<Rgba32> Source = Image.Load<Rgba32>(ImagePath);
            Source.Mutate(x => x.Resize(new ResizeOptions()
            {
                Size = TargetSize,
                Mode = SelectedResizeMode,
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

            Environment.Exit(0);
        }
    }
}
