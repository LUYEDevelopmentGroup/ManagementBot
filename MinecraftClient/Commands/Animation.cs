using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Animation : Command
    {
        public override string CMDName => "animation";
        public override string CMDDesc => "animation <mainhand|offhand>";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length > 0)
                {
                    if (args[0] == "mainhand" || args[0] == "0")
                    {
                        handler.DoAnimation(0);
                        return "Done";
                    }
                    else if (args[0] == "offhand" || args[0] == "1")
                    {
                        handler.DoAnimation(1);
                        return "Done";
                    }
                    else
                    {
                        return CMDDesc;
                    }
                }
                else
                {
                    return CMDDesc;
                }
            }
            else
            {
                return CMDDesc;
            }
        }
    }
}
