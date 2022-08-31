using System.Collections.Generic;

namespace ImageOptimApi
{
    public class OptimizedImageResult
    {
        public double ElapsedSeconds { get; set; }
        public byte[] File { get; set; }
        public string FileType { get; set; }
        public int OriginalSize { get; set; }
        public string ServerHeader { get; set; }
        public Status Status { get; set; }
        public string StatusMessage { get; set; }
        public string ViaHeader { get; set; }
        public IEnumerable<string> Warnings { get; set; }
    }
}