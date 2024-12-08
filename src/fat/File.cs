using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAT.Data
{
    internal class File : Cluster
    {
        public byte[] Data { get; set; }

        public File(int clusterSize)
        {
            Data = new byte[clusterSize];
        }

        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(Data, 0, Data.Length).Replace("\0", "");
        }
    }
}
