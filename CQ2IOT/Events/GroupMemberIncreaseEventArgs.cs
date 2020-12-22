﻿using Newtonsoft.Json.Linq;

namespace CQ2IOT.Events
{
    public class GroupMemberDecreaseEventArgs : EventArgs
    {
        public GroupMemberDecreaseEventArgs(JObject json)
        {
            throughgroup = new Model.Group() { id = json["EventMsg"].Value<long>("FromUin") };
            user = new Model.QQ() { qq = json["EventData"].Value<long>("UserID"), name = json["EventData"].Value<string>("UserName") };
        }
    }
}
