using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT.Data;

namespace FAT.MetaData
{
    internal class ClusterMetadata
    {
        public bool Available {  get; set; }
        public bool Damaged { get; set; }
        public bool Reserved { get; set; }
        public int Next {  get; set; }
        public bool End { get; set; }
        public Cluster Self { get; set; }

        public ClusterMetadata(Cluster self, bool available = true, int next = -1, bool end = true) 
        { 
            this.Available = available;
            this.Next = next;
            this.End = end;
            this.Self = self;
            Damaged = false;
            Reserved = false;
        }
    }
}
