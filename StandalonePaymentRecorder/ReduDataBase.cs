using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandalonePaymentRecorder
{
    class ReduDataBase
    {
        private string sqladdr;
        private string sqluser;
        private string sqlpawd;

        public bool busy = false;
        public MySqlConnection sql;
        public bool connected = false;

        #region 底层封装
        public ReduDataBase(string addr, string user, string passwd)
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
            string conStr = "server=" + sqladdr + ";port=3306;user=" + sqluser + ";password=\"" + sqlpawd + "\"; database=luye_redundancy;Allow User Variables=True";
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
                Console.WriteLine("数据库 到数据库的连接丢失，试图重连");
                if (!connect())
                {
                    Console.WriteLine("数据库 无法连接到数据库，查询丢失");
                    return false;
                }
                else
                {
                    Console.WriteLine("数据库 连接已建立");
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        public bool execsql(string cmd_, Dictionary<string, string> args)
        {
            busy = true;
            lock (sql)
            {
                checkconnection();
                try
                {
                    Console.WriteLine(cmd_);
                    using (MySqlCommand cmd = new MySqlCommand(cmd_, sql))
                    {
                        foreach (KeyValuePair<string, string> arg in args)
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
                    Console.WriteLine(e.Message);
                    connected = false;
                    busy = false;
                    return false;
                }
            }
        }


        public bool execsql(string cmd_, Dictionary<string, string> args, out int rolls)
        {
            busy = true;
            lock (sql)
            {
                checkconnection();
                try
                {
                    Console.WriteLine(cmd_);
                    using (MySqlCommand cmd = new MySqlCommand(cmd_, sql))
                    {
                        foreach (KeyValuePair<string, string> arg in args)
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
                    Console.WriteLine(e.Message);
                    connected = false;
                    busy = false;
                    rolls = 0;
                    return false;
                }
            }
        }


        public int execsql_firstmatch(string sqlc, Dictionary<string, string> args)
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
                        foreach (KeyValuePair<string, string> arg in args)
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
                    Console.WriteLine(e.Message);
                    connected = false;
                    busy = false;
                    return -1;
                }
            }
        }

        public List<List<string>> querysql(string cmd_, Dictionary<string, string> args, List<int> rolls)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(cmd_, sql))
                {
                    busy = true;
                    lock (sql)
                    {
                        checkconnection();
                        foreach (KeyValuePair<string, string> arg in args)
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
                Console.WriteLine(e.Message);
                connected = false;
                return null;
            }
        }

        public int count(string sql, Dictionary<string, string> args)
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
        public bool logDMK(long timestamp, string json, long lid = -1, string eventid = "")
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@timestamp", timestamp.ToString() },
                { "@json", json },
                { "@liveid", lid.ToString() },
                { "@eventid", eventid }
            };
            return execsql("INSERT INTO rawdmk (timestamp, json, liveid, eventid) VALUES (@timestamp, @json, @liveid, @eventid);", args);
        }

        public bool logCREW(long uid, int type, int len, long timestamp,string eventid)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@uid", uid.ToString() },
                { "@type", type.ToString() },
                { "@len", len.ToString() },
                { "@timestamp", timestamp.ToString() },
                { "@eventid", eventid }
            };
            return execsql("INSERT INTO crewlog (uid, type, len, timestamp, eventid) VALUES (@uid, @type, @len, @timestamp, @eventid);", args);
        }

        public bool log(string msg)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@msg", msg }
            };
            return execsql("INSERT INTO log (time, msg) VALUES (NOW(), @msg);", args);
        }

        public bool logErr(string msg)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@msg", msg }
            };
            return execsql("INSERT INTO errtrace (time, msg) VALUES (NOW(), @msg);", args);
        }
    }
}
