using Mirai_CSharp.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace tech.msgp.groupmanager.Code
{
    public class Broadcaster
    {

        public Broadcaster()
        {
            MainHolder.refreshFriendsList();
        }



        public bool BroadcastToUserGroup(IMessageBase[] message)
        {
            try
            {
                List<long> groups = DataBase.me.listGroup();
                bool success = true;
                Random rand = new Random();
                foreach (long gpid in groups)
                {
                    success = success & SendToGroup(gpid, message);
                    Thread.Sleep(rand.Next(500,1500));
                }
                return success;
            }
            catch
            {
                return false;
            }
        }

        public bool BroadcastToUserGroup(string message)
        {
            return BroadcastToUserGroup(new PlainMessage[] { new PlainMessage(message) });
        }

        public bool BroadcastToAdminGroup(IMessageBase[] message)
        {
            try
            {
                List<long> groups = DataBase.me.listAdminGroup();
                bool success = true;
                Random rand = new Random();
                foreach (long gpid in groups)
                {
                    success = success & SendToGroup(gpid, message);
                    Thread.Sleep(rand.Next(500, 1500));
                }
                return success;
            }
            catch
            {
                return false;
            }
        }

        public bool BroadcastToAdminGroup(string message)
        {
            return BroadcastToAdminGroup(new PlainMessage[] { new PlainMessage(message) });
        }

        public bool SendToGroup(long group, IMessageBase[] msg)
        {
            MainHolder.session.SendGroupMessageAsync(group, msg).Wait();
            return true;
        }

        public bool SendToGroup(long group, string msg)
        {
            return SendToGroup(group, new PlainMessage[] { new PlainMessage(msg) });
        }

        public bool BroadcastToAllGroup(IMessageBase[] msg)
        {
            return BroadcastToUserGroup(msg) & BroadcastToAdminGroup(msg);

        }

        public bool BroadcastToAllGroup(string msg)
        {
            return BroadcastToAllGroup(new PlainMessage[] { new PlainMessage(msg) });
        }

        public bool BroadcastToAllGroup(string msg, IMessageBase external)
        {
            return BroadcastToAllGroup(new IMessageBase[] { new PlainMessage(msg), external });
        }

        public bool BroadcastToCrewGroup(IMessageBase[] message)
        {
            List<long> groups = DataBase.me.getCrewGroup();
            bool success = true;
            Random rand = new Random();
            foreach (long gpid in groups)
            {
                success &= SendToGroup(gpid, message);
                Thread.Sleep(rand.Next(500, 1500));
            }
            return success;
        }

        public bool BroadcastToCrewGroup(string msg)
        {
            return BroadcastToCrewGroup(new PlainMessage[] { new PlainMessage(msg) });
        }

        public bool SendToAnEgg(string msg)
        {
            return SendToQQ(1250542735, new PlainMessage[] { new PlainMessage(msg) });
        }

        public bool SendToQQ(long qq, IMessageBase[] message)
        {
            if (MainHolder.friends.Contains(qq))
            {//好友
                MainHolder.session.SendFriendMessageAsync(qq, message).Wait();
                return true;
            }
            else
            {
                List<long> th_group = DataBase.me.whichGroupsAreTheUserIn(qq);
                if (th_group.Count > 0)
                {
                    MainHolder.session.SendTempMessageAsync(qq, th_group[0], message);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SendToQQ(long qq, string msg, long tg)
        {
            MainHolder.session.SendTempMessageAsync(qq, tg, new PlainMessage(msg));
            return true;
        }

        public bool SendToQQ(long qq, string msg)
        {
            return SendToQQ(qq, new PlainMessage[] { new PlainMessage(msg) });
        }

        [Obsolete("队列不再自行维护", true)]
        public void ProcessQueueMsgSend()
        {
            return;
        }
    }
}
