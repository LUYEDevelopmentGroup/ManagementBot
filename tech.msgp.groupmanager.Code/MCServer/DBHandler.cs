using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace tech.msgp.groupmanager.Code.MCServer
{
    public class DBHandler
    {
        private string sqladdr;
        private string sqluser;
        private string sqlpawd;

        public static DBHandler me => ConnectionPool.getMCDBConnection();
        public MySqlConnection sql;
        public bool connected = false;

        public bool busy = false;

        #region 底层封装
        public DBHandler(string addr,string user,string passwd)
        {
            sqladdr = addr;
            sqluser = user;
            sqlpawd = passwd;
            connect();
        }

        public static string genNoSlashUUID(string id, out bool mojang)
        {
            return ThirdPartAPIs.getNoSlashMCUUID(id, out mojang);
        }

        public static string genSlashUUID(string id)
        {
            string no_slash = genNoSlashUUID(id, out bool mojang);
            StringBuilder sb = new StringBuilder();
            sb.Append(substr(no_slash, 0, 8) + "-");
            sb.Append(substr(no_slash, 8, 4) + "-");
            sb.Append(substr(no_slash, 12, 4) + "-");
            sb.Append(substr(no_slash, 16, 4) + "-");
            sb.Append(substr(no_slash, 20));
            return sb.ToString();
        }


        public static string substr(string str, int start, int len)
        {
            return str.Substring(start, len);
        }
        public static string substr(string str, int start)
        {
            return str.Substring(start);
        }

        public static int ord(int input)
        {
            byte[] array = new byte[1];
            array = System.Text.Encoding.ASCII.GetBytes(input.ToString());
            int asciicode = array[0];
            return asciicode;
        }

        public static char chr(int input)
        {
            byte[] array = new byte[1];
            array[0] = (byte)(Convert.ToInt32(input));
            return Convert.ToString(System.Text.Encoding.ASCII.GetString(array))[0];
        }

        private static int[] hex2bin(string hexString)
        {
            List<int> ress = new List<int>();
            foreach (char c in hexString)
            {
                int v = Convert.ToInt32(c.ToString(), 16);
                int v2 = Convert.ToInt32(c.ToString(), 16);
                ress.Add(v2);
            }
            return ress.ToArray();
        }

        private static string bin2hex(int[] bin)
        {
            string rt = "";
            foreach (int i in bin)
            {
                rt += Convert.ToString(i, 16);
            }
            return rt;
        }

        public static string md5(string stringToHash)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] emailBytes = Encoding.UTF8.GetBytes(stringToHash);
            byte[] hashedEmailBytes = md5.ComputeHash(emailBytes);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashedEmailBytes)
            {
                sb.Append(b.ToString("x2").ToLower());
            }
            return sb.ToString();
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
            string conStr = "server=" + sqladdr + ";port=3306;user=" + sqluser + ";password=\"" + sqlpawd + "\"; database=minecraft;Allow User Variables=True";
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

        public bool execsql(string cmd_, Dictionary<string, string> args)
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
                    MainHolder.Logger.Error("数据库", e.Message);
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
                    MainHolder.Logger.Debug("sql", cmd_);
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
                    MainHolder.Logger.Error("数据库", e.Message);
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
                    MainHolder.Logger.Error("数据库", e.Message);
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
                MainHolder.Logger.Error("数据库", e.Message);
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

        public bool isRegistered(long qq)
        {
            string username = qq + "@qq.com";
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@user", username }
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return count("SELECT COUNT(*) from Users where username like @user ;", args) > 0;
        }

        public bool isNameTaken(string pname)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@name", pname }
            };
            //SELECT * from (SELECT * FROM A ORDER BY time) a GROUP BY a.id;
            return count("SELECT COUNT(*) from Profiles where name like @name ;", args) > 0;
        }

        public bool addUser(long qq, string passwd)
        {
            string username = qq + "@qq.com";
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@user", username },
                { "@passwd", CrewKeyProcessor.Sha1(username + passwd + "THIS IS SAULT").Replace("-", "") },
                { "@uuid", genNoSlashUUID(username, out bool mojang) }
            };
            return execsql("INSERT INTO Users (userid, username, passwd, regtime) VALUES (@uuid, @user, @passwd, NOW());", args);
        }

        public bool addProfile(string pname, long qq, out bool mojang)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@uuid", genNoSlashUUID(pname, out mojang) },
                { "@suuid", genSlashUUID(pname) },
                { "@name", pname },
                { "@owneruuid", getUserUUID(qq) }
            };
            return execsql("INSERT INTO Profiles (cuuid, suuid, name, owner) VALUES (@uuid, @suuid, @name, @owneruuid);", args);
        }

        /// <summary>
        /// 设置Profile皮肤
        /// </summary>
        /// <param name="filename">皮肤图片文件名</param>
        /// <param name="puuid">Profile的无划线UUID</param>
        /// <returns></returns>
        /*
        public bool setSkin(string filename,string puuid)
        {
            //TODO
        }
        */
        public string getUserUUID(long qq)
        {
            string username = qq + "@qq.com";
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@user", username }
            };
            List<int> vs = new List<int>
            {
                0
            };
            List<List<string>> re = querysql("SELECT * from Users where username like @user ;", args, vs);
            return re[0][0];
        }

        public bool changeProfileUUID(string profileuuid, string new_unslashed, string new_slashed)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@cuuid", profileuuid },
                { "@data", new_unslashed },
                { "@datasl", new_slashed }
            };
            return execsql("UPDATE Profiles SET cuuid = @data, suuid = @datasl where cuuid like @cuuid ;", args);
        }

        public string getUserOwnedProfileUUID(long qq)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@oname", getUserUUID(qq) }
            };
            List<int> vs = new List<int>
            {
                0
            };
            List<List<string>> re = querysql("SELECT * from Profiles where owner = @oname limit 1;", args, vs);
            return re[0][0];
        }

        public bool setProfileUUIDTexture(string UUID, string texturedata)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@uuid", UUID },
                { "@data", texturedata }
            };
            return execsql("UPDATE Profiles SET texturedata = @data WHERE cuuid = @uuid;", args);
        }

        public string getProfileUUIDTexture(string UUID)
        {
            try
            {
                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    { "@uuid", UUID }
                };
                List<int> vs = new List<int>
                {
                    3
                };
                List<List<string>> re = querysql("SELECT * from Profiles where cuuid = @uuid limit 1;", args, vs);
                return re[0][0];
            }
            catch
            {
                return null;
            }
        }

        public string getUserOwnedProfileName(long qq)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@oname", getUserUUID(qq) }
            };
            List<int> vs = new List<int>
            {
                2
            };
            List<List<string>> re = querysql("SELECT * from Profiles where owner = @oname limit 1;", args, vs);
            return re[0][0];
        }

    }
}
