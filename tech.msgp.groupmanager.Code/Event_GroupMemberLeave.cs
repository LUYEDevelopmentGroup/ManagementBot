using CQ2IOT.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tech.msgp.groupmanager.Code
{
    public class Event_GroupMemberLeave
    {
        public void GroupMemberDecrease(object sender, GroupMemberDecreaseEventArgs e)
        {
            try
            {

                MainHolder.broadcaster.broadcastToAdminGroup(DataBase.me.getUserName(e.user.qq) + "退出了" + DataBase.me.getGroupName(e.throughgroup.id) + "\n已删除该用户");
                DataBase.me.recUserLeave(e.user.qq, e.throughgroup.id, null);
                DataBase.me.removeUser(e.user.qq, e.throughgroup.id);
            }
            catch (Exception err)
            {
                MainHolder.broadcaster.broadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[已退群的处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
        }
    }
}