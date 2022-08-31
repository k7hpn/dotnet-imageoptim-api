using CommandLine;

namespace ImageOptimApi.Console
{
    internal class Options
    {
        [Option(HelpText = "RGB color as 3 or 6 hex digits, e.g. FFAAF8 (without #), to use as a background color when adding padding to the image.")]
        public string? BgColor { get; set; }

        [Option(HelpText = "Image is scaled and cropped to completely fill the given width and height, so that the resulting image always has exactly the dimensions specified.")]
        public bool Crop { get; set; }

        [Option(HelpText = "Select auto for auto-selection of the most important area or top, left, right or bottom to favor that side")]
        public CropType CropType { get; set; }

        [Option(HelpText = "The image will be cropped so that the given point always remains in the image. Value is a percentage.")]
        public int? CropXFocalPoint { get; set; }

        [Option(HelpText = "The image will be cropped so that the given point always remains in the image. Value is a percentage.")]
        public int? CropYFocalPoint { get; set; }

        [Option('d', HelpText = "Output debug logging to the console with additional information about tthe process.")]
        public bool Debug { get; set; }

        [Option(HelpText = "Image is resized to completely fit within the given dimensions without cropping. Aspect ratio is preserved. Small images will be enlarged if necessary.")]
        public bool Fit { get; set; }

        [Option('f',
            HelpText = "By default the best file format is chosen automatically. You can request images converted to a specific format.")]
        public Format Format { get; set; }

        [Option('h',
            HelpText = "Maximum image height. Dimensions are specified in CSS pixels, images are always resized preserving aspect ratio.")]
        public int? Height { get; set; }

        [Option(Default = HighDpi.Dpi1x,
            HelpText = "Multiply image dimensions by 2 or 3 for High-DPI ('Retina') displays.")]
        public HighDpi HighDpi { get; set; }

        [Option('i',
            Required = true,
            HelpText = "Image to optimize - can be a path to a local file or a URL starting with http or https")]
        public string Image { get; set; } = "";

        [Option('o',
            Default = "optimized",
            HelpText = "Path to output optimized files, defaults to a subdirectory named 'optimized'")]
        public string OutputPath { get; set; } = "optimized";

        [Option(HelpText = "Specifies desired image quality when saving images in a lossy format, defaults to medium.")]
        public Quality Quality { get; set; }

        [Option(HelpText = "Test the process - go through everything but don't make the Web call.")]
        public bool Test { get; set; }

        [Option('t',
            Default = 30,
            HelpText = "Maximum time allowed to spend on optimization. In seconds, defaults to 30 seconds.")]
        public int Timeout { get; set; }

        [Option(HelpText = "Removes solid-color border from the image. Only trims pixels that have exactly the same color.")]
        public bool TrimBorder { get; set; }

        [Option('u',
            HelpText = "Your username as given to you by the imageoptim.com API: https://imageoptim.com/api/post - can also bet set in the IMAGEOPTIMAPI_USERNAME environment variable")]
        public string Username { get; set; } = "";

        [Option('w',
            HelpText = "Maximum image width. If not specified, the height will vary depending on image's aspect ratio. Dimensions are specified in CSS pixels.")]
        public int? Width { get; set; }
    }
}