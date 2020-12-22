using System;

namespace tech.msgp.groupmanager.Code.BiliAPI
{
    public class BiliBannedUser
    {
        public int uid, len, op, id;
        public string uname, opname;
        public DateTime optime, endtime;
        public BanReason banreason;
    }
    public struct BanReason
    {
        public string message;
        public DateTime messagetime;
    }
}
