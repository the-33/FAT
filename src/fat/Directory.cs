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
        private struct Entry
        {
            public string Name {  get; set; }
            public string Type { get; set; }
            public ClusterMetadata StartingCluster { get; set; }

            public Entry(string name, string type, ClusterMetadata startingCluster)
            {
                this.Name = name;
                this.Type = type;
                this.StartingCluster = startingCluster;
            }
        };

        private List<Entry> Entries { get; set; }

        public Directory()
        {
            Entries = new List<Entry>();
        }
    }
}
