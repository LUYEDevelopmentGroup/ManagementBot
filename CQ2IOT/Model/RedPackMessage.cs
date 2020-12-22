using Newtonsoft.Json.Linq;

namespace CQ2IOT.Model
{
    public class RedPackMessage : Message
    {
        public RedPackMessage(JObject json) : base(json)
        {

        }
    }
}
