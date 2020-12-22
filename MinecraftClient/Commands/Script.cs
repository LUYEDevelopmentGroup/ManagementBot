using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Script : Command
    {
        public override string CMDName => "script";
        public override string CMDDesc => "script <scriptname>: run a script file.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                handler.BotLoad(new ChatBots.Script(getArg(command), null, localVars));
                return "";
            }
            else
            {
                return CMDDesc;
            }
        }
    }
}
