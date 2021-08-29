using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Mirai_CSharp.Plugin.Interfaces;

namespace tech.msgp.groupmanager.Code.EventHandlers
{
    public class GroupEnterRequest : IGroupApply
    {
        public async Task<bool> GroupApply(MiraiHttpSession session, IGroupApplyEventArgs e)
        {
            if (DataBase.me.isUserBlacklisted(e.FromQQ))
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("入群的用户 " + e.NickName + "(" + e.FromQQ + ") 存在于黑名单中，自动拒绝。");
                await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "您被设置不能加入任何粉丝群。");
                return true;
            }
            switch (DataBase.me.isUserTrusted(e.FromQQ))
            {
                case 1:
                    DataBase.me.removeUserTrustlist(e.FromQQ);
                    MainHolder.broadcaster.BroadcastToAdminGroup("入群的用户 " + e.NickName + "(" + e.FromQQ + ") 受到单次信任，同意入群。\n该次信任已被移除。");
                    goto case 9;//显式允许直接进入下一个case
                case 0:
                    MainHolder.broadcaster.BroadcastToAdminGroup("入群的用户 " + e.NickName + "(" + e.FromQQ + ") 受到永久信任，同意入群。");
                    goto case 9;//显式允许直接进入下一个case
                    break;
                case 9:
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Allow);
                    return true;
            }
            if (DataBase.me.isCrewGroup(e.FromGroup))
            {//是舰长群
                CrewChecker cr = new CrewChecker();
                if (DataBase.me.isUserBoundedUID(e.FromQQ))//舰长绑定
                {
                    var uid = DataBase.me.getUserBoundedUID(e.FromQQ);
                    if (DataBase.me.isBiliUserGuard(uid))
                    {
                        await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Allow);
                        MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n！正在加入舰长群\n是舰长，同意");
                    }
                    else
                    {
                        await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "没有您的大航海数据，如有疑问请联系管理。");
                        MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n！正在加入舰长群\n不是舰长，拒绝");
                    }
                }
                else
                {
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "您的QQ没有绑定任何UID，如有疑问请联系管理。");
                    MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n！正在加入舰长群\n未知QQ，拒绝");
                }
            }
            else
            {
                
                var groups = DataBase.me.whichGroupsAreTheUserIn(e.FromQQ);
                if (groups.Count > 1)
                {
                    string gps = "";
                    foreach (long group in groups)
                    {
                        gps += DataBase.me.getGroupName(group) + "(" + group + ")\n";
                    }
                    MainHolder.broadcaster.BroadcastToAdminGroup(e.NickName + "(" + e.FromQQ + ") 加入群  " +
                        e.FromGroupName + "(" + e.FromGroup + ") \n，自动拒绝。\n该用户同时加入以下群聊：\n" + gps);
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "已加入其它粉丝群 如有疑问请联系管理");
                    return true;
                }
            }
            return true;
        }
    }
}
