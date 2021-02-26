using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace BroadTicketUtility
{
    [Serializable]
    public class Ticket
    {
        [Serializable]
        public enum CrewLevel
        {
            舰长 = 3, 总督 = 1, 提督 = 2
        }

        [Serializable]
        public struct DataArea
        {
            public DateTime GenerateTime;
            public int Uid;
            public CrewLevel Level;
            public string SpecType;
            public Guid SerialNumber;
        }

        public byte[] Signature;
        public DataArea Data;

        public new string ToString()
        {
            return TicketCoder.GetString(this);
        }
    }
}
