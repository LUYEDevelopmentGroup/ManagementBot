using System;
using System.Collections.Generic;
using System.Threading;
using BiliApi;

namespace tech.msgp.groupmanager.Code
{
    internal class SecondlyTask
    {
        public static List<int> listened_uids = new List<int>();
        public static Thread main;
        public static int gbcounter = 57600;
        public static DateTime lastrecv;
        public static DateTime laststat;
        //public static List<long> sent = new List<long>();
        public static void startthreads()
        {
            if (laststat == null)
            {
                laststat = DateTime.Now;
            }

            if (main != null && main.IsAlive)
            {
            }
            else
            {
                main = new Thread(new ThreadStart(run));
                main.Start();
            }
        }

        public static void run()
        {
            int counter = 0;
            int lasterr = 0;
            while (true)
            {
                try
                {
                    counter++;
                    if (counter % 600 == 0)
                    {//5分钟一次
                        GC.Collect(5, GCCollectionMode.Optimized, true, true);
                    }
                    if (counter % 3600 == 0)
                    {//一小时执行一次
                        MainHolder.broadcaster.BroadcastToAdminGroup("[在线]\n" +
                            "最近一条消息：" + lastrecv.ToString() + "\n" +
                            "时段总接收消息：" + MainHolder.MsgCount + "\n" +
                            "上次检查：" + laststat.ToString());
                        laststat = DateTime.Now;
                        MainHolder.MsgCount = 0;
                    }
                    if (counter % 30 == 0)
                    {//每半分钟一次
                        if (MainHolder.bilidmkproc?.lid <= 0)
                        {
                            MainHolder.bilidmkproc?.PickupRunningLive();
                        }
                    }
                    if (counter % (60 * 60 * 12) == 0)
                    {//12小时一次
                        MainHolder.checkCrewGroup();
                    }
                    if ((counter + (60 * 60 * 6)) % (60 * 60 * 12) == 0)
                    {//12小时一次，错位+6小时      帮小伙伴续费黑名单>_<
                        trigger_BanRefresh();
                    }
                    if (counter >= (60 * 60 * 24))
                    {
                        counter = 0;//以一天为循环体
                    }

                    MainHolder.bilidmkproc?.UpdateLiveDataToDB();//每秒都更新数据库
                }
                catch (Exception err)
                {
                    if ((counter - lasterr) < (60 * 60))
                    {
                        continue;
                    }

                    MainHolder.broadcaster.BroadcastToAdminGroup("[计划任务失败]\n计划任务未能顺利完成(每小时仅报错一次防止持续错误刷屏)\n" + err.Message + "\nStack:" + err.StackTrace);
                }
                Thread.Sleep(1000);
            }
        }

        public static void trigger_BanRefresh()
        {
            List<BiliBannedUser> banlist = MainHolder.bilidmkproc.blr.manage.getBanlist();
            List<int> tobebanned = DataBase.me.listPermbans();
            string log = "";
            foreach (int uid in tobebanned)
            {
                BiliBannedUser bbu = getBBUbyUID(banlist, uid);
                if (bbu == null)
                {
                    if (MainHolder.bilidmkproc.blr.manage.banUID(uid, 720))
                    {
                        log += "#" + uid + " -> 自动封禁 √\n";
                    }
                    else
                    {
                        log += "#" + uid + " -> 自动封禁 E\n";
                    }
                }
                else
                if ((bbu.endtime - DateTime.Now).TotalHours < 24)
                {
                    MainHolder.bilidmkproc.blr.manage.debanBID(bbu.id);
                    if (MainHolder.bilidmkproc.blr.manage.banUID(uid, 720))
                    {
                        log += bbu.uname + "#" + uid + " -> 自动续费 √\n";
                    }
                    else
                    {
                        log += bbu.uname + "#" + uid + " -> 自动续费 E\n";
                    }
                }
                else
                {
                    log += bbu.uname + "#" + uid + " -> 无需续费(" + (bbu.endtime - DateTime.Now).TotalHours + "h) ×\n";
                }
            }
            MainHolder.broadcaster.BroadcastToAdminGroup("[直播间禁言自动续费]<试运行>\n" + log);
        }

        public static BiliBannedUser getBBUbyUID(List<BiliBannedUser> list, int uid)
        {
            foreach (BiliBannedUser bb in list)
            {
                if (bb.uid == uid)
                {
                    return bb;
                }
            }
            return null;
        }
    }
}
