using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Send : Command
    {
        public override string CMDName => "send";
        public override string CMDDesc => "send <text>: send a chat message or command.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                handler.SendText(getArg(command));
                return "";
            }
            else
            {
                return CMDDesc;
            }
        }
    }
}
