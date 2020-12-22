using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CQ2IOT.Model
{
    public class MixedMessage : Message
    {
        public JObject subjson;
        public PictureFile picture;
        public MixedMessage(JObject json) : base(json)
        {
            subjson = (JObject)JsonConvert.DeserializeObject(content);
            picture = new PictureFile(subjson);
        }
    }
}
