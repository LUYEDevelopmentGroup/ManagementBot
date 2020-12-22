using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Respawn : Command
    {
        public override string CMDName => "respawn";
        public override string CMDDesc => "respawn: Use this to respawn if you are dead.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            handler.SendRespawnPacket();
            return "You have respawned.";
        }
    }
}
