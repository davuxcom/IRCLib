using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRCLib;
using System.Threading;

namespace IRCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IRCLib.Server s = new IRCLib.Server("irc.newnet.net", 6667);
            IRCLib.Profile p = new IRCLib.Profile();

            p.Nick = "DaveTest";
            p.User = "Davux";
           // p.RealName = "Dave IRC Test";
            

            //s.OnStatusMessage += new IRCLib.Server.MessageHandler(s_OnStatusMessage);
            //s.OnJoined += new Server.ChannelHandler(s_OnJoined);
            s.OnConnected += new Server.ServerHandler(s_OnConnected);
            s.Profile = p;
            s.Connect();



            while (true)
            {
                Console.Write("irc: ");
                string line = Console.ReadLine();
                if (line == "show")
                {
                    foreach (Channel c in s.Channels.ToArray())
                    {
                        Console.WriteLine(c);
                    }
                }
                else
                {
                    s.Send(line);
                }

            }
        }

        static void s_OnConnected(Server s)
        {
            Console.WriteLine("CONNECTED *************");
            while (!s.Connected)
            {
                Thread.Sleep(1000);
            }


            s.Join("#davux", null);
        }

        static void s_OnJoined(Server s, Channel c)
        {
            Console.WriteLine("Joined Channel: " + c.Name);
        }

        static void s_OnStatusMessage(Server s, Message m)
        {
            Console.WriteLine(m.ListString);
        }
    }
}
