using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace tech.msgp.groupmanager.Code.MCServer
{
    internal class SkinHandler
    {
        public long timestamp;
        public string profileId, profileName;
        public List<Texture> textures;
        /*
            {
                "timestamp":该属性值被生成时的时间戳（Java 时间戳格式，即自 1970-01-01 00:00:00 UTC 至今经过的毫秒数）,
                "profileId":"角色 UUID（无符号）",
                "profileName":"角色名称",
                "textures":{ // 角色的材质
                    "材质类型（如 SKIN）":{ // 若角色不具有该项材质，则不必包含
                        "url":"材质的 URL",
                        "metadata":{ // 材质的元数据，若没有则不必包含
                            "键":"值"
                            // ,...（可以有更多）
                        }
                    }
                    // ,...（可以有更多）
                }
            }
         */
        public SkinHandler(string puuid, string pname, List<Texture> textures)
        {
            timestamp = GetTimeStamp();
            profileId = puuid;
            profileName = pname;
            this.textures = textures;
        }

        public override string ToString()
        {
            JObject jb = new JObject
            {
                { "timestamp", timestamp },
                { "profileId", profileId },
                { "profileName", profileName }
            };
            JObject ts = new JObject();
            foreach (Texture t in textures)
            {
                ts.Add(t.type, t.ToJson());
            }
            jb.Add("textures", ts);
            return jb.ToString();
        }

        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (long)ts.TotalMilliseconds;
        }

        public static string getPictureHash(Image i)
        {
            StringBuilder str = new StringBuilder();
            Bitmap image = new Bitmap(i);
            int w = image.Width;
            int h = image.Height;
            str.Append(w.ToString() + h.ToString());
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    System.Drawing.Color pixcolor = image.GetPixel(x, y);
                    str.Append(pixcolor.A);
                    str.Append(pixcolor.R);
                    str.Append(pixcolor.G);
                    str.Append(pixcolor.B);
                }
            }
            return SHA256(str.ToString());
        }

        public static string SHA256(string str)
        {
            //如果str有中文，不同Encoding的sha是不同的！！
            byte[] SHA256Data = Encoding.UTF8.GetBytes(str);

            System.Security.Cryptography.SHA256Managed Sha256 = new System.Security.Cryptography.SHA256Managed();
            byte[] by = Sha256.ComputeHash(SHA256Data);

            return BitConverter.ToString(by).Replace("-", "").ToLower(); //64
                                                                         //return Convert.ToBase64String(by);                         //44
        }

        public static Image getImageFromWeb(string url)
        {
            using (Stream fs = WebRequest.Create(url).GetResponse().GetResponseStream())
            {
                System.Drawing.Image image = System.Drawing.Image.FromStream(fs);
                return image;
            }
        }

        public static bool checkPick(Image image)
        {
            double whratio = (image.Width / (double)image.Height);
            List<double> alloedratio = new List<double>
            { 64.0 / 32.0,
                64.0 / 64.0
            };
            return alloedratio.Contains(whratio);
        }
    }

    internal class Texture
    {
        public string url;
        public Dictionary<string, string> metadata;
        public string type;
        public Texture(string url, string type, Dictionary<string, string> metadata)
        {
            this.url = url;
            this.type = type;
            this.metadata = metadata;
        }
        public override string ToString()
        {
            JObject jb = new JObject
            {
                { "url", url }
            };
            JObject meta = new JObject();
            foreach (KeyValuePair<string, string> kvp in metadata)
            {
                meta.Add(kvp.Key, kvp.Value);
            }
            jb.Add("metadata", meta);
            return jb.ToString();
        }
        public JObject ToJson()
        {
            JObject jb = new JObject
            {
                { "url", url }
            };
            JObject meta = new JObject();
            foreach (KeyValuePair<string, string> kvp in metadata)
            {
                meta.Add(kvp.Key, kvp.Value);
            }
            jb.Add("metadata", meta);
            return jb;
        }
    }


}
