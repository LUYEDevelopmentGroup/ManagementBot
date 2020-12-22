using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    internal class UseItem : Command
    {
        public override string CMDName => "useitem";
        public override string CMDDesc => "useitem: Use (left click) an item on the hand";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            if (handler.GetInventoryEnabled())
            {
                handler.UseItemOnHand();
                return "Used an item";
            }
            else
            {
                return "Please enable inventoryhandling in config to use this command.";
            }
        }
    }
}
