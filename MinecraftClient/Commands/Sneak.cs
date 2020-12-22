using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Sneak : Command
    {
        private bool sneaking = false;
        public override string CMDName => "Sneak";
        public override string CMDDesc => "Sneak: Toggles sneaking";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (sneaking)
            {
                bool result = handler.sendEntityAction(Protocol.EntityActionType.StopSneaking);
                if (result)
                {
                    sneaking = false;
                }

                return result ? "You aren't sneaking anymore" : "Fail";
            }
            else
            {
                bool result = handler.sendEntityAction(Protocol.EntityActionType.StartSneaking);
                if (result)
                {
                    sneaking = true;
                }

                return result ? "You are sneaking now" : "Fail";
            }

        }
    }
}