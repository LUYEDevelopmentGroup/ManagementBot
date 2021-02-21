using System;
using System.Collections.Generic;
using System.Text;

namespace BroadTicketUtility.Exceptions
{
    public class SignatureInvalidException : Exception
    {
        public SignatureInvalidException() : base("Signature Invalid.")
        {
        }
    }
}
