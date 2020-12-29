﻿using BililiveRecorder.Core;
using BiliveDanmakuCli;
using Mirai_CSharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using tech.msgp.groupmanager.Code.BiliAPI;
using tech.msgp.groupmanager.Code.BiliAPI.BiliPrivMessage;

namespace tech.msgp.groupmanager.Code
{
    public class BiliDanmakuProcessor
    {
        private readonly int liveid;
        public int lid = -1;
        private int new_commers = 0;

        //int actviewers = 0;
        private int selver_coins = 0;
        private int gold_coins = 0;
        private readonly int streamer_id = 5659864;
        private readonly Dictionary<string, string> crews = new Dictionary<string, string>();
        private readonly List<int> viewerlist = new List<int>();


        public BiliLiveRoom blr;
        public BiliDanmakuProcessor(int roomid)
        {
            liveid = roomid;
        }
        public void Init_connection()
        {
            LiveRoom lr = new LiveRoom(liveid);
            lr.sm.ReceivedDanmaku += Receiver_ReceivedDanmaku;
            lr.sm.StreamStarted += StreamStarted;
            lr.sm.StreamStopped += StreamStopped;
            lr.sm.ExceptionHappened += ExceptionHappened;
            lr.init_connection();
            blr = new BiliLiveRoom(liveid);
        }

        public void StreamStarted(object sender, StreamStartedArgs e)
        {
            if (lid > 0 && !ispickedup)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("开播事件被重复推送，将忽略并沿用原事件ID。\n" +
                    "原事件ID:" + lid + "\n" + "https://live.bilibili.com/" + liveid);
                return;
            }
            ispickedup = false;
            blr = new BiliLiveRoom(liveid);
            lid = blr.lid;
            new_commers = 0;
            viewerlist.Clear();
            gold_coins = 0;
            selver_coins = 0;
            tlist.Clear();
            bool atall = (DateTime.Now.Hour < 23 && DateTime.Now.Hour > 6);
            DataBase.me.recBLive(lid, blr.title);
            MainHolder.broadcaster.BroadcastToAllGroup(new IMessageBase[] {
                new PlainMessage("【直播通知】\n" + blr.title + "\nhttp://xn--z6ut02b.xn--8nx142eqwi.xn--6qq986b3xl/" + new Random().Next(100,99999)),
                new ImageMessage(null,blr.cover,null),
                atall ? (IMessageBase)new AtAllMessage():new PlainMessage("<@[免打扰模式]>") });
            blr.sendDanmaku(atall ? "已推送直播通知" : "已推送直播通知(免打扰模式)");
            if (!ispickedup)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("鹿野已经开播\n事件识别ID:" + lid + "\n" + "https://live.bilibili.com/" + liveid);
            }
            else
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("鹿野已经开播\n事件识别ID:" + lid + "(覆盖pickup数据)\n" + "https://live.bilibili.com/" + liveid);
            }
        }

        private bool ispickedup = false;
        public int PickupRunningLive()
        {
            blr = new BiliLiveRoom(liveid);
            if (blr.status != BiliLiveRoom.STATUS_LIVE)
            {
                lid = -1;
                return lid;
            };
            if (lid > 0)
            {
                if (lid != blr.lid)
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("！之前有一个直播数据并未正确结束\n旧lid:" + lid + "\n新lid:" + blr.lid + "\n新的直播数据被加载，之前的数据可能已经丢失。");
                }
                return lid;
            }
            ispickedup = true;
            lid = blr.lid;
            MainHolder.broadcaster.BroadcastToAdminGroup("发现正在进行(中途数据中断)的直播\n事件识别ID:" + lid + "\n" + "https://live.bilibili.com/" + liveid);
            DataBase.me.getBLiveData(lid, out new_commers, out gold_coins, out selver_coins);
            return lid;
        }

        public void UpdateLiveDataToDB()
        {
            if (lid > 0)
            {
                DataBase.me.recBLiveUpdate(lid, new_commers, viewerlist.Count(), 0, selver_coins, gold_coins);
            }
        }

        public void StreamStopped(object sender)
        {
            DataBase.me.recBLiveEnd(lid, new_commers, viewerlist.Count(), 0, selver_coins, gold_coins);
            MainHolder.broadcaster.BroadcastToAdminGroup("直播结束\n事件识别ID " + lid + "\n\n直播数据统计(系统在线期间) \n" +
                "活跃观众  " + (ispickedup ? "-" : viewerlist.Count().ToString()) + "<+" + new_commers + ">\n" +
                "金瓜子  " + gold_coins + "\n" +
                "银瓜子  " + selver_coins);
            gold_coins = 0;
            selver_coins = 0;
            new_commers = 0;
            viewerlist.Clear();
            lid = -1;
            MainHolder.checkCrewGroup();
            ListTenMoreGradeUsers();
        }

        public static void ListTenMoreGradeUsers()
        {
            string str = "[十级监控]<试运行>\n";
            foreach (KeyValuePair<string, int> t in tlist)
            {
                str += t.Key + " -> " + t.Value;
            }
            MainHolder.broadcaster.BroadcastToAdminGroup(str);
        }

        public void ExceptionHappened(object sender, string message, Exception err)
        {
            MainHolder.logger("弹幕连接", message + " - " + err.StackTrace);
        }

        private static readonly Dictionary<string, int> tlist = new Dictionary<string, int>();
        public void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            MainHolder.pool.submitWorkload(() =>
            {
                try
                {
                    JObject json = (JObject)JsonConvert.DeserializeObject(e.Danmaku.RawData);
                    MainHolder.tms.sendDanmakuToCli(e.Danmaku.RawData);
                    switch (e.Danmaku.MsgType)
                    {
                        case MsgTypeEnum.Comment:
                            if (e.Danmaku.IsAdmin)
                            {
                                Log(ConsoleColor.Yellow, "<房管>" + e.Danmaku.UserName + "#" + e.Danmaku.UserID + " > ");
                            }
                            else
                            {
                                Log(ConsoleColor.Gray, e.Danmaku.UserName + "#" + e.Danmaku.UserID + " > ");
                            }

                            Log(ConsoleColor.White, e.Danmaku.CommentText + "\n");
                            int ml = GetMedalLevelMatchUID(streamer_id, json);
                            if (ml >= 10)
                            {
                                if (!tlist.ContainsKey(e.Danmaku.UserName))
                                {
                                    tlist.Add(e.Danmaku.UserName, ml);
                                }
                            }
                            DataBase.me.recBLiveDanmaku(e.Danmaku.UserID, e.Danmaku.CommentText, e.Danmaku.SendTime, lid);
                            if (!DataBase.me.isBiliUserExist(e.Danmaku.UserID))
                            {
                                new_commers++;
                            }

                            if (!viewerlist.Contains(e.Danmaku.UserID))
                            {
                                viewerlist.Add(e.Danmaku.UserID);
                            }

                            DataBase.me.addBiliUser(e.Danmaku.UserID, e.Danmaku.UserName);
                            {
                                List<string> bwords = DataBase.me.listBiliveBanwords();
                                foreach (string bword in bwords)
                                {
                                    if (e.Danmaku.CommentText.IndexOf(bword) >= 0)
                                    {
                                        MainHolder.broadcaster.BroadcastToAdminGroup("直播间检测到疑似违规弹幕：\n" + e.Danmaku.CommentText + "\n发送者：" + e.Danmaku.UserName + "(" + e.Danmaku.UserID + ")");
                                    }
                                }
                            }
                            break;
                        case MsgTypeEnum.GuardBuy:
                            string dpword = "??未知??";
                            if (!DataBase.me.isBiliUserExist(e.Danmaku.UserID))
                            {
                                new_commers++;
                            }

                            if (!viewerlist.Contains(e.Danmaku.UserID))
                            {
                                viewerlist.Add(e.Danmaku.UserID);
                            }

                            DataBase.me.addBiliUser(e.Danmaku.UserID, e.Danmaku.UserName);
                            int coin_gold_ = 0;
                            switch (e.Danmaku.UserGuardLevel)
                            {
                                case 1:
                                    coin_gold_ = 19998000;
                                    dpword = "总督";
                                    break;
                                case 2:
                                    coin_gold_ = 1998000;
                                    dpword = "提督";
                                    break;
                                case 3:
                                    coin_gold_ = 158000;
                                    dpword = "舰长";
                                    break;
                            }
                            gold_coins += coin_gold_;
                            bool isnew = !DataBase.me.isBiliUserGuard(e.Danmaku.UserID);
                            if (lid > 0)
                            {
                                if (isnew)
                                {
                                    MainHolder.broadcaster.BroadcastToAdminGroup("欢迎新" + dpword + "！\n" + e.Danmaku.UserName + " #" + e.Danmaku.UserID + "\n时长:" + e.Danmaku.GiftCount);
                                    blr.sendDanmaku("欢迎新" + dpword + "！请留意私信哦~");
                                }
                                else
                                {
                                    MainHolder.broadcaster.BroadcastToAdminGroup("欢迎" + dpword + "续航！\n" + e.Danmaku.UserName + " #" + e.Danmaku.UserID + "\n时长:" + e.Danmaku.GiftCount);
                                    blr.sendDanmaku("欢迎<" + e.Danmaku.UserName + ">续航！");
                                }
                            }
                            else
                            {
                                if (isnew)
                                {
                                    MainHolder.broadcaster.BroadcastToAdminGroup("侦测到虚空·新" + dpword + "\n" + e.Danmaku.UserName + " #" + e.Danmaku.UserID + "\n时长:" + e.Danmaku.GiftCount);
                                    blr.sendDanmaku("虚空·新" + dpword + " 已记录 请留意私信~");
                                }
                                else
                                {
                                    MainHolder.broadcaster.BroadcastToAdminGroup("侦测到虚空·" + dpword + "续航\n" + e.Danmaku.UserName + " #" + e.Danmaku.UserID + "\n时长:" + e.Danmaku.GiftCount);
                                    blr.sendDanmaku("侦测到虚空续航，嘀嘀嘀~");
                                }
                            }
                            DataBase.me.recUserBuyGuard(e.Danmaku.UserID, e.Danmaku.GiftCount, e.Danmaku.UserGuardLevel, lid);
                            int timestamp = e.Danmaku.SendTime;
                            if (timestamp < 1600000000)
                            {
                                timestamp = TimestampHandler.GetTimeStamp(DateTime.Now);//使用备用时间戳生成方式
                            }
                            SendKeyToCrewMember(e.Danmaku.UserID, e.Danmaku.GiftCount, e.Danmaku.UserGuardLevel, timestamp, dpword, isnew);
                            break;

                        case MsgTypeEnum.GiftSend:
                            string cointype = json["data"].Value<string>("coin_type");//silver | gold
                            int coins = json["data"].Value<int>("total_coin");
                            if (cointype == "silver")
                            {
                                selver_coins += coins;
                            }
                            else
                            {
                                gold_coins += coins;
                            }
                            DataBase.me.recBGift(lid, e.Danmaku.UserID, cointype, coins, e.Danmaku.GiftName);
                            if (!DataBase.me.isBiliUserExist(e.Danmaku.UserID))
                            {
                                new_commers++;
                            }

                            if (!viewerlist.Contains(e.Danmaku.UserID))
                            {
                                viewerlist.Add(e.Danmaku.UserID);
                            }

                            DataBase.me.addBiliUser(e.Danmaku.UserID, e.Danmaku.UserName);
                            break;
                        case MsgTypeEnum.Welcome:
                            if (!DataBase.me.isBiliUserExist(e.Danmaku.UserID))
                            {
                                new_commers++;
                            }

                            if (!viewerlist.Contains(e.Danmaku.UserID))
                            {
                                viewerlist.Add(e.Danmaku.UserID);
                            }

                            DataBase.me.addBiliUser(e.Danmaku.UserID, e.Danmaku.UserName);
                            break;
                        case MsgTypeEnum.LiveEnd:
                        case MsgTypeEnum.LiveStart:
                            break;
                        default:
                            JObject obj = JObject.Parse(e.Danmaku.RawData);
                            if (obj["cmd"]?.ToObject<string>() == "ROOM_BLOCK_MSG")
                            {
                                List<int> tobebanned = DataBase.me.listPermbans();
                                int uid = (int)obj["uid"]?.ToObject<int>();
                                string name = obj["uname"]?.ToObject<string>();
                                if (tobebanned.Contains(uid))
                                {
                                    break;
                                }

                                MainHolder.broadcaster.BroadcastToAdminGroup("【直播间禁言】\n" + name + "#" + uid + "被禁言。\n使用\"#bpban " + uid + "\"可将其永久封禁。");
                                MainHolder.bilidmkproc.blr.sendDanmaku(name + "被禁言");
                                DataBase.me.recBLiveBan(lid, uid, -1);
                            }
                            break;
                    }
                }
                catch (Exception err)
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[弹幕监视]\nMessageLoop工作正常，该错误不会影响其它弹幕接收。\n\n" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);

                }
            });
        }

        public static int GetMedalLevelMatchUID(int streamer_uid, JObject json)
        {
            if (json["info"][3].Count() < 1)
            {
                return -2;
            }

            int level = int.Parse(json["info"][3][0].ToString());
            int uid = int.Parse(json["info"][3][12].ToString());
            if (streamer_uid == uid)
            {
                return level;
            }
            else
            {
                return -1;
            }
        }

        public static bool SendKeyToCrewMember(int uid, int length, int crewlevel, int timestamp, string clevel, bool isnew)
        {
            string token = CrewKeyProcessor.getToken(uid, length, crewlevel, timestamp);
            PrivMessageSession session = PrivMessageSession.openSessionWith(uid);
            bool succeed = true;
            if (isnew)
            {
                if (!DataBase.me.isUserBoundedQQ(uid))
                {
                    succeed = succeed && session.sendMessage("欢迎新" + clevel + "上船！成为船员您可以加入舰长群，并有机会获得小礼品。请妥善保管下面的凭证，在系统出错时它将能证明您的船员身份。");
                    succeed = succeed && session.sendMessage(token);
                    succeed = succeed && session.sendMessage("能告诉我您的QQ号吗？我将通知管理协助您进入舰长群并登记享受更多权益。【请回复纯数字，该系统无人值守，私信由软件接收】");
                    DataBase.me.addBiliPending(uid);
                    MainHolder.broadcaster.BroadcastToAdminGroup("已通过B站私信将token发送给 新·" + clevel);
                }
                else
                {//已绑定QQ
                    long qq = DataBase.me.getUserBoundedQQ(uid);
                    long tgroup = DataBase.me.whichGroupsAreTheUserIn(qq)[0];
                    bool res = MainHolder.broadcaster.SendToQQ(qq, "欢迎新" + clevel + "上船！请妥善保管下面的凭证，在系统出错时它将能证明您的船员身份。", tgroup);
                    if (res)
                    {
                        res = MainHolder.broadcaster.SendToQQ(qq, token, tgroup);
                        succeed = res;
                        MainHolder.broadcaster.BroadcastToAdminGroup("已通过QQ将token发送给 新·" + clevel);
                    }
                    else//用QQ发送失败 FallBack到B站私信
                    {
                        succeed = succeed && session.sendMessage("欢迎新" + clevel + "上船！请妥善保管下面的凭证，在系统出错时它将能证明您的船员身份。");
                        succeed = succeed && session.sendMessage(token);
                        MainHolder.broadcaster.BroadcastToAdminGroup("已通过B站私信将token发送给 新·" + clevel);
                    }
                }
            }
            else
            {
                if (!DataBase.me.isUserBoundedQQ(uid))
                {
                    succeed = succeed && session.sendMessage("欢迎您续航" + clevel + "！请妥善保管下面的凭证，在系统出错时它将能证明您的船员身份。");
                    succeed = succeed && session.sendMessage(token);
                    succeed = succeed && session.sendMessage("能告诉我您的QQ号吗？我将通知管理协助您进入舰长群并登记享受更多权益。【请回复纯数字，该系统无人值守，私信由软件接收】");
                    DataBase.me.addBiliPending(uid);
                    MainHolder.broadcaster.BroadcastToAdminGroup("已通过B站私信将token发送给 续航·" + clevel);
                }
                else
                {//已绑定QQ
                    long qq = DataBase.me.getUserBoundedQQ(uid);
                    long tgroup = 0;
                    try
                    {
                        tgroup = DataBase.me.whichGroupsAreTheUserIn(qq)[0];
                    }
                    catch
                    {
                        //fall back to BiliPrivMessage
                        succeed = succeed && session.sendMessage("欢迎您续航" + clevel + "！请妥善保管下面的凭证，在系统出错时它将能证明您的船员身份。");
                        succeed = succeed && session.sendMessage(token);
                        MainHolder.broadcaster.BroadcastToAdminGroup("已通过B站私信(QQ无群联系)将token发送给 续航·" + clevel);
                        return succeed;
                    }
                    bool res = MainHolder.broadcaster.SendToQQ(qq, "欢迎您续航" + clevel + "！请妥善保管下面的凭证，在系统出错时它将能证明您的船员身份。", tgroup);
                    if (res)
                    {
                        res = MainHolder.broadcaster.SendToQQ(qq, token, tgroup);
                        succeed = res;
                        MainHolder.broadcaster.BroadcastToAdminGroup("已通过QQ将token发送给 续航·" + clevel);
                    }
                    else//用QQ发送失败 FallBack到B站私信
                    {
                        succeed = succeed && session.sendMessage("欢迎您续航" + clevel + "！请妥善保管下面的凭证，在系统出错时它将能证明您的船员身份。");
                        succeed = succeed && session.sendMessage(token);
                        MainHolder.broadcaster.BroadcastToAdminGroup("已通过B站私信将token发送给 续航·" + clevel);
                    }
                }
            }
            return succeed;
        }

        public void Log(ConsoleColor color, string text)
        {
            MainHolder.Logger.Info("danmaku_log_debug_recvs", text);
        }
    }
}
