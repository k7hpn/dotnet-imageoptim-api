using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Reflection;

namespace ImageOptimApi.Console
{
    internal static class Program
    {
        private static ServiceProvider? _provider;

        private static void ConfigureClient(Client client, Options options)
        {
            if (!string.IsNullOrEmpty(options.BgColor))
            {
                client.BgColor = options.BgColor;
            }
            client.Crop = options.Crop;
            if (options.CropType != CropType.Default)
            {
                client.CropType = options.CropType;
            }
            if (options.CropXFocalPoint.HasValue)
            {
                client.CropXFocalPoint = options.CropXFocalPoint;
            }
            if (options.CropYFocalPoint.HasValue)
            {
                client.CropYFocalPoint = options.CropYFocalPoint;
            }
            client.Fit = options.Fit;
            if (options.Format != Format.Auto)
            {
                client.Format = options.Format;
            }
            if (options.Height.HasValue)
            {
                client.Height = options.Height;
            }
            if (options.HighDpi != HighDpi.Dpi1x)
            {
                client.HighDpi = options.HighDpi;
            }
            if (options.Quality != Quality.Medium)
            {
                client.Quality = options.Quality;
            }
            client.DisableWebCall = options.Test;
            client.TrimBorder = options.TrimBorder;

            client.Username = options.Username;
            if (string.IsNullOrEmpty(client.Username))
            {
                client.Username = Environment.GetEnvironmentVariable("IMAGEOPTIMAPI_USERNAME");
            }

            if (client.Username == null)
            {
                throw new OptionException("You must supply a username through the -u option or in the IMAGEOPTIMAPI_USERNAME environment variable.");
            }

            if (options.Width.HasValue)
            {
                client.Width = options.Width;
            }
        }

        private static string GetExtension(Format format) => format switch
        {
            Format.Png => "png",
            Format.Jpeg => "jpg",
            Format.WebM => "webm",
            Format.H264 => "h264",
            _ => string.Empty,
        };

        private static async Task<int> Main(string[] args)
        {
            var title = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<AssemblyTitleAttribute>()?
                .Title ?? nameof(ImageOptimApi) + '.' + nameof(ImageOptimApi.Console);
            var version = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "Unknown";

            var services = new ServiceCollection();
            services.AddLogging(_ => _.AddConsole());
            services.AddHttpClient<Client>()
                .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
                {
                    AllowAutoRedirect = true
                })
                .ConfigureHttpClient(_ =>
                {
                    _.Timeout = TimeSpan.FromSeconds(30);
                    _.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(title, version));
                });
            _provider = services.BuildServiceProvider();

            try
            {
                return await Parser
                    .Default
                    .ParseArguments<Options>(args)
                    .MapResult(RunOptionsAsync, _ => Task.FromResult(1));
            }
            catch (OptionException oex)
            {
                System.Console.Error.WriteLine($"An error occurred: {oex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"An error occurred: {ex.Message}");
                System.Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static async Task<int> RunOptionsAsync(Options options)
        {
            if (_provider == null)
            {
                throw new Exception("No service provider, cannot proceed");
            }
            var client = _provider.GetRequiredService<Client>();

            ConfigureClient(client, options);

            Uri? uri = null;
            string inputFilePath = string.Empty;

            if (options.Image.StartsWith("https://")
                || options.Image.StartsWith("http://"))
            {
                uri = new Uri(options.Image);
            }
            else
            {
                inputFilePath = options.Image;
            }

            string inputFilename = string.IsNullOrEmpty(inputFilePath)
                ? Path.GetFileName(uri?.LocalPath) ?? ""
                : Path.GetFileName(inputFilePath);

            string outputFilename = client.Format == Format.Auto
                    ? inputFilename
                    : Path.GetFileNameWithoutExtension(inputFilename)
                        + '.'
                        + GetExtension(client.Format);
            try
            {
                var result = string.IsNullOrEmpty(inputFilePath)
                    ? await client.OptimizeAsync(uri)
                    : await client.OptimizeAsync(inputFilePath);

                if (options.Debug)
                {
                    if (!string.IsNullOrEmpty(result.ServerHeader))
                    {
                        System.Console.WriteLine($"Server header: {result.ServerHeader}");
                    }

                    if (!string.IsNullOrEmpty(result.ViaHeader))
                    {
                        System.Console.WriteLine($"Via header: {result.ViaHeader}");
                    }
                    if (!string.IsNullOrEmpty(result.FileType))
                    {
                        System.Console.WriteLine($"File type: {result.FileType}");
                    }
                    System.Console.WriteLine($"Status: {result.Status}");
                    if (!string.IsNullOrEmpty(result.StatusMessage))
                    {
                        System.Console.WriteLine($"Status Message: {result.StatusMessage}");
                    }
                }

                if (result.Warnings?.Count() > 0)
                {
                    foreach (var warning in result.Warnings)
                    {
                        System.Console.Error.WriteLine($"Warning: {warning}");
                    }
                }

                if (result.Status == Status.Success)
                {
                    if (result.File?.Length > 0)
                    {
                        if (string.IsNullOrEmpty(inputFilePath))
                        {
                            System.Console.WriteLine($"Optimized {inputFilename} to {outputFilename} {result.File.Length:N0} bytes in {result.ElapsedSeconds:N0}s");
                        }
                        else
                        {
                            System.Console.WriteLine($"Optimized {inputFilename} to {outputFilename} from {result.OriginalSize:N0} to {result.File.Length:N0} bytes in {result.ElapsedSeconds:N0}s");
                        }

                        if (!string.IsNullOrEmpty(options.OutputPath))
                        {
                            Directory.CreateDirectory(options.OutputPath);
                        }

                        await File.WriteAllBytesAsync(Path.Combine(options.OutputPath, outputFilename), result.File);
                        return 1;
                    }
                    else
                    {
                        System.Console.Error.WriteLine("Returned file is empty.");
                    }
                }
                else
                {
                    System.Console.Error.WriteLine($"Status: {result.Status}");
                    if (result.StatusMessage != null)
                    {
                        System.Console.Error.WriteLine($"Status details: {result.StatusMessage}");
                    }
                }
            }
            catch (ParameterException pex)
            {
                System.Console.Error.WriteLine($"Parameter parsing exception: {pex.Message}");
            }
            return 0;
        }
    }
}
