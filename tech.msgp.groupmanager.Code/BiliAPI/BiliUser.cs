using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace tech.msgp.groupmanager.Code.BiliAPI
{
    public class BiliUser
    {
        public static Dictionary<int, BiliUser> userlist;
        public int uid { get; private set; }
        public string name { get; private set; }
        public string sex { get; private set; }
        public string sign { get; private set; }
        public bool fans_badge { get; private set; }
        public int coins { get; private set; }
        public string face { get; private set; }
        public int level { get; private set; }
        public int rank { get; private set; }
        public int fans { get; private set; }
        public JObject raw_json { get; private set; }

        public static BiliUser getUser(int uid)
        {
            if (userlist.ContainsKey(uid))
            {
                return userlist[uid];
            }

            return new BiliUser(uid);
        }

        /// <summary>
        /// 手动数据初始化
        /// 尽量别用！！
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="name"></param>
        /// <param name="sex"></param>
        /// <param name="sign"></param>
        /// <param name="fans_badge"></param>
        /// <param name="coins"></param>
        /// <param name="face"></param>
        /// <param name="level"></param>
        /// <param name="rank"></param>
        public BiliUser(int uid, string name, string sex, string sign, bool fans_badge, int coins, string face, int level, int rank)
        {
            this.uid = uid;
            this.name = name;
            this.sex = sex;
            this.sign = sign;
            this.fans_badge = fans_badge;
            this.coins = coins;
            this.face = face;
            this.level = level;
            this.rank = rank;
            if (userlist.ContainsKey(uid))
            {
                userlist.Remove(uid);
            }

            userlist.Add(uid, this);
        }

        /// <summary>
        /// 从UID创建数据
        /// 系统会自己去抓数据
        /// </summary>
        /// <param name="uid"></param>
        public BiliUser(int uid, bool nocache = false)
        {
            try
            {
                raw_json = (JObject)JsonConvert.DeserializeObject(ThirdPartAPIs.getBiliUserInfoJson(uid));
                this.uid = int.Parse(raw_json["data"]["mid"].ToString());
                name = raw_json["data"]["name"].ToString();
                sex = raw_json["data"]["sex"].ToString();
                sign = raw_json["data"]["sign"].ToString();
                rank = int.Parse(raw_json["data"]["rank"].ToString());
                level = int.Parse(raw_json["data"]["level"].ToString());
                coins = int.Parse(raw_json["data"]["coins"].ToString());
                face = raw_json["data"]["face"].ToString();
                fans_badge = raw_json["data"]["fans_badge"].ToString() != "false";
            }
            catch
            {
                this.uid = uid;
            }
            if (userlist.ContainsKey(uid))
            {
                userlist.Remove(uid);
            }

            userlist.Add(uid, this);
        }

        /// <summary>
        /// 从已有json字符串实例化
        /// </summary>
        /// <param name="json"></param>
        public BiliUser(string json)
        {
            raw_json = (JObject)JsonConvert.DeserializeObject(json);
            if (raw_json["data"] == null)
            {
                uid = int.Parse(raw_json["mid"].ToString());
                name = raw_json["name"].ToString();
                sex = raw_json["sex"].ToString();
                sign = raw_json["sign"].ToString();
                rank = int.Parse(raw_json["rank"].ToString());
                level = int.Parse(raw_json["level_info"]["current_level"].ToString());
                face = raw_json["face"].ToString();
                fans = raw_json.Value<int>("fans");
            }
            else
            {
                uid = int.Parse(raw_json["data"]["mid"].ToString());
                name = raw_json["data"]["name"].ToString();
                sex = raw_json["data"]["sex"].ToString();
                sign = raw_json["data"]["sign"].ToString();
                rank = int.Parse(raw_json["data"]["rank"].ToString());
                level = int.Parse(raw_json["data"]["level"].ToString());
                coins = int.Parse(raw_json["data"]["coins"].ToString());
                face = raw_json["data"]["face"].ToString();
                fans_badge = raw_json["data"]["fans_badge"].ToString() != "false";
            }
            if (userlist.ContainsKey(uid))
            {
                userlist.Remove(uid);
            }

            userlist.Add(uid, this);
        }

        /// <summary>
        /// 从Json对象初始化
        /// </summary>
        /// <param name="json"></param>
        public BiliUser(JObject json)
        {
            raw_json = json;
            uid = int.Parse(raw_json["data"]["mid"].ToString());
            name = raw_json["data"]["name"].ToString();
            sex = raw_json["data"]["sex"].ToString();
            sign = raw_json["data"]["sign"].ToString();
            rank = int.Parse(raw_json["data"]["rank"].ToString());
            level = int.Parse(raw_json["data"]["level"].ToString());
            coins = int.Parse(raw_json["data"]["coins"].ToString());
            face = raw_json["data"]["face"].ToString();
            fans_badge = raw_json["data"]["fans_badge"].ToString() != "false";
            if (userlist.ContainsKey(uid))
            {
                userlist.Remove(uid);
            }

            userlist.Add(uid, this);
        }

        public override bool Equals(object obj)
        {
            try
            {
                BiliUser o = (BiliUser)obj;
                return uid.Equals(o.uid);
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return uid.GetHashCode();
        }
    }
}
