using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCLib
{
    class Event
    {
        public static void Invoke(Server s, ServerEventType e, object o)
        {

        }

        public enum ServerEventType
        {
            ConnectedNoLogin = 1,
            ConnectedLoginSent = 2,
            Created = 3,
            Disconnected = 4,
            Connected = 5,
            Message = 6,
            JoiningChannel = 7,
            JoinedChannel = 8,
            KickingUser = 9,
            KickedUser = 10,

        }
    }
}
