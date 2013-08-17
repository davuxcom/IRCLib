using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCLib
{
    public class Profile
    {

        public Profile()
        {
            QuitMessage = "irc.net http://www.daveamenta.com/";
            User = "ircnet";
            Nick = "ircuser";
            RealName = "irc.net http://www.daveamenta.com/"; 
        }

        public string User { get; set; }
        public string Nick { get; set; }
        public string RealName { get; set; }
        public string QuitMessage { get; set; }

        public string GetNick(string failedNick)
        {
            return failedNick + "_";
        }
    }
}
