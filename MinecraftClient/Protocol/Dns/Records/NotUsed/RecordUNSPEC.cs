/*

 */

namespace Heijden.DNS
{
    public class RecordUNSPEC : Record
    {
        public byte[] RDATA;

        public RecordUNSPEC(RecordReader rr)
        {
            // re-read length
            ushort RDLENGTH = rr.ReadUInt16(-2);
            RDATA = rr.ReadBytes(RDLENGTH);
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }

    }
}
