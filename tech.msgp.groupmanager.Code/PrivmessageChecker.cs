﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BiliApi.BiliPrivMessage;

namespace tech.msgp.groupmanager.Code
{
    internal class PrivmessageChecker
    {
        public static PrivSessionManager man;
        //public static List<long> sent = new List<long>();
        public static void startthreads()
        {
            if (!MainHolder.useBiliRecFuncs) return;
            man = new PrivSessionManager(MainHolder.biliapi);
            MainHolder.pool.submitWorkload(run);
        }
        public static void run()
        {
            run_head:
            try
            {
                man.fetchUnfollowed();
                foreach (PrivMessageSession session in man.unfollowed_sessions)
                {
                    try
                    {
                        session.fetch();
                        session.pick_latest_messages();//先取一遍消息防止抓到老消息
                    }
                    catch { }
                }
                while (true)
                {
                    try
                    {
                        man.smartRefresh();
                        foreach (PrivMessageSession session in man.unfollowed_sessions)
                        {
                            try
                            {
                                //session.fetch();//Sessions会被自动更新，不再需要手动更新
                                List<PrivMessage> messages = session.pick_latest_messages();
                                foreach (PrivMessage pm in messages)
                                {
                                    if (pm.content == null || pm.content.Length < 1)
                                    {
                                        continue;//假私信
                                    }

                                    if (pm.talker.uid == man.MyUID) continue;//自己的消息不处理

                                    MainHolder.Logger.Info("B站私信", pm.content);
                                    MainHolder.broadcaster.BroadcastToAdminGroup("私信.[" + pm.talker.name + "#" + pm.talker.uid + "]:" + pm.content);
                                    Task.Delay(1000).Wait();
                                    long.TryParse(pm.content, out long qq);
                                    if (qq > 1000)
                                    {//是个数字
                                        if (DataBase.me.isBiliPending(pm.talker.uid))//等待绑定QQ
                                        {
                                            if (DataBase.me.boundBiliWithQQ(pm.talker.uid, qq))
                                            {
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 绑定了TA的QQ号:" + qq);
                                                //PrivMessageSession privsession = PrivMessageSession.openSessionWith(pm.talker.uid, MainHolder.biliapi);
                                                session.sendMessage("[自动回复] 好的，管理将在稍后尝试与您取得联系。您也可以尝试使用下面的链接自助入群：https://jq.qq.com/?_wv=1027&k=3WZDODeC");
                                            }
                                            else
                                            {
                                                session.sendMessage("系统故障，请稍后重试或联系管理员。");
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "绑定TA的QQ号:" + qq + "\n该操作由于一个系统错误未能完成。");
                                            }
                                        }
                                        else
                                        {
                                            MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "试图重新绑定QQ  已被拒绝");
                                            session.sendMessage("[自动回复] 好的，管理将在稍后尝试与您取得联系。您也可以尝试使用下面的链接自助入群：https://jq.qq.com/?_wv=1027&k=3WZDODeC");
                                        }
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToAnEgg("私信.[" + pm.talker.name + "#" + pm.talker.uid + "]:" + pm.content);
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                if (session.lastjson != null && session.lastjson.Length > 1)
                                {
                                    MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[B站私信部分_内循环]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace + "\n出错的消息：");
                                    MainHolder.broadcaster.BroadcastToAdminGroup(session.lastjson);
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[B站私信部分_外循环]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace + "\n-= Func Failure =-");
                    }
                    Thread.Sleep(2 * 60 * 1000);
                }
            }
            catch (Exception err)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[PART_FALIURE]\n一个模块发生了不可恢复的错误，已经停止运行，将尝试重启。\n[B站私信部分]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace + "\n出错的消息：");
                MainHolder.broadcaster.BroadcastToAdminGroup(man.lastjson);
                Thread.Sleep(1000);
                MainHolder.broadcaster.SendToAnEgg("B站私信部分崩溃。\n" + err.Message + "\n\n" + err.StackTrace);
                goto run_head;
            }
        }
    }
}
