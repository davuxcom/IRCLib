using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCLib
{
    public class IRCException : Exception
    {
        public IRCException(string message) : base(message)
        {
    
        }
    }
    public class Message
    {
        public string Prefix { get; private set; }
        public string Command { get; private set; }
        public string Target { get;  set; }
        public string Arg2 { get; set; }
        public string Arg3 { get; set; }
        public string Arg4 { get; set; }
        public string ListString { get;  set; }
        public string AfterCommandString { get; set; }

        private string msg = "";

        public override string ToString()
        {
            return msg; // return "[Prefix " + Prefix + "] [Cmd " + Command + "] [Target " + Target + "] [Target2 " + Arg2 + "]  [Target3 " + Arg3 + "]  [List " + ListString + "]";
        }

        public bool TargetIsChannel
        {
            get
            {
                return Target.StartsWith("#") || Target.StartsWith("&");
            }
        }

        public string PrefixNick
        {
            get
            {
                if (Prefix.IndexOf("!") > -1)
                {
                    return Prefix.Substring(0, Prefix.IndexOf("!"));
                }
                else
                {
                    return Prefix;
                }
            }
        }

        public string PrefixUser
        {
            get
            {
                int loc = Prefix.IndexOf("!");
                int at = Prefix.IndexOf("@");
                if (loc > -1 && at > -1)
                {
                    return Prefix.Substring(Prefix.IndexOf("!"), at - loc );
                }
                else
                {
                    return Prefix;
                }
            }
        }

        public string PrefixHost
        {
            get
            {
                int loc = Prefix.IndexOf("@");
                if (loc > -1 && loc < Prefix.Length - 1)
                {
                    return Prefix.Substring(Prefix.IndexOf("@") + 1);
                }
                else
                {
                    return Prefix;
                }
            }
        }

        public Message(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                throw new IRCException("Empty Message");
            }
            this.msg = msg;
            string[] parts = msg.Split(new char[] { ' ' });

            if (parts.Length >= 2)
            {
                bool AddtoList = false;
                ListString = "";
                for (int i = 1; i < parts.Length; i++)
                {
                    string p = parts[i];
                    if (p.StartsWith(":"))
                    {
                        AddtoList = true;
                    }

                    if (AddtoList)
                    {
                        ListString += p + " ";
                    }
                }
                ListString = ListString.Trim();


                Prefix = parts[0];
                if (Prefix.StartsWith(":"))
                {
                    Prefix = Prefix.Remove(0, 1);
                }
                Command = parts[1];

                string la = "";
                for (int i = 2; i < parts.Length; i++)
                {
                    la += " " + parts[i];
                }
                la = la.Trim();
                if (la.StartsWith(":"))
                {
                    la = la.Remove(0, 1);
                }
                AfterCommandString = la;

                Target = parts[2];
                if (Target.StartsWith(":"))
                {
                    Target = Target.Remove(0, 1);
                }
                if (parts.Length > 3)
                {
                    Arg2 = parts[3];
                    if (Arg2.StartsWith(":"))
                    {
                        Arg2 = Arg2.Remove(0, 1);
                    }
                }
                if (parts.Length > 4)
                {
                    Arg3 = parts[4];
                    if (Arg3.StartsWith(":"))
                    {
                        Arg3 = Arg3.Remove(0, 1);
                    }
                }
                if (parts.Length > 5)
                {
                    Arg4 = parts[5];
                    if (Arg4.StartsWith(":"))
                    {
                        Arg4 = Arg4.Remove(0, 1);
                    }
                }
                if (ListString.StartsWith(":"))
                {
                    ListString = ListString.Remove(0, 1);
                }

                
            }
            else
            {
                throw new IRCException("Invalid Message: " + msg);
            }
        }
    }
}
