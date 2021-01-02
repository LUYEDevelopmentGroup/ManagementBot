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
            
            if (DataBase.me.isCrewGroup(e.FromGroup))
            {//是舰长群
                if (DataBase.me.isUserBlacklisted(e.FromQQ))
                {
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "很抱歉，我们不能让您入群。请联系管理员。");
                    MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n！正在加入舰长群\n黑名单，拒绝");
                }
                else
                if (DataBase.me.isUserBoundedUID(e.FromQQ))//舰长绑定
                {
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Allow);
                    MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n！正在加入舰长群\n是舰长，同意");
                }
                else
                {
                    await MainHolder.session.HandleGroupApplyAsync(e, GroupApplyActions.Deny, "您没有大航海数据，如有疑问请咨询管理。");
                    MainHolder.broadcaster.BroadcastToAdminGroup(e.FromQQ + "\n！正在加入舰长群\n无付费记录，拒绝");
                }
            }
            return true;
        }
    }
}
