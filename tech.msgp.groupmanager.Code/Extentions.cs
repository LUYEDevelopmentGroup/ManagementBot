using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tech.msgp.groupmanager.Code
{
    public static class Extentions
    {
        public static bool Contains<T1, T2>(this List<T1> i, T2 key)
        {
            foreach (var item in i)
            {
                if (item.Equals(key)) { return true; }
            }
            return false;
        }
    }
}
