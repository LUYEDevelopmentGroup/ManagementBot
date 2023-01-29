using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tech.msgp.groupmanager.Code.FunctionMods
{
    class AdaptiveBroadcaster
    {
        public Dictionary<string, string[]> AdaptiveArgs { get; set; }
        public string MessageTemplate;

        public AdaptiveBroadcaster(Dictionary<string, string[]> adaptiveArgs, string messageTemplate)
        {
            AdaptiveArgs = adaptiveArgs;
            MessageTemplate = messageTemplate;
        }

        public string[] GetMessages(int count)
        {
            string[] messages = new string[count];
            for (int i = 0; i < count; i++)
            {
                messages[i] = MessageTemplate;
                foreach (var args in AdaptiveArgs)
                {
                    messages[i] = messages[i].Replace("{" + args.Key + "}", args.Value[i]);
                }
            }
            return messages;
        }
    }
}
