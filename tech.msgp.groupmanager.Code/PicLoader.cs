using System;
using System.Drawing;
using System.IO;
using System.Net;

namespace tech.msgp.groupmanager.Code
{
    public static class PicLoader
    {
        public static Image loadPictureFromURL(string url)
        {
            Image i = Image.FromStream(WebRequest.Create(url).GetResponse().GetResponseStream());
            return i;
        }

        public static string getIMGUrlString(string fname)
        {

            string cd = Environment.CurrentDirectory;
            string datafile = cd + @"\data\image\" + fname + ".cqimg";
            StreamReader sr = new StreamReader(datafile);
            do
            {
                if (sr.EndOfStream)
                {
                    break;
                }

                string str = sr.ReadLine();
                if (str.Length < 1)
                {
                    break;
                }

                if (str.Substring(0, 4) == "url=")
                {
                    string url = str.Substring(4);
                    return url;
                }
            } while (!sr.EndOfStream);
            return null;
        }

        public static Image loadPictureFromCQ(string fname)
        {
            return loadPictureFromURL(getIMGUrlString(fname));
        }

    }
}
