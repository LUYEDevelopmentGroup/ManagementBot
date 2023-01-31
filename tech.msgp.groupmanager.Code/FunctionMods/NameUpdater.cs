using BiliApi;
using Mirai.CSharp.HttpApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tech.msgp.groupmanager.Code.FunctionMods
{
    static class NameUpdater
    {
        public const long gid = 781858343;
        public static bool isRunning = false;

        public static async Task UpdateAllNames()
        {
            if (isRunning) return;
            isRunning = true;
            var memberlist = await MainHolder.session.GetGroupMemberListAsync(gid);
            Random rnd = new Random();
            int check = 0, change = 0;
            foreach (var member in memberlist)
            {
                try
                {
                    if (member.Name.StartsWith('*') || (member.Name.StartsWith("总督 ") || member.Name.StartsWith("提督 ") || member.Name.StartsWith("舰长 ")))
                    { }
                    else { continue; }
                    var memberinfo = await MainHolder.session.GetGroupMemberInfoAsync(member.Id, gid);
                    if (memberinfo.Name.StartsWith('*') || (memberinfo.Name.StartsWith("总督 ") || memberinfo.Name.StartsWith("提督 ") || memberinfo.Name.StartsWith("舰长 ")))
                    {
                        var uid = DataBase.me.getUserBoundedUID(member.Id);
                        if (uid < 1) continue;
                        BiliUser user = new BiliUser(uid, MainHolder.biliapi, true);
                        check++;
                        if (memberinfo.Name != "*" + user.name)
                        {
                            IGroupMemberCardInfo iginfo = new GroupMemberCardInfo("*" + user.name, null);
                            await MainHolder.session.ChangeGroupMemberInfoAsync(memberinfo.Id, gid, iginfo);
                            change++;
                        }
                    }
                }
                catch (Exception err)
                {
                    MainHolder.DumpException(err);
                    MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群成员ID同步]\n同步一个用户的ID时出现问题\n" + err.Message + "\n" + err.StackTrace);
                }
                Thread.Sleep(rnd.Next(3000, 6000));
            }
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群成员ID同步]\n已检查" + check + "位成员，并更新" + change + "位成员的ID。");
            isRunning = false;
        }
    }
}
