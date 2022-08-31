using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageOptimApi.Console
{
    internal class OptionException : Exception
    {
        public OptionException() : base()
        {
        }

        public OptionException(string? message) : base(message)
        {
        }

        public OptionException(string? message, Exception? innerException) 
            : base(message, innerException)
        {
        }
    }
}
