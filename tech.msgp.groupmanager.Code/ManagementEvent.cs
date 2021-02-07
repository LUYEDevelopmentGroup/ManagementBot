using System;

namespace tech.msgp.groupmanager.Code
{
    public class ManagementEvent
    {
        public const string WARN = "WARN";
        public const string SILENCE = "SILE";
        public const string RM_SILENCE = "RMSI";
        public const string KICK = "KICK";

        public long op;
        public long affected;
        public string type;
        public string reason;
        public long id;
        public int timestamp;

        /// <summary>
        /// 管理事件
        /// </summary>
        /// <param name="id">事件ID</param>
        /// <param name="type">事件类型</param>
        /// <param name="op">操作者</param>
        /// <param name="affected">被操作者</param>
        /// <param name="reason">操作原因</param>
        public ManagementEvent(long id, string type, long op, long affected, string reason)
        {
            this.op = op;
            this.affected = affected;
            this.type = type;
            this.reason = reason;
            this.id = id;
        }

        public ManagementEvent(string type, long op, long affected, string reason)
        {
            this.op = op;
            this.affected = affected;
            this.type = type;
            this.reason = reason;
            id = BiliApi.TimestampHandler.GetTimeStamp16(DateTime.Now);
        }

        #region 等量比较实现
        public override bool Equals(object obj)
        {
            if (obj.GetType().FullName == GetType().FullName)
            {
                ManagementEvent m_e = (ManagementEvent)obj;
                return id.Equals(m_e.id);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
        #endregion
    }
}
