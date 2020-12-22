using CQ2IOT;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using tech.msgp.groupmanager.Code;

namespace CQ2IOT_HOST
{
    internal class Program
    {
        private static string host;
        private static long me_qq;
        private static string key;
        private static string keyword = "";
        private static pThreadPool pool;

        private static void Main(string[] args)
        {
            logger("MainThread", "Getting IPv4 Address...");
            string ipv4_ip = NetworkInfo.GetLocalIpAddress();
            MainHolder.logger = logger;
            bool booted = false;
            Exception exc = null;
            #region 读取配置
            StreamReader cfile = new StreamReader("config.json");
            JObject config = (JObject)JsonConvert.DeserializeObject(cfile.ReadToEnd());
            cfile.Close();
            host = config["mirai"].Value<string>("server");
            me_qq = config["mirai"].Value<long>("user");
            key = config["mirai"].Value<string>("key");
            //如果是本地ip就走本地
            if (host == ipv4_ip && Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                logger("MainThread", "Running on the same server! Using 127.0.0.1 to connect mirai.");
                host = "127.0.0.1";
            }
            #endregion
            while (true)//故障自动重启
            {
                try
                {
                    //Console.Title = "ManageBot By Developer_ken - Initializing...";
                    logger("MainThread", "Pushing up the engine...", ConsoleColor.Black, ConsoleColor.Green);
                    MiraiHttpSessionOptions options = new MiraiHttpSessionOptions(host, 8080, key);
                    MainHolder.session = new MiraiHttpSession();
                    MainHolder.session.ConnectAsync(options, me_qq).Wait();
                    //MainHolder.session.GetFriendListAsync().Wait();
                    logger("MainThread", "BotAPI is up.", ConsoleColor.Black, ConsoleColor.Green);
                    pool = new pThreadPool();
                    MainHolder.pool = pool;
                    logger("MainThread", "Threadpool is UP.", ConsoleColor.Black, ConsoleColor.Green);
                    pool.submitWorkload(() =>
                    {
                        while (true)
                        {
                            logger("threadpool", "threads= " + pool.min_size + "<" + pool.size + "/" + pool.busythread + "<" + pool.max_size +
                                " | works= " + pool.queuelen + "<" + pool.queue_max_len + " | errors= " + pool.excepttionlen + "<" + pool.exceptionmaxlen
                                );
                            Dictionary<Guid, Exception> err = pool.popException();
                            if (err != null)
                            {
                                foreach (KeyValuePair<Guid, Exception> e in err)
                                {
                                    logger("threadpool_exception", "Work-" + e.Key + " -> " + e.Value.Message, ConsoleColor.DarkRed);
                                    logger("threadpool_exception", "Work-" + e.Key + " -> " + e.Value.StackTrace, ConsoleColor.DarkRed);
                                }
                            }
                            pool.clearExceptions();
                            Thread.Sleep(30000);
                        }
                    });
                    pool.onWorkloadStartProcess += startwork;
                    pool.onWorkloadStopProcess += stopwork;
                    logger("MainThread", "Events registered.", ConsoleColor.Black, ConsoleColor.Green);
                    MainHolder.INIT(config);
                    //string xml = "<?xml version='1.0' encoding='UTF-8' standalone='yes' ?><msg serviceID=\"35\" templateID=\"1\" action=\"viewMultiMsg\" brief=\"[聊天记录]\" m_resid=\"y0oBW4IOb1T2mMOQXiMI9tajqUkTEioFVMFc66YCia2fQEx2+Sp1Bogtcn80e6R+\" m_fileName=\"6858932750478640422\" tSum=\"34\" sourceMsgId=\"0\" url=\"\" flag=\"3\" adverSign=\"0\" multiMsgFlag=\"0\"><item layout=\"1\" advertiser_id=\"0\" aid=\"0\"><title size=\"34\" maxLines=\"2\" lineSpace=\"12\">群聊的聊天记录</title><title size=\"26\" color=\"#777777\" maxLines=\"4\" lineSpace=\"12\">古小艺:  《迷惑行为》新增1个影像</title><title size=\"26\" color=\"#777777\" maxLines=\"4\" lineSpace=\"12\">柠檬味的海鲜龙:  嗯呢</title><title size=\"26\" color=\"#777777\" maxLines=\"4\" lineSpace=\"12\">一只鸡蛋:  新的机器人核心功能已经快移植好了\n现在是离线状态\n这几天要辛苦大家手动处理这些…</ title >< title size =\"26\" color=\"#777777\" maxLines=\"4\" lineSpace=\"12\">呆 萌呆萌瓜:  ohhhhhhh</title><hr hidden=\"false\" style=\"0\" /><summary size=\"26\" color=\"#777777\">查看34条转发消息</summary></item><source name=\"聊天记录\" icon=\"\" action=\"\" appid=\"-1\" /></msg>";
                    //api.sendXmlMessage(417944217, xml, MsgType.GROUP);
                    //Console.Title = "ManageBot By Developer_ken - Standby";
                    try
                    {
                        MainHolder.session.GroupMessageEvt += new Event_GroupMessage().GroupMessage;
                        /*
                        han.onGroupMessageReceive += new Event_GroupMessage().GroupMessage;
                        han.onPrivateMessageReceive += new Event_PrivMessage().PrivateMessage;
                        han.onGroupMemberIncrease += new Event_GroupMemberIncrease().GroupMemberIncrease;
                        han.onGroupMemberDecrease += new Event_GroupMemberLeave().GroupMemberDecrease;
                        han.onGroupEnterRequest += new Event_GroupMemberRequest().GroupAddRequest;
                        han.onGroupMessageSendOkay += onSendSuccess;
                        */
                        logger("MainThread", "Event Recevier is UP.", ConsoleColor.Black, ConsoleColor.White);

                    }
                    catch (Exception)
                    {
                        logger("MainThread", "Event Recevier FAILED.", ConsoleColor.Black, ConsoleColor.Red);
                    }
                    logger("MainThread", "Stand by.  The bot is up and ready to go. Type to set an log filter.", ConsoleColor.Black, ConsoleColor.Green);


                    if (booted)
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup("严重故障\n机器人遭遇了不可恢复的错误，主线程无法继续运行。为了确保稳定运行，" +
                            "主线程已被系统重置。该操作成功完成，程序将会继续运行，但未保存的工作可能已经丢失。\n");
                        MainHolder.broadcaster.BroadcastToAdminGroup("错误报告\n" + exc.Message + "\nStackTrace >" + exc.StackTrace);
                    }

                    booted = true;


                    while (true)
                    {
                        string field = "";
                        do
                        {
                            ConsoleKeyInfo k = Console.ReadKey(true);
                            if (k.Key == ConsoleKey.Enter)
                            {
                                break;
                            }

                            if (k.Key == ConsoleKey.Backspace)
                            {
                                if (field.Length >= 1)
                                {
                                    field = field.Substring(0, field.Length - 1);
                                }

                                continue;
                            }
                            if (k.Key == ConsoleKey.Delete)
                            {
                                field = "";
                                continue;
                            }
                            field += k.KeyChar;
                            //Console.Title = "input>" + field + " | Press [Enter] to " + (field.Length >= 1 ? "set" : "remove") + " a filter.";
                        } while (true);
                        keyword = field;
                        if (keyword != "")
                        {
                            logger("control", "Filter set.", ConsoleColor.Black, ConsoleColor.Green);
                            //Console.Title = "Filter: " + keyword + " | Press [Enter] to remove filter";
                        }
                        else
                        {
                            logger("control", "Filter removed.", ConsoleColor.Black, ConsoleColor.Green);
                            //Console.Title = "ManageBot By Developer_ken - Standby";
                        }
                    };
                }
                catch (Exception err)
                {
                    exc = err;
                    logger("EXCEPTION", "E_FATAL " + err.Message, ConsoleColor.Black, ConsoleColor.Red);
                    logger("EXCEPTION", "STACKTRACE " + err.StackTrace, ConsoleColor.Black, ConsoleColor.Red);
                    //throw;
                }
            }
        }

        //string threadpool = "";

        public static void startwork(Guid id)
        {
            logger("WORKLOAD", id.ToString() + " - Started.");
        }

        public static void stopwork(Guid id)
        {
            logger("WORKLOAD", id.ToString() + " - Ended.");
        }

        public static void logger(string type, string msg, ConsoleColor backcolor = ConsoleColor.Black, ConsoleColor frontcolor = ConsoleColor.White)
        {
            //if (type.ToLower().IndexOf("debug") > -1) return;//隐藏所有debug信息
            lock ("MAIN_LOGGER")
            {
                if (((keyword.Length > 1 && keyword.Substring(0, 1) == "-") && !(type.ToLower().IndexOf(keyword.ToLower().Substring(1)) > -1)) ||
                    (keyword.Length <= 1 || type.ToLower().IndexOf(keyword.ToLower()) > -1) ||
                    type == "control")
                {
                    Console.BackgroundColor = backcolor;
                    Console.ForegroundColor = frontcolor;
                    Console.Write(type);
                    Console.ResetColor();
                    Console.Write("->" + msg + "\n");
                }
            }
        }
    }
}
