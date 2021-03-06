﻿using CQ2IOT;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BiliApi;
using tech.msgp.groupmanager.Code.TCPMessageProcessor;
using System.Web;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BiliApi.Auth;
using tech.msgp.groupmanager.Code.ScriptHandler;
using tech.msgp.groupmanager.Code.MCServer;
using Newtonsoft.Json;

namespace tech.msgp.groupmanager.Code
{
    public class MainHolder
    {
        public const double GLOBAL_WARN_WEIGHT = 1F / 3F;

        public static List<BiliSpaceDynamic> dynamics = new List<BiliSpaceDynamic>();
        public static Broadcaster broadcaster;
        public static BiliDanmakuProcessor bilidmkproc;
        public static List<long> issending = new List<long>();
        public static TCPMessageServer tms;
        public static int MsgCount;
        public static List<long> friends;
        public static pThreadPool pool;
        public static MiraiHttpSession session;
        public static BiliApi.Auth.QRLogin bililogin;
        public static BiliApi.ThirdPartAPIs biliapi;
        public static bool doBiliLogin = true;

        /// <summary>
        /// 推送动态的B站UID列表
        /// </summary>
        public static List<int> BiliWatchUIDs;
        /// <summary>
        /// 侦听直播的直播间号
        /// </summary>
        public static int LiveRoom;
        /// <summary>
        /// 是否启用B站弹幕、私信功能
        /// </summary>
        public static bool useBiliRecFuncs;
        /// <summary>
        /// 是否启用鹿野高度定制的功能 
        /// 这些功能已经高度定制，难以在短时间内移植
        /// </summary>
        public static bool enableNativeFuncs;
        public static logger_ logger;

        public delegate void logger_(string cato, string msg, ConsoleColor backcolor = ConsoleColor.Black, ConsoleColor frontcolor = ConsoleColor.White);


        public static class Logger
        {
            public static void Error(string cato, string msg)
            {
                MainHolder.logger(cato, msg, System.ConsoleColor.DarkRed);
            }
            public static void Warning(string cato, string msg)
            {
                MainHolder.logger(cato, msg, System.ConsoleColor.DarkYellow);
            }
            public static void Debug(string cato, string msg)
            {
                MainHolder.logger(cato, msg, System.ConsoleColor.Black, System.ConsoleColor.Gray);
            }
            public static void Info(string cato, string msg)
            {
                MainHolder.logger(cato, msg);
            }
        }

        public static void refreshFriendsList()
        {
            IFriendInfo[] f = session.GetFriendListAsync().Result;
            friends = new List<long>();
            foreach (IFriendInfo fr in f)
            {
                friends.Add(fr.Id);
            }
        }
        public static void INIT(JObject config)
        {

            while (true)
            {
                try
                {
                    ConnectionPool.initConnections(
                        config["sql"].Value<string>("server"), config["sql"].Value<string>("user"), config["sql"].Value<string>("passwd"),
                        config["minecraft"].Value<string>("server"), config["minecraft"].Value<string>("user"), config["minecraft"].Value<string>("passwd")
                        );
                    MainHolder.Logger.Info("SideLoad", "DBConnectiong pool is UP.");
                    break;
                }
                catch (Exception e)
                {
                    MainHolder.logger("SideLoad", "FATAL - DBConnectiong FAILED.", ConsoleColor.Black, ConsoleColor.Red);
                    MainHolder.logger("Database", e.Message, ConsoleColor.Black, ConsoleColor.Yellow);
                    MainHolder.logger("Database", e.StackTrace, ConsoleColor.Black, ConsoleColor.Yellow);
                }
            }

            //B站登录、加载相关模块
            pool.submitWorkload(new pThreadPool.workload(() =>
            {
                while (true)
                {
                    try
                    {
                        while (!doBiliLogin) Thread.Sleep(500);
                        while (true)
                        {
                            if (File.Exists("bili_login_info.json"))
                            {
                                bililogin = null;
                                try
                                {
                                    var js = File.ReadAllText("bili_login_info.json");
                                    bililogin = JsonConvert.DeserializeObject<QRLogin>(js);

                                    if (bililogin.IsOnline())
                                    {
                                        logger("Bililogin", "已使用预先保存的状态登录");
                                        broadcaster.BroadcastToAdminGroup("已从存档恢复相关数据并获取必要的授权，将释放被挂起的模块。");
                                        break;
                                    }
                                    else
                                    {
                                        logger("Bililogin", "保存的登录状态不可用");
                                    }
                                }
                                catch
                                {
                                    logger("Bililogin", "未能从bililogin.bin中恢复保存的登录状态");
                                }
                            }
                            bililogin = new BiliApi.Auth.QRLogin();
                            broadcaster.BroadcastToAdminGroup(new IMessageBase[] {
                            new ImageMessage(null, "https://api.pwmqr.com/qrcode/create/?url=" + HttpUtility.UrlEncode(bililogin.QRToken.ScanUrl), null),
                            new PlainMessage("Token="+bililogin.QRToken.OAuthKey+"\n部分模块依赖B站账号访问权，已挂起。授权完成后将释放它们。")
                            });
                            bililogin.Login();
                            broadcaster.BroadcastToAdminGroup("已获取必要的授权，将释放被挂起的模块。");
                            if (File.Exists("bili_login_info.json")) File.Delete("bili_login_info.json");
                            try
                            {
                                var js = JsonConvert.SerializeObject(bililogin, Formatting.None);
                                File.WriteAllText("bili_login_info.json", js);
                            }
                            catch (Exception err)
                            {
                                broadcaster.BroadcastToAdminGroup("BiliApi.NET返回了一处错误：" + err.Message + "\n 该错误不致命，将忽略该错误并继续执行剩余操作。");
                            }
                            break;
                        }
                        biliapi = new BiliApi.ThirdPartAPIs(bililogin.Cookies);
                        break;
                    }
                    catch (Exception err)
                    {
                        broadcaster.BroadcastToAdminGroup("BiliApi.NET返回了一处错误：" + err.Message + "\n输入#lb再次尝试登录");
                        doBiliLogin = false;
                        Thread.Sleep(1000);
                    }
                }

                //B站登录完成，加载相关模块
                try
                {
                    BiliUser.userlist = new Dictionary<int, BiliUser>();
                    dynamics = new List<BiliSpaceDynamic>();
                    string str = "";
                    foreach (Cookie c in bililogin.Cookies)
                    {
                        str += c.Name + "=" + c.Value + ";";
                    }
                    bilidmkproc = new BiliDanmakuProcessor(2064239, str);
                    bilidmkproc.Init_connection();
                    MainHolder.logger("SideLoad", "BLive-DMKReceiver is UP.", ConsoleColor.Black, ConsoleColor.White);
                }
                catch (Exception)
                {
                    MainHolder.logger("SideLoad", "BLive-DMKReceiver FAILED.", ConsoleColor.Black, ConsoleColor.Red);
                }
                try
                {
                    if (MainHolder.useBiliRecFuncs)
                    {
                        PrivmessageChecker.startthreads();
                        MainHolder.logger("SideLoad", "BiliPrivMessageReceiver is UP.", ConsoleColor.Black, ConsoleColor.White);
                    }
                    else
                    {
                        MainHolder.logger("SideLoad", "BiliPrivMessageReceiver is DISABLED.", ConsoleColor.Black, ConsoleColor.White);
                    }
                }
                catch (Exception)
                {
                    MainHolder.logger("SideLoad", "BiliPrivMessageReceiver FAILED.", ConsoleColor.Black, ConsoleColor.Red);
                }
                try
                {
                    /*
                     AdminJScriptHandler.JsEngine.SetValue("BiliAPI", biliapi);
                     AdminJScriptHandler.JsEngine.SetValue("StreamMonitor", bilidmkproc.lr.sm);
                     AdminJScriptHandler.JsEngine.SetValue("LiveRoom", bilidmkproc.blr);
                     AdminJScriptHandler.JsEngine.SetValue("DataBase", DataBase.me);
                     AdminJScriptHandler.JsEngine.SetValue("MCDataBase", DBHandler.me);
                     */
                }
                catch
                {
                    //不处理错误
                }
                return;
                try
                {
                    DynChecker.startthreads();
                    MainHolder.logger("SideLoad", "DynChecker is UP.", ConsoleColor.Black, ConsoleColor.White);
                }
                catch (Exception)
                {
                    MainHolder.logger("SideLoad", "DynChecker FAILED.", ConsoleColor.Black, ConsoleColor.Red);
                }
            }));

            try
            {
                AdminJScriptHandler.InitEngine();
                UserJScriptHandler.InitEngine();

                //AdminJScriptHandler.JsEngine.SetValue("biliready", false);

                MainHolder.logger("SideLoad", "JSEngine is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "JSEngine FAILED.", ConsoleColor.Black, ConsoleColor.Red);
            }

            try
            {
                broadcaster = new Broadcaster();
                MainHolder.logger("SideLoad", "Broadcaster is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "Broadcaster FAILED.", ConsoleColor.Black, ConsoleColor.Red);
            }

            try
            {
                tms = new TCPMessageServer(15510);
                tms.init_server_async();
                MainHolder.logger("SideLoad", "Connection-Point service is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "Connection-Point service FAILED.", ConsoleColor.Black, ConsoleColor.Red);
            }

            try
            {
                SecondlyTask.startthreads();
                MainHolder.logger("SideLoad", "ScheduledTask-Manager is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "FATAL ScheduledTask-Manager FAILED.", ConsoleColor.Black, ConsoleColor.Red);
            }

            try
            {
                /*
                han.host.threadpool.submitWorkload(() =>
                {
                    while (true)
                    {
                        try
                        {
                            broadcaster.processQueueMsgSend();
                        }
                        catch
                        {
                            logger.Warning("msgsendqueue_FATAL", "消息发送环路出现严重错误，已忽略本次发送");
                        }
                    }
                });
                */
                MainHolder.logger("SideLoad", "Broadcaster-SenderLoop is NOLONGER HOSTED.", ConsoleColor.Black, ConsoleColor.Yellow);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "Broadcaster-SenderLoop FAILED.", ConsoleColor.Black, ConsoleColor.Red);
            }

            //MCServerChecker.startthreads();
            MainHolder.logger("SideLoad", "MCServer checker is DISABLED.", ConsoleColor.Black, ConsoleColor.Red);
        }


        public static void clearcache()
        {
            BiliUser.userlist.Clear();
        }

        public static void checkCrewGroup()
        {
            Dictionary<int, long> l = DataBase.me.listCrewBound();
            List<long> g = DataBase.me.getCrewGroup();
            List<long> crews = new List<long>();
            string msg = "";
            foreach (long gg in g)
            {
                IGroupMemberInfo[] gpmemberlist = session.GetGroupMemberListAsync(gg).Result;
                foreach (IGroupMemberInfo ginfo in gpmemberlist)
                {
                    crews.Add(ginfo.Id);
                }
            }
            foreach (KeyValuePair<int, long> kvp in l)
            {
                if (!crews.Contains(kvp.Value))
                {
                    BiliUser u = new BiliUser(kvp.Key, biliapi);
                    msg += "Bili:" + u.name + " => QQ:" + kvp.Value + "\n";
                }
            }
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群维护]\n以下舰长已绑定QQ但是还没有进入舰长群：\n" + msg);
        }

        public static string getLocalIP()
        {
            //得到本机名 
            string hostname = Dns.GetHostName();
            //解析主机名称或IP地址的system.net.iphostentry实例。
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
            if (localhost != null)
            {
                foreach (IPAddress item in localhost.AddressList)
                {
                    //判断是否是内网IPv4地址
                    if (item.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return item.MapToIPv4().ToString();
                    }
                }
            }
            return "127.0.0.1";
        }
    }
}
