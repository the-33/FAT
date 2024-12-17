using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FAT.Data
{
    [JsonDerivedType(typeof(File), 0)]
    [JsonDerivedType(typeof(Directory), 1)]
    public class Cluster{}

    public class File : Cluster
    {
        public byte[] data { get; set; }

        public File(int clusterSize)
        {
            data = new byte[clusterSize];
        }

        [JsonConstructor]
        public File(byte[] data)
        {
            this.data = data;
        }

        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(data, 0, data.Length).Replace("\0", "");
        }
    }

    public class Directory : Cluster
    {
        public List<Fat.Entry> entries { get; set; }

        public Directory()
        {
            entries = new List<Fat.Entry>();
        }

        [JsonConstructor]
        public Directory(List<Fat.Entry> entries)
        {
            this.entries = entries;
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
