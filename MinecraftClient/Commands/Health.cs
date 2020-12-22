using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    internal class Health : Command
    {
        public override string CMDName => "health";
        public override string CMDDesc => "health: Display Health and Food saturation.";

        public override string Run(McTcpClient handler, string command, Dictionary<string, object> localVars)
        {
            return "Health: " + handler.GetHealth() + ", Saturation: " + handler.GetSaturation() + ", Level: " + handler.GetLevel() + ", TotalExperience: " + handler.GetTotalExperience();
        }
    }
}
