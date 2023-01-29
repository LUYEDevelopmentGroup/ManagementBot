using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using tech.msgp.groupmanager.Code;

namespace tech.msgp.groupmanager.Code
{
    public class ActivationCodeClaimer
    {
        List<long> claimerIds = new List<long>();
        public ActivationCodeClaimer()
        {
            UpdateClaimerId();
        }

        public int UpdateClaimerId()
        {
            var newlist = MainHolder.bilidmkproc.blr.GetCurrentCrewListUID();

            lock (claimerIds) claimerIds = newlist;

            return claimerIds.Count;
        }

        public string CheckWhenBuy(long uid, out bool success)
        {
            string tempalate = DataBase.me.GetCodeTempalate();
            success = false;
            if (tempalate.Length < 7) return "";
            try
            {
                string code = DataBase.me.GetActivationCode(uid);
                success = true;
                return tempalate.Replace("{CODE}", code);
            }
            catch (Exception ex)
            {
                if (ex.Message == "激活码耗尽")
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("[激活码派发]\n未能向#" + uid + "发送激活码\n库存耗尽");
                    return "当前激活码库存不足，请联系鸡蛋(1250542735)";
                }
                MainHolder.broadcaster.BroadcastToAdminGroup("[激活码派发]\n未能向#" + uid + "发送激活码\n发生错误：" + ex.Message + "\n" + ex.StackTrace);
                return "发生故障，请联系鸡蛋(1250542735)";
            }
        }

        public string Check(long qq, out bool success)
        {
            long uid = DataBase.me.getUserBoundedUID(qq);
            success = false;
            if (uid == 0)
            {
                return "您的QQ号未关联到Bilibili UID，因此无法领取此福利。请在您的B站私信中向[鹿野灸官方录屏组]发送您当前的QQ号，收到回复后再次领取此福利。\n如果您无法解决这个问题，请联系鸡蛋(1250542735)";
            }
            return CheckUID(uid, out success);
        }

        public string CheckUID(long uid, out bool success)
        {
            success = false;
            string tempalate = DataBase.me.GetCodeTempalate();
            if (tempalate.Length < 7) return "";
            lock (claimerIds)
                if (!claimerIds.Contains(uid))
                {
                    return "目前我们无法验证您的在舰凭证，请确认是否在舰或稍后再试。\n如果您确实在舰，请联系管理并提供相应凭证。";
                }
            try
            {
                string code = DataBase.me.GetActivationCode(uid);
                success = true;
                return tempalate.Replace("{CODE}", code);
            }
            catch (Exception ex)
            {
                if (ex.Message == "激活码耗尽")
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("[激活码派发]\n库存耗尽");
                    return "当前激活码库存不足，请联系管理。";
                }
                MainHolder.broadcaster.BroadcastToAdminGroup("[激活码派发]\n发生错误：" + ex.Message + "\n" + ex.StackTrace);
                return "发生意外错误，请联系管理。";
            }
        }
    }
}