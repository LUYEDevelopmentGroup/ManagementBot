﻿using CQ2IOT;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using tech.msgp.groupmanager.Code.BiliAPI;
using tech.msgp.groupmanager.Code.TCPMessageProcessor;

namespace tech.msgp.groupmanager.Code
{
    public class MainHolder
    {
        public static List<BiliSpaceDynamic> dynamics = new List<BiliSpaceDynamic>();
        public static Broadcaster broadcaster;
        public static BiliDanmakuProcessor bilidmkproc;
        public static List<long> issending = new List<long>();
        public static TCPMessageServer tms;
        public static int MsgCount;
        public static DiscordAPI.WebAPI disca;
        public static List<long> friends;
        public static pThreadPool pool;
        public static MiraiHttpSession session;

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
                catch (Exception)
                {
                    MainHolder.logger("SideLoad", "FATAL - DBConnectiong FAILED.", ConsoleColor.Black, ConsoleColor.Red);
                }
            }

            try
            {

                MainHolder.logger("SideLoad", "BotAPI is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "BotAPI Failed.", ConsoleColor.Black, ConsoleColor.Red);
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
                BiliUser.userlist = new Dictionary<int, BiliUser>();
                dynamics = new List<BiliSpaceDynamic>();
                bilidmkproc = new BiliDanmakuProcessor(2064239);
                bilidmkproc.Init_connection();
                MainHolder.logger("SideLoad", "BLive-DMKReceiver is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "BLive-DMKReceiver FAILED.", ConsoleColor.Black, ConsoleColor.Red);
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
                PrivmessageChecker.startthreads();
                MainHolder.logger("SideLoad", "BiliPrivMessageReceiver is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "BiliPrivMessageReceiver FAILED.", ConsoleColor.Black, ConsoleColor.Red);
            }
            try
            {
                DynChecker.startthreads();
                MainHolder.logger("SideLoad", "DynChecker is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "DynChecker FAILED.", ConsoleColor.Black, ConsoleColor.Red);
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
                disca = new DiscordAPI.WebAPI("https://discord.com/api/webhooks/747828316404449320/ysUymAoAeonVSVO8-GvZArPPjy0WvAu4VxBRTP4hIo9rWDvsjQSecp8H3gYj6zsdqkD4");
                pool.submitWorkload(() =>
                {
                    try
                    {
                        while (true)
                        {
                            disca._PROC();
                            Thread.Sleep(100);
                        }
                    }
                    catch { }
                });
                MainHolder.logger("SideLoad", "Discord-sync is UP.", ConsoleColor.Black, ConsoleColor.White);
            }
            catch (Exception)
            {
                MainHolder.logger("SideLoad", "Discord-sync FAILED.", ConsoleColor.Black, ConsoleColor.Red);
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
                    BiliUser u = new BiliUser(kvp.Key);
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
