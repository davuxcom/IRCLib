using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCLib
{
    public class User : IMessageable
    {
        public delegate void QuitHandler(User u, string msg);
        public delegate void ModeChanged(User u);
        public delegate void NickChanged(string old, string newNick);
        public event QuitHandler OnQuit;
        public event ModeChanged OnModeChanged;
        public event NickChanged OnNickChanged;

        public List<string> Modes = new List<string>();

        public string DisplayMode
        {
            get
            {
                return Server.ChannelModes.GetTopSymbol(Modes.ToArray());
            }
        }

        private Server Server = null;
        private string mask = null;

        public string TopMode
        {
            get
            {
                return Server.ChannelModes.GetTopSymbol(Modes.ToArray());
            }
        }

        public void SetMask(string mask)
        {
            if (mask.StartsWith(":"))
            {
                mask = mask.Remove(0, 1);
            }

            string firstChar = mask.Substring(0, 1);

            string mode = Server.ChannelModes.ModeForSymbol(firstChar);

            if (mode != "")
            {
                Modes.Add(mode);
                mask = mask.Remove(0, 1);
            }

            // turn a nick into a mask: DavX!@
            if (!mask.Contains('!'))
            {
                mask += "!";
            }
            if (!mask.Contains('@'))
            {
                mask += "@";
            }
            this.mask = mask;
        }

        public User(Server Server, string mask)
        {
            this.Server = Server;
            SetMask(mask);
        }

        public string Nick
        {
            get
            {
                return mask.Substring(0, mask.IndexOf("!"));
            }
        }

        public string UserName
        {
            get
            {
                int loc = mask.IndexOf("!") + 1;
                int at = mask.IndexOf("@");
                if (loc > -1 && at > -1)
                {
                    return mask.Substring(loc, at - loc);
                }
                return mask;
            }
        }

        public string Host
        {
            get
            {
                int at = mask.IndexOf("@");
                if (at > -1)
                {
                    return mask.Substring(at + 1);
                }
                return mask;
            }
        }

        internal void Quit(string msg)
        {
            if (OnQuit != null)
            {
                OnQuit.Invoke(this, msg);
            }
        }

        public string Name
        {
            get { return Nick; }
        }

        public void SendMessage(string msg)
        {
            Server.Send("PRIVMSG " + this.Nick + " :" + msg);
        }

        public override string ToString()
        {
            return Nick + "(" + mask + ")";
        }

        internal void ModesChanged()
        {
            if (OnModeChanged != null)
            {
                OnModeChanged.Invoke(this);
            }
        }

        internal void NickDidChange(string oldNick, string NewNick)
        {
            if (OnNickChanged != null)
            {
                OnNickChanged.Invoke(oldNick, NewNick);
            }
        }
    }
}
