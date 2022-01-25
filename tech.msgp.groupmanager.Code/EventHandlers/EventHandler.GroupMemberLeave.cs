using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mirai.CSharp;
using Mirai.CSharp.HttpApi.Handlers;
using Mirai.CSharp.HttpApi.Models.ChatMessages;
using Mirai.CSharp.HttpApi.Models.EventArgs;
using Mirai.CSharp.HttpApi.Parsers;
using Mirai.CSharp.HttpApi.Parsers.Attributes;
using Mirai.CSharp.HttpApi.Session;

namespace tech.msgp.groupmanager.Code.EventHandlers
{
    [RegisterMiraiHttpParser(typeof(DefaultMappableMiraiHttpMessageParser<IGroupMemberPositiveLeaveEventArgs, GroupMemberPositiveLeaveEventArgs>))]
    [RegisterMiraiHttpParser(typeof(DefaultMappableMiraiHttpMessageParser<IGroupMemberKickedEventArgs, GroupMemberKickedEventArgs>))]
    public partial class EventHandler : IMiraiHttpMessageHandler<IGroupMemberPositiveLeaveEventArgs>, IMiraiHttpMessageHandler<IGroupMemberKickedEventArgs>
    {
        public async Task HandleMessageAsync(IMiraiHttpSession client, IGroupMemberKickedEventArgs e)
        {
            if (!DataBase.me.IsGroupRelated(e.Member.Group.Id)) return;
            string name = e.Member.Name;
            long qq = e.Member.Id;
            long gid = e.Member.Group.Id;
            string gname = e.Member.Group.Name;
            long opid = e.Operator.Id;
            string opname = DataBase.me.getAdminName(opid);
            try
            {
                MainHolder.broadcaster.BroadcastToAdminGroup(new IChatMessage[]{ 
                    new PlainMessage(name + "被" + opname + "踢出了" + DataBase.me.getGroupName(gid) + "\n已自动拉黑该用户"),
                    new AtMessage(opid)
                });
                DataBase.me.recUserLeave(qq, gid, opid);
                DataBase.me.removeUser(qq, gid);
                DataBase.me.addUserBlklist(qq, "踢出触发的自动拉黑", opid);
            }
            catch (Exception err)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[已退群的处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
            return;
        }

        public async Task HandleMessageAsync(IMiraiHttpSession client, IGroupMemberPositiveLeaveEventArgs e)
        {
            if (!DataBase.me.IsGroupRelated(e.Member.Group.Id)) return;
            string name = e.Member.Name;
            long qq = e.Member.Id;
            long gid = e.Member.Group.Id;
            string gname = e.Member.Group.Name;
            try
            {
                MainHolder.broadcaster.BroadcastToAdminGroup(name + "退出了" + DataBase.me.getGroupName(gid) + "\n已删除该用户");
                DataBase.me.recUserLeave(qq, gid, null);
                DataBase.me.removeUser(qq, gid);
            }
            catch (Exception err)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[已退群的处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
            return;
        }
    }
}
