using System;
using System.Collections.Generic;
using static tech.msgp.groupmanager.Code.DataBase;

namespace tech.msgp.groupmanager.Code
{
    public class CrewChecker
    {
        private Dictionary<int, CrewMember> processed;
        public void checkCrews()
        {
            getAllCrewMembers();
            Dictionary<int, CrewMember> cr = getCurrentCrewMembers();
            string str = "";
            foreach (KeyValuePair<int, CrewMember> kvp in cr)
            {
                if (kvp.Value.days_left < 5)
                {
                    str += kvp.Value.uid + "->剩余" + kvp.Value.days_left + "天\n";
                }
            }
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长追踪]<测试>\n截至目前，共有" + cr.Count + "位记录在案的舰长；\n以下舰长即将过期：\n" + str);
        }

        public Dictionary<int, CrewMember> getCurrentCrewMembers()
        {
            Dictionary<int, CrewMember> rt = new Dictionary<int, CrewMember>();
            foreach (KeyValuePair<int, CrewMember> kvp in processed)
            {
                if (!kvp.Value.expired)
                {
                    rt.Add(kvp.Key, kvp.Value);
                }
            }
            return rt;
        }

        public Dictionary<int, CrewMember> getAllCrewMembers()
        {
            List<CrewMember> crmember = DataBase.me.listCrewMembers();
            Dictionary<int, CrewMember> processed = new Dictionary<int, CrewMember>();
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
            foreach (KeyValuePair<int, CrewMember> kvp in processed)
            {
                TimeSpan ts = DateTime.Now - kvp.Value.buytime;
                kvp.Value.days_left = (int)Math.Ceiling(kvp.Value.len_days - ts.TotalDays);
                kvp.Value.expired = ts.TotalDays > kvp.Value.len_days;
            }
            this.processed = processed;
            return processed;
        }
    }
}
