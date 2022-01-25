using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mirai.CSharp;
using Mirai.CSharp.HttpApi.Handlers;
using Mirai.CSharp.HttpApi.Models.EventArgs;
using Mirai.CSharp.HttpApi.Parsers;
using Mirai.CSharp.HttpApi.Parsers.Attributes;
using Mirai.CSharp.HttpApi.Session;
using Mirai.CSharp.Models;

namespace tech.msgp.groupmanager.Code.EventHandlers
{
    [RegisterMiraiHttpParser(typeof(DefaultMappableMiraiHttpMessageParser<IGroupApplyEventArgs, GroupApplyEventArgs>))]
    public partial class EventHandler : IMiraiHttpMessageHandler<IGroupApplyEventArgs>
    {
        public async Task HandleMessageAsync(IMiraiHttpSession session, IGroupApplyEventArgs e)
        {
            if (!DataBase.me.IsGroupRelated(e.FromGroup)) return;
            if (DataBase.me.isUserBlacklisted(e.FromQQ))
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("入群的用户 " + e.NickName + "(" + e.FromQQ + ") 存在于黑名单中，自动拒绝。");
                await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "您被设置不能加入任何粉丝群。");
                return;
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
                case 9:
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Allow);
                    return;
            }
            int qqlevel = -1;
            if (e.FromGroup != 964206367)
            {
                qqlevel = ThirdPartAPIs.getQQLevel(e.FromQQ, 2);
                if (qqlevel < 0)
                {
                    Thread.Sleep(2000);
                    qqlevel = ThirdPartAPIs.getQQLevel(e.FromQQ, 2);
                }
                if (qqlevel < 0)
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("入群的用户 " + e.NickName + "(" + e.FromQQ + ") 等级查询失败(try3,2s,try3),已提示重新申请");
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "等级查询失败,请重新申请入群");
                    return;
                }
                else
                if (qqlevel < 16)
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("入群的用户 " + e.NickName + "(" + e.FromQQ + ") 等级过低(" + qqlevel + "<16), 拒绝");
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "您的QQ等级过低, 如有疑问请联系管理");
                    return;
                }
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
                        MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n！正在加入舰长群(" + qqlevel + ">=16)\n是舰长，同意");
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
                return;
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
                    return;
                }
            }

            {
                //await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Allow);
                MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n不在黑名单,等级条件满足(" + qqlevel + ">=16)\n等待人工处理");
                return;
            }
        }
    }
}
