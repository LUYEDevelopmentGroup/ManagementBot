using Mirai.CSharp;
using Mirai.CSharp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using BiliApi;
using BiliApi.BiliPrivMessage;
using BroadTicketUtility;
using System.IO;
using System.Drawing.Imaging;
using tech.msgp.groupmanager.Code.ScriptHandler;
using Mirai.CSharp.HttpApi.Models.EventArgs;
using Mirai.CSharp.HttpApi.Models.ChatMessages;
using Mirai.CSharp.HttpApi.Session;

namespace tech.msgp.groupmanager.Code
{
    internal class Commands
    {
        public static string stagename = "summer";

        private static string[] bannedKeywords = { "open(", "import", "system(", "exec", "eval", "input", "read",
        "sleep","delay","while","write","stream"};
        public static List<string> getAllPictures(IGroupMessageEventArgs e)
        {
            List<string> list = new List<string>();
            foreach (IChatMessage msg in e.Chain)
            {
                if (msg.Type == "Image")
                {
                    ImageMessage imgmsg = (ImageMessage)msg;
                    list.Add(imgmsg.Url);
                }
            }
            return list;
        }

        public static void SendTicket(int uid, int level)
        {
            return;
            Ticket a = new Ticket()
            {
                Data = new Ticket.DataArea()
                {
                    GenerateTime = DateTime.Now,
                    Level = (Ticket.CrewLevel)level,
                    SerialNumber = Guid.NewGuid(),
                    SpecType = stagename,
                    Uid = uid
                }
            };
            var img = TicketCoder.Encode(a);
            MemoryStream ms = new MemoryStream();
            img.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            bool succeed = false;
            while (true)
            {
                /*
                if (DataBase.me.isUserBoundedQQ(uid))
                {
                    var msg = MainHolder.session.UploadPictureAsync(UploadTarget.Temp, ms).Result;
                    var tgroups = DataBase.me.whichGroupsAreTheUserIn(DataBase.me.getUserBoundedQQ(uid));
                    if (tgroups.Count >= 1)
                    {
                        var tgroup = tgroups[0];
                        var r = MainHolder.session.SendTempMessageAsync(DataBase.me.getUserBoundedQQ(uid), tgroup, msg).Result;
                        succeed = true;
                        break;
                    }
                }
                */
                PrivMessageSession session = PrivMessageSession.openSessionWith(uid, MainHolder.biliapi);
                succeed = true;
                if (!session.SendImage(img))
                {
                    succeed = session.sendMessage(a.ToString());
                    session.sendMessage("发不了图片QAQ 复制上面的消息找[管理组鸡蛋🥚]索要船票噢");
                }
                break;
            }
            if (!succeed)
            {
                var msg = MainHolder.session.UploadPictureAsync(UploadTarget.Group, ms).Result;
                MainHolder.broadcaster.BroadcastToAdminGroup(new IChatMessage[] { new PlainMessage("船员<" + uid + ">的船票无法送达"), (IChatMessage)msg });
            }
            else
            {
                MainHolder.broadcaster.BroadcastToAdminGroup(new IChatMessage[] { new PlainMessage("船员<" + uid + ">船票已送达\nSerial=" + a.Data.SerialNumber.ToString()) });
            }
        }

        public static List<long> getAllAts(IGroupMessageEventArgs e)
        {
            List<long> list = new List<long>();
            foreach (IChatMessage msg in e.Chain)
            {
                if (msg.Type == "At")
                {
                    AtMessage imgmsg = (AtMessage)msg;
                    list.Add(imgmsg.Target);
                }
            }
            return list;
        }

        public static void Warn(IMiraiHttpSession session, IGroupMessageEventArgs e, string clearstr, double weigh)
        {
            string[] cmd = clearstr.Split(' ');
            string imgdata = "";
            int imgid = 0;
            List<string> pics = getAllPictures(e);
            List<long> ats = getAllAts(e);
            if (pics.Count > 0)
            {
                imgdata = "\n\n附件图片：";
                foreach (string pic in pics)
                {
                    if (pic == null)
                    {
                        continue;
                    }

                    imgid++;
                    imgdata += "\n[PICTURE]" + pic;
                }
            }
            string eviid = BiliApi.TimestampHandler.GetTimeStamp16(DateTime.Now).ToString();

            if (ats.Count <= 0)
            {
                //考虑到不方便at的情况，使用QQ号
                if (long.TryParse(cmd[1], out long w_qq))
                {
                    DataBase.me.recQQWarn(w_qq, e.Sender.Id, weigh, clearstr.Substring(3) + imgdata);
                    double wcount = DataBase.me.getQQWarnCount(w_qq);
                    string iname = "";
                    try
                    {
                        iname = "<" + new UserData(w_qq).name + ">";
                    }
                    catch
                    {
                        iname = "* [" + w_qq + "]";
                    }
                    ManagementEvent me = new ManagementEvent(ManagementEvent.WARN, e.Sender.Id, w_qq, clearstr);
                    DataBase.me.recManagementEvents(me);
                    MainHolder.broadcaster.BroadcastToAdminGroup(iname + " #" + w_qq + "在" + e.Sender.Group.Name + "被警告\n第<" + wcount + ">次被警告\n附件数：" + imgid + "\n事件ID：" + me.id);
                }
                else
                {
                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "请在使用<#警告>时@出警告对象");
                }
            }
            string data = clearstr;
            string r = "[警告处理结果]\n" + e.Sender.Group.Name + "\n";
            List<long> metioned = new List<long>();
            foreach (long warn_qq in ats)
            {
                if (metioned.Contains(warn_qq))
                {
                    continue;
                }

                metioned.Add(warn_qq);
                DataBase.me.recQQWarn(warn_qq, e.Sender.Id, weigh, clearstr + imgdata);
                double warncount = DataBase.me.getQQWarnCount(warn_qq);
                r += DataBase.me.getUserName(warn_qq) + " => " + warncount + "\n";
            }
            MainHolder.broadcaster.BroadcastToAdminGroup(r + "\n附件数：" + imgid);
            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, r + "\n附件数：" + imgid);
        }

        public static long cocogroup = -1;
        public static DateTime lastUserexecute = DateTime.Now;

        public static void Proc(IMiraiHttpSession session, IGroupMessageEventArgs e, string clearstr)
        {
            if (clearstr == null || clearstr.Length < 2 || clearstr.IndexOf("#") != 0)
            {

            }
            else
            {//是一条指令
                if (clearstr.IndexOf("//script") == 0)
                {//是脚本
                    if (DataBase.me.isUserOperator(e.Sender.Id))
                    {//管理员脚本
                        var result = AdminJScriptHandler.EvaluateJs(clearstr);
                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "<Script Engine>\nRole=管理员\n执行结果>\n" + result);
                    }
                    else
                    {
                        if ((DateTime.Now - lastUserexecute).TotalSeconds < 10)
                        {
                            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "<Script Engine>\n拒绝执行：用户不能频繁提交脚本");
                        }
                        var result = UserJScriptHandler.EvaluateJs(clearstr);
                        lastUserexecute = DateTime.Now;
                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "<Script Engine>\nRole=用户\n执行结果>\n" + result);
                    }
                    return;
                }
                if (DataBase.me.isUserOperator(e.Sender.Id))//仅允许数据库中的管理员
                {
                    try
                    {
                        string[] cmd = clearstr.Split(' ');
                        switch (cmd[0])
                        {
                            case "#INIT_GROUP":
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "！该操作涉及大量数据库操作，需要一定时间");
                                DataBase.me.init_groupdata(e.Sender.Group.Id);
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已初始化群数据");
                                break;
                            case "#REFETCH_MEMBERS":
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "！该操作涉及大量数据库操作，需要一定时间");
                                DataBase.me.update_groupmembers(e.Sender.Group.Id);
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已更新群成员数据");
                                break;
                            case "#getcode":
                                cocogroup = e.Sender.Group.Id;
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "将解析下一条特殊消息。");
                                break;
                            case "#json":
                                {
                                    int pos = clearstr.IndexOf("{");
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[] { new JsonMessage() { Json = clearstr.Substring(pos) } });
                                }
                                break;
                            case "#xml":
                                {
                                    int pos = clearstr.IndexOf("<");
                                    string st = clearstr.Substring(pos);
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[] { new XmlMessage() { Xml = st } });
                                }
                                break;
                            case "#app":
                                {
                                    int pos = clearstr.IndexOf("{");
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[] { new AppMessage() { Content = clearstr.Substring(pos) } });
                                }
                                break;
                            case "#解除拉黑":
                            case "#unban":
                                {
                                    if (DataBase.me.removeUserBlklist(long.Parse(cmd[1])))
                                    {
                                        string name = "";
                                        try
                                        {
                                            name = "<" + new UserData(long.Parse(cmd[1])).name + ">";
                                        }
                                        catch
                                        {
                                            name = "* <[GETUSERNICK(" + cmd[1] + ")]>";
                                        }
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已将" + name + "从黑名单移除");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "无法执行该操作，建议检查该用户是否已被拉黑，或中央数据库可能暂时不可用");
                                    }
                                }
                                break;
                            case "#手动拉黑":
                            case "#ban":
                                {
                                    if (DataBase.me.addUserBlklist(long.Parse(cmd[1]), (
                                        (cmd.Length > 2 ? (cmd[2]) : ("管理员手动拉黑"))
                                        ), e.Sender.Id))
                                    {
                                        string name = "";
                                        try
                                        {
                                            name = "<" + new UserData(long.Parse(cmd[1])).name + ">";
                                        }
                                        catch
                                        {
                                            name = "* <[GETUSERNICK(" + cmd[1] + ")]>";
                                        }
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已将" + name + "加入黑名单");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "无法执行该操作，中央数据库可能暂时不可用");
                                    }
                                }
                                break;
                            case "#存证查询":
                            case "#cm":
                                {
                                    string fname = cmd[1];
                                    DataBase.me.getMessageGroup(fname, out string fresid, out int tsum, out int flag, out int serviceID, out int m_fileSize);
                                    XmlMessage xmlMessage = new XmlMessage("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<msg brief=\"[存证]\" m_fileName=\"" + fname + "\" action=\"viewMultiMsg\" tSum=\"" + tsum + "\" flag=\"" + flag + "\" m_resid=\"" + fresid + "\" serviceID=\"" + serviceID + "\" m_fileSize=\"" + m_fileSize + "\"  > <item layout=\"1\"> <title color=\"#000000\" size=\"34\" > 聊天记录存证 </title> <title color=\"#000000\" size=\"26\" > 腾讯消息存档 </title> <title color=\"#000000\" size=\"26\" > " + fname + " </title> <hr></hr> <summary color=\"#808080\" size=\"26\" > 查看该条存证  </summary> </item><source name=\"聊天记录\"></source> </msg>");
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[] { xmlMessage });
                                }
                                break;
                            case "#不信任":
                            case "#untrust":
                                {
                                    if (DataBase.me.removeUserTrustlist(long.Parse(cmd[1])))
                                    {
                                        string name = "";
                                        try
                                        {
                                            name = "<" + new UserData(long.Parse(cmd[1])).name + ">";
                                        }
                                        catch
                                        {
                                            name = "* <[GETUSERNICK(" + cmd[1] + ")]>";
                                        }
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已解除信任" + name + "。");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "无法执行该操作。用户可能未被信任或中央数据库暂时不可用。");
                                    }
                                }
                                break;
                            case "#信任":
                            case "#trust":
                                {
                                    if (DataBase.me.addUserTrustlist(long.Parse(cmd[1]), false, e.Sender.Id))
                                    {
                                        string name = "";
                                        try
                                        {
                                            name = "<" + new UserData(long.Parse(cmd[1])).name + ">";
                                        }
                                        catch
                                        {
                                            name = "* <[GETUSERNICK(" + cmd[1] + ")]>";
                                        }
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已信任" + name + "。\n该信任是永久的，将帮助该用户直接通过今后的所有检查。请谨慎。");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "无法执行该操作，中央数据库可能暂时不可用");
                                    }
                                }
                                break;
                            case "#crews":
                                {
                                    CrewChecker cr = new CrewChecker();
                                    cr.checkCrews();
                                }
                                break;
                            case "#信任一次":
                            case "#trustonce":
                                {
                                    if (DataBase.me.addUserTrustlist(long.Parse(cmd[1]), true, e.Sender.Id))
                                    {
                                        string name = "";
                                        try
                                        {
                                            name = "<" + new UserData(long.Parse(cmd[1])).name + ">";
                                        }
                                        catch
                                        {
                                            name = "* <[GETUSERNICK(" + cmd[1] + ")]>";
                                        }
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已信任" + name + "。\n该信任是一次性的，仅使对应用户直接通过下一次检查。");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "无法执行该操作，中央数据库可能暂时不可用");
                                    }
                                }
                                break;
                            case "#push_live":
                            case "#推直播":
                                {
                                    BiliLiveRoom lroom = new BiliLiveRoom(2064239, MainHolder.bililogin);
                                    MainHolder.broadcaster.BroadcastToAllGroup("[ATALL()][开播啦！]\n" + lroom.title + "\nhttps://live.bilibili.com/" + lroom.roomid);
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "Done.");
                                }
                                break;
                            case "#sendpmsg":
                                {
                                    PrivMessageSession psession = PrivMessageSession.openSessionWith(int.Parse(cmd[1]), MainHolder.biliapi);
                                    psession.sendMessage(clearstr.Substring(clearstr.IndexOf(cmd[1]) + cmd[1].Length + 1));
                                }
                                break;
                            case "#cow":
                                {
                                    if (long.TryParse(cmd[1], out long w_qq))
                                    {
                                        double wcount = DataBase.me.getQQWarnCount(w_qq);
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, w_qq + "> " + wcount);
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "请给出查询对象QQ");
                                    }
                                }
                                break;
                            case "#warn":
                                Warn(session, e, clearstr, double.Parse(cmd[2]));
                                break;
                            case "#weigh":
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[]{
                                    new AtMessage(e.Sender.Id),
                                    new PlainMessage("您的权重为<"+DataBase.me.getOPWeigh(e.Sender.Id)+">\n该值用于稳定违规处罚力度，可能会动态调整。")
                                    });
                                break;
                            case "#警告":
                                {
                                    Warn(session, e, clearstr, 3);
                                }
                                break;
                            case "#注意":
                                {
                                    Warn(session, e, clearstr, 2);
                                }
                                break;
                            case "#提醒":
                                {
                                    Warn(session, e, clearstr, 1);
                                }
                                break;
                            case "#查用户警告":
                            case "#listwarn":
                            case "#lw":
                                {
                                    List<Warn> ws = DataBase.me.listWarnsQQ(long.Parse(cmd[1]));
                                    string rpl = "该用户总计有" + ws.Count + "次警告记录：\n[id]<时间>{管理员}\n";
                                    foreach (Warn w in ws)
                                    {
                                        rpl += "[" + w.id + "]<" + w.time.ToString() + ">{" + DataBase.me.getAdminName(w.op) + "}\n";
                                    }
                                    rpl += "\n可使用指令\"#cw [id]\"查询对应警告的详细信息";
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, rpl);
                                }
                                break;
                            case "#查警告":
                            case "#checkwarn":
                            case "#cw":
                                {
                                    Warn w = DataBase.me.getWarnByID(int.Parse(cmd[1]));
                                    string rpl = "【警告】\n";
                                    rpl += "识别号 " + w.id + "\n";
                                    rpl += "时间   " + w.time.ToString() + "\n";
                                    rpl += "操作者 " + DataBase.me.getAdminName(w.op) + "#" + w.op + "\n";
                                    rpl += "被警告 " + DataBase.me.getUserName(w.qq) + "#" + w.qq + "\n";
                                    rpl += "备注 \n";
                                    rpl += w.note + "\n\n";
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, rpl);
                                }
                                break;
                            case "#验证":
                            case "#check":
                                int uid, len, level, tstamp;
                                if (cmd[1] == null || cmd[1].Length < 2)
                                {
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "指令出错×\n" +
                                        "请确保#check和凭据文本之间只存在一个空格");
                                }
                                try
                                {
                                    if (CrewKeyProcessor.checkToken(cmd[1], out uid, out len, out level, out tstamp))
                                    {
                                        string dppword = "??未知??";
                                        switch (level)
                                        {
                                            case 1:
                                                dppword = "总督";
                                                break;
                                            case 2:
                                                dppword = "提督";
                                                break;
                                            case 3:
                                                dppword = "舰长";
                                                break;
                                        }
                                        long qqid;
                                        if (DataBase.me.isUserBoundedQQ(uid))
                                        {
                                            qqid = DataBase.me.getUserBoundedQQ(uid);
                                        }
                                        else
                                        {
                                            qqid = -1;
                                        }
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "签名有效√\n" +
                                            "颁发给UID:" + uid + "" + ((qqid > 0 ? "(绑定QQ:[GETUSERNICK(" + qqid + ")] #" + qqid + ")" : "(未绑定QQ)")) + "\n" +
                                            "用以证明购买 " + dppword + "*" + len + " 月\n" +
                                            "颁发时间:" + BiliApi.TimestampHandler.GetDateTime(tstamp) + "\n" +
                                            "\n" +
                                            "⚠该凭据仅表明以上购买信息真实有效，不做其它用途");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "该凭据签名无效×\n" +
                                            "⚠该凭据上没有有效的签名。它包含的信息可能已遭到篡改。不要信任该凭据指示的任何信息。");
                                    }
                                }
                                catch
                                {
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "该凭据无效×\n" +
                                            "⚠无法解码该凭据。数据已遭到篡改或破坏。不要信任该凭据指示的任何信息。");
                                }
                                break;
                            case "#颁发":
                            case "#gentoken":
                                if (cmd.Length < 4)
                                {
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "指令用法错误。\n" +
                                        "" + cmd[0] + " uid 时长 等级 是否新\n" +
                                        "例如：" + cmd[0] + " 5659864 12 1 0\n表示鹿野上了12个月总督(1)，不是新舰长(0)");
                                    return;
                                }
                                string dpword = "??未知??";
                                switch (int.Parse(cmd[3]))
                                {
                                    case 1:
                                        dpword = "总督";
                                        break;
                                    case 2:
                                        dpword = "提督";
                                        break;
                                    case 3:
                                        dpword = "舰长";
                                        break;
                                }
                                BiliDanmakuProcessor.SendKeyToCrewMember(int.Parse(cmd[1]), int.Parse(cmd[2]), int.Parse(cmd[3]), BiliApi.TimestampHandler.GetTimeStamp(DateTime.Now), dpword, (cmd[4] != null && (cmd[4] == "新" || cmd[4] == "1")));
                                break;
                            case "#同意":
                            case "#pass":
                                MainHolder.broadcaster.BroadcastToAdminGroup("DEBUG_TRIGGER_PASS");
                                //new Event_GroupMemberRequest().checkAndProcessReqQueue();
                                break;
                            case "#debug_getlevel":
                                int llevel = ThirdPartAPIs.getQQLevel(int.Parse(cmd[1]), 1);
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, ">DEBUG<\nDEBUG_GET_LEVEL\n" + cmd[1] + " = " + llevel);
                                break;
                            case "#挂起请求":
                            case "#hangreq":
                                //Event_GroupMemberRequest.hang_req = true;
                                MainHolder.broadcaster.BroadcastToAdminGroup("!符合条件的!加群请求将被挂起留待管理处理。");
                                break;
                            case "#挂起全部":
                            case "#hangall":
                                //Event_GroupMemberRequest.hang_all = true;
                                MainHolder.broadcaster.BroadcastToAdminGroup("!所有!加群请求将被挂起留待管理处理。");
                                break;
                            case "#释放请求":
                            case "#releasereq":
                                //Event_GroupMemberRequest.hang_req = false;
                                //Event_GroupMemberRequest.hang_all = false;
                                MainHolder.broadcaster.BroadcastToAdminGroup("加群请求将按照规则自动处理");
                                break;
                            case "#手动开播":
                            case "#setlive":
                                if (MainHolder.bilidmkproc.lid < 0)
                                {
                                    MainHolder.bilidmkproc.lid = int.Parse(cmd[1]);
                                    MainHolder.broadcaster.BroadcastToAdminGroup("已将直播事件ID手动绑定为：" + MainHolder.bilidmkproc.lid);
                                }
                                else if (int.Parse(cmd[1]) < 0)
                                {
                                    MainHolder.broadcaster.BroadcastToAdminGroup("已销毁事件ID：" + MainHolder.bilidmkproc.lid);
                                    MainHolder.bilidmkproc.lid = int.Parse(cmd[1]);
                                }
                                else
                                {
                                    MainHolder.broadcaster.BroadcastToAdminGroup("WARNING 替换正在处理的直播ID！\n" + MainHolder.bilidmkproc.lid + " 换为 " + int.Parse(cmd[1]));
                                    MainHolder.bilidmkproc.lid = int.Parse(cmd[1]);
                                }
                                break;
                            case "#debug_checkcrew":
                                MainHolder.broadcaster.BroadcastToAdminGroup("上舰次数：" + DataBase.me.getBiliUserGuardCount(int.Parse(cmd[1])));
                                break;
                            case "#debug_iscrew":
                                MainHolder.broadcaster.BroadcastToAdminGroup("是否舰长：" + (DataBase.me.isBiliUserGuard(int.Parse(cmd[1])) ? "是" : "否"));
                                break;
                            case "#debug_ticket":
                                SendTicket(int.Parse(cmd[1]), int.Parse(cmd[2]));
                                break;
                            case "#debug_crewbuy":
                                int uuid = int.Parse(cmd[1]);
                                dpword = "??未知??";
                                switch (1)
                                {
                                    case 1:
                                        dpword = "总督";
                                        break;
                                }
                                bool isnew = !DataBase.me.isBiliUserGuard(uuid);
                                if (MainHolder.bilidmkproc.lid > 0)
                                {
                                    if (isnew)
                                    {
                                        MainHolder.broadcaster.BroadcastToAdminGroup("欢迎新" + dpword + "！\n[DEBUG] #" + uuid + "\n时长:1");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.BroadcastToAdminGroup("欢迎" + dpword + "续航！\n[debug] #" + uuid + "\n时长:1");
                                    }
                                }
                                else
                                {
                                    if (isnew)
                                    {
                                        MainHolder.broadcaster.BroadcastToAdminGroup("侦测到虚空·新" + dpword + "\n[DEBUG] #" + uuid + "\n时长:1");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.BroadcastToAdminGroup("侦测到虚空·" + dpword + "续航\n[DEBUG] #" + uuid + "\n时长:1");
                                    }
                                }
                                //sendKeyToCrewMember(e.Danmaku.UserID, e.Danmaku.GiftCount, e.Danmaku.UserGuardLevel, e.Danmaku.SendTime, dpword, isnew);
                                //DataBase.me.recUserBuyGuard(e.Danmaku.UserID, e.Danmaku.GiftCount, e.Danmaku.UserGuardLevel, lid);
                                break;
                            case "#debug_mcuuid":
                                bool mo;
                                string nsuuid = MCServer.DBHandler.genNoSlashUUID(cmd[1], out mo);
                                string suuid = MCServer.DBHandler.genSlashUUID(cmd[1]);
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[调试 - MC UUID]\n该用户的UUID计算结果为：\n" + suuid + "\n" + nsuuid);
                                break;
                            case "#debug_check_fans":
                                DynChecker.check_fans(true);
                                break;
                            case "#dropmsg":
                                PrivmessageChecker.DropMessages = !PrivmessageChecker.DropMessages;
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[调试 - 丢弃私信]" + (PrivmessageChecker.DropMessages ? "是" : "否"));
                                break;
                            case "#blockmsg":
                                PrivmessageChecker.BlockReceiver = !PrivmessageChecker.BlockReceiver;
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[调试 - 阻断私信处理]" + (PrivmessageChecker.BlockReceiver ? "是" : "否"));
                                break;
                            case "#boundqq":
                                DataBase.me.boundBiliWithQQ(long.Parse(cmd[1]), long.Parse(cmd[2]));
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[手动QQ绑定]" + (PrivmessageChecker.BlockReceiver ? "是" : "否"));
                                break;
                            /*
                        case "#cape":
                        case "#披风":
                            long qq = -1;
                            if (cmd.Length < 2)
                            {
                                if (!long.TryParse(cmd[1], out qq))
                                {
                                    MainHolder.broadcaster.sendToGroup(e.Sender.Group.Id, "！正在操作您自己的账号");
                                    qq = e.Sender.Id;
                                }
                                else
                                if (e.message.type != "PicMsg")
                                {
                                    MainHolder.broadcaster.sendToGroup(e.Sender.Group.Id, "用法：\n" + cmd[0] + " (玩家QQ) 【图片】");
                                    break;
                                }
                            }
                            CQ2IOT.Model.PicMessage msg = (CQ2IOT.Model.PicMessage)e.message;
                            string url = msg.picture[0].url;
                            Image im = SkinHandler.getImageFromWeb(url);
                            if (qq < 0) qq = long.Parse(cmd[1]);
                            if (!MCServer.SkinHandler.checkPick(im))
                            {
                                MainHolder.broadcaster.sendToGroup(e.Sender.Group.Id, "图片格式有误。必须是有效PNG图片，且为64*32或64*64比例。");
                                break;
                            }
                            string hash = MCServer.SkinHandler.getPictureHash(im);
                            string savepath = "Y:/skin/CAPE/" + hash + ".png";
                            s.host.logger("MC皮肤", "图片存储到" + "Y:/skin/CAPE" + hash + ".png");
                            if (!Directory.Exists("Y:/skin/CAPE"))
                            {
                                s.host.logger("MC皮肤", "转储目录无效", ConsoleColor.DarkYellow);
                            }
                            if (!File.Exists(savepath))
                            {
                                s.host.logger("MC皮肤", "文件已存在", ConsoleColor.DarkYellow);
                            }
                            string uUuid = DBHandler.me.getUserOwnedProfileUUID(qq);
                            string pname = DBHandler.me.getUserOwnedProfileName(qq);
                            string oldtdata = DBHandler.me.getProfileUUIDTexture(uUuid);
                            JObject oldtexture = (JObject)JsonConvert.DeserializeObject(oldtdata);
                            List<Texture> l = new List<Texture>();
                            if (oldtexture != null)
                            {
                                if (oldtexture != null)
                                {
                                    if (oldtexture["textures"]["SKIN"] != null)
                                    {
                                        Dictionary<string, string> ddd = new Dictionary<string, string>();
                                        Texture tt = new Texture(oldtexture["textures"]["SKIN"].Value<string>("url"), "SKIN", ddd);
                                        l.Add(tt);
                                    }
                                }
                            }
                            Dictionary<string, string> d = new Dictionary<string, string>();
                            Texture t = new Texture(url, "CAPE", d);
                            l.Add(t);
                            SkinHandler sh = new SkinHandler(DBHandler.me.getUserOwnedProfileUUID(qq), DBHandler.me.getUserOwnedProfileName(qq), l);
                            if (DBHandler.me.setProfileUUIDTexture(uUuid, sh.ToString()))
                            {
                                MainHolder.broadcaster.sendToGroup(e.Sender.Group.Id, "操作成功，已为玩家" + pname + "设置披风。\n" + "http://textures.mc.microstorm.tech:15551/CAPE/" + hash);
                            }
                            else
                            {
                                MainHolder.broadcaster.sendToGroup(e.Sender.Group.Id, "操作失败");
                            }
                            break;
                            */
                            case "#lb":
                                MainHolder.doBiliLogin = true;
                                break;
                            case "#lq":
                                MainHolder.doQQLogin = true;
                                break;
                            case "#发弹幕":
                            case "#senddmk":
                                switch (cmd.Length)
                                {
                                    case 2:
                                        if (MainHolder.useBiliRecFuncs) MainHolder.bilidmkproc.blr.sendDanmaku(cmd[1]);
                                        break;
                                    case 3:
                                        if (MainHolder.useBiliRecFuncs) MainHolder.bilidmkproc.blr.sendDanmaku(cmd[1], int.Parse(cmd[2]));
                                        break;
                                    case 4:
                                        if (MainHolder.useBiliRecFuncs) MainHolder.bilidmkproc.blr.sendDanmaku(cmd[1], int.Parse(cmd[2]), int.Parse(cmd[3]));
                                        break;
                                    case 5:
                                        if (MainHolder.useBiliRecFuncs) MainHolder.bilidmkproc.blr.sendDanmaku(cmd[1], int.Parse(cmd[2]), int.Parse(cmd[3]), int.Parse(cmd[4]));
                                        break;
                                    default:
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "#senddmk 文本内容 字体大小 BLive颜色代码 BLive气泡代码");
                                        break;
                                }
                                break;
                            case "#sendpriv":
                                {

                                    PrivMessageSession sess = PrivMessageSession.openSessionWith(int.Parse(cmd[1]), MainHolder.biliapi);
                                    sess.sendMessage(cmd[2]);
                                }
                                break;
                            case "#CERT":
                                string note;
                                string cname = DataBase.me.getCertificateName(e.Sender.Id, out note);
                                if (cname == null)
                                {
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "您(" + e.Sender.Name + "#" + e.Sender.Id + ")的认证信息：\n[Operator]\n* 该认证表明对应用户是管理组的一员(包括获得授权的见习管理)");
                                }
                                else
                                {
                                    if (note != null && note.Length > 0)
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "您(" + e.Sender.Name + "#" + e.Sender.Id + ")的认证信息：\n" + cname + "\n备注:" + note);
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "您(" + e.Sender.Name + "#" + e.Sender.Id + ")的认证信息：\n" + cname);
                                    }
                                }
                                break;
                            case "#CGC":
                            case "#舰列扫":
                                MainHolder.checkCrewGroup();
                                break;
                            case "#debug_liveban":
                                string rpll = MainHolder.biliapi.banUIDfromroom(int.Parse(cmd[1]), int.Parse(cmd[2]), int.Parse(cmd[3]));
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, rpll);
                                break;
                            case "#debug_trigger_banrefresh":
                                SecondlyTask.trigger_BanRefresh();
                                break;
                            case "#直播永封":
                            case "#bpban":
                                {
                                    if (DataBase.me.setBiliPermBan(int.Parse(cmd[1]), e.Sender.Id))
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "已将Bililbili用户 #" + cmd[1] + " 加入永封黑名单\n系统将每12小时检测一次封禁剩余时长并对其续费。");
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "无法执行该操作，中央数据库可能暂时不可用");
                                    }
                                }
                                break;
                            case "#":
                            case "#stat":
                                MainHolder.broadcaster.BroadcastToAdminGroup("[在线]\n" +
                                "最近一条消息：" + SecondlyTask.lastrecv.ToString() + "\n" +
                                "时段总接收消息：" + MainHolder.MsgCount + "\n" +
                                "上次检查：" + SecondlyTask.laststat.ToString());
                                SecondlyTask.laststat = DateTime.Now;
                                MainHolder.MsgCount = 0;
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception err)
                    {
                        MainHolder.Logger.Error("ERROR_MGRCMD", err.Message + "\n" + err.StackTrace);
                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "执行指令出错。请检查指令及其参数是否正确。如果确认操作无误，请将下面的信息转发给@鸡蛋\n" + "未处理的异常：" + err.Message + "\n堆栈追踪：" + err.StackTrace);
                    }
                }
                else
                {//非管理指令
                    try
                    {
                        string[] cmd = clearstr.Split(' ');
                        switch (cmd[0])
                        {
                            case "#CERT":
                                string note;
                                string cname = DataBase.me.getCertificateName(e.Sender.Id, out note);
                                if (cname == null)
                                {
                                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "找不到有效的认证信息。\n如果您是知名up或代表知名机构，请向管理提交资料认证。");
                                }
                                else
                                {
                                    if (note != null && note.Length > 0)
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "您(" + e.Sender.Name + "#" + e.Sender.Id + ")的认证信息：\n" + cname + "\n备注:" + note);
                                    }
                                    else
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "您(" + e.Sender.Name + "#" + e.Sender.Id + ")的认证信息：\n" + cname);
                                    }
                                }
                                break;
                        }
                    }
                    catch
                    {

                    }
                }


                //通用指令
                try
                {
                    string[] cmd = clearstr.Split(' ');
                    switch (cmd[0])
                    {
                        case "#MC":
                            if (!MainHolder.enableNativeFuncs) return;
                            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "请查看文档：如何加入鹿野的MC粉丝服务器\nhttps://docs.qq.com/doc/DTUJObWZteWVXS1Zi");
                            break;
                        case "#MCSTATUS":
                            {
                                if (!MainHolder.enableNativeFuncs) return;
                                string res = "[Minecraft 粉丝服务器状态]\n";
                                bool anygood = false;
                                //res += "苏州电信 -> 停机维护\n";
                                foreach (KeyValuePair<string, bool> sta in MCServerChecker.results)
                                {
                                    MCServerChecker.MCServer ser = MCServerChecker.servers[sta.Key];
                                    res += ser.name + " -> " + (sta.Value ? MCServerChecker.servers[sta.Key].addr : "不可用") + "\n";
                                    if (sta.Value)
                                    {
                                        anygood = true;
                                    }
                                }
                                res += "\n最后检测:[" + MCServerChecker.last_update.ToString() + "]";
                                if (anygood)
                                {
                                    res += "\n√ 请选用可用的线路连接服务器";
                                }
                                else
                                {
                                    res += "\n× 所有线路都无法连接，请通知[@鸡蛋]并等待处理";
                                }

                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, res);
                            }
                            break;
                        case "#VOICE":
                            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[] { new VoiceMessage(null, "http://192.168.1.7:8910/rand.php?rand" + new Random().Next().ToString(), null) });
                            break;
                    }
                }
                catch
                {

                }

                //Python指令
                if (clearstr.Split('\n')[0].ToUpper().Contains("PYTHON"))
                {
                    if (!MainHolder.enableNativeFuncs) return;
                    MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "Python引擎存在重大安全漏洞，目前已经被下架。");
                    return;
                    {//安全性检测
                        string str = clearstr.ToLower().Replace(" ", "");
                        foreach (string ban in bannedKeywords)
                        {
                            if (str.Contains(ban))
                            {
                                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "Python代码不允许尝试与外部环境交互，也不允许while循环和延时。您的代码没有被执行。");
                                return;
                            }
                        }
                    }

                    string resu = "";// MainHolder.py.runPyCommand(clearstr);
                    if (resu != null && resu.Length > 0)
                    {
                        Array lines = (Array)resu.Split('\n');
                        if (lines.Length > 10)
                        {
                            int ii = 0;
                            StringBuilder sb = new StringBuilder();
                            foreach (string line in lines)
                            {
                                ii++;
                                sb.Append(line);
                                if (ii >= 10) break;
                            }
                            resu = sb.ToString().Substring(0, 100);
                            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[]{
                                new AtMessage(e.Sender.Id),
                                new PlainMessage("\n"+resu+"\n<输出过长被截断>")
                            });
                            resu = null;
                        }
                        else
                        if (resu.Length > 100)
                        {
                            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[]{
                                new AtMessage(e.Sender.Id),
                                new PlainMessage("\n"+resu.Substring(0,100)+"\n<输出过长被截断>")
                            });
                            resu = null;
                        }
                        else
                        {
                            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, new IChatMessage[]{
                                new AtMessage(e.Sender.Id),
                                new PlainMessage("\n"+resu)
                            });
                        }
                    }
                }
            }
        }
    }
}