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
        public static Bitmap QunQRCode;
        public static bool BlockReceiver = false;
        public static bool DropMessages = false;
        public static List<long> pendingUnderlevelQQ = new List<long>();
        //public static List<long> sent = new List<long>();
        public static void startthreads()
        {
            QunQRCode = (Bitmap)Image.FromFile("quncode.png");
            if (!MainHolder.useBiliRecFuncs) return;
            man = new PrivSessionManager(MainHolder.biliapi);
            MainHolder.pool.submitWorkload(run);
        }

        public static bool Collides(List<long> a, List<long> b, out List<long> collition)
        {
            collition = new List<long>();
            foreach (var i in a)
            {
                if (b.Contains(i))
                {
                    collition.Add(i);
                }
            }
            return collition.Count > 0;
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
                                if (DropMessages)
                                {
                                    MainHolder.Logger.Info("PRIVMSG", "DropMsg=True, drop session[#" + session.talker_id + "].");
                                    session.Close();
                                    continue;
                                }
                                while (BlockReceiver) Thread.Sleep(0);
                                session.fetch();//Sessions会被自动更新，不再需要手动更新 EDIT:划掉
                                List<PrivMessage> messages = session.pick_latest_messages();
                                if (lateststamp >= session.lastmessage.timestamp) continue;//会话最后一条消息在上一次处理之前就已经发送，很可能处理过了
                                File.WriteAllText("saves/timestamp.int", lateststamp.ToString());
                                foreach (PrivMessage pm in messages)
                                {
                                    if (DropMessages)
                                    {
                                        MainHolder.Logger.Info("PRIVMSG", "DropMsg=True, drop message[#" + pm.msg_seqno + "].");
                                        continue;
                                    }
                                    while (BlockReceiver) Thread.Sleep(0);
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
                                    {
                                        //获取激活码
                                        if (pm.content == "#激活码")
                                        {
                                            var code = MainHolder.codeclaimer.CheckUID(pm.talker.uid, out bool succ);
                                            session.sendMessage(code);
                                        }
                                        //绑定QQ
                                        if (pm.content[0] == 'F')
                                        {
                                            if (long.TryParse(pm.content.Substring(1), out long fq) && fq > 1000)
                                            {
                                                BoundQQ(fq, session, pm, true);
                                            }
                                        }
                                        else
                                        {
                                            if (long.TryParse(pm.content, out long qq) && qq > 1000)
                                            {
                                                BoundQQ(qq, session, pm, false);
                                            }
                                            else
                                            {
                                                MainHolder.broadcaster.SendToAnEgg("私信.[" + pm.talker.name + "#" + pm.talker.uid + "]:" + pm.content);
                                            }
                                        }
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
                            Thread.Sleep(1 * 1000);
                        }
                        lateststamp = BiliApi.TimestampHandler.GetTimeStamp(DateTime.Now);
                        WatchDog.FeedDog("pmsgchk");
                    }
                    catch (Exception err)
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[B站私信部分_外循环]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace + "\n-= Func Failure =-");
                    }
                    Thread.Sleep(5 * 1000);
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

        public static void BoundQQ(long qq, PrivMessageSession session, PrivMessage pm, bool force = false)
        {
            if (qq > 9999999999999999)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 发送的数字序列不能被识别为QQ号：" + qq);
                return;
            }
            if (DataBase.me.isUserBlacklisted(qq))
            {
                session.sendMessage("[自动回复] 您不能使用该QQ号，因为它存在严重违规记录，已被禁止加群。\n如需帮助，请联系鸡蛋(QQ1250542735)");
                MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 尝试绑定黑名单中的账号：" + qq);
                return;
            }
            {
                var boundeduid = DataBase.me.getUserBoundedUID(qq);
                if (boundeduid != 0)
                {
                    if (boundeduid == pm.talker.uid)
                    {
                        session.sendMessage("[自动回复] 您已绑定过该账号，请扫描下方二维码入群。\n如需帮助，请联系鸡蛋(QQ1250542735)");
                        session.SendImage(QunQRCode);
                        MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 重复绑定相同账号：" + qq);
                        return;
                    }
                    {
                        session.sendMessage("[自动回复] 该QQ账号目前已被绑定到其它B站账号。\n如需帮助，请联系鸡蛋(QQ1250542735)");
                        MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 试图绑定已有账号：" + qq);
                        return;
                    }
                }
                int qqlevel = ThirdPartAPIs.getQQLevel(qq, 2);
                if (qqlevel < 0)
                {
                    session.sendMessage("[自动回复] 该账号不存在或查询失败，请稍后重试。\n如需帮助，请联系鸡蛋(QQ1250542735)");
                    MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " QQ号无效或查询失败：" + qq);
                    return;
                }
                if (qqlevel < 16)
                {
                    session.sendMessage("[自动回复] 抱歉，您使用的QQ号等级过低(" + qqlevel + "<16)，我们将按照以下方式保证您的舰长权益：\n* 鹿野将会在直播结束后通过您提供的QQ号与您联系\n* 请保存下面的二维码，在您未来等级足够后仍可扫码加群\n\n若24h后仍未收到好友申请，或对上述等级判定有异议，请联系鸡蛋(QQ1250542735)");
                    session.SendImage(QunQRCode);
                    MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 已绑定等级过低的账号(" + qqlevel + "<16)：" + qq + "\n绑定信息会保留，但不允许进入舰长群。");
                    pendingUnderlevelQQ.Add(qqlevel);
                    return;
                }
            }
            if (DataBase.me.isBiliPending(pm.talker.uid))//等待绑定QQ
            {
                if (DataBase.me.boundBiliWithQQ(pm.talker.uid, qq))
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + " 绑定了TA的QQ号:" + qq);
                    session.sendMessage("[自动回复] 好的，请扫描下方二维码入群。(转发无效)\n" +
                        "如果您的QQ号输入错误，可随时重新发送新的QQ号，我会为您换绑。\n" +
                        "如需帮助，请联系鸡蛋(QQ1250542735)");
                    session.SendImage(QunQRCode);
                    return;
                }
                else
                {
                    session.sendMessage("[自动回复] 系统故障，请稍后重试或联系管理员(鸡蛋QQ1250542735)");
                    MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "绑定TA的QQ号:" + qq + "\n失败：似乎无法操作数据库。");
                    return;
                }
            }
            else
            {
                var cgroups = DataBase.me.getCrewGroup();
                var oldqq = DataBase.me.getUserBoundedQQ(pm.talker.uid);
                var usergroup = DataBase.me.whichGroupsAreTheUserIn(oldqq, false);
                if ((!force) && Collides(cgroups, usergroup, out List<long> _))
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "重新绑定QQ(" + oldqq + "=>" + qq + ")\n拒绝，因为原QQ已经在舰长群中");
                    session.sendMessage("[自动回复] 您不能换绑QQ，因为您原先绑定的QQ(" + oldqq + ")已经在舰长群中了。如果需要换绑QQ，请先将原QQ退出舰长群。\n" +
                        "如果强行换绑，系统将会踢出原先的QQ。需强行换绑请发送：\n" +
                        "F" + qq);
                }
                else
                {
                    if (DataBase.me.boundBiliWithQQ(pm.talker.uid, qq))
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup(pm.talker.name + "重新绑定QQ(" + oldqq + "=>" + qq + ")\n已更新数据库");
                        session.sendMessage("[自动回复] 您已成功换绑QQ。原先的QQ号(" + oldqq + ")将被解绑，请使用新绑定的QQ号加入舰长群。");
                        if (pendingUnderlevelQQ.Contains(oldqq))
                        {
                            pendingUnderlevelQQ.Remove(oldqq);
                        }
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
    }
}
