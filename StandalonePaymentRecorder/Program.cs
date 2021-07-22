using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using tech.msgp.groupmanager.Code;

namespace StandalonePaymentRecorder
{
    class Program
    {
        DataBase db;
        BiliDanmakuProcessor dmk;
        static void Main(string[] args)
        {
            Console.WriteLine("Standalone Payment Recorder");
            Console.Write("Reading config...");
            StreamReader cfile = new StreamReader("config.json");
            Console.WriteLine("Done");
            JObject config = (JObject)JsonConvert.DeserializeObject(cfile.ReadToEnd());
            DataBase db = new DataBase(config.Value<string>("addr"),
                config.Value<string>("user"), config.Value<string>("passwd"));
            dmk = new BiliDanmakuProcessor(2064239);
        }
    }
}
