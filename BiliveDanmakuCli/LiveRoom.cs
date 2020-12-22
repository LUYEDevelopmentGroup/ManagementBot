using BililiveRecorder.Core;
using System;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace BiliveDanmakuCli
{
    public class LiveRoom
    {
        public int rid;
        private readonly ClientWebSocket sock;
        public StreamMonitor sm;
        public LiveRoom(int roomid)
        {
            rid = roomid;
            sm = new StreamMonitor(roomid, new Func<TcpClient>(Tcpcli));
            sock = new ClientWebSocket();
        }

        public bool init_connection()
        {
            return sm.Start();
        }

        public TcpClient Tcpcli()
        {
            return new TcpClient();
        }
    }
}
