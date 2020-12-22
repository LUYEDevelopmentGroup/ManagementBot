using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Exit : Command
    {
        public override string CMDName => "exit";
        public override string CMDDesc => "exit: disconnect from the server.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            Program.Exit();
            return "";
        }

        public override IEnumerable<string> getCMDAliases()
        {
            return new string[] { "quit" };
        }
    }
}
