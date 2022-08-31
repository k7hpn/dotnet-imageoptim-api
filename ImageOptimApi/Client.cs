using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace ImageOptimApi
{
    public class Client
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public Client(HttpClient httpClient, ILogger<Client> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var thisAssembly = Assembly.GetAssembly(GetType());
            var title = thisAssembly
                .GetCustomAttribute<AssemblyTitleAttribute>()?
                .Title ?? nameof(ImageOptimApi);
            var version = thisAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "Unknown";

            BaseAddress = new Uri("https://im2.io/");
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(title, version));
        }

        public Uri BaseAddress { get; set; }

        public string BgColor { get; set; }
        public bool Crop { get; set; }
        public CropType CropType { get; set; }
        public int? CropXFocalPoint { get; set; }
        public int? CropYFocalPoint { get; set; }
        public bool DisableWebCall { get; set; }
        public bool Fit { get; set; }
        public Format Format { get; set; }
        public int? Height { get; set; }
        public HighDpi HighDpi { get; set; }
        public Quality Quality { get; set; }
        public bool TrimBorder { get; set; }
        public string Username { get; set; }
        public int? Width { get; set; }

        public Task<OptimizedImageResult> OptimizeAsync(Uri imageUri)
        {
            if (imageUri == null || string.IsNullOrEmpty(imageUri.AbsoluteUri))
            {
                throw new ArgumentNullException(nameof(imageUri));
            }

            return OptimizeImageInternalAsync(null, imageUri);
        }

        public Task<OptimizedImageResult> OptimizeAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return OptimizeImageInternalAsync(path, null);
        }

        private static string ConvertCropType(CropType cropType) => cropType switch
        {
            CropType.Auto => "crop=auto",
            CropType.Top => "crop=top",
            CropType.Left => "crop=left",
            CropType.Right => "crop=right",
            CropType.Bottom => "crop=bottom",
            _ => "crop",
        };

        private static string ConvertFormat(Format format) => format switch
        {
            Format.H264 => "format=h264",
            Format.Jpeg => "format=jpeg",
            Format.Png => "format=png",
            Format.WebM => "format=webm",
            _ => "format=same"
        };

        private static string ConvertQuality(Quality quality) => quality switch
        {
            Quality.Low => "low",
            Quality.High => "high",
            Quality.Lossless => "lossless",
            _ => "medium"
        };

        private async Task<OptimizedImageResult> OptimizeImageInternalAsync(string path, Uri imageUri)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogDebug("Preparing parameters");
            var urlPath = PrepareParameters(_httpClient.Timeout);

            var optimized = new OptimizedImageResult();

            HttpResponseMessage httpResponseMessage = null;
            byte[] image = null;
            string filename = null;
            string contentType = null;
            Uri requestUri;

            if (string.IsNullOrEmpty(path))
            {
                requestUri = new Uri(BaseAddress + urlPath + imageUri);
                _logger.LogDebug("Using image from URL, image path: {ImagePath}", imageUri);
            }
            else
            {
                if (!System.IO.File.Exists(path))
                {
                    throw new ParameterException($"File not found: {path}");
                }
                filename = System.IO.Path.GetFileName(path);
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(filename, out contentType))
                {
                    throw new ParameterException($"Unable to determine type of file: {filename}");
                }
                _logger.LogDebug("Reading file at path: {ImagePath}", path);
                image = await System.IO.File.ReadAllBytesAsync(path);
                optimized.OriginalSize = image.Length;
                requestUri = new Uri(BaseAddress + urlPath);
            }

            MultipartFormDataContent form = null;
            ByteArrayContent byteContent = null;

            try
            {
                if (image != null)
                {
                    form = new MultipartFormDataContent();
                    byteContent = new ByteArrayContent(image);
                    byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    form.Add(byteContent, name: "file", fileName: filename);
                }

                _logger.LogDebug("Making Web call at {Elapsed}s", sw.Elapsed.TotalSeconds);
                try
                {
                    if (!DisableWebCall)
                    {
                        httpResponseMessage = await _httpClient.PostAsync(requestUri, form);
                    }
                }
                catch (HttpRequestException hrex)
                {
                    optimized.Status = Status.OtherError;
                    optimized.StatusMessage = $"Http request error: {hrex.Message}";
                    _logger.LogDebug(hrex,
                        "Exception on Web call at {Elapsed}s: {ErrorMessage}",
                        sw.Elapsed.TotalSeconds,
                        hrex.Message);
                }
                _logger.LogDebug("Back from Web call at {Elapsed}s", sw.Elapsed.TotalSeconds);
            }
            finally
            {
                byteContent?.Dispose();
                form?.Dispose();
            }

            if (DisableWebCall)
            {
                optimized.File = image ?? new byte[] { 0xff };
                optimized.Status = Status.TestSuccess;
                optimized.StatusMessage = "Test call, file is not an optimized message.";
                sw.Stop();
                optimized.ElapsedSeconds = sw.Elapsed.TotalSeconds;
                return optimized;
            }

            if (httpResponseMessage == null)
            {
                optimized.Status = Status.OtherError;
                if (string.IsNullOrEmpty(optimized.StatusMessage))
                {
                    optimized.StatusMessage = "Web response was empty.";
                }
            }
            else if (httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.LogDebug("Success status code");
                optimized.Status = Status.Success;
                optimized.File = await httpResponseMessage.Content.ReadAsByteArrayAsync();
                if (httpResponseMessage.Headers.TryGetValues("Content-type", out var fileType))
                {
                    optimized.FileType = fileType.FirstOrDefault();
                }
            }
            else
            {
                _logger.LogDebug("Non-success status code");
                switch (httpResponseMessage.StatusCode)
                {
                    case System.Net.HttpStatusCode.BadRequest:
                        optimized.Status = Status.OptionsOrImageIncorrect;
                        optimized.StatusMessage = await httpResponseMessage.Content.ReadAsStringAsync();
                        break;

                    case System.Net.HttpStatusCode.PaymentRequired:
                        optimized.Status = Status.PaymentRequired;
                        optimized.StatusMessage = await httpResponseMessage.Content.ReadAsStringAsync();
                        break;

                    case System.Net.HttpStatusCode.Forbidden:
                        optimized.Status = Status.UsernameMissingIncorrect;
                        optimized.StatusMessage = await httpResponseMessage.Content.ReadAsStringAsync();
                        break;

                    case System.Net.HttpStatusCode.NotFound:
                        optimized.Status = Status.CannotFindImage;
                        optimized.StatusMessage = await httpResponseMessage.Content.ReadAsStringAsync();
                        break;

                    default:
                        optimized.Status = Status.OtherError;
                        optimized.StatusMessage = await httpResponseMessage.Content.ReadAsStringAsync();
                        break;
                }
            }

            if (httpResponseMessage.Headers.TryGetValues("Server", out var server))
            {
                optimized.ServerHeader = server.First();
            }

            if (httpResponseMessage.Headers.TryGetValues("Via", out var via))
            {
                optimized.ViaHeader = via.First();
            }

            if (httpResponseMessage.Headers.TryGetValues("Warning", out var warnings))
            {
                optimized.Warnings = warnings;
            }

            sw.Stop();
            optimized.ElapsedSeconds = sw.Elapsed.TotalSeconds;

            return optimized;
        }

        private string PrepareParameters(TimeSpan timeout)
        {
            VerifyParameters();

            var options = new List<string>();

            if (Width.HasValue)
            {
                options.Add(Height.HasValue
                    ? $"{Width.Value}x{Height.Value}"
                    : $"{Width.Value}");
            }
            else
            {
                options.Add("full");
            }

            if (HighDpi != HighDpi.Dpi1x)
            {
                options.Add(HighDpi == HighDpi.Dpi2x ? "2x" : "3x");
            }

            if (Fit)
            {
                options.Add("fit");
            }

            if (Crop)
            {
                if (CropXFocalPoint.HasValue)
                {
                    options.Add($"crop={CropXFocalPoint.Value}x{CropYFocalPoint.Value}");
                }
                else
                {
                    options.Add(ConvertCropType(CropType));
                }
            }

            if (TrimBorder)
            {
                options.Add("trim=border");
            }

            if (!string.IsNullOrEmpty(BgColor))
            {
                options.Add(BgColor);
            }

            if (Quality != Quality.Medium)
            {
                options.Add(ConvertQuality(Quality));
            }

            if (Format != Format.Auto)
            {
                options.Add(ConvertFormat(Format));
            }

            if (timeout != TimeSpan.FromSeconds(30))
            {
                options.Add($"timeout={timeout.TotalSeconds}");
            }

            return string.Join("/", new[] { Username, string.Join(",", options) }) + '/';
        }

        private void VerifyParameters()
        {
            if (string.IsNullOrEmpty(Username))
            {
                throw new ParameterException("A Username must be supplied.");
            }

            if (Height.HasValue && !Width.HasValue)
            {
                throw new ParameterException("You must supply a Width or Width and Height, not just a Height.");
            }

            if (CropYFocalPoint.HasValue && !CropXFocalPoint.HasValue)
            {
                throw new ParameterException("You must supply a CropXFocalPoint if you supply a CropYFocalPoint.");
            }
            if (!string.IsNullOrEmpty(BgColor))
            {
                if (BgColor.StartsWith("#"))
                {
                    throw new ParameterException("Do not preface the BgColor value with a #.");
                }
                if (BgColor.Length != 3 && BgColor.Length != 6)
                {
                    throw new ParameterException("You must supply the BgColor in three or six hex digits.");
                }
            }
        }
    }
}