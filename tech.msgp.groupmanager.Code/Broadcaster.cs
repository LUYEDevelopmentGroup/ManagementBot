﻿using Mirai.CSharp.HttpApi.Models.ChatMessages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace tech.msgp.groupmanager.Code
{
    public class Broadcaster
    {

        public Broadcaster()
        {
            MainHolder.refreshFriendsList();
        }



        public bool BroadcastToUserGroup(IChatMessage[] message)
        {
            try
            {
                List<long> groups = DataBase.me.listGroup();
                bool success = true;
                Random rand = new Random();
                foreach (long gpid in groups)
                {
                    success = success & SendToGroup(gpid, message);
                    Thread.Sleep(rand.Next(1000, 3000));
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
            return BroadcastToUserGroup(new PlainMessage[] { new PlainMessage(message + "\n" + GenerateCheckCode(10)) });
        }

        public bool BroadcastToAdminGroup(IChatMessage[] message)
        {
            try
            {
                List<long> groups = DataBase.me.listAdminGroup();
                bool success = true;
                Random rand = new Random();
                foreach (long gpid in groups)
                {
                    success = success & SendToGroup(gpid, message);
                    Thread.Sleep(rand.Next(1000, 3000));
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
            return BroadcastToAdminGroup(new PlainMessage[] { new PlainMessage(message + "\n" + GenerateCheckCode(10)) });
        }

        public bool SendToGroup(long group, IChatMessage[] msg)
        {
            Thread.Sleep(1000);
            MainHolder.session.SendGroupMessageAsync(group, msg).Wait();
            return true;
        }

        public bool SendToGroup(long group, string msg)
        {
            return SendToGroup(group, new PlainMessage[] { new PlainMessage(msg + "\n" + GenerateCheckCode(10)) });
        }

        public bool BroadcastToAllGroup(IChatMessage[] msg)
        {
            return BroadcastToUserGroup(msg) & BroadcastToAdminGroup(msg);

        }

        public bool BroadcastToAllGroup(string msg)
        {
            return BroadcastToAllGroup(new PlainMessage[] { new PlainMessage(msg + "\n" + GenerateCheckCode(10)) });
        }

        public bool BroadcastToAllGroup(string msg, IChatMessage external)
        {
            return BroadcastToAllGroup(new IChatMessage[] { new PlainMessage(msg + "\n" + GenerateCheckCode(10)), external });
        }

        public bool BroadcastToCrewGroup(IChatMessage[] message)
        {
            List<long> groups = DataBase.me.getCrewGroup();
            bool success = true;
            Random rand = new Random();
            foreach (long gpid in groups)
            {
                success &= SendToGroup(gpid, message);
                Thread.Sleep(rand.Next(1000, 3000));
            }
            return success;
        }

        public bool BroadcastToCrewGroup(string msg)
        {
            return BroadcastToCrewGroup(new PlainMessage[] { new PlainMessage(msg + "\n" + GenerateCheckCode(10)) });
        }

        public bool SendToAnEgg(string msg)
        {
            return SendToQQ(1250542735, new PlainMessage[] { new PlainMessage(msg + "\n" + GenerateCheckCode(10)) });
        }

        public bool SendToQQ(long qq, IChatMessage[] message)
        {
            if (MainHolder.friends.Contains(qq))
            {//好友
                Thread.Sleep(1000);
                MainHolder.session.SendFriendMessageAsync(qq, message).Wait();
                return true;
            }
            else
            {
                List<long> th_group = DataBase.me.whichGroupsAreTheUserIn(qq);
                if (th_group.Count > 0)
                {
                    Thread.Sleep(1000);
                    MainHolder.session.SendTempMessageAsync(qq, th_group[0], message);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SendToQQ(long qq, IChatMessage[] message, long tg)
        {
            Thread.Sleep(1000);
            MainHolder.session.SendTempMessageAsync(qq, tg, message);
            return true;
        }

        public bool SendToQQ(long qq, string msg, long tg)
        {
            Thread.Sleep(1000);
            MainHolder.session.SendTempMessageAsync(qq, tg, new PlainMessage(msg));
            return true;
        }

        public bool SendToQQ(long qq, string msg)
        {
            return SendToQQ(qq, new PlainMessage[] { new PlainMessage(msg) });
        }

        int rep = 0;
        private string GenerateCheckCode(int codeCount)
        {
            string str = string.Empty;
            long num2 = DateTime.Now.Ticks + this.rep;
            this.rep++;
            Random random = new Random(((int)(((ulong)num2) & 0xffffffffL)) | ((int)(num2 >> this.rep)));
            for (int i = 0; i < codeCount; i++)
            {
                char ch;
                int num = random.Next();
                if ((num % 2) == 0)
                {
                    ch = (char)(0x30 + ((ushort)(num % 10)));
                }
                else
                {
                    ch = (char)(0x41 + ((ushort)(num % 0x1a)));
                }
                str = str + ch.ToString();
            }
            return str;
        }

        [Obsolete("队列不再自行维护", true)]
        public void ProcessQueueMsgSend()
        {
            return;
        }
    }
}
