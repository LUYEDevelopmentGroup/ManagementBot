using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Mirai_CSharp.Plugin.Interfaces;
using BiliApi.BiliPrivMessage;
using static tech.msgp.groupmanager.Code.DataBase;

namespace tech.msgp.groupmanager.Code.EventHandlers
{
    public class GroupMemberIncrease : IGroupMemberJoined
    {
        public async Task<bool> GroupMemberJoined(MiraiHttpSession session, IGroupMemberJoinedEventArgs e)
        {
            string usname = e.Member.Name;
            long qq = e.Member.Id;
            long groupId = e.Member.Group.Id;
            string gname = DataBase.me.getGroupName(groupId);
            gname =
                gname == "UNDEFINED_IN_DATABASE"
                ? e.Member.Group.Name : gname;
            try
            {
                if (DataBase.me.isUserBlacklisted(qq))
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("入群的用户 " + usname + "(" + qq + ") 存在于黑名单中，请三思！");
                }
                if (DataBase.me.whichGroupsAreTheUserIn(qq).Count > 1)
                {
                    List<long> groups_in = DataBase.me.whichGroupsAreTheUserIn(qq);
                    if (groups_in.Count > 1)
                    {
                        string gps = "";
                        foreach (long group in groups_in)
                        {
                            gps += DataBase.me.getGroupName(group) + "(" + group + ")\n";
                        }
                        MainHolder.broadcaster.BroadcastToAdminGroup(usname + "(" + qq + ") 加入群  " +
                            gname + "(" + groupId + ") \n该用户同时加入以下群聊：\n" + gps);
                    }
                }
                if (DataBase.me.isCrewGroup(e.Member.Group.Id))
                {
                    long uid = DataBase.me.getUserBoundedUID(qq);

                    if (uid <= 0)
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup(usname + "(" + qq + ")加入舰长群  " +
                        gname + "(" + groupId + ") \n[未能验证舰长记录 该用户未绑定UID]");
                        MainHolder.broadcaster.SendToGroup(groupId, "欢迎加入舰长群！\n[未绑定B站账号]");
                        //e.BeingOperateQQ.SendPrivateMessage("欢迎来到舰长群，感谢您对鹿野的支持！\n当前QQ未和Bilibili绑定，可发送#uid [uid]来绑定B站账号。例如：\n#uid 23696210\n不会操作也可以联系鸡蛋🥚");
                    }
                    else
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup(usname + "(" + qq + ")<舰长> 加入群  " +
                            gname + "(" + groupId + ") \nB站信息:https://space.bilibili.com/" + uid + "\n");
                        PrivMessageSession psession = PrivMessageSession.openSessionWith((int)uid, MainHolder.biliapi);
                        BiliApi.BiliUser bu = BiliApi.BiliUser.getUser((int)uid, MainHolder.biliapi);
                        CrewChecker cr = new CrewChecker();
                        cr.getAllCrewMembers();
                        Dictionary<int, CrewMember> crewlist = cr.getCurrentCrewMembers();
                        CrewMember thismember = crewlist[(int)uid];
                        string dpword = "";
                        switch (thismember.level)
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
                        MainHolder.broadcaster.SendToGroup(groupId, "欢迎" + dpword + "<" + bu.name + ">加入舰长群！");
                        IGroupMemberCardInfo iginfo = new GroupMemberCardInfo(dpword + " " + bu.name, null);
                        await MainHolder.session.ChangeGroupMemberInfoAsync(qq, groupId, iginfo);
                        MainHolder.broadcaster.SendToQQ(qq, "欢迎来到舰长群，感谢您对奶狗狗的支持！\n您的QQ号已和Bilibili账号<" + bu.name + ">绑定，如有疑问请联系鸡蛋🥚(这套系统的开发者，QQ号1250542735)");
                        psession.sendMessage("您已经成功加入了舰长群。感谢您对大总攻(XNG)的支持！");
                    }
                }
                else
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup(usname + "加入了" + gname + "\n已建立用户信息");
                }
                DataBase.me.addUser(qq, groupId, usname);
            }
            catch (Exception err)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[已入群的处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
            return true;
        }
    }
}