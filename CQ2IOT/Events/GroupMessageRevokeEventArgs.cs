using Newtonsoft.Json.Linq;

namespace CQ2IOT.Events
{
    public class GroupMessageRevokeEventArgs : EventArgs
    {
#pragma warning disable CS0108 // '“GroupMessageRevokeEventArgs.op”隐藏继承的成员“EventArgs.op”。如果是有意隐藏，请使用关键字 new。
        public long op, rand;
#pragma warning restore CS0108 // '“GroupMessageRevokeEventArgs.op”隐藏继承的成员“EventArgs.op”。如果是有意隐藏，请使用关键字 new。
        public int seq;

        public GroupMessageRevokeEventArgs(JObject json)
        {
            JObject j = (JObject)json["EventData"];
            user = new Model.QQ()
            {
                qq = j.Value<long>("UserID")
            };
            throughgroup = new Model.Group()
            {
                id = j.Value<long>("GroupID")
            };
            op = j.Value<long>("AdminUserID");
            rand = j.Value<long>("MsgRandom");
            seq = j.Value<int>("MsgSeq");
        }
    }
}
