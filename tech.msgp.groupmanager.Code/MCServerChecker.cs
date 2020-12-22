using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using System;
using System.Collections.Generic;
using System.Threading;

namespace tech.msgp.groupmanager.Code
{
    internal class MCServerChecker
    {
        public static Dictionary<string, MCServer> servers = new Dictionary<string, MCServer>();
        public static Dictionary<string, bool> results = new Dictionary<string, bool>();
        public static Thread main;
        public static DateTime last_update;
        //public static List<long> sent = new List<long>();
        public static void startthreads()
        {
            if (main != null && main.IsAlive)
            {
            }
            else
            {
                //servers.Add("a", new MCServer { name = "主线", addr = "mc.luye.furrytale.cn", port = 25500 });
                servers.Add("b", new MCServer { name = "镇江多线", addr = "mc1.luye.furrytale.cn", port = 20559 });
                servers.Add("c", new MCServer { name = "佛山电信", addr = "mc2.luye.furrytale.cn", port = 20559 });
                servers.Add("d", new MCServer { name = "宿迁移动", addr = "mc3.luye.furrytale.cn", port = 20559 });
                main = new Thread(new ThreadStart(run));
                main.Start();
            }
        }

        public struct MCServer
        {
            public string name;
            public string addr;
            public ushort port;
#pragma warning disable CS0649 // 从未对字段“MCServerChecker.MCServer.isonline”赋值，字段将一直保持其默认值 false
            public bool isonline;
#pragma warning restore CS0649 // 从未对字段“MCServerChecker.MCServer.isonline”赋值，字段将一直保持其默认值 false
#pragma warning disable CS0649 // 从未对字段“MCServerChecker.MCServer.onlines”赋值，字段将一直保持其默认值 0
            public int onlines;
#pragma warning restore CS0649 // 从未对字段“MCServerChecker.MCServer.onlines”赋值，字段将一直保持其默认值 0
        }

        public static void run()
        {
            while (true)
            {
                foreach (KeyValuePair<string, MCServer> s in servers)
                {
                    ForgeInfo forgeInfo = null;
                    int pversion = 0;
                    string addr = s.Value.addr;
                    ushort port = s.Value.port;
                    ProtocolHandler.MinecraftServiceLookup(ref addr, ref port);
                    bool result = ProtocolHandler.GetServerInfo(addr, port, ref pversion, ref forgeInfo);
                    if (results.ContainsKey(s.Key))
                    {
                        results.Remove(s.Key);
                    }

                    results.Add(s.Key, result);
                }
                last_update = DateTime.Now;
                Thread.Sleep(30000);
            }
        }
    }
}
