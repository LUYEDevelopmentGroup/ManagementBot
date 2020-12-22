using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace tech.msgp.groupmanager.Code.BiliAPI
{
    //使用API：
    //https://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/space_history?host_uid=5659864&offset_dynamic_id=0&need_top=1
    public class BiliSpaceDynamic
    {
        private readonly int uid = -1;
        private List<Dyncard> dyns = new List<Dyncard>();
        private readonly List<long> checked_ids = new List<long>();
        public BiliSpaceDynamic(int uid)
        {
            this.uid = uid;
        }

        public bool refresh()
        {
            return refresh(getLatest());
        }

        public bool refresh(List<Dyncard> rawdata)
        {
            try
            {
                List<Dyncard> d = new List<Dyncard>(rawdata);
                d.AddRange(dyns);
                dyns = d;
                while (dyns.Count > 5)
                {
                    dyns.RemoveAt(5);
                }
                while (checked_ids.Count > 20)
                {
                    dyns.RemoveAt(0);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<Dyncard> fetchDynamics()
        {
            List<Dyncard> dynss = new List<Dyncard>();
            //try
            {
                string jstring = ThirdPartAPIs.getBiliUserDynamicJson(uid);
                JObject jb = (JObject)JsonConvert.DeserializeObject(jstring);
                if (jb == null || jb["data"] == null || jb["data"]["cards"] == null)
                {
                    return new List<Dyncard>();
                }
                foreach (JToken jobj in jb["data"]["cards"])
                {
                    try
                    {
                        JObject j = (JObject)jobj;
                        JObject card = (JObject)JsonConvert.DeserializeObject(j["card"].ToString());
                        Dyncard dyn = new Dyncard
                        {
                            dynid = j["desc"].Value<long>("dynamic_id"),
                            sendtime = TimestampHandler.GetDateTime(j["desc"].Value<long>("timestamp")),
                            type = j["desc"].Value<int>("type"),
                            view = j["desc"].Value<int>("view"),
                            repost = j["desc"].Value<int>("repost"),
                            like = j["desc"].Value<int>("like"),
                            rid = j["desc"].Value<long>("rid")
                        };
                        if (j["desc"]["user_profile"]["info"]["uname"] != null)//如果给了更多信息，就直接用上
                        {
                            dyn.sender = new BiliUser(j["desc"]["user_profile"]["info"].Value<int>("uid"),
                                j["desc"]["user_profile"]["info"].Value<string>("uname"),
                                "未知",
                                j["desc"]["user_profile"].Value<string>("sign"),
                                false,
                                0,
                                j["desc"]["user_profile"]["info"].Value<string>("face"),
                                j["desc"]["user_profile"]["level_info"].Value<int>("current_level"),
                                0);
                        }
                        else//如果没有信息就从缓存抓取
                        if (BiliUser.userlist.ContainsKey(j["desc"]["user_profile"]["info"].Value<int>("uid")))
                        {
                            dyn.sender = BiliUser.getUser(j["desc"]["user_profile"]["info"].Value<int>("uid"));
                            //使用用户数据缓存来提高速度
                            //因为监听的是同一个账号，所以缓存命中率超高
                        }
                        else//如果缓存未命中，就拿获得的UID抓取剩余信息
                        {
                            dyn.sender = BiliUser.getUser(j["desc"]["user_profile"]["info"].Value<int>("uid"));
                        }

                        if (j["desc"]["orig_type"] != null)
                        {
                            dyn.origintype = j["desc"].Value<int>("orig_type");
                        }
                        if (card["origin"] != null)
                        {
                            dyn.card_origin = (JObject)JsonConvert.DeserializeObject(card["origin"].ToString());
                        }
                        switch (dyn.type)
                        {
                            case 1://普通动态
                            case 4://？出现在转发和普通动态
                                dyn.dynamic = card["item"].Value<string>("content");
                                if (dyn.dynamic.Length > 23)
                                {
                                    dyn.short_dynamic = dyn.dynamic.Substring(0, 20) + "...";
                                }
                                else
                                {
                                    dyn.short_dynamic = dyn.dynamic;
                                }

                                break;
                            case 2://图片
                                dyn.dynamic = card["item"].Value<string>("description");
                                if (dyn.dynamic.Length > 23)
                                {
                                    dyn.short_dynamic = dyn.dynamic.Substring(0, 20) + "...";
                                }
                                else
                                {
                                    dyn.short_dynamic = dyn.dynamic;
                                }

                                break;
                            case 256://音频
                                break;
                            case 8://视频
                                dyn.vinfo = new Videoinfo
                                {
                                    bvid = j["desc"].Value<string>("bvid"),
                                    title = card.Value<string>("title"),
                                    discription = card.Value<string>("desc")
                                };
                                if (dyn.vinfo.discription.Length > 23)
                                {
                                    dyn.vinfo.short_discription = dyn.vinfo.discription.Substring(0, 20) + "...";
                                }
                                else
                                {
                                    dyn.vinfo.short_discription = dyn.vinfo.discription;
                                }

                                dyn.vinfo.av = j["desc"].Value<int>("rid");
                                dyn.dynamic = card.Value<string>("dynamic");
                                break;
                            default:
                                break;
                        }
                        dynss.Add(dyn);
                    }
                    catch (Exception err)
                    {
                        MainHolder.broadcaster.SendToAnEgg("[Exception]\n[动态抓取]" + err.Message + "\n\n堆栈跟踪：\n" + err.StackTrace);
                        MainHolder.broadcaster.SendToAnEgg("[Exception]\n试图处理的信息：" + jobj.ToString());
                    }
                }
                while (dynss.Count > 5)
                {
                    dynss.RemoveAt(5);
                }
                return dynss;
            }
            //catch
            {
#pragma warning disable CS0162 // 检测到无法访问的代码
                return null;
#pragma warning restore CS0162 // 检测到无法访问的代码
            }
        }

        public List<Dyncard> getLatest()
        {
            List<Dyncard> fetched = fetchDynamics();
            List<Dyncard> latese = new List<Dyncard>();
            foreach (Dyncard dync in fetched)
            {
                if (dyns.Contains(dync))
                {
                    continue;
                }

                if (checked_ids.Contains(dync.dynid))
                {
                    continue;
                }

                latese.Add(dync);
                checked_ids.Add(dync.dynid);
            }
            return latese;
        }
    }

    public struct Dyncard
    {
        public long dynid, rid;
        public int type, view, repost, like, origintype;
        public string dynamic;
        public string short_dynamic;
        public BiliUser sender;
        public DateTime sendtime;
        public Videoinfo vinfo;
        public JObject card_origin;
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is Dyncard)
            {
                Dyncard b = (Dyncard)obj;
                return dynid == b.dynid;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            DateTime dt_1970 = new DateTime(1970, 1, 1, 8, 0, 0);
            return (int)(sendtime - dt_1970).TotalSeconds;
        }
    }

    public struct Videoinfo
    {
        public string discription;
        public string short_discription;
        public string bvid;
        public string title;
        public int av;
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is Videoinfo)
            {
                Videoinfo b = (Videoinfo)obj;
                return bvid == b.bvid;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return av;
        }
    }
}
