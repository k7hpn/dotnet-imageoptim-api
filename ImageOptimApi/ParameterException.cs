using System;

namespace ImageOptimApi
{
    public class ParameterException : System.Exception
    {
        public ParameterException() : base()
        {
        }

        public ParameterException(string message)
            : base(message)
        {
        }

        public ParameterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}