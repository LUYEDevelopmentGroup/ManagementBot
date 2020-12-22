using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Log : Command
    {
        public override string CMDName => "log";
        public override string CMDDesc => "log <text>: log some text to the console.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                ConsoleIO.WriteLogLine(getArg(command));
                return "";
            }
            else
            {
                return CMDDesc;
            }
        }
    }
}
