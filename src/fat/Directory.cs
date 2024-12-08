using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT.MetaData;
using FAT;

namespace FAT.Data
{
    internal class Directory : Cluster
    {
        public List<Fat.Entry> Entries { get; set; }

        public Directory()
        {
            Entries = new List<Fat.Entry>();
        }

        public override string ToString()
        {
            string returnStr = "";

            foreach (Fat.Entry e in Entries)
            {
                returnStr += e.ToString() + "\n";
            }

            return returnStr;
        }
    }
}
