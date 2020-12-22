using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace tech.msgp.groupmanager.Code.BiliAPI
{
    //https://api.live.bilibili.com/xlive/web-room/v1/index/getInfoByRoom?room_id=2064239
    public class BiliLiveRoom
    {
        public int roomid;
        public int shortid;
        public string title;
        public string cover;
        public string[] tags;
        public string keyframe;
        public int lid;
        public short status;
        public static short STATUS_LIVE = 1;
        public static short STATUS_OFFLINE = 0;
        public static short STATUS_VEDIOPLAY = 2;
        public LiveManagement manage;

        public BiliLiveRoom(int roomid)
        {
            string data = ThirdPartAPIs._get("https://api.live.bilibili.com/xlive/web-room/v1/index/getInfoByRoom?room_id=" + roomid);
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            roomid = json["data"]["room_info"].Value<int>("room_id");
            shortid = json["data"]["room_info"].Value<int>("short_id");
            title = json["data"]["room_info"].Value<string>("title");
            cover = json["data"]["room_info"].Value<string>("cover");
            tags = json["data"]["room_info"].Value<string>("tags").Split(',');
            keyframe = json["data"]["room_info"].Value<string>("keyframe");
            status = json["data"]["room_info"].Value<short>("live_status");
            lid = json["data"]["room_info"].Value<int>("live_start_time");
            this.roomid = roomid;
            manage = new LiveManagement(this);
            //keyframe
        }

        public BiliLiveRoom(JObject json)
        {
            roomid = json["data"]["room_info"].Value<int>("room_id");
            shortid = json["data"]["room_info"].Value<int>("short_id");
            title = json["data"]["room_info"].Value<string>("title");
            cover = json["data"]["room_info"].Value<string>("cover");
            tags = json["data"]["room_info"].Value<string>("tags").Split(',');
            keyframe = json["data"]["room_info"].Value<string>("keyframe");
            status = json["data"]["room_info"].Value<short>("live_status");
            lid = json["data"]["room_info"].Value<int>("live_start_time");

            //keyframe
        }

        public static short getLiveStatus(int roomid)
        {
            string data = ThirdPartAPIs._get("https://api.live.bilibili.com/xlive/web-room/v1/index/getInfoByRoom?room_id=" + roomid);
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            return json["data"]["room_info"].Value<short>("live_status");
        }

        private DateTime lastsend_dmk;
        public bool sendDanmaku(string message, int fsize = 25, int color = 16777215, int bubble = 0)
        {
            if (lastsend_dmk != null)
            {
                while ((DateTime.Now - lastsend_dmk).TotalSeconds < 3)
                {
                    ;
                }
            }
            lastsend_dmk = DateTime.Now;
            Dictionary<string, string> kvs = new Dictionary<string, string>();
            CookieCollection ck = DataBase.me.getBiliLoginCookie().GetCookies(new Uri("https://www.bilibili.com"));
            JObject job = new JObject();
            kvs.Add("color", color.ToString());
            kvs.Add("fontsize", fsize.ToString());
            kvs.Add("mode", "1");
            kvs.Add("msg", message);
            kvs.Add("rnd", TimestampHandler.GetTimeStamp(DateTime.Now).ToString());
            kvs.Add("roomid", roomid.ToString());
            kvs.Add("bubble", bubble.ToString());
            kvs.Add("csrf_token", ck["bili_jct"].Value);
            kvs.Add("csrf", ck["bili_jct"].Value);
            string response = ThirdPartAPIs._post_with_cookies("https://api.live.bilibili.com/msg/send", kvs);
            if (response == "")
            {
                return false;
            }

            JObject raw_json = (JObject)JsonConvert.DeserializeObject(response);
            return raw_json.Value<int>("code") == 0;
        }
    }
}
