using BiliApi;
using BiliveDanmakuAgent;
using BiliveDanmakuAgent.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using tech.msgp.groupmanager.Code;

namespace StandalonePaymentRecorder
{
    class Program
    {
        static ReduDataBase db;
        static LiveRoom lr;
        static int lid = -1;
        static void Main(string[] args)
        {
            Console.WriteLine("Standalone Payment Recorder");
            //Console.Write("Reading config...");
            //StreamReader cfile = new StreamReader("config.json");
            //Console.WriteLine("Done");
            //JObject config = (JObject)JsonConvert.DeserializeObject(cfile.ReadToEnd());
            db = new ReduDataBase("rm-uf6ewx55v3s1iu4ok6o.mysql.rds.aliyuncs.com",
                "reduluye", "#luyeredundancy%");
            lr = new LiveRoom(2064239);
            lr.sm.ReceivedDanmaku += Receiver_ReceivedDanmaku;
            lr.sm.StreamStarted += StreamStarted;
            lr.sm.ExceptionHappened += Sm_ExceptionHappened; ;
            lr.sm.LogOutput += Sm_LogOutput;
            lr.init_connection();
            db.connect();
            while (true) Thread.Sleep(int.MaxValue);
        }

        private static void Sm_LogOutput(object sender, string text)
        {
            Console.WriteLine(text);
            db.log(text);
        }

        private static void Sm_ExceptionHappened(object sender, Exception e, string desc)
        {
            db.logErr(desc + ": " + e.Message);
        }

        private static void StreamStarted(object sender, StreamStartedArgs e)
        {
            lid = TimestampHandler.GetTimeStamp(DateTime.Now);
        }

        private static void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            var d = e.Danmaku;
            string guid = "";
            switch (d.MsgType)
            {
                case MsgTypeEnum.GuardBuy:
                    guid = Guid.NewGuid().ToString("N");
                    db.logCREW(d.UserID, d.UserGuardLevel, d.GiftCount, TimestampHandler.GetTimeStamp(DateTime.Now), guid);
                    break;
                case MsgTypeEnum.LiveEnd:
                    lid = -1;
                    break;
            }
            db.logDMK(TimestampHandler.GetTimeStamp16(DateTime.Now), d.RawData, lid, guid);
        }
    }
}
