using Mirai.CSharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using BiliApi;
using Mirai.CSharp.HttpApi.Models.ChatMessages;

namespace tech.msgp.groupmanager.Code
{
    internal class DynChecker
    {
        public static List<int> listened_uids = new List<int>();
        public static Thread main;
        //public static List<long> sent = new List<long>();
        public static void startthreads()
        {
            if (main != null && main.IsAlive)
            {
            }
            else
            {
                listened_uids = MainHolder.BiliWatchUIDs;
                foreach (int uid in listened_uids)
                {
                    MainHolder.dynamics.Add(new BiliApi.BiliSpaceDynamic(uid, MainHolder.bililogin));
                }
                MainHolder.pool.submitWorkload(run);
            }
        }

        public static void run()
        {
            foreach (BiliApi.BiliSpaceDynamic dyn in MainHolder.dynamics)
            {
                dyn.refresh();//先refresh一下，防止处理到存货
            }
            while (true)
            {
                foreach (BiliApi.BiliSpaceDynamic dyn in MainHolder.dynamics)
                {
                    try
                    {
                        List<Dyncard> d = new List<Dyncard>();
                        try
                        {
                            d = dyn.getLatest();
                        }
                        catch (Exception err)
                        {
                            MainHolder.DumpException(err, "DynChecker");
                            continue;
                        }
                        Thread.Sleep(0);
                        dyn.refresh(d);
                        foreach (Dyncard dc in d)
                        {
                            try
                            {
                                if ((DateTime.Now - dc.sendtime).TotalSeconds > 30)
                                {
                                    continue;
                                }
                                bool atall = (DateTime.Now.Hour < 23 && DateTime.Now.Hour > 6);
                                atall &= !dc.dynamic.Contains("$Silent$");
                                switch (dc.type)
                                {
                                    case 1://普通动态
                                    case 2://包含图片的动态
                                    case 4://？出现在转发和普通动态
                                        if (dc.card_origin["author"].Value<long>("mid") != dyn.uid)
                                        {
                                            MainHolder.broadcaster.BroadcastToAdminGroup("[转发他人动态]\nUP主:" + dc.sender.name + "\n原UP主：" + dc.card_origin["author"].Value<string>("name") + "\nhttps://t.bilibili.com/" + dc.dynid + "\n<按策略不推送>");
                                            break;
                                        }
                                        if (isLivedanmakuAndBroadcast(dc))
                                        {
                                            break; //如果是转发的直播，分出去单独处理
                                        }
                                        MainHolder.broadcaster.BroadcastToAllGroup("[有新动态！]\nUP主:" + dc.sender.name + "\n" + dc.short_dynamic + "\nhttps://t.bilibili.com/" + dc.dynid, atall ? (IChatMessage)new AtAllMessage() : new PlainMessage("<@[免打扰模式]>"));
                                        break;
                                    case 256://音频
                                        break;
                                    case 8://视频
                                        MainHolder.broadcaster.BroadcastToAllGroup("[有新视频！]\n" + dc.vinfo.title + "\nUP主:" + dc.sender.name + "\n" + dc.vinfo.short_discription + "\nhttps://www.bilibili.com/video/" + dc.vinfo.bvid + "\n", atall ? (IChatMessage)new AtAllMessage() : new PlainMessage("<@[免打扰模式]>"));
                                        break;
                                    case 4200://直播
                                        break;
                                    default:
                                        break;
                                }
                            }
                            catch (Exception err)
                            {
                                string vardump = dc.Dump();
                                string UUID = Guid.NewGuid().ToString();
                                MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n此子模块因发生预期之内的错误，在一个预设调试点断下。\n" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
                                MainHolder.broadcaster.BroadcastToAdminGroup("[VarDump]\n此次错误包含一个小型状态快照，请使用以下信息调试：\nRayId=" + UUID);
                                MainHolder.broadcaster.SendToAnEgg("[VarDump]\nRayId=" + UUID + "\n" + vardump);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[B站动态部分]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
                    }
                }
                try
                {//检测鹿野
                    if (MainHolder.enableNativeFuncs) check_fans();
                }
                catch (Exception err)
                {
                    MainHolder.Logger.Warning("BiliDynChecker", "检查粉丝数时遇到错误：" + err.Message + " - " + err.StackTrace);
                }
                Thread.Sleep(15000);
            }
        }
        public static void check_fans(bool debug = false)
        {
            string LuYe_UpStat = MainHolder.biliapi.getUpState(5659864);
            if (LuYe_UpStat != null)
            {
                JObject jb1 = (JObject)JsonConvert.DeserializeObject(LuYe_UpStat);
                if (jb1.Value<int>("code") == 0)
                {
                    int fans = jb1["data"].Value<int>("follower");
                    if ((!DataBase.me.isCountAlreadyRiched(fans)) || (debug))//新达成或在de
                    {
                        List<int> ur_fancount = DataBase.me.listUnachievedCount();
                        int fcr = int.MaxValue;
                        foreach (int fc in ur_fancount)
                        {
                            if (fc <= fans)
                            {
                                fcr = fc;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (fcr == int.MaxValue && debug)
                        {
                            fcr = ur_fancount[0] - 500;
                        }

                        int offset = fans - fcr;
                        int page = (offset / 10) + 1;
                        int index = offset % 10;
                        string fanlist = MainHolder.biliapi.getFanList(5659864, page, 10);
                        if (fanlist != null)
                        {
                            JObject jb2 = (JObject)JsonConvert.DeserializeObject(fanlist);
                            if (jb2.Value<int>("code") == 0)
                            {
                                if (!debug)
                                {
                                    int achieved_count = DataBase.me.setCountReached(fans, jb2["data"]["list"][0].Value<int>("mid"));
                                    MainHolder.broadcaster.BroadcastToAllGroup("【里程碑！】\n成功达成<" + achieved_count + ">粉！\n第" + achieved_count + "粉：" + jb2["data"]["list"][index].Value<string>("uname"));
                                }
                                else
                                {
                                    MainHolder.broadcaster.SendToAnEgg("[DEBUG]\n偏移=" + offset + "\n页面大小=10\n页码=" + page + "\n下标=" + index + "\n第[整数]粉：" + jb2["data"]["list"][index].Value<string>("uname"));
                                }
                            }
                            else
                            {
                                MainHolder.broadcaster.SendToAnEgg("FANLIST_UNKNOWN_MESSAGE\n" + fanlist);
                            }
                        }
                        else
                        {
                            MainHolder.broadcaster.SendToAnEgg("FANLIST_EMPTY_MESSAGE\n");
                        }
                    }
                }
                else
                {
                    MainHolder.broadcaster.SendToAnEgg("UPSTAT_UNKNOWN_MESSAGE\n" + LuYe_UpStat);
                }
            }
            else
            {
                MainHolder.broadcaster.SendToAnEgg("UPSTAT_EMPTY_MESSAGE\n" + LuYe_UpStat);
            }
        }
        public static bool isLivedanmakuAndBroadcast(Dyncard dync)
        {
            if (dync.card_origin == null)
            {
                return false;
            }

            if (dync.origintype != 4200)
            {
                return false;
            }

            MainHolder.broadcaster.BroadcastToAllGroup("[直播动态]\nUP主:" + dync.card_origin.Value<string>("uname") + "\n" + dync.card_origin.Value<string>("title") + "\n" + dync.short_dynamic + "\nhttps://live.bilibili.com/" + dync.card_origin.Value<int>("roomid"), new AtAllMessage());
            return true;
        }
    }
}
