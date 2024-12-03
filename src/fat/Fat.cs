using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
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

        private int findDirectoryCluster(string path)
        {
            path = path.Replace("C://", "");
            string[] routing = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int cluster = -1;

            if(routing.Length > 0)
            {
                cluster = metadata.RootDirectory.Entries.IndexOf(metadata.RootDirectory.Entries.Find(e => e.Name == routing[0]));
                for(int i = 1; i<routing.Length; i++)
                {
                    FAT.Data.Directory d = (FAT.Data.Directory)data.Clusters[cluster];
                    cluster = d.Entries.IndexOf(d.Entries.Find(e => e.Name == routing[i]));
                }
            }
            return cluster;
        }

        public bool addDirectory(string name, string path)
        {
            ClusterMetadata? cluster = metadata.Clusters.Find(x => x.Available = true);

            if (cluster == null)
            {
                cluster = new ClusterMetadata();
                metadata.Clusters.Add(cluster);
                data.Clusters.Add(new FAT.Data.Directory());
            }

            cluster.Available = false;
            cluster.End = true;
            cluster.Next = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -1) metadata.RootDirectory.Entries.Add(new RootDirectory.Entry(name, "", metadata.Clusters.IndexOf(cluster)));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new FAT.Data.Directory.Entry(name, "", metadata.Clusters.IndexOf(cluster)));
            
            return true;
        }

        public void removeDirectory(string name) 
        { 
            
        }

        public string listDirectory(string path)
        {
            int directoryCluster = findDirectoryCluster(path);
            if (directoryCluster == -1) return metadata.RootDirectory.ToString();
            else return ((FAT.Data.Directory)data.Clusters[directoryCluster]).ToString();
        }

        public bool addFile(string name, string path)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split(".")[0];

            ClusterMetadata? cluster = metadata.Clusters.Find(x => x.Available = true);

            if (cluster == null)
            {
                cluster = new ClusterMetadata();
                metadata.Clusters.Add(cluster);
                data.Clusters.Add(new FAT.Data.File(clusterSize));
            }

            cluster.Available = false;
            cluster.End = true;
            cluster.Next = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -1) metadata.RootDirectory.Entries.Add(new RootDirectory.Entry(name, fileType, metadata.Clusters.IndexOf(cluster)));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new FAT.Data.Directory.Entry(name, fileType, metadata.Clusters.IndexOf(cluster)));

            return true;
        }
    }
}
