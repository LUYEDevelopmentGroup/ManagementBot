using CQ2IOT;
using Mirai.CSharp;
using Mirai.CSharp.Models;
using System;
using System.Threading.Tasks;
using System.Xml;
using BiliApi;
using Newtonsoft.Json.Linq;
using System.Drawing;
using BroadTicketUtility;
using BroadTicketUtility.Exceptions;
using Mirai.CSharp.HttpApi.Parsers.Attributes;
using Mirai.CSharp.HttpApi.Parsers;
using Mirai.CSharp.HttpApi.Models.EventArgs;
using Mirai.CSharp.HttpApi.Handlers;
using Mirai.CSharp.HttpApi.Session;
using Mirai.CSharp.HttpApi.Models.ChatMessages;

namespace tech.msgp.groupmanager.Code.EventHandlers
{
    [RegisterMiraiHttpParser(typeof(DefaultMappableMiraiHttpMessageParser<IGroupMessageEventArgs, GroupMessageEventArgs>))]
    public partial class EventHandler : IMiraiHttpMessageHandler<IGroupMessageEventArgs>
    {
        public void processVideoBilibili(IGroupMessageEventArgs e, string bvn)
        {

            BiliVideo biliv = new BiliVideo(bvn, MainHolder.biliapi);
            if (DataBase.me.isUserOperator(e.Sender.Id))
            {
                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "【视频分享】\n《" + biliv.title + "》\nBy " + biliv.owner.name);
                return; //不处理管理员行为
            }

            string dt = "[视频分享]\n群:" + e.Sender.Group.Name + "\n人:" + e.Sender.Name + "\n分享视频：\n" + biliv.title + "\nUP：" + biliv.owner.name + "\nhttps://www.bilibili.com/video/" + biliv.vid + "\n";
            if (biliv.owner.uid == 5659864 ||
                biliv.participants.Contains(new BiliUser(5659864, "", "", "", false, 0, "", 0, 0, new BiliUser.OfficialInfo(), MainHolder.biliapi))
                ||
                biliv.owner.uid == 415413197 ||
                biliv.participants.Contains(new BiliUser(415413197, "", "", "", false, 0, "", 0, 0, new BiliUser.OfficialInfo(), MainHolder.biliapi))
                )//鹿野发布或参与
            {
                MainHolder.broadcaster.BroadcastToAdminGroup(dt + "【鹿野视频，通过】");
            }
            else if (biliv.owner.fans > 500000)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup(dt + "【通识up，通过】");
            }
            else if (biliv.owner.official.Type == BiliUser.OfficialType.Organization)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup(dt + "【机构官方，通过】");
            }
            else
            {
                //违规！警告涉事者并撤回消息
                MainHolder.session.RevokeMessageAsync(((SourceMessage)e.Chain[0]).Id);
                MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "该视频不被允许分享。请仔细阅读群规。\n特殊情况请联系管理员哦");
                MainHolder.broadcaster.BroadcastToAdminGroup(dt + "【涉嫌推广，已撤回】");
                return;
            }
            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "《" + biliv.title + "》\nBy " + biliv.owner.name);
        }

#pragma warning disable CS1998 // 此异步方法缺少 "await" 运算符，将以同步方式运行。请考虑使用 "await" 运算符等待非阻止的 API 调用，或者使用 "await Task.Run(...)" 在后台线程上执行占用大量 CPU 的工作。
        public async Task HandleMessageAsync(IMiraiHttpSession client, IGroupMessageEventArgs e)
#pragma warning restore CS1998 // 此异步方法缺少 "await" 运算符，将以同步方式运行。请考虑使用 "await" 运算符等待非阻止的 API 调用，或者使用 "await Task.Run(...)" 在后台线程上执行占用大量 CPU 的工作。
        {
            try
            {
                var session = MainHolder.session;
                
                WatchDog.FeedDog("grpmsg");
                pThreadPool pool = MainHolder.pool;
                //MainHolder.Logger.Debug("CQPLUGIN", "Event_GroupMessageFired");
                MainHolder.MsgCount++;
                SecondlyTask.lastrecv = DateTime.Now;
                if (true)
                {
                    IGroupInfo gpinfo = e.Sender.Group;
                    IGroupMemberInfo gminfo = e.Sender;
                    string gpname = DataBase.me.getGroupName(gpinfo.Id);
                    if (!DataBase.me.connected)
                    {
                        MainHolder.Logger.Error("数据库", "数据库未连接");
                    }

                    foreach (IChatMessage msg in e.Chain)
                    {
                        switch (msg.Type)
                        {
                            case "Plain":
                                {
                                    PlainMessage message = (PlainMessage)msg;
                                    if (!DataBase.me.recQQmsg(e.Sender.Id, e.Sender.Group.Id, message.Message))
                                    {
                                        MainHolder.Logger.Error("数据库", "未能将消息存入数据库");
                                    }
                                    //message.Message
                                    {
                                        string abvn = AVFinder.abvFromString(message.Message);
                                        if (abvn != null && abvn != "")
                                            processVideoBilibili(e, abvn);
                                    }
                                    Commands.Proc(session, e, message.Message);
                                }
                                break;
                            case "Xml":
                                {
                                    XmlMessage message = (XmlMessage)msg;
                                    if (!DataBase.me.recQQmsg(e.Sender.Id, e.Sender.Group.Id, message.Xml))
                                    {
                                        MainHolder.Logger.Error("数据库", "未能将消息存入数据库");
                                    }

                                    if (Commands.cocogroup == e.Sender.Group.Id)
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[Xml解析]\n" + message.Xml);
                                    }

                                    XmlDocument doc = new XmlDocument();
                                    doc.LoadXml(message.Xml);
                                    {
                                        if (doc["msg"] == null)
                                        {
                                            MainHolder.broadcaster.SendToAnEgg(e.Sender.Group.Id + "无msg标签的Xml消息\n" + message.Xml);
                                        }
                                        if (doc["msg"].HasAttribute("action") && doc["msg"].GetAttribute("action") == "viewMultiMsg" && DataBase.me.isAdminGroup(e.Sender.Group.Id))
                                        {
                                            string fname = doc["msg"].GetAttribute("m_fileName");
                                            string fresid = doc["msg"].GetAttribute("m_resid");
                                            int tsum = int.Parse(doc["msg"].GetAttribute("tSum"));
                                            int flag = int.Parse(doc["msg"].GetAttribute("flag"));
                                            int serviceID = int.Parse(doc["msg"].GetAttribute("serviceID"));
                                            int m_fileSize = int.Parse(doc["msg"].GetAttribute("m_fileSize"));
                                            DataBase.me.saveMessageGroup(fname, fresid, tsum, flag, serviceID, m_fileSize);
                                            MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[消息存证]\n该条消息记录已提交至腾讯服务器\n存根ID:" + fname);
                                            return;
                                            //不再处理该条消息
                                        }
                                        if (doc["msg"]["source"] != null && doc["msg"]["source"].HasAttribute("name"))
                                        {
                                            switch (doc["msg"]["source"].GetAttribute("name"))
                                            {
                                                case "哔哩哔哩"://B站分享
                                                    if (doc["msg"].GetAttribute("url").IndexOf("/live.bilibili.com/") > 0)//直播分享
                                                    {

                                                    }
                                                    if (doc["msg"].GetAttribute("url").IndexOf("/b23.tv/") > 0)//可能是视频分享
                                                    {
                                                        try
                                                        {
                                                            string bvn = BiliApi.AVFinder.bvFromB23url(doc["msg"].GetAttribute("url"));
                                                            if (bvn != null)//真的是视频分享
                                                            {
                                                                processVideoBilibili(e, bvn);
                                                                return;
                                                            }
                                                        }
                                                        catch (Exception)
                                                        {

                                                        }
                                                    }
                                                    break;
                                                case "网页分享":
                                                    if (doc["msg"].GetAttribute("url").IndexOf("/live.bilibili.com/") > 0)//直播分享
                                                    {

                                                    }
                                                    if (doc["msg"].GetAttribute("url").IndexOf("/www.bilibili.com/video/") > 0)//视频分享
                                                    {
                                                        string bvn = BiliApi.AVFinder.bvFromPlayURL(doc["msg"].GetAttribute("url"));
                                                        if (bvn != null)//真的是视频分享
                                                        {
                                                            processVideoBilibili(e, bvn);
                                                            return;
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "Json":
                                {
                                    JsonMessage message = (JsonMessage)msg;
                                    if (Commands.cocogroup == e.Sender.Group.Id)
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[Json解析]\n" + message.Json);
                                    }
                                    if (!DataBase.me.recQQmsg(e.Sender.Id, e.Sender.Group.Id, message.Json))
                                    {
                                        MainHolder.Logger.Error("数据库", "未能将消息存入数据库");
                                    }
                                }
                                break;
                            case "App":
                                {
                                    AppMessage message = (AppMessage)msg;
                                    if (Commands.cocogroup == e.Sender.Group.Id)
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[AppContent解析]\n" + message.Content);
                                    }
                                    if (!DataBase.me.recQQmsg(e.Sender.Id, e.Sender.Group.Id, message.Content))
                                    {
                                        MainHolder.Logger.Error("数据库", "未能将消息存入数据库");
                                    }
                                    {
                                        string abvn = null;
                                        try
                                        {
                                            JObject jb = JObject.Parse(message.Content);
                                            abvn = AVFinder.bvFromB23url(jb["meta"]["detail_1"]["qqdocurl"].ToString());
                                        }
                                         catch
                                        {

                                        }
                                        if (abvn != null && abvn != "")
                                            processVideoBilibili(e, abvn);

                                    }
                                }
                                break;
                            case "Source":
                                break;
                            case "Image":
                                {
                                    /*try
                                    {
                                        ImageMessage message = (ImageMessage)msg;
                                        Ticket t = TicketCoder.Decode(new Bitmap(PicLoader.loadPictureFromURL(message.Url)));
                                        string l = "未知";
                                        switch (t.Data.Level)
                                        {
                                            case Ticket.CrewLevel.舰长:
                                                l = "舰长";
                                                break;
                                            case Ticket.CrewLevel.总督:
                                                l = "总督";
                                                break;
                                            case Ticket.CrewLevel.提督:
                                                l = "提督";
                                                break;
                                        }
                                        BiliUser bu = new BiliUser(t.Data.Uid, MainHolder.biliapi);
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "[船票]\n" +
                                            "版本=" + t.Data.SpecType + "\n" +
                                            "绑定账号=" + bu.name + "#" + t.Data.Uid + "\n" +
                                            "签发时间=" + t.Data.GenerateTime.ToString("yyyy MM dd HH:mm:ss") + "\n" +
                                            "等级=" + l + "\n" +
                                            "签名有效");
                                    }
                                    catch (SignatureInvalidException err)
                                    {
                                        MainHolder.broadcaster.SendToGroup(e.Sender.Group.Id, "该船票签名无效");
                                    }
                                    catch { }
                                    */
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[群消息接收处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
            return;
        }
    }
}
