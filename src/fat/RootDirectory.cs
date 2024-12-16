using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT;

namespace FAT.MetaData
{
    public class RootDirectory
    {
        public List<Fat.Entry> entries {  get; set; }

        public RootDirectory()
        {
            entries = new List<Fat.Entry>();
        }

        public override string ToString()
        {
            string returnStr = "";

            foreach (Fat.Entry e in entries)
            {
                returnStr += e.ToString() + "\n";
            }

            return returnStr;
        }
    }
}
