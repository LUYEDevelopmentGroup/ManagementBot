using Mirai.CSharp.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace tech.msgp.groupmanager.Code
{
    public class DataBase
    {
        private string sqladdr;
        private string sqluser;
        private string sqlpawd;

        public bool busy = false;
        public static DataBase me => ConnectionPool.getMainConnection();
        public MySqlConnection sql;
        public bool connected = false;

        #region 底层封装
        public DataBase(string addr, string user, string passwd)
        {
            sqladdr = addr;
            sqluser = user;
            sqlpawd = passwd;
            connect();
        }

        public bool connect()
        {
            try
            {
                if (sql != null)
                {
                    sql.Close();
                }
            }
            catch { }
            string conStr = "server=" + sqladdr + ";port=3306;user=" + sqluser + ";password=\"" + sqlpawd + "\"; database=luyemanager;Allow User Variables=True";
            sql = new MySqlConnection(conStr);

            try
            {
                sql.Open();
                connected = true;
            }
            catch (MySqlException)
            {
                connected = false;
            }
            return connected;
        }

        public bool checkconnection()
        {
            connected = (sql.State == System.Data.ConnectionState.Open);
            if (!connected)
            {
                MainHolder.Logger.Info("数据库", "到数据库的连接丢失，试图重连");
                if (!connect())
                {
                    MainHolder.Logger.Error("数据库", "无法连接到数据库，查询丢失");
                    return false;
                }
                else
                {
                    MainHolder.Logger.Info("数据库", "连接已建立");
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        public bool execsql(string cmd_, Dictionary<string, object> args)
        {
            busy = true;
            lock (sql)
            {
                checkconnection();
                try
                {
                    MainHolder.Logger.Debug("sql", cmd_);
                    using (MySqlCommand cmd = new MySqlCommand(cmd_, sql))
                    {
                        foreach (KeyValuePair<string, object> arg in args)
                        {
                            cmd.Parameters.AddWithValue(arg.Key, arg.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    busy = false;
                    return true;
                }
                catch (Exception e)
                {
                    MainHolder.Logger.Error("数据库", e.Message);
                    connected = false;
                    busy = false;
                    return false;
                }
            }
        }

        public struct ArgPack
        {
            public string Name;
            public Object Value;
            public MySqlDbType Type;
        }

        public bool execsql(string cmd_, Dictionary<string, ArgPack> args)
        {
            busy = true;
            lock (sql)
            {
                checkconnection();
                try
                {
                    MainHolder.Logger.Debug("sql", cmd_);
                    using (MySqlCommand cmd = new MySqlCommand(cmd_, sql))
                    {
                        foreach (KeyValuePair<string, ArgPack> arg in args)
                        {
                            cmd.Parameters.Add(arg.Key, arg.Value.Type).Value = arg.Value.Value;
                        }
                        cmd.ExecuteNonQuery();
                    }
                    busy = false;
                    return true;
                }
                catch (Exception e)
                {
                    MainHolder.Logger.Error("数据库", e.Message);
                    connected = false;
                    busy = false;
                    return false;
                }
            }
        }

        public bool execsql(string cmd_, Dictionary<string, object> args, out int rolls)
        {
            busy = true;
            lock (sql)
            {
                checkconnection();
                try
                {
                    MainHolder.Logger.Debug("sql", cmd_);
                    using (MySqlCommand cmd = new MySqlCommand(cmd_, sql))
                    {
                        foreach (KeyValuePair<string, object> arg in args)
                        {
                            cmd.Parameters.AddWithValue(arg.Key, arg.Value);
                        }
                        rolls = cmd.ExecuteNonQuery();
                    }
                    busy = false;
                    return true;
                }
                catch (Exception e)
                {
                    MainHolder.Logger.Error("数据库", e.Message);
                    connected = false;
                    busy = false;
                    rolls = 0;
                    return false;
                }
            }
        }


        public int execsql_firstmatch(string sqlc, Dictionary<string, object> args)
        {
            busy = true;
            lock (sql)
            {
                try
                {
                    checkconnection();
                    int id = -1;
                    using (MySqlCommand cmd = new MySqlCommand(sqlc, sql))
                    {
                        foreach (KeyValuePair<string, object> arg in args)
                        {
                            cmd.Parameters.AddWithValue(arg.Key, arg.Value);
                        }
                        if (cmd.ExecuteScalar() != null)
                        {
                            id = (int)cmd.ExecuteScalar();
                        }
                    }
                    busy = false;
                    return id;
                }
                catch (Exception e)
                {
                    MainHolder.Logger.Error("数据库", e.Message);
                    connected = false;
                    busy = false;
                    return -1;
                }
            }
        }

        public List<List<string>> querysql(string cmd_, Dictionary<string, object> args, List<int> rolls)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(cmd_, sql))
                {
                    busy = true;
                    lock (sql)
                    {
                        checkconnection();
                        foreach (KeyValuePair<string, object> arg in args)
                        {
                            cmd.Parameters.AddWithValue(arg.Key, arg.Value);
                        }
                        MySqlDataReader mdr = cmd.ExecuteReader();
                        List<List<string>> data = new List<List<string>>();
                        if (mdr.HasRows)
                        {
                            while (mdr.Read())
                            {
                                List<string> line = new List<string>();
                                foreach (int roll in rolls)
                                {
                                    line.Add(mdr.GetString(roll));
                                }
                                data.Add(line);
                            }
                        }
                        mdr.Close();
                        busy = false;
                        return data;
                    }
                }
            }
            catch (Exception e)
            {
                MainHolder.Logger.Error("数据库", e.Message);
                connected = false;
                return null;
            }
        }

        public int count(string sql, Dictionary<string, object> args)
        {
            checkconnection();
            List<int> rolls = new List<int>
            {
                0
            };
            string rtv = querysql(sql, args, rolls)[0][0];
            return int.Parse(rtv);
        }

        public static string get_uft8(string unicodeString)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] encodedBytes = utf8.GetBytes(unicodeString);
            string decodedString = utf8.GetString(encodedBytes);
            return decodedString;
        }

        #endregion

        #region 上层封装
        /// <summary>
        /// 记录一条QQ消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="group"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool recQQmsg(long sender, long group, string msg)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@sender", sender },
                { "@group", group },
                { "@msg", msg }
            };
            return execsql("INSERT INTO qq_msgrec (sender_qq, from_group, msg_text, type, time) VALUES (@sender, @group, @msg, 'text', NOW());", args);
        }

        /// <summary>
        /// 记录一个成员(被)退群
        /// </summary>
        /// <param name="kicked"></param>
        /// <param name="group"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public bool recUserLeave(long kicked, long group, long? op)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@kicked_qq", kicked },
                { "@group", group }
            };
            if (op != null)
            {
                args.Add("@operator", op.ToString());
                return execsql("INSERT INTO qq_leaves (qq, from_group, oprator, time) VALUES (@kicked_qq, @group, @operator, NOW());", args);
            }
            else
            {
                return execsql("INSERT INTO qq_leaves (qq, from_group, time) VALUES (@kicked_qq, @group, NOW());", args);
            }
        }

        /// <summary>
        /// 记录一个成员被禁言
        /// </summary>
        /// <param name="user"></param>
        /// <param name="group"></param>
        /// <param name="op"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public bool recUserSilenced(long user, long group, long op, int len)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", user },
                { "@group", group },
                { "@operator", op },
                { "@len", len }
            };
            return execsql("INSERT INTO qq_silences (qq, from_group, oprator, time, len) VALUES (@qq, @group, @operator, NOW(), @len);", args);
        }

        public bool recUserSilenceRemoved(long user, long group, long op)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", user },
                { "@group", group },
                { "@operator", op }
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return execsql("UPDATE qq_silences SET removedby = @operator WHERE qq = @qq and from_group = @group ORDER BY time DESC limit 1;", args);
        }
        public bool recUserBuyGuard(long uid, int len, int level, int lid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid },
                { "@len", len },
                { "@level", level },
                { "@lid", lid }
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return execsql("INSERT INTO bili_crew (uid, len, level, lid, timestamp) VALUES (@uid, @len, @level, @lid, NOW());", args);
        }

        public bool recUserBuyGuard(long uid, int len, int level, int lid, DateTime time)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid },
                { "@len", len },
                { "@level", level },
                { "@lid", lid },
                { "@time", time }
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return execsql("INSERT INTO bili_crew (uid, len, level, lid, timestamp) VALUES (@uid, @len, @level, @lid, @time);", args);
        }

        public bool recManagementEvents(ManagementEvent me)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@eventid", me.id},
                { "@type", me.type },
                { "@operator", me.op},
                { "@affected", me.affected},
                { "@reason", me.reason }
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return execsql("INSERT INTO management_events (eventid, type, operator, affected, reason) VALUES (@eventid, @type, @operator, @affected, @reason);", args);
        }

        public bool recTicket(ManagementEvent relatedEvent)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@eventid", relatedEvent.id},
                { "@operator", relatedEvent.op},
                { "@affected", relatedEvent.affected}
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return execsql("INSERT INTO management_tickets (evid, sender, gen_time, is_closed, avoid_op) VALUES (@eventid, @affected, NOW(), 0, @operator);", args);
        }

        public bool hasTicket(long evid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@eventid", evid}
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return count("SELECT COUNT(*) from management_tickets where evid like @eventid ;", args) > 0;
        }

        public bool hasunTakenTicket(long evid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@eventid", evid}
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return count("SELECT COUNT(*) from management_tickets where evid like @eventid and op is null;", args) > 0;
        }

        public bool takeTicket(long evid, long op)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@eventid", evid},
                { "@op", op}
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return execsql("UPDATE management_tickets SET op = @op WHERE evid = @eventid and op is null;", args);
        }

        public bool hasKickVoteFor(long qq, long g)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq},
                { "@g", g}
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return count("SELECT COUNT(*) from votes where target like @qq and `group` like @g and result is null;", args) > 0;
        }

        public bool hasKickVote(long evid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@evid", evid}
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return count("SELECT COUNT(*) from votes where evid like @evid and result is null;", args) > 0;
        }

        public long openKickVote(long op, long target, long group, int targetvotes)
        {
            long idd = BiliApi.TimestampHandler.GetTimeStamp16(DateTime.Now);
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@op", op},
                { "@target", target},
                { "@group", group},
                { "@evid", idd},
                { "@tt", targetvotes}
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            if (execsql("INSERT INTO votes (evid, `trigger`, target, `group`, targetvotes) VALUES (@evid, @op, @target, @group, @tt);", args))
            {
                return idd;
            }
            else
            {
                return -1;
            }
        }
        /*
        public bool scheduletask(long op, long target, long group, int targetvotes)
        {
            long idd = BiliApi.TimestampHandler.GetTimeStamp16(DateTime.Now);
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("@op", op.ToString());
            args.Add("@target", target.ToString());
            args.Add("@group", group.ToString());
            args.Add("@evid", idd.ToString());
            args.Add("@tt", targetvotes.ToString());
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            if (execsql("INSERT INTO votes (evid, `trigger`, target, `group`, targetvotes) VALUES (@evid, @op, @target, @group, @tt);", args))
            {
                return idd;
            }
            else
                return -1;
        }
        */

        public int getVotesByVoteID(long id)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@evid", id}
            };
            return count("SELECT COUNT(*) from vote_records where evid like @evid;", args);
        }

        public List<Vote> listVotes()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                0,
                1,
                2,
                3,
                4,
                5
            };
            List<List<string>> re = querysql("SELECT * from votes;", args, vs);
            List<Vote> group = new List<Vote>();
            foreach (List<string> line in re)
            {
                Vote v = new Vote()
                {
                    evid = long.Parse(line[0]),
                    op = long.Parse(line[1]),
                    target = long.Parse(line[2]),
                    group = long.Parse(line[3]),
                    votes = getVotesByVoteID(long.Parse(line[0])),
                    tarvotes = int.Parse(line[5])
                };
                group.Add(v);
            }
            return group;
        }

        public Vote getVote(long evid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@evid", evid}
            };
            List<int> vs = new List<int>
            {
                0,
                1,
                2,
                3,
                4,
                5
            };
            List<List<string>> re = querysql("SELECT * from votes where evid like @evid;", args, vs);
            List<Vote> group = new List<Vote>();
            foreach (List<string> line in re)
            {
                Vote v = new Vote()
                {
                    evid = long.Parse(line[0]),
                    op = long.Parse(line[1]),
                    target = long.Parse(line[2]),
                    group = long.Parse(line[3]),
                    votes = getVotesByVoteID(long.Parse(line[0])),
                    tarvotes = int.Parse(line[5])
                };
                group.Add(v);
            }
            return group[0];
        }

        public class Vote
        {
            public long evid, op, target, group;
            public int votes, tarvotes;
        }

        public bool hasVotedFor(long evid, long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@evid", evid},
                { "@qq", qq}
            };
            return count("SELECT COUNT(*) from vote_records where evid like @evid and qq like @qq;", args) > 0;
        }

        public bool voteFor(long evid, long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@evid", evid},
                { "@qq", qq}
            };
            return execsql("INSERT INTO vote_records (evid, qq) VALUES (@evid, @qq);", args);
        }

        public ManagementEvent getManagementEventByID(long id)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@eventid", id}
            };
            List<int> vs = new List<int>
            {
                0,
                1,
                2,
                3,
                4
            };
            List<string> re = querysql("SELECT * from management_events where eventid like @eventid limit 1;", args, vs)[0];
            return new ManagementEvent(long.Parse(re[0]), re[1], long.Parse(re[2]), long.Parse(re[3]), re[4]);
        }

        public string getCertificateName(long id, out string note)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", id}
            };
            List<int> vs = new List<int>
            {
                1,
                2
            };
            List<List<string>> re = querysql("SELECT * from qq_certificate where qq like @qq limit 1;", args, vs);
            if (re == null || re.Count < 1)
            {
                note = null;
                return null;
            }
            note = re[0][1];
            return re[0][0];
        }

        public string getCertificateName(long id)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", id}
            };
            List<int> vs = new List<int>
            {
                1,
                2
            };
            List<List<string>> re = querysql("SELECT * from qq_certificate where qq like @qq limit 1;", args, vs);
            if (re == null || re.Count < 1)
            {
                return null;
            }
            return re[0][0];
        }

        public bool isBiliUserGuard(long uid)
        {
            return getBiliUserGuardCount(uid) > 0;
        }

        public int getBiliUserGuardCount(long uid)
        {

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid}
            };
            return count("SELECT COUNT(*) from bili_crew where uid like @uid ;", args);
        }

        public bool isUserBlacklisted(long user)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", user}
            };
            return (count("SELECT COUNT(*) from blacklist_q where qq like @qq ;", args) > 0);
        }

        public bool isUserOperator(long user)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", user}
            };
            return (count("SELECT COUNT(*) from qq_operator where qq like @qq ;", args) > 0);
        }

        public bool isBiliUserExist(long user)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", user}
            };
            return (count("SELECT COUNT(*) from bili_users where uid like @uid ;", args) > 0);
        }

        public Dictionary<long, bool> cache_is_admin_group = new Dictionary<long, bool>();
        public bool isAdminGroup(long group)
        {
            if (cache_is_admin_group.ContainsKey(group))
            {
                return cache_is_admin_group[group];
            }

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@gpid", group}
            };
            bool res = (count("SELECT COUNT(*) from qq_admingroup where group_id like @gpid ;", args) > 0);
            cache_is_admin_group.Add(group, res);
            return res;
        }

        public Dictionary<long, bool> cache_is_ME_ignore_group = new Dictionary<long, bool>();
        public bool isMEIgnoreGroup(long group)
        {
            if (isAdminGroup(group))
            {
                return true;
            }

            if (cache_is_ME_ignore_group.ContainsKey(group))
            {
                return cache_is_ME_ignore_group[group];
            }

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@gpid", group}
            };
            bool res = count("SELECT COUNT(*) from qq_ignoregroup where group_id like @gpid ;", args) > 0;
            cache_is_ME_ignore_group.Add(group, res);
            return res;
        }

        public List<long> getCrewGroup()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<long> list = new List<long>();
            List<int> vs = new List<int>
            {
                0
            };
            List<List<string>> re = DataBase.me.querysql("SELECT * from qq_crewgroup;", args, vs);
            foreach (List<string> s in re)
            {
                list.Add(long.Parse(s[0]));
            }
            return list;
        }

        public bool isCrewGroup(long group)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@gpid", group}
            };
            bool res = count("SELECT COUNT(*) from qq_crewgroup where gpid like @gpid ;", args) > 0;
            return res;
        }

        public bool isUserSilenced(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            return (count("SELECT COUNT(*) from qq_silences where qq like @qq ;", args) > 0);
        }

        public bool isUserQuitted(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            return (count("SELECT COUNT(*) from qq_leaves where qq like @qq and oprator is null ;", args) > 0);
        }

        public bool isUserKicked(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            return (count("SELECT COUNT(*) from qq_leaves where qq like @qq and oprator is not null ;", args) > 0);
        }

        public List<int> checkForBadRecords(long qq)
        {
            List<int> badrec = new List<int>();
            if (isUserBlacklisted(qq))
            {
                badrec.Add(1);
            }

            if (isUserSilenced(qq))
            {
                badrec.Add(2);
            }

            if (isUserQuitted(qq))
            {
                badrec.Add(3);
            }

            if (isUserKicked(qq))
            {
                badrec.Add(4);
            }

            return badrec;
        }

        public CookieContainer getBiliLoginCookie()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>() { 0, 1, 2 };
            List<List<string>> re = DataBase.me.querysql("SELECT * from bili_logincookie;", args, vs);
            CookieContainer cc = new CookieContainer();
            foreach (List<string> row in re)
            {
                cc.Add(new Cookie(row[0], row[1], "/", row[2]));
            }

            return cc;
        }

        public CookieContainer updateBiliLoginCookie()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>() { 0, 1, 2 };
            List<List<string>> re = DataBase.me.querysql("SELECT * from bili_logincookie;", args, vs);
            CookieContainer cc = new CookieContainer();
            foreach (List<string> row in re)
            {
                cc.Add(new Cookie(row[0], row[1], "/", row[2]));
            }

            return cc;
        }

        public string getBiliCSRF(string url)
        {
            return getBiliLoginCookie().GetCookies(new Uri(url))["crsf"].Value;
        }

        public CookieContainer getBiliManagementAccLoginCookie()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>() { 0, 1, 2 };
            List<List<string>> re = DataBase.me.querysql("SELECT * from bili_logincookie_mana;", args, vs);
            CookieContainer cc = new CookieContainer();
            foreach (List<string> row in re)
            {
                cc.Add(new Cookie(row[0], row[1], "/", row[2]));
            }

            return cc;
        }

        public string getBiliManaCSRF(string url)
        {
            return getBiliManagementAccLoginCookie().GetCookies(new Uri(url))["bili_jct"].Value;
        }

        /*
        public void setBiliLoginCookie(CookieContainer cc)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("@qq", user.Id.ToString());
            args.Add("@group", group.Id.ToString());
            args.Add("@operator", op.Id.ToString());
            args.Add("@len", len.ToString());
            return execsql("INSERT INTO qq_silences (qq, from_group, oprator, time, len) VALUES (@qq, @group, @operator, NOW(), @len);", args);

        }
        */

        public string getAdminName(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            List<int> vs = new List<int>
            {
                2
            };
            List<List<string>> re = DataBase.me.querysql("SELECT * from qq_operator where qq like @qq ;", args, vs);
            return re[0][0];
        }

        public string getUserName(long qq)
        {
            try
            {
                Dictionary<string, object> args = new Dictionary<string, object>
                {
                    { "@qq", qq}
                };
                List<int> vs = new List<int>
                {
                    2
                };
                List<List<string>> re = DataBase.me.querysql("SELECT * from userdata where qq like @qq ;", args, vs);
                return re[0][0];
            }
            catch
            {
                return "#" + qq;
            }
        }

        public void clearCache()
        {
            cache_is_admin_group.Clear();
            cache_is_ME_ignore_group.Clear();
            group_name_cache.Clear();
        }

        public List<long> whichGroupsAreTheUserIn(long user, bool IgnoreMEIGroups = true)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", user}
            };
            List<int> vs = new List<int>
            {
                4
            };
            List<List<string>> re = querysql("SELECT * from userdata where qq like @qq ;", args, vs);
            List<long> group = new List<long>();
            foreach (List<string> line in re)
            {
                long gpn = long.Parse(line[0]);
                if ((!group.Contains(gpn)) && ((!isMEIgnoreGroup(gpn)) || !IgnoreMEIGroups))
                {
                    group.Add(gpn);
                }
            }
            return group;
        }

        public bool init_groupdata(long g)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@group", g},
                //args.Add("@gname", g.GetGroupInfo().Name);
                { "@gname", "unknown" }
            };
            execsql("delete from qq_groups where groupid = @group;", args);
            execsql("INSERT INTO qq_groups (groupid, groupname, allow_me) VALUES (@group, @gname, 0);", args);//把群信息存下来
            update_groupmembers(g);
            return true;
        }

        public bool addBiliPending(long uid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid}
            };
            return execsql("INSERT INTO bili_qqbound (uid, type) VALUES (@uid,1);", args);
        }

        public bool isBiliPending(long uid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid}
            };
            return (count("SELECT COUNT(*) from bili_qqbound where uid like @uid and qq is null;", args) > 0);
        }

        public bool boundBiliWithQQ(long uid, long qq)
        {
            if (!isBiliPending(uid))
            {
                addBiliPending(uid);
            }
            try
            {
                Dictionary<string, object> args = new Dictionary<string, object>
                {
                    { "@uid", uid},
                    { "@qq", qq}
                };
                execsql("UPDATE bili_qqbound SET qq = @qq WHERE uid = @uid;", args, out int a);
                return (a > 0);
            }
            catch (Exception e)
            {
                MainHolder.broadcaster.SendToAnEgg(e.Message + "\n" + e.StackTrace);
                return false;
            }
        }

        public bool isUserBoundedQQ(long uid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid}
            };
            return (count("SELECT COUNT(*) from bili_qqbound where uid like @uid and qq is not null;", args) > 0);
        }

        public long getUserBoundedQQ(long uid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid}
            };
            List<int> vs = new List<int>
            {
                1
            };
            List<List<string>> re = querysql("SELECT * from bili_qqbound where uid like @uid and qq is not null;", args, vs);
            long group = 0;
            foreach (List<string> line in re)
            {
                long gpn = long.Parse(line[0]);
                group = gpn;
            }
            return group;
        }

        public bool isUserBoundedUID(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            return (count("SELECT COUNT(*) from bili_qqbound where qq like @qq and uid is not null;", args) > 0);
        }

        //public bool isUserKickedOut

        public long getUserBoundedUID(long qq)
        {
            try
            {
                Dictionary<string, object> args = new Dictionary<string, object>
                {
                    { "@qq", qq}
                };
                List<int> vs = new List<int>
                {
                    0
                };
                List<List<string>> re = querysql("SELECT * from bili_qqbound where qq like @qq;", args, vs);
                long group = 0;
                foreach (List<string> line in re)
                {
                    long gpn = long.Parse(line[0]);
                    group = gpn;
                }
                return group;
            }
            catch
            {
                return 0;
            }
        }

        public bool saveMessageGroup(string fname, string fresid, int tsum, int flag, int serviceID, int fsize)//qq_msgsave
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@fname", fname },
                { "@fresid", fresid },
                { "@tsum", tsum},
                { "@flag", flag},
                { "@service", serviceID},
                { "@fsize", fsize}
            };
            return execsql("INSERT INTO qq_msgsave(m_fileName, m_resid, tSum, flag, serviceID, m_fileSize) VALUES(@fname, @fresid, @tsum, @flag, @service, @fsize); ", args);
        }

        public void getMessageGroup(string fname, out string fresid, out int tsum, out int flag, out int serviceID, out int fsize)//qq_msgsave
        {
            List<int> vs = new List<int>() { 1, 2, 3, 4, 5 };
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@fname", fname }
            };
            List<List<string>> re = querysql("SELECT * from qq_msgsave WHERE m_fileName = @fname;", args, vs);
            List<string> data = re[0];
            fresid = data[0];
            tsum = int.Parse(data[1]);
            flag = int.Parse(data[2]);
            serviceID = int.Parse(data[3]);
            fsize = int.Parse(data[4]);
        }

        public bool update_groupmembers(long g)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@group", g}
            };
            execsql("delete from userdata where from_group = @group;", args);//清除之前的成员列表
            IGroupMemberInfo[] members = MainHolder.session.GetGroupMemberListAsync(g).Result;//抓群成员
            foreach (IGroupMemberInfo minfo in members)//一个一个放进数据库
            {
                args = new Dictionary<string, object>
                {
                    { "@group", g},
                    { "@qq", minfo.Id},
                    { "@fname", minfo.Name}
                };
                execsql("INSERT INTO userdata (qq, friendly_name, from_group, adddate) VALUES (@qq, @fname, @group, NOW());", args);
            }
            return true;
        }

        public bool addUser(long q, long g, string fname)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@group", g},
                { "@qq", q},
                { "@fname", fname }
            };
            execsql("delete from userdata where qq = @qq and from_group = @group;", args);//清除这个成员(如果有)
            return execsql("INSERT INTO userdata (qq, friendly_name, from_group, adddate) VALUES (@qq, @fname, @group, NOW());", args);
        }

        public bool removeUser(long q, long g)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@group", g},
                { "@qq", q}
            };
            return execsql("delete from userdata where qq = @qq and from_group = @group;", args);//清除这个成员(如果有)
        }

        public bool addUserBlklist(long q, string reason, long op)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", q},
                { "@reason", reason },
                { "@op", op}
            };
            execsql("delete from blacklist_q where qq = @qq;", args);//清除这个记录(如果有)
            return execsql("INSERT INTO blacklist_q (qq, reason, operator, ban_time) VALUES (@qq, @reason, @op, NOW());", args);
        }

        public bool setBiliPermBan(long uid, long op)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid},
                { "@op", op}
            };
            execsql("UPDATE bili_bans SET op = @op WHERE uid = @uid;", args, out int a);
            if (a < 1)
            {
                return execsql("INSERT INTO bili_bans (lid, uid, op, eventtime) VALUES (-1, @uid, @op, NOW());", args);
            }
            return (a > 0);
        }

        public bool addUserTrustlist(long q, bool once, long op)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", q},
                { "@isonce", (once) ? "1" : "0" },
                { "@op", op}
            };
            execsql("delete from qq_trusts where qq = @qq;", args);//清除这个记录(如果有)
            return execsql("INSERT INTO qq_trusts (qq, isonce, operator, timestamp) VALUES (@qq, @isonce, @op, NOW());", args);
        }
        public bool removeUserTrustlist(long q)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", q}
            };
            execsql("UPDATE qq_trusts SET isonce = -1 WHERE qq = @qq;", args, out int a);
            return (a > 0);
        }

        /// <summary>
        /// 获取用户信任状态
        /// </summary>
        /// <param name="qq"></param>
        /// <returns>-1 = 不信任 | 0 = 永久信任 | 1 = 信任一次</returns>
        public int isUserTrusted(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            if (count("SELECT COUNT(*) from qq_trusts where qq like @qq ;", args) > 0)
            {
                List<int> vs = new List<int>
                {
                    1
                };
                List<List<string>> re = querysql("SELECT * from qq_trusts where qq like @qq ;", args, vs);
                return int.Parse(re[0][0]);
            }
            else
            {
                return -1;
            }
        }

        public int getUserTrustOperator(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            if (count("SELECT COUNT(*) from qq_trusts where qq like @qq ;", args) > 0)
            {
                List<int> vs = new List<int>
                {
                    2
                };
                List<List<string>> re = querysql("SELECT * from qq_trusts where qq like @qq ;", args, vs);
                return int.Parse(re[0][0]);
            }
            else
            {
                return -1;
            }
        }

        public List<int> listUnachievedCount()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1
            };
            List<List<string>> re = querysql("SELECT * from bili_fans WHERE done not like 1;", args, vs);
            List<int> group = new List<int>();
            foreach (List<string> line in re)
            {
                int gpn = int.Parse(line[0]);
                if (!group.Contains(gpn))
                {
                    group.Add(gpn);
                }
            }
            return group;
        }

        private Dictionary<string, object> bwords_tmp;

        public Dictionary<string, object> listBanWords()
        {
            if (bwords_tmp != null)
            {
                return bwords_tmp;
            }

            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1,
                2
            };
            List<List<string>> re = querysql("SELECT * from badwords;", args, vs);
            Dictionary<string, object> group = new Dictionary<string, object>();
            foreach (List<string> line in re)
            {
                if (!group.ContainsKey(line[0]))
                {
                    group.Add(line[0], line[1]);
                }
            }
            bwords_tmp = group;
            return group;
        }

        public Dictionary<int, long> listCrewBound()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                0,
                1
            };
            List<List<string>> re = querysql("SELECT * from bili_qqbound where qq is not null and type like 1;", args, vs);
            Dictionary<int, long> group = new Dictionary<int, long>();
            foreach (List<string> line in re)
            {
                group.Add(int.Parse(line[0]), long.Parse(line[1]));
            }
            return group;
        }

        public List<long> listCrewunBoundUID()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                0
            };
            List<List<string>> re = querysql("SELECT * from bili_qqbound where qq is null and type like 1;", args, vs);
            List<long> group = new List<long>();
            foreach (List<string> line in re)
            {
                group.Add(long.Parse(line[0]));
            }
            return group;
        }

        public List<CrewMember> listCrewMembers()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1,
                2,
                3,
                4,
                5
            };
            List<List<string>> re = querysql("SELECT * from bili_crew;", args, vs);
            List<CrewMember> group = new List<CrewMember>();
            foreach (List<string> line in re)
            {
                CrewMember c = new CrewMember()
                {
                    uid = long.Parse(line[0]),
                    len_days = int.Parse(line[1]) * 30,
                    buytime = DateTime.Parse(line[2]),
                    lid = int.Parse(line[4]),
                    level = int.Parse(line[3])
                };
                group.Add(c);
            }
            group.Sort((x, y) => x.buytime.CompareTo(y.buytime));
            return group;
        }

        public class CrewMember : IComparable
        {
            public long uid;
            public int len_days;
            public DateTime buytime;
            public int lid;
            public int level;
            public bool expired = false;
            public int days_left;
            public int CompareTo(object obj)
            {
                CrewMember other = (CrewMember)obj;
                return buytime.CompareTo(other.buytime);
            }
        }

        public bool isCountAlreadyRiched(int count)
        {
            List<int> ur_fancount = listUnachievedCount();
            return (ur_fancount[0] > count);
        }



        public int setCountReached(int count, long latest_fan)
        {
            List<int> ur_fancount = listUnachievedCount();
            int fcr = int.MaxValue;
            foreach (int fc in ur_fancount)
            {
                if (fc <= count)
                {
                    fcr = fc;
                }
                else
                {
                    break;
                }
            }
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@fanuid", latest_fan},
                { "@fc", fcr}
            };
            execsql("UPDATE bili_fans SET achieved_time = Now(), done = 1, latest_fan_uid = @fanuid WHERE fancount <= @fc and latest_fan_uid is null;", args);
            return fcr;
        }

        public bool removeUserBlklist(long q)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", q}
            };
            return execsql("delete from blacklist_q where qq = @qq;", args);
        }

        public List<long> listUser()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1
            };
            List<List<string>> re = querysql("SELECT * from userdata;", args, vs);
            List<long> group = new List<long>();
            foreach (List<string> line in re)
            {
                long gpn = long.Parse(line[0]);
                if (!group.Contains(gpn))
                {
                    group.Add(gpn);
                }
            }
            return group;
        }

        public List<KeyValuePair<string, object>> listBannedPic()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                0,
                1
            };
            List<List<string>> re = querysql("SELECT * from qq_bannedpictures;", args, vs);
            List<KeyValuePair<string, object>> group = new List<KeyValuePair<string, object>>();
            foreach (List<string> line in re)
            {
                KeyValuePair<string, object> data = new KeyValuePair<string, object>(line[0], line[1]);
                if (!group.Contains(data))
                {
                    group.Add(data);
                }
            }
            return group;
        }

        public bool banPick(string hash, string uuhash, string note)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@hash", hash },
                { "@note", note },
                { "@uuhash", uuhash }
            };
            return execsql("INSERT INTO qq_bannedpictures (hash, note, uuhash, eventtime) VALUES (@hash, @note, @uuhash, NOW());", args);
        }

        public List<string> listBiliveBanwords()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1
            };
            List<List<string>> re = querysql("SELECT * from live_banwords;", args, vs);
            List<string> group = new List<string>();
            foreach (List<string> line in re)
            {
                if (!group.Contains(line[0]))
                {
                    group.Add(line[0]);
                }
            }
            return group;
        }

        public List<long> listGroup()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1
            };
            List<List<string>> re = querysql("SELECT * from qq_groups;", args, vs);
            List<long> group = new List<long>();
            foreach (List<string> line in re)
            {
                long gpn = long.Parse(line[0]);
                if ((!group.Contains(gpn)) && !isAdminGroup(gpn))
                {
                    group.Add(gpn);
                }
            }
            return group;
        }

        public List<long> listAdminGroup()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1
            };
            List<List<string>> re = querysql("SELECT * from qq_admingroup;", args, vs);
            List<long> group = new List<long>();
            foreach (List<string> line in re)
            {
                long gpn = long.Parse(line[0]);
                if ((!group.Contains(gpn)))
                {
                    group.Add(gpn);
                }
            }
            return group;
        }

        /// <summary>
        /// 找出重复加群的人
        /// </summary>
        /// <returns>重复加群的人加的群</returns>
        public Dictionary<long, List<long>> findMeUser()
        {
            List<long> users = listUser();
            Dictionary<long, List<long>> ret = new Dictionary<long, List<long>>();
            foreach (long user in users)
            {
                if (DataBase.me.isUserOperator(user))
                {
                    continue;
                }

                List<long> groups = whichGroupsAreTheUserIn(user);
                if (groups.Count > 1)
                {
                    ret.Add(user, groups);
                }
            }
            return ret;
        }

        private readonly Dictionary<long, string> group_name_cache = new Dictionary<long, string>();

        public string getGroupName(long group)
        {
            if (group_name_cache.ContainsKey(group))
            {
                return group_name_cache[group];
            }

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@id", group}
            };
            List<int> vs = new List<int>
            {
                2
            };
            List<List<string>> re = DataBase.me.querysql("SELECT * from qq_groups where groupid like @id ;", args, vs);
            if (re == null || re.Count < 1 || re[0] == null || re[0].Count < 1 || re[0][0] == null)
            {
                return "UNDEFINED_IN_DATABASE";
            }

            group_name_cache.Add(group, re[0][0]);
            return re[0][0];
        }

        public bool recBLiveDanmaku(long uid, string message, int timestamp, int lid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid},
                { "@msg", message },
                { "@timestamp", timestamp},
                { "@lid", lid}
            };
            return execsql("INSERT INTO live_danmakurec (sender_uid, send_msg, raw_timestamp, lid) VALUES (@uid, @msg, @timestamp, @lid);", args);
        }

        public bool recBLive(int lid, string title)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@lid", lid},
                { "@title", title }
            };
            return execsql("INSERT INTO bili_lives (lid, livetime, livetitle) VALUES (@lid, Now(), @title);", args);
        }

        public bool recBLiveUpdate(int lid, int newviersers, int act_viewers, int peakviewers = 0, int selvercoins = 0, int goldcoins = 0)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@lid", lid},
                { "@nvs", newviersers},
                { "@pvs", peakviewers},
                { "@scn", selvercoins},
                { "@gcn", goldcoins},
                { "@act", act_viewers}
            };
            return execsql("UPDATE bili_lives SET newviewers = @nvs, peakviewers = @pvs, gold_coins = @gcn, selver_coins = @scn, activeviewers = @act WHERE lid = @lid;", args);
        }

        public void getBLiveData(int lid, out int newcomers, out int gcoins, out int scoins)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@lid", lid}
            };
            List<int> vs = new List<int>
            {
                4,
                6,
                7,
                8
            };
            List<List<string>> re = DataBase.me.querysql("SELECT * from bili_lives where lid like @lid;", args, vs);
            newcomers = int.Parse(re[0][0]);
            gcoins = int.Parse(re[0][1]);
            scoins = int.Parse(re[0][2]);
        }

        public bool recBLiveEnd(int lid, int newviersers, int act_viewers, int peakviewers = 0, int selvercoins = 0, int goldcoins = 0)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@lid", lid},
                { "@nvs", newviersers},
                { "@pvs", peakviewers},
                { "@scn", selvercoins},
                { "@gcn", goldcoins},
                { "@act", act_viewers}
            };
            return execsql("UPDATE bili_lives SET liveend = Now(), newviewers = @nvs, peakviewers = @pvs, gold_coins = @gcn, selver_coins = @scn, activeviewers = @act WHERE lid = @lid;", args);
        }

        public bool recBGift(int lid, long uid, string type, int amount, string name, int cost = 0)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@lid", lid},
                { "@uid", uid},
                { "@type", type },
                { "@amount", amount},
                { "@name", name}
            };
            return execsql("INSERT INTO bili_gift (lid, uid, type, amount, name, etime) VALUES (@lid, @uid, @type, @amount, @name, NOW());", args);
        }

        public bool recBLiveBan(int lid, long uid, int opuid = 0)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@lid", lid},
                { "@uid", uid},
                { "@op", opuid}
            };
            return execsql("INSERT INTO bili_bans (lid, uid, op, eventtime) VALUES (@lid, @uid, @op, NOW());", args);
        }

        public bool addBiliUser(long uid, string uname)
        {
            if (isBiliUserExist(uid))
            {
                return true;
            }

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid},
                { "@name", uname}
            };
            execsql("delete from bili_users where uid = @uid;", args);//清除这个成员(如果有)
            return execsql("INSERT INTO bili_users (uid, uname, addtime) VALUES (@uid, @name, NOW());", args);
        }

        private Dictionary<long, double> opweightmp = new Dictionary<long, double>();

        public double getOPWeigh(long qq, bool cache = true)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            List<int> vs = new List<int>
            {
                3
            };
            List<List<string>> re = DataBase.me.querysql("SELECT * from qq_operator where qq like @qq ;", args, vs);
            if (opweightmp.ContainsKey(qq))
            {
                if (cache) return opweightmp[qq];
                else
                    opweightmp[qq] = double.Parse(re[0][0]);
            }
            else opweightmp.Add(qq, double.Parse(re[0][0]));
            return double.Parse(re[0][0]);
        }

        public bool recQQWarn(long qq, long op, string note)
        {
            double weigh = 1;
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq},
                { "@op", op},
                { "@weigh", weigh},
                { "@note", note}
            };
            return execsql("INSERT INTO qq_warns (qq, operator, note, weigh, timestamp) VALUES (@qq, @op, @note, @weigh, NOW());", args);
        }

        public bool recQQWarn(long qq, long op, double weigh, string note)
        {
            double ww = weigh;
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq},
                { "@op", op},
                { "@weigh", ww},
                { "@note", note}
            };
            return execsql("INSERT INTO qq_warns (qq, operator, note, weigh, timestamp) VALUES (@qq, @op, @note, @weigh, NOW());", args);
        }

        public double getQQWarnCount(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq}
            };
            List<int> vs = new List<int>
            {
                2,
                5
            };
            List<List<string>> re = querysql("SELECT * from qq_warns where qq = @qq;", args, vs);
            double sum = 0;
            foreach (List<string> line in re)
            {
                sum += double.Parse(line[1]) * getOPWeigh(long.Parse(line[0])) * MainHolder.GLOBAL_WARN_WEIGHT;
            }
            return sum;
        }

        public List<Warn> listWarnsQQ(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                0,
                1,
                2,
                3,
                4,
                5
            };

            args.Add("@qq", qq.ToString());

            List<List<string>> re = querysql("SELECT * from qq_warns where qq = @qq;", args, vs);
            List<Warn> group = new List<Warn>();
            foreach (List<string> line in re)
            {
                Warn v = new Warn()
                {
                    qq = qq,
                    op = long.Parse(line[2]),
                    id = int.Parse(line[0]),
                    note = line[4],
                    time = DateTime.Parse(line[3])
                };
                group.Add(v);
            }
            return group;
        }

        public Warn getWarnByID(int id)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                0,
                1,
                2,
                3,
                4,
                5
            };

            args.Add("@id", id.ToString());

            List<List<string>> re = querysql("SELECT * from qq_warns where id = @id;", args, vs);
            foreach (List<string> line in re)
            {
                Warn v = new Warn()
                {
                    qq = long.Parse(line[1]),
                    op = long.Parse(line[2]),
                    id = int.Parse(line[0]),
                    note = line[4],
                    time = DateTime.Parse(line[3]),
                    weigh = double.Parse(line[5])
                };
                return v;
            }
            throw new Exception("未能找到该ID对应的警告数据");
        }

        /// <summary>
        /// 列出直播封禁
        /// </summary>
        /// <returns></returns>
        public List<int> listPermbans()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1
            };
            List<List<string>> re = querysql("SELECT * from bili_bans where op != -1;", args, vs);
            List<int> group = new List<int>();
            foreach (List<string> line in re)
            {
                group.Add(int.Parse(line[0]));
            }
            return group;
        }

        /// <summary>
        /// 登记直播标记
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="lid"></param>
        /// <param name="timeline"></param>
        /// <returns></returns>
        public string recLiveMark(long uid, int lid, int timeline)
        {
            string uuid = Guid.NewGuid().ToString();
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@uid", uid},
                { "@pointid", uuid },
                { "@lid", lid},
                { "@timeline", timeline},
                { "@pointtime", (timeline/60)},
            };
            execsql("INSERT INTO bili_livemarks (uid, pointid, lid, timeline, pointtime) VALUES (@uid, @pointid, @lid, @timeline, @pointtime);", args);
            return uuid;
        }

        /// <summary>
        /// 获得QQ等级缓存
        /// </summary>
        /// <param name="qq"></param>
        /// <returns></returns>
        public int getQQLevelTemp(long qq)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1,
                2
            };

            args.Add("@qq", qq.ToString());

            List<List<string>> re = querysql("SELECT * from qqlevel_tmp where qq = @qq;", args, vs);
            foreach (List<string> line in re)
            {
                var lastupdate = DateTime.Parse(line[1]);
                if ((DateTime.Now - lastupdate).Days < 1)
                    return int.Parse(line[0]);
            }
            return -1;
        }

        /// <summary>
        /// 写入QQ等级缓存
        /// </summary>
        /// <param name="qq"></param>
        /// <param name="level"></param>
        public void setQQLevelTemp(long qq, int level)
        {
            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@qq", qq},
                { "@level", level},
            };
            execsql("INSERT INTO qqlevel_tmp(qq,level,lastupdate) VALUE(@qq,@level,NOW()) ON DUPLICATE KEY UPDATE level= @level,lastupdate=NOW()", args);
        }

        /// <summary>
        /// 判断指定群是否应由机器人处理
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool IsGroupRelated(long group)
        {
            return (this.isAdminGroup(group) || this.isCrewGroup(group) ||
                this.isMEIgnoreGroup(group) || !this.getGroupName(group).Equals("UNDEFINED_IN_DATABASE"));
        }

        /// <summary>
        /// 获取指定UID的最后一段在舰时间
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public CrewLogItem GetLastestCrewspan(long uid)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                0,
                2,
                3
            };

            args.Add("@uid", uid.ToString());

            List<List<string>> re = querysql("SELECT * from bili_crewtimeline where uid = @uid ORDER BY id DESC LIMIT 0,1 ;", args, vs);
            if (re.Count == 0) return new CrewLogItem() { Duration = TimeSpan.Zero };
            var item = re.LastOrDefault();
            return new CrewLogItem
            {
                DataId = long.Parse(item[0]),
                Uid = uid,
                Start = DateTime.Parse(item[1]),
                End = DateTime.Parse(item[2])
            };
        }

        /// <summary>
        /// 更新舰长时间轴信息
        /// </summary>
        /// <param name="data"></param>
        public void WriteCrewspan(CrewLogItem data)
        {
            string command = "";
            Dictionary<string, ArgPack> args;
            if (data.DataId >= 0)
            {
                command =
                   "INSERT INTO bili_crewtimeline(id,start,end,duration) VALUE(@dataid,@start,@end,@duration) ON DUPLICATE KEY UPDATE start=@start, end=@end, duration=@duration;";
                args = new Dictionary<string, ArgPack>
                {
                    { "@dataid", new ArgPack{Type=MySqlDbType.Int32,Value=data.DataId} },
                    { "@start", new ArgPack{Type=MySqlDbType.DateTime,Value=(data.Start)} },
                    { "@end", new ArgPack{Type=MySqlDbType.DateTime,Value=(data.End)} },
                    { "@duration", new ArgPack{Type=MySqlDbType.Int32,Value=(data.Duration.TotalDays)} }
                };
            }
            else
            {
                command =
                   "INSERT INTO bili_crewtimeline(uid,start,end,duration) VALUE(@uid,@start,@end,@duration);";
                args = new Dictionary<string, ArgPack>
                {
                    { "@uid", new ArgPack{Type=MySqlDbType.Int64,Value=(data.Uid)} },
                    { "@start", new ArgPack{Type=MySqlDbType.DateTime,Value=(data.Start)} },
                    { "@end", new ArgPack{Type=MySqlDbType.DateTime,Value=(data.End)} },
                    { "@duration", new ArgPack{Type=MySqlDbType.Int32,Value=(data.Duration.TotalDays)} }
                };
            }
            execsql(command, args);
        }

        public List<CrewLevelLogItem> DumpCrewDataFromRedundancy()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            List<int> vs = new List<int>
            {
                1,
                2,
                3,
                4
            };
            List<List<string>> re = querysql("SELECT * from crewlog;", args, vs);
            List<CrewLevelLogItem> group = new List<CrewLevelLogItem>();
            foreach (List<string> line in re)
            {
                CrewLevelLogItem c = new CrewLevelLogItem()
                {
                    Uid = long.Parse(line[0]),
                    Level = int.Parse(line[1]),
                    Duration = TimeSpan.FromDays(int.Parse(line[2]) * 30),
                    Start = GetDateTime(long.Parse(line[3]))
                };
                group.Add(c);
            }
            return group;
        }

        public void ClearCrewTimelineData()
        {
            execsql("truncate table bili_crewtimeline;", new Dictionary<string, object>());
        }
        #endregion

        public static string DatetimeConvert(DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime GetDateTime(long strLongTime)
        {
            long begtime = strLongTime * 10000000;//100毫微秒为单位,textBox1.text需要转化的int日期
            DateTime dt_1970 = new DateTime(1970, 1, 1, 8, 0, 0);
            long tricks_1970 = dt_1970.Ticks;//1970年1月1日刻度
            long time_tricks = tricks_1970 + begtime;//日志日期刻度
            DateTime dt = new DateTime(time_tricks);//转化为DateTim
            return dt;
        }
    }
    public class Warn
    {
        public long qq, op;
        public int id;
        public string note;
        public DateTime time;
        public double weigh;
    }

}
