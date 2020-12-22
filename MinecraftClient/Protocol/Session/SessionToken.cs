using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MinecraftClient.Protocol.Session
{
    [Serializable]
    public class SessionToken
    {
        private static readonly Regex JwtRegex = new Regex("^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$");

        public string ID { get; set; }
        public string PlayerName { get; set; }
        public string PlayerID { get; set; }
        public string ClientID { get; set; }

        public SessionToken()
        {
            ID = string.Empty;
            PlayerName = string.Empty;
            PlayerID = string.Empty;
            ClientID = string.Empty;
        }

        public override string ToString()
        {
            return string.Join(",", ID, PlayerName, PlayerID, ClientID);
        }

        public static SessionToken FromString(string tokenString)
        {
            string[] fields = tokenString.Split(',');
            if (fields.Length < 4)
            {
                throw new InvalidDataException("Invalid string format");
            }

            SessionToken session = new SessionToken
            {
                ID = fields[0],
                PlayerName = fields[1],
                PlayerID = fields[2],
                ClientID = fields[3]
            };

            if (!JwtRegex.IsMatch(session.ID))
            {
                throw new InvalidDataException("Invalid session ID");
            }

            if (!ChatBot.IsValidName(session.PlayerName))
            {
                throw new InvalidDataException("Invalid player name");
            }

            if (!Guid.TryParseExact(session.PlayerID, "N", out Guid temp))
            {
                throw new InvalidDataException("Invalid player ID");
            }

            if (!Guid.TryParseExact(session.ClientID, "N", out temp))
            {
                throw new InvalidDataException("Invalid client ID");
            }

            return session;
        }
    }
}
