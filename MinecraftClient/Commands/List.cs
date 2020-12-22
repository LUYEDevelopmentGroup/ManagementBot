using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class List : Command
    {
        public override string CMDName => "list";
        public override string CMDDesc => "list: get the player list.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            return "PlayerList: " + string.Join(", ", handler.GetOnlinePlayers());
        }
    }
}

