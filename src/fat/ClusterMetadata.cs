using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FAT.Data;

namespace FAT.MetaData
{
    public class ClusterMetadata
    {
        public bool available {  get; set; }
        public bool damaged { get; set; }
        public bool reserved {get; set;}
        public int next {  get; set; }
        public bool end { get; set; }

        public ClusterMetadata(bool available = true, int next = -1, bool end = false) 
        { 
            this.available = available;
            this.next = next;
            this.end = end;
            damaged = false;
            reserved = false;
        }

        [JsonConstructor]
        public ClusterMetadata(bool available, bool damaged, bool reserved, int next, bool end)
        {
            this.available = available;
            this.damaged = damaged;
            this.reserved = reserved;
            this.next = next;
            this.end = end;
        }

        public override string ToString()
        {
            char availableChar = (available) ? 'T' : 'F';
            char damagedChar = (damaged) ? 'T' : 'F';
            char reservedChar = (reserved) ? 'T' : 'F';
            char endChar = (end) ? 'T' : 'F';

            string returnStr = "{ Avaliable: " + availableChar + "\tDamaged: " + damagedChar + "\tReserved: " + reservedChar + "\tNext: ";
            if (next != -1) returnStr += "Cluster " + next;
            else returnStr += "NULL";
            returnStr += "\tEnd: " + endChar + " }";

            return returnStr;
        }
    }
}
