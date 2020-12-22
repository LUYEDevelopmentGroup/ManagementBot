using Newtonsoft.Json.Linq;

namespace CQ2IOT.Model
{
    public class VoiceFile : MessageMediaFile
    {
        public VoiceFile(JObject json)
        {
            url = json.Value<string>("Url");
            tip = json.Value<string>("Tips");
        }
    }
}
