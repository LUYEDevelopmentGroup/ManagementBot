using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mirai_CSharp.Plugin.Interfaces;
using tech.msgp.groupmanager.Code.MCServer;
using static tech.msgp.groupmanager.Code.DataBase;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace tech.msgp.groupmanager.Code
{
    public class PrivMessageHan : ITempMessage, IFriendMessage
    {
        static Dictionary<long, List<long>> me_user_tmp = new Dictionary<long, List<long>>();

        static public bool CheckEncode(string srcString)
        {
            return Encoding.UTF8.GetBytes(srcString).Length > srcString.Length;
        }
        public void PrivateMessage(MsgReply rep, string msgtext, string[] pictures)
        {
            try
            {
                if (msgtext.IndexOf("#") != 0)
                {
                    try
                    {
                        var ticket = BroadTicketUtility.TicketCoder.Decode(msgtext);
                        long qq = DataBase.me.getUserBoundedQQ(ticket.Data.Uid);
                        if (qq <= 0)
                        {
                            DataBase.me.boundBiliWithQQ(ticket.Data.Uid, rep.qq);
                            rep.reply("已将您的QQ与Bilibili账号绑定。");
                            qq = rep.qq;
                        }
                        if (qq != rep.qq)
                        {
                            rep.reply("我无法为您兑换船票，因为您Bilibili账号绑定的QQ与当前QQ不符。\n绑定的QQ：******" + qq.ToString()[6..] + "\n如果您确信这是一个误会，请联系运维@鸡蛋(1250542735)");
                            MainHolder.broadcaster.BroadcastToAdminGroup("船票兑换失败\nQQ<" + rep.qq + ">试图兑换船票{Serial=" + ticket.Data.SerialNumber + "}\n该船票颁发给BiliUID#" + ticket.Data.Uid + "@QQ<" + qq + ">\n拒绝兑换。");
                            return;
                        }
                        try
                        {
                            rep.reply(BroadTicketUtility.TicketCoder.Encode(ticket, false));
                            MainHolder.broadcaster.BroadcastToAdminGroup("船票兑换成功\nQQ<" + rep.qq + ">试图兑换船票{Serial=" + ticket.Data.SerialNumber + "}\n该船票颁发给BiliUID#" + ticket.Data.Uid + "\n已兑换。");
                            rep.reply("兑换成功，感谢您对鹿野的支持！");
                        }
                        catch (Exception err)
                        {
                            MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n兑换船票时发生了一个错误：\n" + err.Message + "\nStack:\n" + err.StackTrace);
                            rep.reply("出现了一个错误，无法为您生成船票。请复制本消息并联系运维@鸡蛋(1250542735)\n" + err.Message + "\nStack:\n" + err.StackTrace);
                        }
                        return;
                    }
                    catch { }
                    MainHolder.broadcaster.SendToAnEgg(msgtext);
                }
                else
                {//是一条指令
                    try
                    {
                        string[] cmd = msgtext.Split(' ');
                        switch (cmd[0])
                        {

                            case "#reg":
                            case "#注册":
                                {
                                    if (cmd.Length < 3)
                                    {
                                        rep.reply("用法：\n" + cmd[0] + " 角色名 密码");
                                        break;
                                    }
                                    if (DBHandler.me.isRegistered(rep.qq))
                                    {
                                        rep.reply("您的QQ已经被绑定到一个MC账号，我们不允许重复注册。如果需要销号，请联系管理员。");
                                        break;
                                    }
                                    else if (DBHandler.me.isNameTaken(cmd[1]))
                                    {
                                        rep.reply("这个用户名已经有人用了，换一个试试吧！");
                                        break;
                                    }
                                    else if (cmd[2].Length < 5)
                                    {
                                        rep.reply("密码长度必须大于等于5位");
                                        break;
                                    }
                                    else if (CheckEncode(cmd[1]))
                                    {
                                        rep.reply("用户名仅允许(A-Z,a-z,0-9,_)，不允许特殊符号和中文。");
                                        break;
                                    }
                                    bool succeed = true;
                                    bool mojang = false;
                                    succeed = succeed && MCServer.DBHandler.me.addUser(rep.qq, cmd[2]);
                                    succeed = succeed && MCServer.DBHandler.me.addProfile(cmd[1], rep.qq, out mojang);
                                    if (succeed)
                                    {
                                        {
                                            string uuid = DBHandler.me.getUserOwnedProfileUUID(rep.qq);
                                            string pname = DBHandler.me.getUserOwnedProfileName(rep.qq);
                                            List<Texture> l = new List<Texture>();
                                            Dictionary<string, string> ddd = new Dictionary<string, string>();
                                            ddd.Add("model", "slim");
                                            Texture tt = new Texture("https://storage.microstorm.tech/skins/default.png", "SKIN", ddd);
                                            l.Add(tt);
                                            SkinHandler sh = new SkinHandler(DBHandler.me.getUserOwnedProfileUUID(rep.qq), DBHandler.me.getUserOwnedProfileName(rep.qq), l);
                                            if (DBHandler.me.setProfileUUIDTexture(uuid, sh.ToString()))
                                            {
                                                rep.reply("注册成功。使用以下信息登录：\n" +
                                         "用户名：" + rep.qq + "@qq.com\n" +
                                         "密码：" + cmd[2] + (mojang ? "\n[正版UUID]" : ""));
                                                MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n玩家<" + cmd[1] + ">在认证服务器中注册" + (mojang ? "\n[正版UUID]" : ""));
                                            }
                                            else
                                            {
                                                rep.reply("注册成功，[但设置默认皮肤时发生了一些问题]。\n联系管理解决后,使用以下信息登录：\n" +
                                         "用户名：" + rep.qq + "@qq.com\n" +
                                         "密码：" + cmd[2] + (mojang ? "\n[正版UUID]" : ""));
                                                MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n玩家<" + cmd[1] + ">在认证服务器中注册，但默认皮肤未能写入。【须进一步操作】" + (mojang ? "\n[正版UUID]" : ""));
                                            }
                                        }

                                    }
                                    else
                                    {
                                        rep.reply("无法与中央数据库通讯。请稍后重试。\n如果该问题持续出现，请联系@鸡蛋<1250542735>");
                                        MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n玩家<" + cmd[1] + ">在认证服务器中注册时发生错误。数据库连接不稳定。");
                                    }
                                }
                                break;
                            case "#skin":
                            case "#皮肤":
                                {
                                    if (cmd.Length < 2)
                                    {
                                        rep.reply("用法：\n" + cmd[0] + " https://xxxxx.xxxxx.xxxxx/xxx.png");
                                        break;
                                    }
                                    if (!MCServer.SkinHandler.CheckSkinSourceTrusted(cmd[1]))
                                    {
                                        rep.reply("使用的图像URL无效。URL必须格式正确且来自可信的图像服务器。");
                                        return;
                                    }
                                    if (!MCServer.SkinHandler.checkPick(cmd[1]))
                                    {
                                        rep.reply("图片格式有误。必须是有效PNG图片，且为64*32或64*64比例。");
                                        return;
                                    }
                                    string uuid = DBHandler.me.getUserOwnedProfileUUID(rep.qq);
                                    string pname = DBHandler.me.getUserOwnedProfileName(rep.qq);
                                    string oldtdata = DBHandler.me.getProfileUUIDTexture(uuid);
                                    List<Texture> l = new List<Texture>();
                                    if (oldtdata != null)
                                    {
                                        JObject oldtexture = (JObject)JsonConvert.DeserializeObject(oldtdata);
                                        if (oldtexture["textures"]["CAPE"] != null)
                                        {
                                            Dictionary<string, string> ddd = new Dictionary<string, string>();
                                            Texture tt = new Texture(oldtexture["textures"]["CAPE"].Value<string>("url"), "CAPE", ddd);
                                            l.Add(tt);
                                        }
                                    }
                                    Dictionary<string, string> d = new Dictionary<string, string>();
                                    d.Add("model", "slim");
                                    Texture t = new Texture(cmd[1], "SKIN", d);
                                    l.Add(t);
                                    SkinHandler sh = new SkinHandler(DBHandler.me.getUserOwnedProfileUUID(rep.qq), DBHandler.me.getUserOwnedProfileName(rep.qq), l);
                                    if (DBHandler.me.setProfileUUIDTexture(uuid, sh.ToString()))
                                    {
                                        rep.reply("操作成功，您的皮肤已设置。");
                                    }
                                    else
                                    {
                                        rep.reply("操作失败");
                                    }
                                }
                                break;
                            case "#UUID_RESET":
                            case "#离线化UUID":
                                {
                                    string __uuid = DBHandler.me.getUserOwnedProfileUUID(rep.qq);
                                    string __pname = DBHandler.me.getUserOwnedProfileName(rep.qq);
                                    bool mmojang;
                                    string nsu = DBHandler.genNoSlashUUID(__pname, out mmojang);
                                    string su = DBHandler.genSlashUUID(__pname);
                                    if (DBHandler.me.changeProfileUUID(__uuid, nsu, su) && mmojang)
                                    {
                                        rep.reply("您(" + __pname + ")的UUID已更新：\n旧UUID：" + __uuid + "\n新UUID：" + nsu);
                                        MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n" + __pname + "发起了UUID更新\n旧UUID：" + __uuid + "\n新UUID：" + nsu + "\n√ 通过");
                                    }
                                    else
                                    {
                                        rep.reply("您(" + __pname + ")的UUID无法更新。数据库出错或非Mojang账号\n旧UUID：" + __uuid);
                                        MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n" + __pname + "发起了UUID更新\n旧UUID：" + __uuid + "\n新UUID：" + nsu + "\n× 失败");

                                    }
                                }
                                break;
                            case "#cloneuuid":
                                {
                                    string __uuid;
                                    if (cmd.Length == 3)
                                    {
                                        __uuid = DBHandler.me.getUserOwnedProfileUUID(long.Parse(cmd[2]));
                                    }
                                    else
                                    {
                                        __uuid = DBHandler.me.getUserOwnedProfileUUID(rep.qq);
                                    }
                                    string __pname = cmd[1];
                                    bool mmojang;
                                    string nsu = DBHandler.genNoSlashUUID(__pname, out mmojang);
                                    string su = DBHandler.genSlashUUID(__pname);
                                    if (!mmojang)
                                    {
                                        rep.reply("无法获取" + __pname + "的UUID，因此无法克隆。");
                                    }
                                    else
                                    {
                                        rep.reply("您申请克隆" + __pname + "的UUID，请等待管理员操作。");
                                        MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n" + __pname + "发起了UUID更新\n旧UUID：" + __uuid + "\n新UUID：" + nsu + "\n#cloneuuid " + __pname + " " + rep.qq + "");
                                    }
                                }
                                break;
                            case "#cape":
                            case "#披风":
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception err)
                    {
                        rep.reply("执行指令出错。请检查指令及其参数是否正确。如果确认操作无误，请将下面的信息转发给@鸡蛋");
                        rep.reply("未处理的异常：" + err.Message + "\n堆栈追踪：" + err.StackTrace);
                    }

                    if (DataBase.me.isUserOperator(rep.qq))//管理才能用的指令
                    {
                        try
                        {
                            string[] cmd = msgtext.Split(' ');
                            switch (cmd[0])
                            {
                                case "#dreg":
                                    {
                                        if (cmd.Length < 3)
                                        {
                                            rep.reply("用法：\n" + cmd[0] + " QQ 角色名 密码");
                                            break;
                                        }

                                        long qq = long.Parse(cmd[1]);
                                        string name = cmd[2];
                                        string passwd = cmd[3];
                                        if (DBHandler.me.isRegistered(qq))
                                        {
                                            rep.reply("QQ已经被绑定到一个MC账号，我们不允许重复注册。如果需要销号，请联系管理员。");
                                            break;
                                        }
                                        else if (DBHandler.me.isNameTaken(name))
                                        {
                                            rep.reply("这个用户名已经有人用了，换一个试试吧！");
                                            break;
                                        }
                                        else if (passwd.Length < 5)
                                        {
                                            rep.reply("密码长度必须大于等于5位");
                                            break;
                                        }
                                        else if (CheckEncode(name))
                                        {
                                            rep.reply("用户名仅允许(A-Z,a-z,0-9,_)，不允许特殊符号和中文。");
                                            break;
                                        }
                                        bool succeed = true;
                                        bool mojang = false;
                                        succeed = succeed && MCServer.DBHandler.me.addUser(qq, passwd);
                                        succeed = succeed && MCServer.DBHandler.me.addProfile(name, qq, out mojang);
                                        if (succeed)
                                        {
                                            {
                                                string uuid = DBHandler.me.getUserOwnedProfileUUID(qq);
                                                string pname = DBHandler.me.getUserOwnedProfileName(qq);
                                                List<Texture> l = new List<Texture>();
                                                Dictionary<string, string> ddd = new Dictionary<string, string>();
                                                ddd.Add("model", "slim");
                                                Texture tt = new Texture("https://storage.microstorm.tech/skins/default.png", "SKIN", ddd);
                                                l.Add(tt);
                                                SkinHandler sh = new SkinHandler(DBHandler.me.getUserOwnedProfileUUID(qq), DBHandler.me.getUserOwnedProfileName(qq), l);
                                                if (DBHandler.me.setProfileUUIDTexture(uuid, sh.ToString()))
                                                {
                                                    rep.reply("注册成功。使用以下信息登录：\n" +
                                             "用户名：" + qq + "@qq.com\n" +
                                             "密码：" + passwd + (mojang ? "\n[正版UUID]" : ""));
                                                    MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n玩家<" + name + ">在认证服务器中注册" + (mojang ? "\n[正版UUID]" : ""));
                                                }
                                                else
                                                {
                                                    rep.reply("注册成功，[但设置默认皮肤时发生了一些问题]。\n联系管理解决后,使用以下信息登录：\n" +
                                             "用户名：" + qq + "@qq.com\n" +
                                             "密码：" + passwd + (mojang ? "\n[正版UUID]" : ""));
                                                    MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n玩家<" + name + ">在认证服务器中注册，但默认皮肤未能写入。【须进一步操作】" + (mojang ? "\n[正版UUID]" : ""));
                                                }
                                            }

                                        }
                                        else
                                        {
                                            rep.reply("无法与中央数据库通讯。请稍后重试。\n如果该问题持续出现，请联系@鸡蛋<1250542735>");
                                            MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n玩家<" + name + ">在认证服务器中注册时发生错误。数据库连接不稳定。");
                                        }
                                    }
                                    break;
                                case "#cloneuuid":
                                    {
                                        string __uuid;
                                        if (cmd.Length == 3)
                                        {
                                            __uuid = DBHandler.me.getUserOwnedProfileUUID(long.Parse(cmd[2]));
                                        }
                                        else
                                        {
                                            __uuid = DBHandler.me.getUserOwnedProfileUUID(rep.qq);
                                        }
                                        string __pname = cmd[1];
                                        bool mmojang;
                                        string nsu = DBHandler.genNoSlashUUID(__pname, out mmojang);
                                        string su = DBHandler.genSlashUUID(__pname);
                                        if (DBHandler.me.changeProfileUUID(__uuid, nsu, su) && mmojang)
                                        {
                                            rep.reply("您(" + __pname + ")的UUID已更新：\n旧UUID：" + __uuid + "\n新UUID：" + nsu);
                                            MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n" + __pname + "发起了UUID更新\n旧UUID：" + __uuid + "\n新UUID：" + nsu + "\n√ 通过");
                                        }
                                        else
                                        {
                                            rep.reply("您(" + __pname + ")的UUID无法更新。数据库出错或非Mojang账号\n旧UUID：" + __uuid);
                                            MainHolder.broadcaster.BroadcastToAdminGroup("[MC AUTH SERVER]\n" + __pname + "发起了UUID更新\n旧UUID：" + __uuid + "\n新UUID：" + nsu + "\n× 失败");
                                        }
                                    }
                                    break;
                                case "#查重":
                                    {
                                        rep.reply("====查重开始====\n" +
                                            "该操作需要大量数据库操作，请耐心等待\n" +
                                            "！如果发现结果不正确，请在对应群发送\n" +
                                            "#REFETCH_MEMBERS\n" +
                                            "！来重新同步数据");
                                        Dictionary<long, List<long>> li;

                                        if (!(cmd.Length > 1 && ((cmd[1] == "从缓存") || (cmd[1] == "cache"))))
                                            li = DataBase.me.findMeUser();
                                        else
                                            li = me_user_tmp;

                                        foreach (KeyValuePair<long, List<long>> groups in li)
                                        {
                                            if (DataBase.me.isUserOperator(groups.Key)) continue;
                                            string gps = "";
                                            foreach (long group in groups.Value)
                                            {
                                                gps += DataBase.me.getGroupName(group) + "(" + group + ")\n";
                                            }
                                            if (!(cmd.Length > 1 && ((cmd[1] == "计数") || (cmd[1] == "count"))))
                                                rep.reply(new UserData(groups.Key).name + "(" + groups.Key + ")\n" + "该用户同时在这些群：\n" + gps);
                                        }
                                        me_user_tmp = li;
                                        rep.reply("共发现" + li.Count + "个重复加群用户\n" +
                                            "====查重结束====\n");
                                    }
                                    break;
                                case "#更新群":
                                case "#refetch":
                                    {
                                        rep.reply("！该操作涉及大量数据库操作，需要一定时间");
                                        DataBase.me.update_groupmembers(long.Parse(cmd[1]));
                                        rep.reply("已更新群成员数据");
                                    }
                                    break;
                                case "#初始化群":
                                case "#init_group":
                                    {
                                        rep.reply("！该操作涉及大量数据库操作，需要一定时间");
                                        DataBase.me.init_groupdata(long.Parse(cmd[1]));
                                        rep.reply("已完成群初始化");
                                    }
                                    break;
                                case "#手动拉黑":
                                case "#ban":
                                    {
                                        if (DataBase.me.addUserBlklist(long.Parse(cmd[1]), (
                                            (cmd.Length > 2 ? (cmd[2]) : ("管理员手动拉黑"))
                                            ), rep.qq))
                                        {
                                            string name = "";
                                            try
                                            {
                                                name = "<" + new UserData(long.Parse(cmd[1])).name + ">";
                                            }
                                            catch
                                            {
                                                name = "* <UNKNOWN>";
                                            }
                                            rep.reply("已将" + name + "加入黑名单");
                                        }
                                        else
                                        {
                                            rep.reply("无法执行该操作，中央数据库可能暂时不可用");
                                        }
                                    }
                                    break;
                                case "#清缓存":
                                case "#clearcache":
                                    {
                                        me_user_tmp.Clear();
                                        MainHolder.clearcache();
                                        DataBase.me.clearCache();
                                        rep.reply("√ 缓存已清空");
                                    }
                                    break;
                                case "#验证":
                                case "#check":
                                    int uid, len, level, tstamp;
                                    if (CrewKeyProcessor.checkToken(cmd[1], out uid, out len, out level, out tstamp))
                                    {
                                        string dpword = "??未知??";
                                        switch (level)
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
                                        rep.reply("签名有效√\n" +
                                            "颁发给UID:" + uid + "\n" +
                                            "用以证明购买 " + dpword + "*" + len + " 月\n" +
                                            "颁发时间:" + BiliApi.TimestampHandler.GetDateTime(tstamp) + "\n" +
                                            "\n" +
                                            "⚠该凭据仅表明以上购买信息真实有效，不做其它用途");
                                    }
                                    else
                                    {
                                        rep.reply("该凭据签名无效×\n" +
                                            "⚠该凭据无法被正确解码或签名失效。这可能意味着它包含的信息遭到篡改。不要信任该凭据指示的任何信息。");
                                    }
                                    break;
                                case "#cape":
                                case "#披风":
                                    {
                                        long qq = -1;
                                        if (cmd.Length < 3)
                                        {
                                            if (!long.TryParse(cmd[1], out qq))
                                            {
                                                rep.reply("！正在操作您自己的账号");
                                                qq = rep.qq;
                                            }
                                            else
                                            if (cmd.Length < 2)
                                            {
                                                rep.reply("用法：\n" + cmd[0] + " (玩家QQ) 【图片】");
                                                break;
                                            }
                                        }
                                        if (qq < 0) qq = long.Parse(cmd[1]);
                                        /*
                                        string fName = e.Message.ReceiveImage(0);
                                        fName = e.CQApi.ReceiveImage(fName);
                                        if (!MCServer.SkinHandler.checkPick(fName))
                                        {
                                            e.FromQQ.SendPrivateMessage("图片格式有误。必须是有效PNG图片，且为64*32或64*64比例。");
                                            break;
                                        }
                                        string hash = MCServer.SkinHandler.getPictureHash(fName);
                                        string savepath = "Y:/skin/CAPE/" + hash + ".png";
                                        e.CQLog.Info("MC皮肤", "图片存储到" + "Y:/skin/CAPE" + hash + ".png");
                                        if (!Directory.Exists("Y:/skin/CAPE"))
                                        {
                                            e.CQLog.Warning("MC皮肤", "转储目录无效");
                                        }
                                        if (!File.Exists(savepath))
                                        {
                                            e.CQLog.Warning("MC皮肤", "文件已存在");
                                        }
                                        SkinHandler.turnPicture(fName, savepath);
                                        string uuid = DBHandler.me.getUserOwnedProfileUUID(qq);
                                        string pname = DBHandler.me.getUserOwnedProfileName(qq);
                                        string oldtdata = DBHandler.me.getProfileUUIDTexture(uuid);
                                        List<Texture> l = new List<Texture>();
                                        if (oldtdata != null)
                                        {
                                            JObject oldtexture = (JObject)JsonConvert.DeserializeObject(oldtdata);
                                            if (oldtexture["textures"]["SKIN"] != null)
                                            {
                                                Dictionary<string, string> ddd = new Dictionary<string, string>();
                                                ddd.Add("model", "slim");
                                                Texture tt = new Texture(oldtexture["textures"]["SKIN"].Value<string>("url"), "SKIN", ddd);
                                                l.Add(tt);
                                            }
                                        }
                                        Dictionary<string, string> d = new Dictionary<string, string>();
                                        Texture t = new Texture("http://textures.mc.microstorm.tech:15551/CAPE/" + hash, "CAPE", d);
                                        l.Add(t);
                                        SkinHandler sh = new SkinHandler(DBHandler.me.getUserOwnedProfileUUID(rep.qq), DBHandler.me.getUserOwnedProfileName(rep.qq), l);
                                        if (DBHandler.me.setProfileUUIDTexture(uuid, sh.ToString()))
                                        {
                                            e.FromQQ.SendPrivateMessage("操作成功，已为玩家" + pname + "设置披风。\n" + "http://textures.mc.microstorm.tech:15551/CAPE/" + hash);
                                        }
                                        else
                                        {
                                            e.FromQQ.SendPrivateMessage("操作失败");
                                        }
                                        */
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception err)
                        {
                            rep.reply("执行指令出错。请检查指令及其参数是否正确。如果确认操作无误，请将下面的信息转发给@鸡蛋");
                            rep.reply("未处理的异常：" + err.Message + "\n堆栈追踪：" + err.StackTrace);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[私聊信息处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
        }

        public static void usersetCapeSelf(string[] cmd, MsgReply rep)
        {
            if (cmd.Length < 2)
            {
                rep.reply("用法：\n" + cmd[0] + " https://xxxx.xxxx.xxxx/xxxx.png");
                return;
            }
            if (!MCServer.SkinHandler.checkPick(cmd[0]))
            {
                rep.reply("图片格式有误。必须是有效PNG图片，且为64*32或64*64比例。");
                return;
            }
            string uuid = DBHandler.me.getUserOwnedProfileUUID(rep.qq);
            string pname = DBHandler.me.getUserOwnedProfileName(rep.qq);
            string oldtdata = DBHandler.me.getProfileUUIDTexture(uuid);
            List<Texture> l = new List<Texture>();
            if (oldtdata != null)
            {
                JObject oldtexture = (JObject)JsonConvert.DeserializeObject(oldtdata);
                if (oldtexture["textures"]["SKIN"] != null)
                {
                    Dictionary<string, string> ddd = new Dictionary<string, string>();
                    Texture tt = new Texture(oldtexture["textures"]["SKIN"].Value<string>("url"), "SKIN", ddd);
                    l.Add(tt);
                }
            }
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("model", "slim");
            Texture t = new Texture(cmd[1], "SKIN", d);
            l.Add(t);
            SkinHandler sh = new SkinHandler(DBHandler.me.getUserOwnedProfileUUID(rep.qq), DBHandler.me.getUserOwnedProfileName(rep.qq), l);
            if (DBHandler.me.setProfileUUIDTexture(uuid, sh.ToString()))
            {
                rep.reply("操作成功，您的皮肤已设置。");
            }
            else
            {
                rep.reply("操作失败");
            }
        }
        public static string genIntake(long qq, int timestamp)
        {
            return "LuYe#BuShiXiaoNaiGou!" + timestamp + "LuYeShi#GeDaMengYi?" + qq;
        }

        /// <summary>  
        /// SHA1 加密，返回大写字符串  
        /// </summary>  
        /// <param name="content">需要加密字符串</param>  
        /// <returns>返回40位UTF8 大写</returns>  

        public static string Sha1(string data)
        {
            byte[] temp1 = Encoding.UTF8.GetBytes(data);
            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            byte[] temp2 = sha.ComputeHash(temp1);
            sha.Clear();
            // 注意， 不能用这个
            // string output = Convert.ToBase64String(temp2);// 不能直接转换成base64string
            var output = BitConverter.ToString(temp2);
            output = output.Replace("-", "");
            output = output.ToLower();
            return output;
        }

        public static int DateTimeToUnixTime(DateTime dateTime)
        {
            return (int)(dateTime - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public async Task<bool> TempMessage(MiraiHttpSession session, ITempMessageEventArgs e)
        {
            MsgReplyTemp r = new MsgReplyTemp() { fromGroup = e.Sender.Group.Id, qq = e.Sender.Id };
            ProcessMessage(r, e.Chain);
            return true;
        }

        public async Task<bool> FriendMessage(MiraiHttpSession session, IFriendMessageEventArgs e)
        {
            MsgReplyFriend r = new MsgReplyFriend() { qq = e.Sender.Id };
            ProcessMessage(r, e.Chain);
            return true;
        }

        public void ProcessMessage(MsgReply rply, IMessageBase[] chain)
        {
            StringBuilder str = new StringBuilder();
            List<string> pic = new List<string>();
            foreach (IMessageBase msg in chain)
            {
                switch (msg.Type)
                {
                    case PlainMessage.MsgType:
                        PlainMessage pmsg = (PlainMessage)msg;
                        str.Append(pmsg.Message);
                        break;
                    case ImageMessage.MsgType:
                        ImageMessage imsg = (ImageMessage)msg;
                        pic.Add(imsg.Url);
                        break;
                    default:
                        break;
                }
            }
            PrivateMessage(rply, str.ToString(), pic.ToArray());
        }

        #region 消息回复处理器
        public abstract class MsgReply
        {
            public long qq;
            public abstract void reply(string msg);
            public abstract void reply(IMessageBase[] chain);
            public abstract void reply(Bitmap image);
        }
        public class MsgReplyTemp : MsgReply
        {
            public long fromGroup;
            public override void reply(string msg)
            {
                MainHolder.broadcaster.SendToQQ(qq, msg, fromGroup);
            }
            public override void reply(IMessageBase[] chain)
            {
                MainHolder.broadcaster.SendToQQ(qq, chain, fromGroup);
            }

            public override void reply(Bitmap image)
            {
                MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                var pmsg = MainHolder.session.UploadPictureAsync(UploadTarget.Temp, ms).Result;
                MainHolder.broadcaster.SendToQQ(qq, new IMessageBase[] { pmsg }, fromGroup);
            }
        }
        public class MsgReplyFriend : MsgReply
        {
            public override void reply(string msg)
            {
                MainHolder.broadcaster.SendToQQ(qq, msg);
            }

            public override void reply(IMessageBase[] chain)
            {
                MainHolder.broadcaster.SendToQQ(qq, chain);
            }

            public override void reply(Bitmap image)
            {
                MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                var pmsg = MainHolder.session.UploadPictureAsync(UploadTarget.Temp, ms).Result;
                MainHolder.broadcaster.SendToQQ(qq, new IMessageBase[] { pmsg });
            }
        }
        #endregion
    }
}
