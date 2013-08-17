using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reflection;

namespace IRCLib
{
    public class Server
    {
        public delegate void NetworkChangedHandler(string newNetworkName);
        public delegate void MessageHandler(Server s, Message m);
        public delegate void UserQuitHandler(Server s, User u, string msg);
        public delegate void ChannelHandler(Server s, Channel c);
        public delegate void ServerHandler(Server s);
        public delegate void ServerErrorHandler(Server s, Exception e);
        public delegate void NickChangedHandler(string oldNIck, string newNick);

        public static event ServerHandler OnConnecting;

        public event MessageHandler OnNotice;
        public event MessageHandler OnStatusMessage;
        public event ChannelHandler OnJoined;
        public event ChannelHandler OnParted;
        public event ServerHandler OnDisconnected;
        public event ServerHandler OnConnected;
        public event ServerErrorHandler OnError;
        public event NetworkChangedHandler OnNetworkChanged;

        [Obsolete("This event has been moved to the Channel object")]
        public event MessageHandler OnMsg;

        [Obsolete("This event has been moved to the Channel object")]
        public event ChannelHandler OnKicked;

        [Obsolete("This event has been moved to the Channel object")]
        public event NickChangedHandler OnNickChanged;

        [Obsolete("This event has been moved to the Channel object")]
        public event UserQuitHandler OnUserQuit;


        

        public int Port { get; private set; }
        public string HostName { get; private set; }
        public string Password { get; set; }
        public Profile Profile { get; set; }
        public ChannelModes ChannelModes = new ChannelModes("(ohv)@%+");

        private string _Network = "";

        public string Network {
            get
            {
                return _Network;
            }
            set
            {
                _Network = value;
                if (OnNetworkChanged != null)
                {
                    OnNetworkChanged.Invoke(value);
                }
            }
        }
        

        public string Nick { get; private set; }

        public TcpClient Connection = null;

        bool autoJoined = false;

        private StreamWriter Writer = null;
        private Thread Reader = null;
        public List<Channel> Channels = new List<Channel>();

        public List<Channel> AutoJoinChannels = new List<Channel>();
        
        private void ReaderStart()
        {
            try
            {
                StreamReader sr = new StreamReader(Connection.GetStream());
                while (Connection != null && Connection.Connected)
                {
                    string line = sr.ReadLine();
                    if (line == null)
                    {
                        break; // null means no connection
                    }
                    if (!string.IsNullOrEmpty(line)) // ignore blank messages too.
                    {
                        OnMessage(line);
                    }
                }
            }
            catch(ThreadAbortException)
            {
                // abort on disco
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("IRC/Server/Reader Error: " + ex);
            }
            Trace.WriteLine("IRC/Server/Reader: EXIT");
            Disconnect();
        }

        private void OnMessage(string line)
        {
            Trace.WriteLine("--> " + line);

            if (line.StartsWith("PING"))
            {
                string pong = line.Substring(5);
                if (pong.StartsWith(":"))
                {
                    pong = pong.Remove(0, 1);
                }
                Send("PONG :" + pong);
                return;
            }

            try
            {
                OnMessage(new Message(line));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("OnMessage Error: " + ex);
            }

        }

        private void OnMessage(Message m)
        {
            Event.Invoke(this, Event.ServerEventType.Message, m);
            switch (m.Command.ToUpper())
            {
                case "NOTICE":
                    if (OnNotice != null)
                    {
                        OnNotice.Invoke(this, m);
                    }
                    break;
                case "TOPIC":
                    // --> :DavX!~DavX@NewNet-F7E4C2C0.ri.ri.cox.net TOPIC #davux :Test topic
                    {
                        Channel c = FindChannel(m.Target);
                        if (c != null)
                        {
                            c.ChangeTopic(new User(this, m.Prefix), m.ListString);
                        }
                        else
                        {
                            Trace.WriteLine("IRC/Server/OnMessage/TOPIC: Invalid Channel for TOPIC: " + m);
                        }
                    }
                    break;
                case "PRIVMSG":
                    if (m.ListString == "\x001VERSION\x001")
                    {
                        Notice(m.PrefixNick, "\x001VERSION irc.net ALPHA " + Assembly.GetExecutingAssembly().GetName().Version + " " + Environment.OSVersion.Platform + "\x001");
                    }
                    else if (m.Target.ToLower() == Nick.ToLower())
                    {
                        Channel cf = FindChannel(m.PrefixNick);
                        User u = new User(this, m.Prefix);
                        if (cf != null)
                        {
                            cf.MessageRcv(u, m.ListString);
                        }
                        else
                        {
                            Channel cn = new Channel(this, m.PrefixNick);
                            Channels.Add(cn);
                            if (OnJoined != null)
                            {
                                OnJoined.Invoke(this, cn);
                            }
                            cn.MessageRcv(u, m.ListString);
                        }
                    }
                    else
                    {
                        Channel cf = FindChannel(m.Target);
                        User u = new User(this, m.Prefix);
                        if (cf != null)
                        {
                            cf.MessageRcv(u, m.ListString);
                        }
                        else
                        {
                            Channel cn = new Channel(this, m.Target);
                            cn.MessageRcv(u, m.ListString);
                            Channels.Add(cn);
                            if (OnJoined != null)
                            {
                                OnJoined.Invoke(this, cn);
                            }
                        }
                    }

                    break;
                case "JOIN":
                    {
                        if (m.PrefixNick.ToLower() == Nick.ToLower())
                        {
                            // i joined a channel!
                            Channel c = new Channel(this, m.Target);
                            if (OnJoined != null)
                            {
                                OnJoined.Invoke(this, c);
                               // Thread.Sleep(1000);
                            }
                            Channels.Add(c);
                        }
                        else
                        {
                            // someone joined a channel that im on
                            Channel c = FindChannel(m.Target);
                            if (c != null)
                            {
                                User ux = new User(this, m.Prefix);
                                if (ux != null)
                                {
                                    c.JoinUser(ux, true);
                                }
                                else
                                {
                                    Trace.WriteLine("IRC/Server/OnMessage: Invalid User for JOIN: " + m);
                                }
                            }
                            else
                            {
                                Trace.WriteLine("IRC/Server/OnMessage: Invalid Channel for JOIN: " + m);
                            }
                        }
                        break;
                    }
                case "376":
                    if (OnStatusMessage != null)
                    {
                        OnStatusMessage.Invoke(this, m);
                    }
                    if (!autoJoined)
                    {
                        autoJoined = true;
                        if (OnConnected != null)
                        {
                            OnConnected.Invoke(this);
                        }
                        JoinAutoChannels();
                    }
                    break;
                case "005":
                    {
                        string[] parts = m.AfterCommandString.Split(' ');

                        foreach (string p in parts)
                        {
                            if (p.ToUpper().StartsWith("NETWORK="))
                            {
                                // p is network
                                Network = p.Remove(0, 8);
                            }
                            else if (p.ToUpper().StartsWith("PREFIX="))
                            {
                                ChannelModes = new ChannelModes(p.Remove(0, 7));
                            }
                        }
                    }
                    break;
                case "MODE":
                // :DaveTest MODE DaveTest :+iwx
                // :irc.aohell.org MODE #davux +nt 
                    if (!autoJoined)
                    {
                        autoJoined = true;
                        if (OnConnected != null)
                        {
                            OnConnected.Invoke(this);
                        }
                        JoinAutoChannels();
                    }

                    if (m.Target == Nick)
                    {
                        // TODO own modes
                        ParseOwnModes(m.ListString);
                    }
                    else if (m.TargetIsChannel)
                    {
                        // setting chan modes
                        Channel ct = FindChannel(m.Target);
                        
                        if (ct != null)
                        {
                            User u = ct.FindUser(m.PrefixNick);
                            if (u != null)
                            {
                                u.SetMask(m.Prefix);
                            }
                            else
                            {
                                u = new User(this, m.Prefix);
                            }
                            ct.SetModes(u, m.AfterCommandString.Substring(m.AfterCommandString.IndexOf(' ')).Trim());
                        }
                        else
                        {
                            Trace.WriteLine("IRC/Server/OnMessage: Invalid Channel to MODE: " + m);
                        }
                    }
                    break;
                case "KICK":
                    // --> :DavX-!~DavX@NewNet-F7E4C2C0.ri.ri.cox.net KICK #davux DavXn :DavX-
                    {

                        Channel c = FindChannel(m.Target);
                        User kicker = c.FindUser(m.PrefixNick);
                        User kicked = c.FindUser(m.Arg2);

                        if (kicker == null || kicked == null)
                        {
                            Trace.WriteLine("IRC/Server/OnMessage/KICK: No kicker or kicked " + m);
                        }
                        else
                        {

                            if (c != null)
                            {
                                c.KickUser(kicked, kicker, m.ListString);
                            }
                            else
                            {
                                Trace.WriteLine("IRC/Server/OnMessage/KICK No Channel: " + m);
                            }
                        }
                    }
                    break;
                case "PART":
                    if (m.PrefixNick.ToLower() == Nick.ToLower())
                    {
                        // i left a channel!
                        Channel c = FindChannel(m.Target);
                        if (c != null)
                        {
                            //c.Part();
                            Channels.Remove(c);
                            if (OnParted != null)
                            {
                                OnParted.Invoke(this, c);
                            }
                        }
                        else
                        {
                            Trace.WriteLine("IRC/Server/OnMessage: Invalid Channel for PART: " + m);
                        }
                    }
                    else
                    {
                        // someone left a channel that im on
                        Channel c = FindChannel(m.Target);
                        if (c != null)
                        {
                            User ud = c.FindUser(m.PrefixNick);
                            ud.SetMask(m.Prefix);
                            if (ud != null)
                            {
                                c.PartUser(ud, m.ListString, true);
                            }
                            else
                            {
                                Trace.WriteLine("IRC/Server/OnMessage: Invalid User for PART: " + m);
                            }
                        }
                        else
                        {
                            Trace.WriteLine("IRC/Server/OnMessage: Invalid Channel for PART: " + m);
                        }
                    }
                    break;
                case "353":
                    {
                        string[] parts = m.ListString.Split(' ');

                        Channel c = FindChannel(m.Arg3);
                        if (c != null)
                        {
                            foreach (string p in parts)
                            {
                                if (!string.IsNullOrEmpty(p))
                                {
                                    string px = p;

                                    if ("" != ChannelModes.ModeForSymbol(px.Substring(0, 1)))
                                    {
                                        px = px.Remove(0, 1);
                                    }
                                    bool add = true;
                                    foreach (User u in c.Users)
                                    {
                                        if (u.Nick.ToLower() == px)
                                        {
                                            add = false;
                                            break;
                                        }
                                    }
                                    if (add)
                                    {
                                        c.JoinUser(new User(this, p), false);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Trace.WriteLine("IRC/Server/OnMessage/353: Invalid Channel: " + m);
                        }
                    }
                    break;
                    // --> :irc.aohell.org 332 DavXn #davux :This is the topic message
                    // --> :irc.aohell.org 333 DavXn #davux DavX- 1260079684
                case "332":
                    {
                        Channel chan = FindChannel(m.Arg2);
                        if (chan != null)
                        {
                            chan.Topic = m.ListString;
                        }
                        else
                        {
                            Trace.WriteLine("IRC/Server/OnMessage/332: Invalid channel: " + m);
                        }
                    }
                    break;
                case "333":
                    {
                        Channel c = FindChannel(m.Arg2);
                        if (c != null)
                        {
                            User u = new User(this, m.Arg3);
                            c.TopicDetails(u, m.Arg4);
                        }
                        else
                        {
                            Trace.WriteLine("IRC/Server/OnMessage/333: Invalid channel: " + m);
                        }
                    }
                    break;
                case "366":
                    // no action.
                    break;
                case "433":
                // --> :irc.redwolfs.net 433 * DavXn :Nickname is already in use.
                    if (OnStatusMessage != null)
                    {
                        OnStatusMessage.Invoke(this, m);
                    }
                    Send("NICK " + Profile.GetNick(m.Arg2));
                    break;
                case "NICK":
                    {
                        string oldNick = m.PrefixNick;
                        string newNick = m.Target;

                        if (oldNick.ToLower() == Nick.ToLower())
                        {
                            Nick = newNick;
                        }

                        foreach (Channel c in Channels)
                        {
                            c.NickChanged(oldNick, newNick);
                        }


                        /*
                        if (OnNickChanged != null)
                        {
                            OnNickChanged.Invoke(oldNick, newNick);
                        }
                        */
                    }
                    break;
                case "001":
                    if (m.Target != Nick)
                    {
                        string oldNick = Nick;
                        Nick = m.Target;
                        if (OnNickChanged != null)
                        {
                            OnNickChanged.Invoke(oldNick, Nick);
                        }
                    }
                    if (OnStatusMessage != null)
                    {
                        OnStatusMessage.Invoke(this, m);
                    }
                    break;
                case "QUIT":
                    // :Clfloat!i0@NewNet-EC430740.dsl.sfldmi.sbcglobal.net QUIT :Quit: Clfloat
                    {
                        User u = new User(this, m.Prefix);

                        if (OnUserQuit != null)
                        {
                            OnUserQuit(this, u, m.ListString);
                        }

                        foreach (Channel c in Channels)
                        {
                            c.UserQuit(u, m.ListString);
                        }
                    }
                    break;
                default:
                    if (OnStatusMessage != null)
                    {
                        OnStatusMessage.Invoke(this, m);
                    }
                    break;
            }
        }

        private void ParseOwnModes(string p)
        {
            //throw new NotImplementedException();
        }

        internal void JoinAutoChannels()
        {
            foreach (Channel c in AutoJoinChannels)
            {
                if (c.Name.StartsWith("#") || c.Name.StartsWith("$"))
                {
                    Join(c.Name, c.Pass);
                }
                else
                {
                    // this is a user, so automatically join them.
                    OpenConversation(c.Name);
                }
            }
        }

        public void OpenConversation(string Name)
        {
            Channel cn = new Channel(this, Name);
            Channels.Add(cn);
            if (OnJoined != null)
            {
                OnJoined.Invoke(this, cn);
            }
        }

        internal void PartChannel(Channel c)
        {
            if (OnParted != null)
            {
                OnParted.Invoke(this, c);
            }
            Channels.Remove(c);
        }

        public Channel FindChannel(string name)
        {
            string lName = name.ToLower();
            foreach (Channel c in Channels.ToArray())
            {
                if (c.Name.ToLower() == lName)
                {
                    return c;
                }
            }
            return null;
        }

        private Server() {
            AutoJoinChannels = new List<Channel>();
        }

        public Server(string HostName, int Port)
        {
            if (string.IsNullOrEmpty(HostName))
            {
                throw new ArgumentNullException("HostName");
            }
            if (Port < 0 || Port > 65535)
            {
                throw new ArgumentOutOfRangeException("Port");
            }
            AutoJoinChannels = new List<Channel>();
            this.Port = Port;
            this.HostName = HostName;
            this.Password = "";
            Event.Invoke(this, Event.ServerEventType.Created, null);
        }

        public bool Connected
        {
            get
            {
                return Connection != null && Connection.Connected;
            }
        }

        public void Connect()
        {
            new Thread(delegate()
            {
                try
                {
                    try
                    {
                        if (Reader != null)
                        {
                            Reader.Abort();
                        }
                        if (Connection != null)
                        {
                            Connection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("IRC/Server/Disconnect: " + ex);
                    }
                    new Thread(delegate()
                        {
                            if (OnConnecting != null)
                            {
                                OnConnecting.Invoke(this);
                            }
                        }).Start();
                    autoJoined = false;
                    Connection = new TcpClient();
                    Connection.Connect(HostName, Port);
                    _Network = HostName;
                    Reader = new Thread(ReaderStart);
                    Reader.Start();

                    Writer = new StreamWriter(Connection.GetStream());
                    Writer.AutoFlush = true;

                    Event.Invoke(this, Event.ServerEventType.ConnectedNoLogin, null);

                    if (!string.IsNullOrEmpty(Password))
                    {
                        Send("PASS " + Password);
                    }
                    Send("NICK " + Profile.Nick);
                    Nick = Profile.Nick;
                    Send("USER " + Profile.User + " hostname servername :" + Profile.RealName);
                    Event.Invoke(this, Event.ServerEventType.ConnectedLoginSent, null);
                    // OPER would happen here, on the event.
                }
                catch (Exception ex)
                {
                    new Thread(delegate()
                    {
                        if (OnError != null)
                        {
                            OnError.Invoke(this, ex);
                        }
                    }).Start();
                }
            }).Start();
        }

        public void Disconnect()
        {
            try
            {
                if (Reader != null)
                {
                    Reader.Abort();
                }
                if (Connection != null)
                {
                    Connection.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("IRC/Server/Disconnect: " + ex);
            }
            finally
            {
                Connection = null;
                Event.Invoke(this, Event.ServerEventType.Disconnected, null);
                if (OnDisconnected != null)
                {
                    OnDisconnected.Invoke(this);
                }
            }
        }

        public void Send(string s)
        {
            if (Connected)
            {
                try
                {
                    Trace.WriteLine("<-- " + s);
                    Writer.Write(s + "\r\n");

                }
                catch (Exception ex)
                {
                    Trace.WriteLine("IRC/Server/Send: " + ex);
                    Disconnect();
                    //throw new IRCException("Not Connected to Server (After Error)");
                }
            }
            else
            {
                throw new Exception("Not Connected to Server");
            }
        }

        public void Quit(string message)
        {
            try
            {
                Send("QUIT :" + message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Quit Error: " + ex);
            }
            Disconnect();
        }

        public void Join(string chan, string pass)
        {
            if (string.IsNullOrEmpty(chan))
            {
                throw new Exception("Cannot join blank channel");
            }
            if (string.IsNullOrEmpty(pass))
            {
                Send("JOIN :" + chan);
            }
            else
            {
                Send("JOIN :" + chan + " " + pass);
            }
        }

        public void Topic(string chan, string topic)
        {
            if (string.IsNullOrEmpty(chan))
            {
                throw new Exception("Cannot set topic on blank channel");
            }

            Send("TOPIC " + chan + " :" + topic);

        }

        internal void Part(Channel chan)
        {
            Send("PART " + chan.Name);
        }

        public void GetNames(string chan)
        {
            if (string.IsNullOrEmpty(chan))
            {
                Send("NAMES");
            }
            else
            {
                Send("NAMES " + chan);
            }
        }

        public void GetList(string chan)
        {
            if (string.IsNullOrEmpty(chan))
            {
                Send("LIST");
            }
            else
            {
                Send("LIST " + chan);
            }
        }

        public void Invite(string nick, string chan)
        {
            if (string.IsNullOrEmpty(nick))
            {
                throw new Exception("Cannot invite blank nick");
            }
            if (string.IsNullOrEmpty(chan))
            {
                throw new Exception("Cannot invite to blank channel");
            }
            Send("INVITE " + nick + " " + chan);
        }

        public void Kick(string nick, string chan, string msg)
        {
            if (string.IsNullOrEmpty(nick))
            {
                throw new Exception("Cannot kick blank nick");
            }
            if (string.IsNullOrEmpty(chan))
            {
                throw new Exception("Cannot kick from blank channel");
            }
            if (string.IsNullOrEmpty(msg))
            {
                Send("KICK " + chan + " " + nick);
            }
            else
            {
                Send("KICK " + chan + " " + nick + " :" + msg);
            }
        }

        public void Msg(string target, string msg)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new Exception("Cannot message blank target");
            }
            if (string.IsNullOrEmpty(msg))
            {
                throw new Exception("Cannot send blank message");
            }
            Send("PRIVMSG " + target + " :" + msg);
        }

        public void Notice(string target, string msg)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new Exception("Cannot notice blank target");
            }
            if (string.IsNullOrEmpty(msg))
            {
                throw new Exception("Cannot send blank notice");
            }
            Send("NOTICE " + target + " :" + msg);
        }

        public void Whois(string nick, string server)
        {
            if (string.IsNullOrEmpty(nick))
            {
                throw new IRCException("Cannot whois blank nick");
            }
            if (string.IsNullOrEmpty(server))
            {
                Send("WHOIS " + nick);
            }
            else
            {
                Send("WHOIS " + server + " " + nick);
            }
        }

        public void WhoWas(string nick, int count)
        {
            if (string.IsNullOrEmpty(nick))
            {
                throw new IRCException("Cannot whowas blank nick");
            }
            if (count <= 0)
            {
                Send("WHOWAS " + nick);
            }
            else
            {
                Send("WHOWAS " + nick + " " + count);
            }
        }

        public void Quit()
        {
            Quit(Profile.QuitMessage);
        }
    }
}
