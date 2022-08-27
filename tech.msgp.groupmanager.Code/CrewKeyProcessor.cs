using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Cryptography;
using System.Text;

namespace tech.msgp.groupmanager.Code
{
    internal static class CrewKeyProcessor
    {
        public static string getToken(long uid, int length, int crewlevel, int timestamp)
        {
            string intake = genIntake(uid, length, crewlevel, timestamp);
            string key = Sha1(intake);
            JObject json = new JObject
            {
                { "u", uid },
                { "l", length },
                { "c", crewlevel },
                { "t", timestamp },
                { "s", key }
            };
            return EncodeBase64("utf-8", json.ToString());
        }

        /// <summary>
        /// 解码token
        /// </summary>
        /// <param name="token">传入token</param>
        /// <param name="uid">传出uid</param>
        /// <param name="length">上舰时长</param>
        /// <param name="crewlevel">船员等级</param>
        /// <param name="timestamp">时间戳</param>
        public static bool checkToken(string token, out long uid, out int length, out int crewlevel, out int timestamp)
        {
            string jsondata = DecodeBase64("utf-8", token);
            JObject json = (JObject)JsonConvert.DeserializeObject(jsondata);
            if (json != null)
            {
                uid = json.Value<long>("u");
                length = json.Value<int>("l");
                crewlevel = json.Value<int>("c");
                timestamp = json.Value<int>("t");
                string signature = json.Value<string>("s");
                string correct_signature = Sha1(genIntake(uid, length, crewlevel, timestamp));
                return signature == correct_signature;
            }
            else
            {
                uid = 0;
                length = 0;
                crewlevel = 0;
                timestamp = 0;
                return false;
            }
        }

        public static string genIntake(long uid, int len, int clevel, int timestamp)
        {
            return "鹿!野?" + clevel + "的$舰%" + len + "长#密&钥$盐" + timestamp + "LuYeS" + uid + "hi#GeXiaoNaiGou?";
        }

        ///编码
        public static string EncodeBase64(string code_type, string code)
        {
            string encode = "";
            byte[] bytes = Encoding.GetEncoding(code_type).GetBytes(code);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = code;
            }
            return encode;
        }
        ///解码
        public static string DecodeBase64(string code_type, string code)
        {
            string decode = "";
            byte[] bytes = Convert.FromBase64String(code);
            try
            {
                decode = Encoding.GetEncoding(code_type).GetString(bytes);
            }
            catch
            {
                decode = code;
            }
            return decode;
        }

        /// <summary>  
        /// SHA1 加密，返回大写字符串  
        /// </summary>  
        /// <param name="content">需要加密字符串</param>  
        /// <returns>返回40位UTF8 大写</returns>  

        public static string Sha1(string data)
        {
            byte[] temp1 = Encoding.UTF8.GetBytes(data);
            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            byte[] temp2 = sha.ComputeHash(temp1);
            sha.Clear();
            // 注意， 不能用这个
            // string output = Convert.ToBase64String(temp2);// 不能直接转换成base64string
            string output = BitConverter.ToString(temp2);
            //output = output.Replace("-", "");
            output = output.ToLower();
            return output;
        }
    }
}
