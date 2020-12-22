using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Reco : Command
    {
        public override string CMDName => "reco";
        public override string CMDDesc => "reco [account]: restart and reconnect to the server.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            string[] args = getArgs(command);
            if (args.Length > 0)
            {
                if (!Settings.SetAccount(args[0]))
                {
                    return "Unknown account '" + args[0] + "'.";
                }
            }
            Program.Restart();
            return "";
        }
    }
}
