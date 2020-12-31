using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PyEngine
{
    class StreamReceiver : StreamWriter
    {
        List<string> buffer = new List<string>();
        #region Event
        public event EventHandler<string> StringWritten;
        #endregion

        #region CTOR
        public StreamReceiver(Stream s) : base(s)
        { }
        #endregion

        #region Private Methods
        private void LaunchEvent(string txtWritten)
        {
            if (StringWritten != null)
            {
                StringWritten(this, txtWritten);
            }
        }
        #endregion


        #region Overrides

        public override void Write(string value)
        {
            base.Write(value);
            buffer.Add(value);
            LaunchEvent(value);
        }
        public override void Write(bool value)
        {
            base.Write(value);
            buffer.Add(value.ToString());
            LaunchEvent(value.ToString());
        }

        public string ReadOnce()
        {
            string pop = buffer[buffer.Count - 1];
            buffer.RemoveAt(buffer.Count - 1);
            return pop;
        }

        public string ReadToEnd()
        {
            StringBuilder sb = new StringBuilder();
            foreach(string str in buffer)
            {
                sb.Append(str);
            }
            buffer.Clear();
            return sb.ToString();
        }
        #endregion
    }
}
