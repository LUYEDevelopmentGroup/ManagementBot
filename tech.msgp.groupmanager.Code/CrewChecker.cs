using System;
using System.Collections.Generic;
using static tech.msgp.groupmanager.Code.DataBase;

namespace tech.msgp.groupmanager.Code
{
    public class CrewChecker
    {
        private Dictionary<long, CrewMember> processed;
        public void checkCrews()
        {
            getAllCrewMembers();
            Dictionary<long, CrewMember> cr = getCurrentCrewMembers();
            string str = "";
            foreach (KeyValuePair<long, CrewMember> kvp in cr)
            {
                if (kvp.Value.days_left < 5)
                {
                    str += kvp.Value.uid + "->剩余" + kvp.Value.days_left + "天\n";
                }
            }
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长追踪]<测试>\n截至目前，共有" + cr.Count + "位记录在案的舰长；\n以下舰长即将过期：\n" + str);
        }

        public Dictionary<long, CrewMember> getCurrentCrewMembers()
        {
            Dictionary<long, CrewMember> rt = new Dictionary<long, CrewMember>();
            foreach (KeyValuePair<long, CrewMember> kvp in processed)
            {
                if (!kvp.Value.expired)
                {
                    rt.Add(kvp.Key, kvp.Value);
                }
            }
            return rt;
        }

        public Dictionary<long, CrewMember> getAllCrewMembers()
        {
            List<CrewMember> crmember = DataBase.me.listCrewMembers();
            var processed = new Dictionary<long, CrewMember>();
            crmember.Sort();
            foreach (CrewMember c in crmember)
            {
                if (processed.ContainsKey(c.uid))
                {
                    TimeSpan ts = c.buytime - processed[c.uid].buytime;
                    int days = processed[c.uid].len_days;
                    if (ts.TotalDays > days)
                    {
                        processed[c.uid] = c;
                    }
                    else
                    {
                        processed[c.uid].len_days += c.len_days;
                        processed[c.uid].level = c.level;
                    }
                }
                else
                {
                    processed.Add(c.uid, c);
                }
            }
            foreach (KeyValuePair<long, CrewMember> kvp in processed)
            {
                TimeSpan ts = DateTime.Now - kvp.Value.buytime;
                kvp.Value.days_left = (int)Math.Ceiling(kvp.Value.len_days - ts.TotalDays);
                kvp.Value.expired = ts.TotalDays > kvp.Value.len_days;
            }
            this.processed = processed;
            return processed;
        }

        public static int FixCrewlistFromRawlog()
        {
            int count = 0;
            var currentlist = DataBase.me.listCrewMembers();
            var rawlog = DataBase.me.DumpCrewDataFromRedundancy();
            foreach (var c in rawlog)
            {
                CrewMember hit = null;
                foreach (var item in currentlist)
                {
                    if (item.uid == c.Uid && item.level == c.Level && item.len_days == c.Duration.Days)
                    {
                        TimeSpan delta = item.buytime - c.Start;
                        if (Math.Abs(delta.Minutes) < 5)
                        {
                            hit = item;
                            break;
                        }
                    }
                }
                if (hit == null)//rawlog里有，但是current里没有
                {
                    MainHolder.logger("FixCrewList", "UID=" + c.Uid + " Len=" + (c.Duration.Days / 30) + " Level=" + c.Level);
                    DataBase.me.recUserBuyGuard(c.Uid, c.Duration.Days / 30, c.Level, 1, c.Start);
                    count++;
                }
                else
                {
                    currentlist.Remove(hit);//成功匹配的项不参加下一次匹配
                }
            }
            return count;
        }

        public static int RegenerateCrewTimeline()
        {
            int count = 0;
            var currentlist = DataBase.me.listCrewMembers();
            DataBase.me.ClearCrewTimelineData();
            foreach(var c in currentlist)
            {
                var userlasttimeline = DataBase.me.GetLastestCrewspan(c.uid);
                if (userlasttimeline.Duration == TimeSpan.Zero)
                {
                    DataBase.me.WriteCrewspan(new CrewLogItem
                    {
                        DataId = -1,
                        Start = c.buytime,
                        Duration = TimeSpan.FromDays(c.len_days),
                        Uid = c.uid
                    });
                }
                else
                {
                    userlasttimeline.Duration.Add(TimeSpan.FromDays(c.len_days));
                    DataBase.me.WriteCrewspan(userlasttimeline);
                }
                count++;
            }
            return count;
        }
    }
}
