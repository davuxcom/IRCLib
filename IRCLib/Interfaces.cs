using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCLib
{
    public interface IMessageable
    {
        string Name { get; }
        void SendMessage(string msg);
    }
}
