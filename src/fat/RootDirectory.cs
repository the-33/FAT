using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT;

namespace FAT.MetaData
{
    internal class RootDirectory
    {
        public List<Fat.Entry> Entries {  get; set; }

        public RootDirectory()
        {
            Entries = new List<Fat.Entry>();
        }

        public override string ToString()
        {
            string returnStr = "";

            foreach (Fat.Entry e in Entries)
            {
                returnStr += e.ToString();
            }

            return returnStr;
        }
    }
}
