using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Set : Command
    {
        public override string CMDName => "set";
        public override string CMDDesc => "set varname=value: set a custom %variable%.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] temp = getArg(command).Split('=');
                if (temp.Length > 1)
                {
                    if (Settings.SetVar(temp[0], getArg(command).Substring(temp[0].Length + 1)))
                    {
                        return ""; //Success
                    }
                    else
                    {
                        return "variable name must be A-Za-z0-9.";
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
