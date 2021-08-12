using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace tech.msgp.groupmanager.Code
{
    class WatchDog
    {
        static Socket udp;
        static bool ready = false;
        static void Init()
        {
            if (ready) return;
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(new IPEndPoint(IPAddress.Loopback, 6002));
            ready = true;
        }

        public static void FeedDog(string type)
        {
            Init();
            byte[] buffer = Encoding.UTF8.GetBytes(type);
            udp.SendTo(buffer, new IPEndPoint(IPAddress.Loopback, 6001));
        }
    }
}
