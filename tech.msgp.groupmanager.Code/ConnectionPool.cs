using System.Collections.Generic;
using System.Threading;
using tech.msgp.groupmanager.Code.MCServer;

namespace tech.msgp.groupmanager.Code
{
    public static class ConnectionPool
    {
        private static readonly List<DataBase> MainDBPool = new List<DataBase>();
        private static readonly List<DBHandler> MCDBPool = new List<DBHandler>();
        public const int maindb_size = 10;
        public const int mcdb_size = 5;


        public static void initConnections(string addr, string user, string passwd, string mcaddr, string mcuser, string mcpasswd)
        {
            for (int i = MainDBPool.Count; i < maindb_size; i++)
            {
                MainDBPool.Add(new DataBase(addr, user, passwd));
            }
            for (int i = MCDBPool.Count; i < mcdb_size; i++)
            {
                MCDBPool.Add(new DBHandler(mcaddr,mcuser,mcpasswd));
            }
            new Thread(new ThreadStart(statchecker)).Start();
        }

        public static void statchecker()
        {
            while (true)
            {//维护连接的线程
                foreach (DataBase db in MainDBPool)
                {
                    if (db.busy)
                    {
                        continue;
                    }

                    if (!db.sql.State.Equals(System.Data.ConnectionState.Open))
                    {
                        db.connect();
                    }
                }
                foreach (DBHandler db in MCDBPool)
                {
                    if (db.busy)
                    {
                        continue;
                    }

                    if (!db.sql.State.Equals(System.Data.ConnectionState.Open))
                    {
                        db.connect();
                    }
                }
                Thread.Sleep(500);
            }
        }

        public static DataBase getMainConnection()
        {
            do
            {
                foreach (DataBase db in MainDBPool)
                {
                    if (!db.busy)
                    {
                        return db;
                    }
                }
                Thread.Sleep(100);
            } while (true);//排队
        }

        public static DBHandler getMCDBConnection()
        {
            do
            {
                foreach (DBHandler db in MCDBPool)
                {
                    if (!db.busy)
                    {
                        return db;
                    }
                }
            } while (true);//排队
        }
    }
}
