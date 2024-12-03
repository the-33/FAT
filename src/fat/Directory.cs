using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT.MetaData;

namespace FAT.Data
{
    internal class Directory : Cluster
    {
        public struct Entry
        {
            public string Name {  get; set; }
            public string Type { get; set; }
            public int StartingCluster { get; set; }

            public Entry(string name, string type, int startingCluster)
            {
                this.Name = name;
                this.Type = type;
                this.StartingCluster = startingCluster;
            }

            public override string ToString()
            {
                if(Type != "") return Name + "." + Type;
                else return Name;
            }
        }

        public List<Entry> Entries { get; set; }

        public Directory()
        {
            Entries = new List<Entry>();
        }

        public override string ToString()
        {
            string returnStr = "";

            foreach (Entry e in Entries)
            {
                returnStr += e.ToString();
            }

            return returnStr;
        }
    }
}
