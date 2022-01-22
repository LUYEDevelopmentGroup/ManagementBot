using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace tech.msgp.groupmanager.Code
{
    public partial class ThirdPartAPIs
    {
        public static string _get(string url)
        {
            try
            {
                string retString = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(myResponseStream);
                retString = streamReader.ReadToEnd();
                streamReader.Close();
                myResponseStream.Close();
                return retString;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string _get_gzip(string url)
        {
            try
            {
                string retString = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stm = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                StreamReader streamReader = new StreamReader(stm);
                retString = streamReader.ReadToEnd();
                streamReader.Close();
                stm.Close();
                return retString;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string _Jget(string url)
        {
            try
            {
                string retString = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json";
                request.UserAgent = "Java Client ; Minecarft PCL Launcher ; Manabot";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(myResponseStream);
                retString = streamReader.ReadToEnd();
                streamReader.Close();
                myResponseStream.Close();
                return retString;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string _get_with_cookies(string url, CookieContainer cookies)
        {
            try
            {
                string retString = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                //request.ContentType = "application/json";
                request.CookieContainer = cookies;
                //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(myResponseStream);
                retString = streamReader.ReadToEnd();
                streamReader.Close();
                myResponseStream.Close();
                return retString;
            }
            catch (Exception)
            {
                //HttpWebResponse response = (HttpWebResponse)ex.
                return "";
            }
        }

        public static string[] getTrustedSkinServer()
        {
            //skinDomains
            string res = _Jget("https://auth.api.microstorm.tech/");
            JObject j = (JObject)JsonConvert.DeserializeObject(res);
            JArray jarray = (JArray)j["skinDomains"];
            List<string> lst = new List<string>();
            foreach (JValue jb in jarray)
            {
                lst.Add(jb.Value<string>());
            }
            return lst.ToArray();
        }

        public static int getQQLevel(long qq, int retry = 0)
        {
            int level = DataBase.me.getQQLevelTemp(qq);
            if (level >= 0) return level;
            retry++;
            for (; retry > 0; retry--)
            {
                var data = _get_gzip("http://check.uomg.com/api/qq/qlevel?token=4c78dff53217b19f6c8e8c6628f9e6d6&qq=" + qq);
                JObject jb1 = (JObject)JsonConvert.DeserializeObject(data);
                var code = jb1.Value<int>("code");
                if (code == 200)
                {
                    level = jb1["data"].Value<int>("level");
                    DataBase.me.setQQLevelTemp(qq, level);
                    return level;
                }
            }
            return -1;
            /*
            try
            {
                CookieContainer cookies = new CookieContainer();
                CookieCollection col = MainHolder.session.get// MainHolder.cqapi.GetCookieCollection("https://h5.vip.qq.com");
                foreach (Cookie c in col)
                {
                    cookies.Add(new Uri("https://h5.vip.qq.com/"), c);
                }
                string r = _get_with_cookies("http://h5.vip.qq.com/p/mc/cardv2/other?_wv=1031&platform=1&qq=" + qq + "&adtag=geren&aid=mvip.pingtai.mobileqq.androidziliaoka.fromqita", cookies);
                int sindex = r.IndexOf("<p><small>LV</small>") + 20;
                string data = r.Substring(sindex);
                int eindex = data.IndexOf("</p>");
                data = data.Substring(0, eindex);
                return int.Parse(data);
            }
            catch
            {
                if (retry > 0) return getQQLevel(qq, retry - 1);
                else return -1;
            }
            */
        }

        public static string getMojangUUID(string uname)
        {
            try
            {
                string ww = _get("https://api.mojang.com/users/profiles/minecraft/" + uname);
                JObject jb1 = (JObject)JsonConvert.DeserializeObject(ww);
                return jb1.Value<string>("id");
            }
            catch
            {
                return null;
            }
        }
        public static string getNoSlashMCUUID(string uname, out bool isMojang)
        {
            string mojanguuid = getMojangUUID(uname);
            if (mojanguuid != null && mojanguuid.Length > 2)
            {
                isMojang = true;
                return mojanguuid;
            }
            isMojang = false;
            return Guid.NewGuid().ToString().Replace("-", "");
            //return _get("http://192.168.1.7:1551/api/uuid.php?uname=" + uname);
        }
    }
}
