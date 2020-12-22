using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Debug : Command
    {
        public override string CMDName => "debug";
        public override string CMDDesc => "debug [on|off]: toggle debug messages.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                Settings.DebugMessages = (getArg(command).ToLower() == "on");
            }
            else
            {
                Settings.DebugMessages = !Settings.DebugMessages;
            }

            return "Debug messages are now " + (Settings.DebugMessages ? "ON" : "OFF");
        }
    }
}
