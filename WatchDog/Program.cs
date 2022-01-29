using Mirai_CSharp;
using Mirai_CSharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WatchDog
{
    class Program
    {
        static Socket server;
        static int t1 = 0, t2 = 0, t3 = 0;
        static int th1 = 4 * 60, th2 = 8, th3 = 60 * 60 * 5;

        static MiraiHttpSession session;
        static string host, key;
        static long me_qq;
        static int port;
        static MiraiHttpSessionOptions op;

        public static string _get_gzip(string url)
        {
            try
            {
                string retString = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stm = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                StreamReader streamReader = new StreamReader(stm);
                retString = streamReader.ReadToEnd();
                streamReader.Close();
                stm.Close();
                return retString;
            }
            catch (Exception)
            {
                return "";
            }
        }
        static void Main(string[] args)
        {

            var data = _get_gzip("http://check.uomg.com/api/qq/qlevel?token=4c78dff53217b19f6c8e8c6628f9e6d6&qq=1250542735");
            server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            server.Bind(new IPEndPoint(IPAddress.Loopback, 6001));//绑定端口号和IP
            StreamReader cfile = new StreamReader("config.json");
            JObject config = (JObject)JsonConvert.DeserializeObject(cfile.ReadToEnd());
            cfile.Close();
            host = config["mirai"].Value<string>("server");
            me_qq = config["mirai"].Value<long>("user");
            key = config["mirai"].Value<string>("key");
            try
            {
                port = config["mirai"].Value<int>("port");
            }
            catch
            {
                port = 8080;
            }
            op = new MiraiHttpSessionOptions(host, port, key);
            session = new MiraiHttpSession();
            session.ConnectAsync(op, me_qq).Wait();
            //MainHolder.session.GetFriendListAsync().Wait();
            session.DisconnectedEvt += Session_DisconnectedEvt; ;


            Console.WriteLine("服务端已经开启");
            Thread t = new Thread(ReciveMsg);//开启接收消息线程
            t.Start();
            int tt = 0;
            int day = 0;
            while (true)
            {

                DateTime time = DateTime.Now;
                if (time.DayOfYear != day && time.TimeOfDay.TotalHours >= 4)
                {
                    day = time.DayOfYear;
                    session.SendGroupMessageAsync(1047079635, new PlainMessage("看门狗\n24H Uptime已至\n重启manabot.dll..."));
                    session.SendGroupMessageAsync(1047079635, new PlainMessage("supervisorctl restart manabot".RunInShell()));
                    t1 = 0; t2 = 0; t3 = 0;
                    Thread.Sleep(5000);
                }

                t1++; t2++; t3++;
                if (t1 > th1 || t2 > th2 || t3 > th3)
                {
                    session.SendGroupMessageAsync(1047079635, new PlainMessage("看门狗\n模块超时：" + t1 + "," + t2 + "," + t3 + "\n重启manabot.dll..."));
                    //PKILL("CQ2IOT_HOST");
                    session.SendGroupMessageAsync(1047079635, new PlainMessage("supervisorctl restart manabot".RunInShell()));
                    t1 = 0; t2 = 0; t3 = 0;
                    Thread.Sleep(5000);
                }
                Thread.Sleep(1000);
                tt++;
                if (tt > 5)
                {
                    tt = 0;
                    Console.WriteLine(t1 + "," + t2 + "," + t3);
                }
            }
        }

        private static void PKILL(string name)
        {
            System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName(name);
            foreach (System.Diagnostics.Process p in process)
            {
                p.Kill();
            }
        }

        private async static System.Threading.Tasks.Task<bool> Session_DisconnectedEvt(MiraiHttpSession sender, Exception e)
        {
            while (true)
                try
                {
                    session.ConnectAsync(op, me_qq).Wait();
                    break;
                }
                catch
                {
                    Thread.Sleep(2000);
                }
            return false;
        }

        /// <summary>
        /// 接收发送给本机ip对应端口号的数据报
        /// </summary>
        static void ReciveMsg()
        {
            while (true)
            {
                EndPoint point = new IPEndPoint(IPAddress.Any, 0);//用来保存发送方的ip和端口号
                byte[] buffer = new byte[1024];
                int length = server.ReceiveFrom(buffer, ref point);//接收数据报
                string message = Encoding.UTF8.GetString(buffer, 0, length);
                Console.WriteLine(point.ToString() + message);
                switch (message)
                {
                    case "grpmsg":
                        t3 = 0;
                        break;
                    case "pmsgchk":
                        t1 = 0;
                        break;
                    case "sckt":
                        t2 = 0;
                        break;
                }
            }
        }
    }
}
