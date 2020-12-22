using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tech.msgp.groupmanager.Code.BiliAPI.BiliPrivMessage;
using static tech.msgp.groupmanager.Code.DataBase;
using CQ2IOT.Events;

namespace tech.msgp.groupmanager.Code
{
    public class Event_GroupMemberIncrease
    {
        public void GroupMemberIncrease(object sender, GroupMemberIncreaseEventArgs e)
        {
            try
            {
                //e.BeingOperateQQ.SendPrivateMessage("欢迎入群！能询问您一下您的B站UID吗？这可以帮助我们为您提供更多自动化服务\n回复会由系统自动统计，请回复您的数字UID，按照下面的格式\n例如：");
                //e.BeingOperateQQ.SendPrivateMessage("UID:1234567");
                if (DataBase.me.isUserBlacklisted(e.user.qq))
                {
                    MainHolder.broadcaster.broadcastToAdminGroup("入群的用户 " + e.user.name + "(" + e.user.qq + ") 存在于黑名单中，请三思！");
                }
                if (DataBase.me.whichGroupsAreTheUserIn(e.user.qq).Count > 1)
                {
                    List<long> groups_in = DataBase.me.whichGroupsAreTheUserIn(e.user.qq);
                    if (groups_in.Count > 1)
                    {
                        string gps = "";
                        foreach (long group in groups_in)
                        {
                            gps += DataBase.me.getGroupName(group) + "(" + group + ")\n";
                        }
                        MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "(" + e.user.qq + ") 加入群  " +
                            DataBase.me.getGroupName(e.throughgroup.id) + "(" + e.throughgroup.id + ") \n该用户同时加入以下群聊：\n" + gps);
                    }
                }
                if (DataBase.me.isCrewGroup(e.throughgroup.id))
                {
                    long uid = DataBase.me.getUserBoundedUID(e.user.qq);
                    string usname = e.user.name;
                    if (uid <= 0)
                    {
                        MainHolder.broadcaster.broadcastToAdminGroup(usname + "(" + e.user.qq + ")加入舰长群  " +
                        DataBase.me.getGroupName(e.throughgroup.id) + "(" + e.throughgroup.id + ") \n[未能验证舰长记录 该用户未绑定UID]");
                        MainHolder.broadcaster.sendToGroup(e.throughgroup.id, "欢迎加入舰长群！\n[未能获取对应B站信息]");
                        //e.BeingOperateQQ.SendPrivateMessage("欢迎来到舰长群，感谢您对鹿野的支持！\n当前QQ未和Bilibili绑定，可发送#uid [uid]来绑定B站账号。例如：\n#uid 23696210\n不会操作也可以联系鸡蛋🥚");
                    }
                    else
                    {
                        MainHolder.broadcaster.broadcastToAdminGroup(usname + "(" + e.user.qq + ")<舰长> 加入群  " +
                            DataBase.me.getGroupName(e.throughgroup.id) + "(" + e.throughgroup.id + ") \nB站信息:https://space.bilibili.com/" + uid + "\n");
                        PrivMessageSession session = PrivMessageSession.openSessionWith((int)uid);
                        BiliAPI.BiliUser bu = BiliAPI.BiliUser.getUser((int)uid);
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
                        MainHolder.broadcaster.sendToGroup(e.throughgroup.id, "欢迎" + dpword + "<" + bu.name + ">加入舰长群！");
                        MainHolder.api.changeMemberNameCard(e.throughgroup.id, e.user.qq, dpword + " " + bu.name);
                        MainHolder.broadcaster.sendToQQ(e.user.qq, "欢迎来到舰长群，感谢您对鹿野的支持！\n您的QQ号已和Bilibili账号<" + bu.name + ">绑定，如有疑问请联系鸡蛋🥚");
                        session.sendMessage("您已经成功加入了舰长群。感谢您对大总攻(XNG)的支持！");
                    }
                }
                else
                {
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "加入了" + e.throughgroup.name + "\n已建立用户信息");
                    MainHolder.broadcaster.sendToGroup(e.throughgroup.id,
                        "[ATUSER(" + e.user.qq + ")]\n" +
                        "欢迎新人！为了群聊长治久安，\n" +
                        "请注意避免下列行为：\n" +
                        "·谈论鹿野三次信息\n" +
                        "·在群内分享或谈论不宜谈论的内\n" +
                        "容(政黄赌暴，含性暗示、普遍审美\n" +
                        "难以接受的内容)\n" +
                        "·任何形式的自我宣传或为他人宣传\n" +
                        "(包括但不限于视频转发)\n" +
                        ">>请查看公告 阅读完整群规<<\n"
                        );
                }
                DataBase.me.addUser(e.user.qq, e.throughgroup.id, e.user.name);

            }
            catch (Exception err)
            {
                MainHolder.broadcaster.broadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[已入群的处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
        }
    }
}