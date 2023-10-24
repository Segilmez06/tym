using CommandLine;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

using System.Net.Http.Headers;
using System.Reflection;

using TYM.Properties;
using static TYM.BlockChars;
using static TYM.Logger;

namespace TYM
{

    /// <summary>
    /// Special Unicode characters for rendering image
    /// </summary>
    static class BlockChars
    {
        public static char TopBlockChar = '\u2580';
        public static char BottomBlockChar = '\u2584';
        public static char EmptyBlockChar = '\u0020';
    }



    /// <summary>
    /// Command line options
    /// </summary>
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



        [Option('m', "resize-method", Required = false, Default = "Contain", HelpText = "Resizing mode. Available options: Contain, Cover (Crop), Stretch, Center")]
        public string? ResizeMethod { get; set; }




        [Option('f', "fullscreen", Required = false, Default = false, HelpText = "Use fullscreen. This overrides margin and size arguments!")]
        public bool UseFullscreen { get; set; }



        [Option('c', "clear", Required = false, Default = false, HelpText = "Clear downloaded cache folder.")]
        public bool ClearCache { get; set; }

    }



    /// <summary>
    /// Console logging utility
    /// </summary>
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



    /// <summary>
    /// TYM main instance
    /// </summary>
    public class App
    {

        /// <summary>
        /// Get command line arguments parser
        /// </summary>
        private readonly Parser CommandLineParser = Parser.Default;


        /// <summary>
        /// Available resize modes
        /// </summary>
        private readonly static Dictionary<string, ResizeMode> ResizeModes = new(){
                {"Contain", ResizeMode.Max},
                {"Cover", ResizeMode.Crop},
                {"Crop", ResizeMode.Crop},
                {"Stretch", ResizeMode.Stretch},
                {"Center", ResizeMode.Pad}
            };


        /// <summary>
        /// TYM instance entry point
        /// </summary>
        /// <param name="Arguments">Command line arguments</param>
        public App(string[] Arguments) 
        {
            // Parse command line arguments and ignore executable name
            CommandLineParser.ParseArguments<Options>(Arguments.Skip(1)).WithParsed(Run);
        }


        /// <summary>
        /// Start the process
        /// </summary>
        /// <param name="CommandLineOptions">Command line options</param>
        private void Run(Options CommandLineOptions)
        {

            // If listing resamplers requested
            if (CommandLineOptions.ListResamplers)
            {

                // Print available resamplers and exit
                LogMsg(LogLevel.Info, Messages.Message_AvailableResamplers);
                typeof(KnownResamplers).GetProperties().ToList().ForEach(x => LogMsg(LogLevel.Verbose, x.Name));
                Environment.Exit(0);

            }


            // Get download directory path
            if (CommandLineOptions.ClearCache)
                Directory.Delete(GetDownloadDirectory(), true);


            // If path is local file
            string? ImagePath = CommandLineOptions.FilePath;
            if (Path.Exists(ImagePath))
            {
                ProcessImageFile(CommandLineOptions, ImagePath);
            }


            // If path is URL
            else if (Uri.IsWellFormedUriString(ImagePath, UriKind.Absolute))
            {

                // Parse the url
                if(!Uri.TryCreate(ImagePath, UriKind.Absolute, out Uri? WebURI))
                    LogExit(LogLevel.Error, Messages.Error_URLParseError);


                // Check if protocol is supported
                if (WebURI.Scheme != Uri.UriSchemeHttps)
                    LogExit(LogLevel.Error, Messages.Error_UnsupportedProtocol);


                // Create HTTP client
                HttpClient Client = new();


                // Add request headers
                GetSupportedMimeTypes().ForEach(x => Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(x)));


                // Send GET request to download the file
                Task<HttpResponseMessage> GetTask = Client.GetAsync(WebURI);
                GetTask.Wait();
                HttpResponseMessage Response = GetTask.Result;


                // If response is success
                if (!Response.IsSuccessStatusCode)
                    LogExit(LogLevel.Error, Messages.Error_ResponseCode, (int)Response.StatusCode);


                // Validate response MIME type
                string ResponseMimeType = Response.Content.Headers.ContentType.MediaType;
                if (!GetSupportedMimeTypes().Any(x => x == ResponseMimeType))
                    LogExit(LogLevel.Error, Messages.Error_InvalidMimeType, ResponseMimeType);


                // Create cache directory
                Directory.CreateDirectory(GetDownloadDirectory());


                // Save response to file
                string TempFile = GenerateTempFileName();
                Task<byte[]> ReadTask = Response.Content.ReadAsByteArrayAsync();
                ReadTask.Wait();
                byte[] Data = ReadTask.Result;
                File.WriteAllBytes(TempFile, Data);


                // Render the file
                ProcessImageFile(CommandLineOptions, TempFile);

            }
            else
            {
                LogExit(LogLevel.Error, Messages.Error_FileError);
            }
        }


        /// <summary>
        /// Process the image by path
        /// </summary>
        /// <param name="CommandLineOptions">Command line options</param>
        /// <param name="ImagePath">Path of the image file</param>
        private static void ProcessImageFile(Options CommandLineOptions, string ImagePath)
        {

            // Get resampler from KnownResamplers class
            PropertyInfo? ResamplerProperty = typeof(KnownResamplers).GetProperty(CommandLineOptions.ResamplerName);
            if (ResamplerProperty == null)
                LogExit(LogLevel.Error, Messages.Error_InvalidResampler);

            IResampler? Resampler = (IResampler)ResamplerProperty.GetValue(typeof(KnownResamplers));
            if (Resampler == null)
                LogExit(LogLevel.Error, Messages.Error_ResamplerError);


            // Define available resize modes
            if (!ResizeModes.ContainsKey(CommandLineOptions.ResizeMethod))
                LogExit(LogLevel.Error, Messages.Error_InvalidResizeMode);
            ResizeMode SelectedResizeMode = ResizeModes.GetValueOrDefault(CommandLineOptions.ResizeMethod);


            // Read image and resize
            Image<Rgba32> Source = Image.Load<Rgba32>(ImagePath);
            Source.Mutate(x => x.Resize(new ResizeOptions()
            {
                Size = GetTargetSize(new(CommandLineOptions.Width, CommandLineOptions.Height), CommandLineOptions.UseFullscreen),
                Mode = SelectedResizeMode,
                Sampler = Resampler
            }));


            // Get the output and print to console
            string ImageOutput = GenerateColoredImageString(Source, new(CommandLineOptions.MarginX, CommandLineOptions.MarginY));
            Console.Write(ImageOutput);


            // Nothing left to do, so exit...
            Environment.Exit(0);
        }


        /// <summary>
        /// Get cache folder
        /// </summary>
        /// <returns>Cache folder path</returns>
        private static string GetDownloadDirectory() => Path.Combine(Path.GetTempPath(), Settings.tempDirectoryName);
        

        /// <summary>
        /// Generate new temp file
        /// </summary>
        /// <returns>Generated file path</returns>
        private static string GenerateTempFileName() => Path.Combine(GetDownloadDirectory(), Guid.NewGuid().ToString());
        

        /// <summary>
        /// Get supported MIME types
        /// </summary>
        /// <returns>List of types</returns>
        private static List<string> GetSupportedMimeTypes() => Settings.supportedImageFormats.Split(",").Select(x => x = $"image/{x}").ToList();
        

        /// <summary>
        /// Get target size by parameters
        /// </summary>
        /// <param name="SpecifiedSize">Manually requested size</param>
        /// <param name="IsFullScreen">Whether to fill the whole terminal</param>
        /// <returns>Target size</returns>
        private static Size GetTargetSize(Size SpecifiedSize, bool IsFullScreen = false)
        {
            Size TermTargetSize = new(Console.BufferWidth / 2, Console.BufferHeight);

            if (TermTargetSize.Width % 2 != 0) TermTargetSize.Width += 1;
            if (TermTargetSize.Height % 2 != 0) TermTargetSize.Height += 1;

            if (IsFullScreen)
                return new(Console.BufferWidth, Console.BufferHeight * 2);

            return new(
                    SpecifiedSize.Width < 1 ? TermTargetSize.Width : SpecifiedSize.Width,
                    SpecifiedSize.Height < 1 ? TermTargetSize.Height : SpecifiedSize.Height
                );
        }
    

        /// <summary>
        /// Generates the output image's string by using VT100 color and position codes
        /// </summary>
        /// <param name="Source">Source image object</param>
        /// <param name="Margins">Margin size</param>
        /// <returns>Image output string</returns>
        private static string GenerateColoredImageString(Image<Rgba32> Source, Point Margins)
        {
            string Buffer = "";
            for (int y = 0; y < Margins.Y; y++)
            {
                Buffer += "\n";
            }
            for (int y = 0; y < Source.Height / 2; y++)
            {
                string Line = "";
                Line += Margins.X > 0 ? $"\x1b[{Margins.X}C" : "";
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
            return Buffer;
        }
    }

}
