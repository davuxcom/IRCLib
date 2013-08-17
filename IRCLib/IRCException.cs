using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCLib
{
    public class IRCException : Exception
    {
        public IRCException(string message)
            : base(message)
        {

        }
    }
}
