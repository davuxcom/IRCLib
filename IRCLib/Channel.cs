using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IRCLib
{
    public class Channel : IMessageable
    {
        public delegate void StatusMessageHandler(string status);
        public delegate void MessageHandler(User u, string msg);
        public delegate void TopicChanged(Channel c, string topic);
        public delegate void UserAction(Channel c, User u, string msg, bool loudly);
        public delegate void KickHandler(Channel c, User kicked, User kicker, string msg);
        public event TopicChanged OnTopicChanged;
        public event UserAction OnUserJoined;
        public event UserAction OnUserParted;
        public event KickHandler OnUserKicked;
        public event MessageHandler OnModeChanged;
        public event MessageHandler TopicDetailsReply;
        public event MessageHandler TopicSet;
        public event MessageHandler Quit;
        public event StatusMessageHandler OnStatusMessage;

        public event MessageHandler OnMessage;

        public event MessageHandler OnQuit;
        

        //public bool UseMessageHandler = false;
        //public Queue<object[]> Messages = new Queue<object[]>();

        public List<string> Modes = new List<string>();

        public string Name { get; set; }
        public string Pass { get; set; }
        public Server Server { get; private set; }
        public List<User> Users { get; private set; }

        public bool IsChannel
        {
            get
            {
                return Name.StartsWith("#") || Name.StartsWith("&");
            }
        }

        public override string ToString()
        {
            string ch = "irc::channel " + Name + "\n\n";
            foreach (User u in Users)
            {
                ch += u + "\n";
            }
            return ch;
        }

        private string _Topic = "";

        public string Topic
        {
            get
            {
                return _Topic;
            }
            set
            {
                if (OnTopicChanged != null)
                {
                    OnTopicChanged.Invoke(this, value);
                }
                _Topic = value;
            }
        }


        public Channel() { }

        internal Channel(Server Server, string Name)
        {
            this.Server = Server;
            this.Name = Name;
            Users = new List<User>();
        }

        public void Part()
        {
            if (Name.StartsWith("#") || Name.StartsWith("&"))
            {
                Server.Part(this);
            }
            else
            {
                Server.PartChannel(this);
            }
        }

        internal void JoinUser(User u, bool loudly)
        {
            Trace.WriteLine("IRCLib/Channel/JoinUser: " + u);
            Users.Add(u);
            if (OnUserJoined != null)
            {
                OnUserJoined.Invoke(this, u, "", loudly);
            }
        }

        internal void PartUser(User u, string msg, bool loudly)
        {
            Users.Remove(u);
            if (OnUserParted != null)
            {
                OnUserParted.Invoke(this, u, msg, loudly);
            }
        }

        internal void MessageRcv(User u, string msg)
        {
            if (OnMessage != null /* && UseMessageHandler */)
            {
                OnMessage.Invoke(u, msg);
            }
            else
            {
                //Messages.Enqueue(new object[] { u, msg });
            }
        }

        internal void NickChanged(string oldNick, string NewNick)
        {
            User u = FindUser(oldNick);
            if (u != null)
            {
                u.NickDidChange(oldNick, NewNick);
            }
            else
            {
                Trace.WriteLine("IRCLib/Channel/NickChanged Blank: " + oldNick + " " + NewNick);
            }
        }

        internal void UserQuit(User u, string msg)
        {
            u.Quit(msg);
        }

        internal void TopicDetails(User u, string msg)
        {
            if (TopicDetailsReply != null)
            {
                TopicDetailsReply.Invoke(u, msg);
            }
        }

        internal void ChangeTopic(User u, string newTopic)
        {
            if (TopicSet != null)
            {
                Trace.WriteLine("IRCLib/Channel/ChangeTopic: " + newTopic);
                TopicSet.Invoke(u, newTopic);
            }
        }

        internal void KickUser(User kicked, User kicker, string msg)
        {

            if (OnUserKicked != null)
            {
                OnUserKicked.Invoke(this, kicked, kicker, msg);
            }

        }

        internal void SetModes(User u, string modes)
        {
            //  Parameters: <channel> {[+|-]|o|p|s|i|t|n|b|v} [<limit>] [<user>] [<ban mask>]
            Trace.WriteLine("Adding Modes: " + modes);
            try
            {
                bool plusMode = false;
                int argc = 1;
                if (modes.StartsWith("+") || modes.StartsWith("-"))
                {
                    string[] pa = modes.Split(' ');

                    for (int i = 0; i < pa[0].Length; i++)
                    {
                        char c = pa[0][i];
                        if (c == '+')
                        {
                            plusMode = true;
                        }
                        else if (c == '-')
                        {
                            plusMode = false;
                        }
                        else
                        {
                            // parse the mode
                            if (Server.ChannelModes.SymbolForMode(c.ToString()) != "")
                            {
                                // this is a user mode in PREFIX
                                // and requires an argument
                                if (argc < pa.Length)
                                {
                                    string arg = pa[argc];

                                    User ua = FindUser(arg);

                                    if (ua != null)
                                    {

                                        if (plusMode)
                                        {
                                            Trace.WriteLine("Adding Mode " + c + " to user " + ua);
                                            ua.Modes.Add(c.ToString());
                                        }
                                        else
                                        {
                                            Trace.WriteLine("Removing Mode " + c + " from user " + ua);
                                            ua.Modes.Remove(c.ToString());
                                        }
                                        ua.ModesChanged();
                                    }
                                    else
                                    {
                                        Trace.WriteLine("Couldn't find user: " + ua);
                                    }

                                    argc++;
                                }
                                else
                                {
                                    Trace.WriteLine("Can't parse mode, missing expected target");
                                }
                            }
                            else
                            {
                                // channel modes!
                                if (plusMode)
                                {
                                    Modes.Add(c.ToString());
                                }
                                else
                                {
                                    if (Modes.Contains(c.ToString()))
                                    {
                                        Modes.Remove(c.ToString());
                                    }
                                    else
                                    {
                                        Trace.WriteLine("IRC/Channel/SetModes: Attempted to remove mode that chanel doesn't have: " + c);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Trace.WriteLine("IRC/Channel/Invalid Mode: " + modes);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error Setting Modes: " + ex);
            }


            if (OnModeChanged != null)
            {
                OnModeChanged.Invoke(u, modes);
            }
        }

        public void SendMessage(string msg)
        {
            if (Server.Connected)
            {
                Server.Send("PRIVMSG " + this.Name + " :" + msg);
            }
            else
            {
                if (OnStatusMessage != null)
                {
                    OnStatusMessage.Invoke("Not Connected to IRC.");
                }
            }
        }

        internal User FindUser(string nick)
        {
            string lNick = nick.ToLower();
            foreach (User u in Users.ToArray())
            {
                if (lNick == u.Nick.ToLower())
                {
                    return u;
                }
            }
            return null;
        }
    }
}
