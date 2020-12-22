using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace DiscordAPI
{
    //
    public class WebAPI
    {
        private readonly List<string> postlist;
        private readonly string webhook;
        public WebAPI(string webhook)
        {
            this.webhook = webhook;
            postlist = new List<string>();
        }
        /// <summary>
        /// 发送post请求
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">发送的json</param>
        /// <returns></returns>
        public void _post(string postData)
        {
            lock ("discord_send")
            {
                postlist.Add(postData);
            }
        }

        public void _PROC()
        {
            List<string> tmp;
            lock ("discord_send")
            {
                tmp = new List<string>(postlist);
                postlist.Clear();
            }
            foreach (string json in tmp)
            {
                try
                {
                    string result = "";
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(webhook);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.Timeout = 5000;
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    req.ContentLength = data.Length;
                    using (Stream reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(data, 0, data.Length);
                        reqStream.Close();
                    }
                    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                    Stream stream = resp.GetResponseStream();
                    //获取响应内容
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        result = reader.ReadToEnd();
                    }
                }
                catch { }
                Thread.Sleep(2000);
            }
        }

        public void sendText(string title, string message)
        {
            JObject json = new JObject
            {
                { "username", title },
                { "content", message }
            };
            _post(json.ToString());
        }

        public void sendPicture(string title, string url)
        {
            JObject json = new JObject
            {
                { "username", title },
                { "file", url }
            };
            _post(json.ToString());
        }

        public void sendTTSText(string title, string message)
        {
            JObject json = new JObject
            {
                { "username", title },
                { "content", message },
                { "tts", true }
            };
            _post(json.ToString());
        }
    }
}
