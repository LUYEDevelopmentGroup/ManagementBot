using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BiliApi.BiliPrivMessage;

namespace tech.msgp.groupmanager.Code
{
    internal class PrivmessageChecker
    {
        public static PrivSessionManager man;
        private static Bitmap QunQRCode;
        //public static List<long> sent = new List<long>();
        public static void startthreads()
        {
            QunQRCode = (Bitmap)Image.FromFile("quncode.png");
            if (!MainHolder.useBiliRecFuncs) return;
            man = new PrivSessionManager(MainHolder.biliapi);
            MainHolder.pool.submitWorkload(run);
        }

        public static bool Collides(List<long> a, List<long> b,out List<long> collition)
        {
            collition = new List<long>();
            foreach (var i in a)
            {
                if (b.Contains(i))
                {
                    collition.Add(i);
                }
            }
            return collition.Count>0;
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
                int lateststamp = -1;
                if (File.Exists("saves/timestamp.int"))
                {
                    bool succ = int.TryParse(File.ReadAllText("saves/timestamp.int"), out int tstamp);
                    if (succ) lateststamp = tstamp;
                }
                while (true)
                {
                    try
                    {
                        man.updateSessions();
                        Thread.Sleep(1000);
                        man.refresh();
                        foreach (PrivMessageSession session in man.unfollowed_sessions)
                        {
                            try
                            {
                                session.fetch();//Sessions会被自动更新，不再需要手动更新 EDIT:划掉
                                List<PrivMessage> messages = session.pick_latest_messages();
                                if (lateststamp >= session.lastmessage.timestamp) continue;//会话最后一条消息在上一次处理之前就已经发送，很可能处理过了
                                File.WriteAllText("saves/timestamp.int", lateststamp.ToString());
                                foreach (PrivMessage pm in messages)
                                {
                                    //var pm = session.lastmessage;
                                    if (pm.content == null || pm.content.Length < 1)
                                    {
                                        continue;//假私信
                                    }

                                    if (pm.talker.uid == man.MyUID) continue;//自己的消息不处理

                                    if (pm.timestamp <= lateststamp) continue;//老消息

                                    MainHolder.Logger.Info("B站私信", pm.content);
                                    MainHolder.broadcaster.BroadcastToAdminGroup("私信.[" + pm.talker.name + "#" + pm.talker.uid + "]:" + pm.content);
                                    Task.Delay(1000).Wait();
                                    if (pm.content[0] == 'F')
                                    {
                                        long.TryParse(pm.content.Substring(1), out long fq);
                                        if (fq > 1000)
                                        {
                                            if (DataBase.me.isUserBlacklisted(fq))
                                            {
                                                session.sendMessage("[自动回复] 您不能使用该QQ号，因为它存在严重违规记录，已被禁止加群。\n如需帮助，请联系鸡蛋(QQ1250542735)");
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 尝试绑定黑名单中的账号：" + fq);
                                            }
                                            var cgroups = DataBase.me.getCrewGroup();
                                            var oldqq = DataBase.me.getUserBoundedQQ(pm.talker.uid);
                                            var usergroup = DataBase.me.whichGroupsAreTheUserIn(oldqq);
                                            if (Collides(cgroups, usergroup, out List<long> cgin))
                                            {
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "\n用户强制换绑QQ。\n将从舰长群踢出"+oldqq+"，然后将新账号"+fq+"录入数据库");
                                                foreach(var g in cgin)
                                                {
                                                    MainHolder.session.KickMemberAsync(oldqq, g, "您通过B站私信绑定了新的舰长QQ。");
                                                }
                                            }
                                            if (DataBase.me.boundBiliWithQQ(pm.talker.uid, fq))
                                            {
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "重新绑定QQ(" + oldqq + "=>" + fq + ")\n已更新数据库");
                                                session.sendMessage("[自动回复] 您已成功换绑QQ。原先的QQ号(" + oldqq + ")已被解绑，请使用新绑定的QQ号加入舰长群。");
                                                session.SendImage(QunQRCode);
                                            }
                                            else
                                            {
                                                session.sendMessage("[自动回复] 系统故障，请稍后重试或联系管理员(鸡蛋QQ1250542735)");
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "重新绑定QQ(" + oldqq + "=>" + fq + ")\n" +
                                                    "该操作由于一个系统错误未能完成。");
                                            }
                                        }
                                    }
                                    long.TryParse(pm.content, out long qq);
                                    if (qq > 1000)
                                    {//是个数字
                                        if (DataBase.me.isUserBlacklisted(qq))
                                        {
                                            session.sendMessage("[自动回复] 您不能使用该QQ号，因为它存在严重违规记录，已被禁止加群。\n如需帮助，请联系鸡蛋(QQ1250542735)");
                                            MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 尝试绑定黑名单中的账号：" + qq);
                                        }
                                        else
                                        if (DataBase.me.isBiliPending(pm.talker.uid))//等待绑定QQ
                                        {
                                            if (DataBase.me.boundBiliWithQQ(pm.talker.uid, qq))
                                            {
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 绑定了TA的QQ号:" + qq);
                                                session.sendMessage("[自动回复] 好的，请扫描下方二维码入群。(转发无效)\n" +
                                                    "如果您的QQ号输入错误，可随时重新发送新的QQ号，我会为您换绑。\n" +
                                                    "如需帮助，请联系鸡蛋(QQ1250542735)");
                                                session.SendImage(QunQRCode);
                                            }
                                            else
                                            {
                                                session.sendMessage("[自动回复] 系统故障，请稍后重试或联系管理员(鸡蛋QQ1250542735)");
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "绑定TA的QQ号:" + qq + "\n该操作由于一个系统错误未能完成。");
                                            }
                                        }
                                        else
                                        {
                                            var cgroups = DataBase.me.getCrewGroup();
                                            var oldqq = DataBase.me.getUserBoundedQQ(pm.talker.uid);
                                            var usergroup = DataBase.me.whichGroupsAreTheUserIn(oldqq);
                                            if (Collides(cgroups, usergroup,out List<long> _))
                                            {
                                                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "重新绑定QQ("+oldqq+"=>"+qq+")\n拒绝，因为原QQ已经在舰长群中");
                                                session.sendMessage("[自动回复] 您不能换绑QQ，因为您原先绑定的QQ("+ oldqq + ")已经在舰长群中了。如果需要换绑QQ，请先将原QQ退出舰长群。\n" +
                                                    "如果强行换绑，系统将会踢出原先的QQ。需强行换绑请发送：\n" +
                                                    "F"+qq);
                                            }
                                            else
                                            {
                                                if (DataBase.me.boundBiliWithQQ(pm.talker.uid, qq))
                                                {
                                                    MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "重新绑定QQ(" + oldqq + "=>" + qq + ")\n已更新数据库");
                                                    session.sendMessage("[自动回复] 您已成功换绑QQ。原先的QQ号(" + oldqq + ")将被解绑，请使用新绑定的QQ号加入舰长群。");
                                                }
                                                else
                                                {
                                                    session.sendMessage("[自动回复] 系统故障，请稍后重试或联系管理员(鸡蛋QQ1250542735)");
                                                    MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "重新绑定QQ(" + oldqq + "=>" + qq + ")\n" +
                                                        "该操作由于一个系统错误未能完成。");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToAnEgg("私信.[" + pm.talker.name + "#" + pm.talker.uid + "]:" + pm.content);
                                    }
                                }
                                session.Close();
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
                        lateststamp = BiliApi.TimestampHandler.GetTimeStamp(DateTime.Now);
                        WatchDog.FeedDog("pmsgchk");
                    }
                    catch (Exception err)
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[B站私信部分_外循环]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace + "\n-= Func Failure =-");
                    }
                    Thread.Sleep(30 * 1000);
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
