using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace QLogin
{
    public class QLogin
    {
        string js_ver;
        string pt_login_sig;
        string qrsig;
        QLoginStatus CurrentStatus = QLoginStatus.Offline;

        public int AppId, Daid;
        public string CallbackUrl;

        public CookieContainer MainCookies = new CookieContainer();
        Random random = new Random();

        public enum QLoginStatus
        {
            Online, Offline, Pending, QRExpired, Failed
        }
        public QLogin(int appid= 8000201,int daid=18,string callbackurl_urlencoded= "https%3A%2F%2Fvip.qq.com%2Floginsuccess.html")
        {
            AppId = appid;
            Daid = daid;
            CallbackUrl = callbackurl_urlencoded;
        }

        public int hash33(string key)
        {
            int h = 0;
            for (int i = 0; i < key.Length; i++)
            {
                h += (h << 5) + key[i];
            }
            return h & 2147483647;
        }

        public Bitmap getQR()
        {
            string stat1 = _get_with_cookies(
                "http://ui.ptlogin2.qq.com/cgi-bin/login?appid=8000201&daid=18&pt_no_auth=1&s_url=https%3A%2F%2Fvip.qq.com%2Floginsuccess.html",
                MainCookies);
            js_ver = Regex.Match(stat1, "ptui_version:encodeURIComponent\\(\"([0-9]*)\"\\)").Groups[1].Value;
            var qr = _get_image_with_cookies(
                "http://ptlogin2.qq.com/ptqrshow?appid=8000201&e=2&l=M&s=3&d=72&v=4&t=0." + random.Next(10000000, 99999999) + random.Next(10000000, 99999999) + "&daid=18",
                MainCookies);
            var collection = MainCookies.GetCookies(new Uri("https://ui.ptlogin2.qq.com/"));
            pt_login_sig = collection["pt_login_sig"].Value;
            qrsig = collection["qrsig"].Value;
            CurrentStatus = QLoginStatus.Pending;
            return qr;
        }

        public QLoginStatus CheckStatus()
        {
            string url = "https://ssl.ptlogin2.qq.com/ptqrlogin?" +
                "u1=https%3A%2F%2Fvip.qq.com%2Floginsuccess.html" +
                "&ptqrtoken=" + hash33(qrsig) + "& ptredirect=0&h=1&t=1&g=1&from_ui=1" +
                "&ptlang=2052&action=2-2-" + timelong() +
                "&js_ver=" + js_ver + "&js_type=1&login_sig=" + pt_login_sig +
                "&pt_uistyle=40&aid=8000201&daid=18&ptdrvs=7fRIHUtdVn*L6rtb4Sbtwj7iqWHop2yqlomOfknzYLmuMGdskWZJ-Sg8I3ruHDGW4Y8LlNCZs88_&sid=6602311080099795114" +
                "&has_onekey=1&";
            var data = _get_with_cookies(url, MainCookies);
            if (data.IndexOf("登录成功") >= 0)
            {
                CurrentStatus = QLoginStatus.Pending;
                string verifyurl = Regex.Match(data, "(http.*)'").Groups[1].Value;
                var verify = _get_with_cookies(verifyurl, MainCookies);
                CurrentStatus = QLoginStatus.Online;
            }
            else if (data.IndexOf("二维码未失效") >= 0)
            {
                CurrentStatus = QLoginStatus.Pending;
            }
            else if (data.IndexOf("二维码已失效") >= 0)
            {
                CurrentStatus = QLoginStatus.QRExpired;
            }
            else if (data.IndexOf("参数错误") >= 0)
            {
                CurrentStatus = QLoginStatus.Failed;
            }
            return CurrentStatus;
        }

        public string Serilize()
        {
            JArray jb = new JArray();
            var cookies = MainCookies.GetCookies(new Uri("https://ui.ptlogin2.qq.com/"));
            foreach (Cookie c in cookies)
            {
                JObject j = new JObject();
                j.Add("k", c.Name);
                j.Add("v", c.Value);
                j.Add("d", c.Domain);
                j.Add("p", c.Path);
                jb.Add(j);
            }
            return jb.ToString();
        }

        public void DeSerilize(string json)
        {
            JArray ja = JArray.Parse(json);
            foreach (JObject jb in ja)
            {
                MainCookies.Add(new Cookie(
                    jb.Value<string>("k"), jb.Value<string>("v"), jb.Value<string>("p"), jb.Value<string>("d")
                    ));
            }
        }

        public QLoginStatus Login()
        {
            while (true)
            {
                var status = CheckStatus();
                if(status != QLoginStatus.Pending)
                {
                    return status;
                }
                else
                {
                    Thread.Sleep(2000);
                }
            }
        }

        public long time() => DateTimeOffset.Now.ToUnixTimeSeconds();
        public long timelong() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public static string _get_with_cookies(string url, CookieContainer cookies)
        {
            try
            {
                string retString = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36";
                request.Headers.Add("Upgrade-Insecure-Requests", "1");
                request.CookieContainer = cookies;
                request.AllowAutoRedirect = true;
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

        public static Bitmap _get_image_with_cookies(string url, CookieContainer cookies)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36";
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.CookieContainer = cookies;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            Bitmap bmap = (Bitmap)Bitmap.FromStream(myResponseStream);
            myResponseStream.Close();
            return bmap;
        }
    }
}
