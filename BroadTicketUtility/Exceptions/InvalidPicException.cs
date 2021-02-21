using System;
using System.Collections.Generic;
using System.Text;

namespace BroadTicketUtility.Exceptions
{
    public class InvalidPicException : Exception
    {
        public InvalidPicException() : base("Image Invalid.")
        {
        }
    }
}
