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
        private bool Damaged;
        private bool Reserved;
        public int Next {  get; set; }
        public bool End { get; set; }

        public ClusterMetadata(bool available = true, int next = -1, bool end = true) 
        { 
            this.Available = available;
            this.Next = next;
            this.End = end;
            Damaged = false;
            Reserved = false;
        }
    }
}
