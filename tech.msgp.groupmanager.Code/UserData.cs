using System.Collections.Generic;

namespace tech.msgp.groupmanager.Code
{
    public class UserData
    {
        public long id;
        public string name;
        public int biliUid;
        public UserData(long qq)
        {
            Dictionary<string, string> args = new Dictionary<string, string>
            {
                { "@qq", qq.ToString() }
            };
            List<int> vs = new List<int>
            {
                2
            };
            List<List<string>> re = DataBase.me.querysql("SELECT * from userdata where qq like @qq ;", args, vs);
            id = qq;
            name = re[0][0];
            //biliUid = int.Parse(re[0][0]);
        }
    }
}
