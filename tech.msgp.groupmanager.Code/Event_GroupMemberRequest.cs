using BililiveRecorder.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Mirai_CSharp.Plugin.Interfaces;

namespace tech.msgp.groupmanager.Code
{
    public class Event_GroupMemberRequest : IGroupApply
    {
        public static Dictionary<long, GroupEntranceInfo> reqs;
        CancellationTokenSource dmTokenSource;
        public static Task repeattask;
        public static bool hang_req = false;
        public static bool hang_all = false;
        static readonly TimeSpan startt = new TimeSpan(7, 0, 0);
        static readonly TimeSpan end = new TimeSpan(23, 0, 0);
        static readonly int groupMaxMember = 1000;
        public IGroupInfo[] grouplist;

        public Event_GroupMemberRequest()
        {
            grouplist = MainHolder.session.GetGroupListAsync().Result;
        }

        bool checkAnswer(string ans)
        {
            return (!(ans.Replace("鹿野", "").IndexOf("鹿") > 0)) && (ans.IndexOf("狗") > 0 || ans.IndexOf("犬") > 0 || ans.IndexOf("柴") > 0);
        }

        public IGroupInfo getGroupInfo(long groupid)
        {
            foreach (IGroupInfo g in grouplist)
            {
                if (g.Id == groupid) return g;
            }
            return null;
        }

        public IGroupConfig getGroupConf(long groupid)
        {
            return MainHolder.session.GetGroupConfigAsync(groupid).Result;
        }

        public int memberCountGroup(long groupid)
        {
            return MainHolder.session.GetGroupMemberListAsync(groupid).Result.Count();
        }

        public long getLeastMemberGroup()
        {
            long least_member_group = 0;
            int l_members = 2147483647;
            List<long> groups = DataBase.me.listGroup();
            foreach (long gid in groups)
            {
                if (DataBase.me.isMEIgnoreGroup(gid)) continue;
                int members = memberCountGroup(gid);
                if (members <= l_members)
                {
                    l_members = members;
                    least_member_group = gid;
                }
            }
            return least_member_group;
        }

        public void checkAndProcessReqQueue()
        {
            if (reqs.Count < 1) return;
            DateTime start = DateTime.Now;
            MainHolder.broadcaster.broadcastToAdminGroup("[队列DEBUG]\n长度=" + reqs.Count);
            Dictionary<long, GroupEntranceInfo> fails = new Dictionary<long, GroupEntranceInfo>();
            string log = "";
            int pass = 0;
            int fail = 0;
            foreach (KeyValuePair<long, GroupEntranceInfo> d in reqs)
            {
                if (d.Value == null) continue;

                IGroupInfo g = getGroupInfo(d.Value.group.Id);
                IGroupConfig gc = getGroupConf(d.Value.group.Id);
                int countmember = memberCountGroup(d.Value.group.Id);

                if (g != null)
                {
                    bool success = true;
                    int maxmember = groupMaxMember;
                    long q = d.Value.qq.qq;
                    QBaseInfo qi = d.Value.qq;
                    int trusted = DataBase.me.isUserTrusted(q);
                    //QQ q = MainHolder.session.get;
                    if (DataBase.me.isUserOperator(q))
                    {
                        MainHolder.session.HandleGroupApplyAsync(d.Value.igpargs, GroupApplyActions.Allow);
                        log += qi.nickname + "-> 是管理员 √\n";
                        pass++;
                    }
                    else if (trusted >= 0)
                    {
                        long opid = DataBase.me.getUserTrustOperator(q);
                        MainHolder.session.HandleGroupApplyAsync(d.Value.igpargs, GroupApplyActions.Allow);
                        if (trusted == 1)
                        {
                            DataBase.me.removeUserTrustlist(q);
                            log += qi.nickname + " -> 信任一次 [ATUSER(" + opid + ")] √\n";
                        }
                        else
                        {
                            log += qi.nickname + " -> 永久信任 [ATUSER(" + opid + ")] √\n";
                        }
                        pass++;
                    }
                    else if (memberCountGroup(g.Id) >= maxmember - 10)
                    {
                        long gpid = getLeastMemberGroup();
                        MainHolder.session.HandleGroupApplyAsync(d.Value.igpargs, GroupApplyActions.Deny, "该群成员已达上限，请加入" + gpid);
                        log += qi.nickname + " -> 群满 ×\n";
                        fail++;
                    }
                    else if (DataBase.me.isUserBlacklisted(q) && DataBase.me.connected)//已被拉黑
                    {
                        MainHolder.session.HandleGroupApplyAsync(d.Value.igpargs, GroupApplyActions.Deny, "您在LUYE通用黑名单中,不允许加群。");
                        log += qi.nickname + " -> 已被拉黑 ×\n";
                        fail++;
                    }
                    else if (DataBase.me.whichGroupsAreTheUserIn(q).Count > 0 && DataBase.me.connected)
                    {//重复加群
                        if (!DataBase.me.isMEIgnoreGroup(g.Id))
                        {
                            MainHolder.session.HandleGroupApplyAsync(d.Value.igpargs, GroupApplyActions.Deny, "您加入了其它鹿野群");
                            log += qi.nickname + " -> 重复加群 ×\n";
                            fail++;
                        }
                        else
                        {
                            MainHolder.session.HandleGroupApplyAsync(d.Value.igpargs, GroupApplyActions.Allow);
                            log += qi.nickname + " -> 不查重 √\n";
                            pass++;
                        }
                    }
                    else
                    {
                        int level = ThirdPartAPIs.getQQLevel(q, 3);
                        if (level < 0)
                        {
                            log += qi.nickname + " -> 等级验证失败 E\n";
                            fails.Add(d.Key, d.Value);
                        }
                        else if (level < 16)
                        {
                            MainHolder.session.HandleGroupApplyAsync(d.Value.igpargs, GroupApplyActions.Deny, "您的QQ等级(" + level + ")低于设定值(16)");
                            log += qi.nickname + " -> 等级过低 ×\n";
                            fail++;
                        }
                        else
                        {
                            log += qi.nickname + " -> 符合要求 √\n";
                            pass++;
                        }
                    }
                }
            }
            MainHolder.broadcaster.broadcastToAdminGroup("[延迟通过队列处理结果]\n" + log + "--------------\n" + pass + "通过，" + fail + "拒绝，" + fails.Count() + "错误\n\n处理耗时" + (DateTime.Now - start).TotalSeconds + "s");
            reqs.Clear();
            reqs = fails;
        }

        public class GroupEntranceInfo
        {
            public IGroupInfo group;
            public QBaseInfo qq;
            public JObject json;
            public IGroupApplyEventArgs igpargs;
        }

        public struct QBaseInfo
        {
            public string nickname;
            public long qq;
        }

        public Task<bool> GroupApply(MiraiHttpSession session, IGroupApplyEventArgs e)
        {
            if (reqs == null) reqs = new Dictionary<long, GroupEntranceInfo>();
            if (repeattask == null)
            {
                dmTokenSource = new CancellationTokenSource();
                repeattask = Repeat.Interval(TimeSpan.FromSeconds(15), () =>
                {
                    TimeSpan now = DateTime.Now.TimeOfDay;
                    if (now > startt && now < end && !hang_req && !hang_all)
                    {
                        checkAndProcessReqQueue();
                    }
                }, dmTokenSource.Token);
            }
            //string answer = e..Replace("鹿野", "");
            if (DataBase.me.isUserOperator(e.user.qq))
            {
                MainHolder.api.doGroupEnterReq(e.json, true);
                MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n管理组成员，无条件通过");
            }
            else
            if (DataBase.me.isCrewGroup(e.throughgroup.id))
            {//是舰长群
                if (DataBase.me.isUserBoundedUID(e.user.qq))//舰长绑定
                {
                    MainHolder.api.doGroupEnterReq(e.json, true);
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "\n！正在加入舰长群\n是舰长，同意");
                }
                else
                {
                    MainHolder.api.doGroupEnterReq(e.json, false, "该QQ无可查询的舰长信息，请联系管理员。");
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "\n！正在加入舰长群\n没有可查询的舰长记录，拒绝");
                }
            }
            else
            if (DataBase.me.isUserTrusted(e.user.qq) >= 0)//有信任
            {
                int t = DataBase.me.isUserTrusted(e.user.qq);
                long op = DataBase.me.getUserTrustOperator(e.user.qq);
                MainHolder.api.doGroupEnterReq(e.json, true);
                MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n[ATUSER(" + op + ")]设置了信任，同意");
                if (t == 1)
                {
                    DataBase.me.removeUserTrustlist(e.user.qq);
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + " 一次性信任失效");
                }
            }
            else
            if (hang_all)
            {
                GroupEntranceInfo geinfo = new GroupEntranceInfo()
                {
                    json = e.json,
                    qq = e.FromQQ,
                    group = e.throughgroup
                };
                reqs.Add(e.user.qq, geinfo);
                MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n请求挂起(UC)");
            }
            else
            if ((answer.IndexOf("鹿") >= 0) || !((answer.IndexOf("奶狗") >= 0) || (answer.IndexOf("柴") >= 0) || (answer.IndexOf("狗") >= 0)))
            {
                MainHolder.api.doGroupEnterReq(e.json, false, "您的答案不太对");
                MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n" + answer + "\n答案不对，拒绝");
            }
            else
            if (DataBase.me.whichGroupsAreTheUserIn(e.user.qq).Count > 0 && !DataBase.me.isMEIgnoreGroup(e.throughgroup.id))
            {
                List<long> li = DataBase.me.whichGroupsAreTheUserIn(e.user.qq);
                string a = "";
                foreach (long g in li)
                {
                    a += DataBase.me.getGroupName(g) + "\n";
                }
                MainHolder.api.doGroupEnterReq(e.json, false, "您已加入多个粉丝群。如有疑问请联系管理");
                MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n重复加群：\n" + a + "拒绝");

            }
            else
            if (DataBase.me.isUserBlacklisted(e.user.qq))
            {
                MainHolder.api.doGroupEnterReq(e.json, false, "您被指定禁止加入本群");
                MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n在黑名单，拒绝");
            }
            else
            {
                long qqlevel = ThirdPartAPIs.getQQLevel(e.user.qq, 1);
                if (qqlevel < 0)
                {
                    GroupEntranceInfo geinfo = new GroupEntranceInfo()
                    {
                        json = e.json,
                        qq = e.user,
                        group = e.throughgroup
                    };
                    reqs.Add(e.user.qq, geinfo);
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n查等级失败 稍后重试");
                }
                else
                if (qqlevel < 16)
                {
                    MainHolder.api.doGroupEnterReq(e.json, false, "您的等级低于16(☀)，根据规定不能加入。");
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n等级过低(" + qqlevel + ")，拒绝");
                }
                else
                if (!(hang_req))
                {
                    MainHolder.api.doGroupEnterReq(e.json, true);
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "申请加入" + DataBase.me.getGroupName(e.throughgroup.id) + "\n" +
                        "√ 答案正确\n" +
                        "√ 没有重复加群\n" +
                        "√ 等级足够：" + qqlevel + "\n" +
                        "√ 没有黑名单限制项目\n" +
                        "通过");
                }
                else
                {
                    GroupEntranceInfo geinfo = new GroupEntranceInfo()
                    {
                        json = e.json,
                        qq = e.user,
                        group = e.throughgroup
                    };
                    reqs.Add(e.user.qq, geinfo);
                    MainHolder.broadcaster.broadcastToAdminGroup(e.user.name + "#" + e.user.qq + "\n请求挂起(C)");
                }
            }

            try
            {

            }
            catch (Exception err)
            {
                MainHolder.broadcaster.broadcastToAdminGroup("[Exception]\n这条消息可能意味着机器人发生了错误。它仍在继续运行，但可能不是很稳定。下面的信息用来帮助鸡蛋定位错误，管理不必在意。\n[申请入群处理]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
            }
        }
    }
}