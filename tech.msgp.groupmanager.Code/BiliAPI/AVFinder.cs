using System;
using System.Net;
using System.Text.RegularExpressions;

namespace tech.msgp.groupmanager.Code.BiliAPI
{
    internal class AVFinder
    {
        public static string _getLocation(string url)
        {
            string retString = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.AllowAutoRedirect = false;
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException err)
                {
                    HttpWebResponse response = (HttpWebResponse)err.Response;
                    if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.MovedPermanently)
                    {
                        return response.GetResponseHeader("Location");
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string bvFromB23url(string url)
        {
            return bvFromPlayURL(_getLocation(url));
        }

        public static string bvFromPlayURL(string uurl)
        {
            try
            {
                int ind = uurl.IndexOf("/video/") + 7;
                uurl = uurl.Substring(ind);
                ind = uurl.IndexOf("/");
                if (ind < 0)
                {
                    ind = uurl.IndexOf("?");
                }

                if (ind < 0)
                {
                    ind = uurl.Length;
                }

                uurl = uurl.Substring(0, ind);
                return uurl;
            }
            catch
            {
                return null;
            }
        }

        public static string UrlFromString(string input)
        {
            Regex wordOnly = new Regex("(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
            return wordOnly.Match(input).Value;
        }

        public static string bvFromString(string input)
        {
            string identifier = "www.bilibili.com/video/";
            int uustart = input.IndexOf(identifier);
            string id = "";
            if (uustart >= 0)//是个B站网址
            {
                id = input.Substring(uustart + identifier.Length).Split('?')[0];
            }
            else
            {
                int avstart = identifier.ToUpper().IndexOf("AV");
                if (avstart < 0)
                {
                    avstart = identifier.ToUpper().IndexOf("BV");
                }
                if (avstart >= 0)
                {
                    id = input.Substring(avstart).Split(' ')[0];
                }
            }
            if (id.Length < 3)
            {
                return null;
            }

            string uuid = id.Substring(2);//去除"AV"或"BV"
            switch (id.Substring(0, 2).ToUpper())
            {
                case "AV"://AV号的话验证一下是不是数字
                    int avn;
                    bool succeed = int.TryParse(uuid, out avn);
                    if (!succeed)
                    {
                        return null;
                    }

                    return avn.ToString();
                case "BV":
                    return id.Substring(2);
                default://不是AV也不是BV
                    return null;
            }
#pragma warning disable CS0162 // 检测到无法访问的代码
            return null;
#pragma warning restore CS0162 // 检测到无法访问的代码
        }
    }
}
