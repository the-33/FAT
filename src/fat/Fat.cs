using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT.Data;
using FAT.MetaData;

namespace FAT
{
    // TODO: Toda la FAT
    public class Fat
    {
        const int clusterSize = 3200;

        private Func<bool> bootCode { get; set; } = () =>
        {
            return true;
        };

        private struct Metadata
        {
            public Func<bool> BootCode { get; set; }
            public string FatCopy {  get; set; }
            public List<ClusterMetadata> Clusters { get; set; }
            public RootDirectory RootDirectory { get; set; }

            public Metadata(Func<bool> bootCode, string fatCopy = "")
            {
                this.BootCode = bootCode;
                this.FatCopy = fatCopy;
                Clusters = new List<ClusterMetadata>();
                RootDirectory = new RootDirectory();
            }
        };

        private struct Data
        {
            public List<Cluster> Clusters { get; set; }

            public Data()
            {
                Clusters = new List<Cluster>();
            }
        };

        private Metadata metadata;
        private Data data;

        public Fat()
        {
            metadata = new Metadata(bootCode);
            data = new Data();
        }

        public bool addFolder()
        {

            return true;
        }
    }
}
