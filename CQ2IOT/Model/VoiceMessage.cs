using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CQ2IOT.Model
{
    public class VoiceMessage : Message
    {
        public JObject subjson;
        public VoiceFile voice;

        public VoiceMessage(JObject json) : base(json)
        {
            subjson = (JObject)JsonConvert.DeserializeObject(content);
            voice = new VoiceFile(subjson);
        }
    }
}
