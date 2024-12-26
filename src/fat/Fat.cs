using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FAT.Data;
using FAT.MetaData;
using Microsoft.VisualBasic.FileIO;
using static Crayon.Output;

namespace FAT
{
    public class Fat
    {
        public struct Metadata
        {
            public readonly BootCode bootCode { get; }
            public string fatCopyPath { get; set; }
            public List<ClusterMetadata> clusters { get; set; }
            public RootDirectory rootDirectory { get; set; }

            public Metadata()
            {
                bootCode = new BootCode();
                this.fatCopyPath = "";
                clusters = new List<ClusterMetadata>();
                rootDirectory = new RootDirectory();
            }

            [JsonConstructor]
            public Metadata(string fatCopyPath, BootCode bootCode, List<ClusterMetadata> clusters, RootDirectory rootDirectory)
            {
                this.fatCopyPath = fatCopyPath;
                this.clusters = clusters;
                this.rootDirectory = rootDirectory;
                this.bootCode = bootCode;
            }

            public override string ToString()
            {
                string returnStr = "";

                returnStr += "[Boot Code]\n\n" + bootCode + "\n\n";
                if (System.IO.File.Exists(fatCopyPath)) returnStr += "[Fat Copy]\n" + System.IO.File.ReadAllText(fatCopyPath) + "\n\n";
                returnStr += "[Clusters]\n\n";

                int i = 0;
                foreach(ClusterMetadata c in clusters)
                {
                    returnStr += "Cluster " + i + ": " + c + "\n";
                    i++;
                }

                returnStr += "\n\n";

                returnStr += "[Root Directory]\n\nName\t\tType\tStarting Cluster\n----\t\t----\t----------------\n\n";

                foreach(Entry e in rootDirectory.entries)
                {
                    if(e.type != "") returnStr += e.name + "\t\t" + e.type + "\t" + e.startingCluster + "\n";
                    else returnStr += e.name + "\t\tDIR\t" + e.startingCluster + "\n";
                }

                return returnStr;
            }
        };

        public struct Data
        {
            public List<Cluster> clusters { get; set; }

            public Data()
            {
                clusters = new List<Cluster>();
            }

            [JsonConstructor]
            public Data(List<Cluster> clusters)
            {
                this.clusters = clusters;
            }
        };

        public struct Entry
        {
            public string name { get; set; }
            public string type { get; set; }
            public int startingCluster { get; set; }

            [JsonConstructor]
            public Entry(string name, string type, int startingCluster)
            {
                this.name = name;
                this.type = type;
                this.startingCluster = startingCluster;
            }

            public override string ToString()
            {
                if (type == "") return "📁 " + name;
                else return "📄 " + name + "." + type;
            }
        };

        public int clusterSize { get; set; }
        public Metadata metadata { get; set; }
        public Data data { get; set; }

        public Fat(int clusterSize)
        {
            metadata = new Metadata();
            data = new Data();
            this.clusterSize = clusterSize;

            metadata.bootCode.recalculateMagicNumber(this);
        }

        [JsonConstructor]
        public Fat(Metadata metadata, Data data, int clusterSize) 
        {
            this.metadata = metadata;
            this.data = data;
            this.clusterSize = clusterSize;
        }

        public void showMetadata()
        {
            Console.Write(metadata);
        }

        private int findDirectoryCluster(string fullPath)
        {
            string path = fullPath.Replace("C:", "");
            string[] routing = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int cluster = -1;

            try
            {
                if (routing.Length > 0)
                {
                    cluster = (metadata.rootDirectory.entries.Exists(e => e.name == routing[0])) ? metadata.rootDirectory.entries.Find(e => e.name == routing[0]).startingCluster : -1;
                    if (cluster == -1) return -2;
                    for (int i = 1; i < routing.Length; i++)
                    {
                        FAT.Data.Directory d = (FAT.Data.Directory)data.clusters[cluster];
                        cluster = (d.entries.Exists(e => e.name == routing[i])) ? d.entries.Find(e => e.name == routing[i]).startingCluster : -1;
                        if (cluster == -1) return -2;
                    }
                }
            }
            catch{ return -2; }

            return cluster;
        }

        public bool addDirectory(string name, string path)
        {
            ClusterMetadata? cluster = metadata.clusters.Find(x => x.available == true);

            if (cluster == null)
            {
                cluster = new ClusterMetadata();
                metadata.clusters.Add(cluster);
                data.clusters.Add(new FAT.Data.Directory());
            }
            else
            {
                data.clusters[metadata.clusters.IndexOf(cluster)] = new FAT.Data.Directory();
            }

            cluster.available = false;
            cluster.end = true;
            cluster.next = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            if (directoryCluster == -1) metadata.rootDirectory.entries.Add(new Entry(name, "", metadata.clusters.IndexOf(cluster)));
            else ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Add(new Entry(name, "", metadata.clusters.IndexOf(cluster)));

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public bool removeDirectory(string path, string name = "")
        {
            int cluster = -1;
            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            if (name != "")
            {
                if (directoryCluster == -1) cluster = metadata.rootDirectory.entries.Find(x => x.name == name).startingCluster;
                else cluster = ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.name == name).startingCluster;

                foreach (Entry e in ((FAT.Data.Directory)data.clusters[cluster]).entries)
                {
                    if (e.type == "") removeDirectory(path + "/" + name, e.name);
                    else removeFile(path + "/" + name, e.name + "." + e.type);
                }

                if (directoryCluster == -1) metadata.rootDirectory.entries.Remove(metadata.rootDirectory.entries.Find(x => x.startingCluster == cluster));
                else ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Remove(((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.startingCluster == cluster));

                metadata.clusters[cluster].available = true;
            }
            else
            {
                if (directoryCluster == -1)
                {
                    foreach (Entry e in metadata.rootDirectory.entries)
                    {
                        if (e.type == "") removeDirectory(path + "/" + name, e.name);
                        else removeFile(path + "/" + name, e.name + "." + e.type);
                    }
                }
                else
                {
                    foreach (Entry e in ((FAT.Data.Directory)data.clusters[directoryCluster]).entries)
                    {
                        if (e.type == "") removeDirectory(path + "/" + name, e.name);
                        else removeFile(path + "/" + name, e.name + "." + e.type);
                    }
                }
            }

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public bool moveDirectory(string path, string name, string newPath, string newName = "")
        {
            if (newName == "") newName = name;

            int directoryCluster = findDirectoryCluster(path);
            int newDirectoryCluster = findDirectoryCluster(newPath);

            if (directoryCluster == -2 || newDirectoryCluster == -2) return false;

            Entry directoryEntry;

            if (directoryCluster == -1)
            {
                directoryEntry = metadata.rootDirectory.entries.Find(x => x.name == name);
                metadata.rootDirectory.entries.Remove(directoryEntry);
            }
            else
            {
                directoryEntry = ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.name == name);
                ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Remove(directoryEntry);
            }

            if (newDirectoryCluster == -1) metadata.rootDirectory.entries.Add(new Entry(newName, "", directoryEntry.startingCluster));
            else ((FAT.Data.Directory)data.clusters[newDirectoryCluster]).entries.Add(new Entry(newName, "", directoryEntry.startingCluster));

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public bool copyDirectory(string path, string name, string newPath, string newName = "")
        {
            if (newName == "") newName = name;

            int directoryCluster = findDirectoryCluster(path + "/" + name);

            if (directoryCluster == -2) return false;
            if (directoryCluster == -1)
            {
                Console.Write(Bold().Red().Text("You can not copy the root directory\n"));
                return false;
            }

            bool success = addDirectory(newPath, newName);

            if (!success)
            {
                Console.Write(Bold().Red().Text("Could not copy the directory \"" + path + "/" + name + "\" to \"" + newPath + "/" + newName + "\"\n"));
                return false;
            }

            foreach (Entry e in ((FAT.Data.Directory)data.clusters[directoryCluster]).entries)
            {
                if (e.type == "") copyDirectory(path + "/" + name, e.name, newPath + "/" + newName, e.name);
                else copyFile(path + "/" + name, e.name, newPath + "/" + newName, e.name);
            }

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public string listDirectory(string path)
        {
            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return "";

            if (directoryCluster == -1) return metadata.rootDirectory.ToString();
            else return ((FAT.Data.Directory)data.clusters[directoryCluster]).ToString();
        }

        public bool directoryExists(string path)
        {
            int directoryCluster = findDirectoryCluster(path);
            bool returnValue;

            returnValue = (directoryCluster == -2);

            if (!returnValue) Console.Write(Bold().Red().Text("Could not find the directory \"" + path + "\"\n"));
            return returnValue;
        }

        public bool addFile(string name, string path)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            ClusterMetadata? cluster = metadata.clusters.Find(x => x.available == true);

            if (cluster == null)
            {
                cluster = new ClusterMetadata();
                metadata.clusters.Add(cluster);
                data.clusters.Add(new FAT.Data.File(clusterSize));
            }
            else
            {
                data.clusters[metadata.clusters.IndexOf(cluster)] = new FAT.Data.File(clusterSize);
            }

            cluster.available = false;
            cluster.end = true;
            cluster.next = -1;

            if (directoryCluster == -1) metadata.rootDirectory.entries.Add(new Entry(fileName, fileType, metadata.clusters.IndexOf(cluster)));
            else ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Add(new Entry(fileName, fileType, metadata.clusters.IndexOf(cluster)));

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public bool removeFile(string name, string path)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];

            int cluster = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            if (directoryCluster == -1) cluster = metadata.rootDirectory.entries.Find(x => x.name == fileName && x.type == fileType).startingCluster;
            else cluster = ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.name == fileName && x.type == fileType).startingCluster;

            if (cluster == -1)
            {
                Console.Write(Bold().Red().Text("Could not find the file \"" + path + "/" + name + "\"\n"));
                return false;
            }

            while (!metadata.clusters[cluster].end)
            {
                metadata.clusters[cluster].available = true;
                if (!metadata.clusters[cluster].end) cluster = metadata.clusters[cluster].next;
            }

            metadata.clusters[cluster].available = true;

            if (directoryCluster == -1) metadata.rootDirectory.entries.Remove(metadata.rootDirectory.entries.Find(x => x.name == fileName && x.type == fileType));
            else ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Remove(((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.name == fileName && x.type == fileType));

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public bool moveFile(string path, string name, string newPath, string newName = "")
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];

            string newfileType = (newName == "") ? fileType : newName.Split('.')[1];
            string newfileName = (newName == "") ? fileName : newName.Split(".")[0];

            int directoryCluster = findDirectoryCluster(path);
            int newDirectoryCluster = findDirectoryCluster(newPath);

            if (directoryCluster == -2 || newDirectoryCluster == -2) return false;

            Entry fileEntry;

            if (directoryCluster == -1)
            {
                fileEntry = metadata.rootDirectory.entries.Find(x => x.name == fileName && x.type == fileType);
                metadata.rootDirectory.entries.Remove(fileEntry);
            }
            else
            {
                fileEntry = ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.name == fileName && x.type == fileType);
                ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Remove(fileEntry);
            }

            if (newDirectoryCluster == -1) metadata.rootDirectory.entries.Add(new Entry(newfileName, newfileType, fileEntry.startingCluster));
            else ((FAT.Data.Directory)data.clusters[newDirectoryCluster]).entries.Add(new Entry(newfileName, newfileType, fileEntry.startingCluster));

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public bool copyFile(string path, string name, string newPath, string newName = "")
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];

            string newfileType = (newName == "") ? fileType : newName.Split('.')[1];
            string newfileName = (newName == "") ? fileName : newName.Split(".")[0];

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            bool success = addFile(newfileName + '.' + newfileType, newPath);

            if (!success)
            {
                Console.Write(Bold().Red().Text("Could not copy the file \"" + path + "/" + name + "\" to \"" + newPath + "/" + newName + "\"\n"));
                return false;
            }

            success = writeToFile(newfileName + '.' + newfileType, newPath, catFile(name, path), true);

            if (!success)
            {
                Console.Write(Bold().Red().Text("Could not copy the file \"" + path + "/" + name + "\" to \"" + newPath + "/" + newName + "\"\n"));
                removeFile(newName, newPath);
                return false;
            }

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public bool writeToFile(string name, string path, string input, bool overwrite = true)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            string content = input;

            if (!overwrite) content = catFile(name, path) + input;

            removeFile(name, path);
            addFile(name, path);

            Queue<string> packets = new Queue<string>();
            string packet = "";

            for (int i = 0, j = 0; i < content.Length; i++, j++)
            {
                packet += content[i];
                if (packet.Length == clusterSize || (i + 1) == content.Length)
                {
                    packets.Enqueue(packet);
                    packet = "";
                }
            }

            int cluster = -1;

            if (directoryCluster == -1) cluster = metadata.rootDirectory.entries.Find(x => x.name == fileName && x.type == fileType).startingCluster;
            else cluster = ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.name == fileName && x.type == fileType).startingCluster;

            if (cluster == -1)
            {
                Console.Write(Bold().Red().Text("Could not find the file \"" + path + "/" + name + "\"\n"));
                return false;
            }

            ClusterMetadata fileCluster;

            while (packets.Count > 0)
            {
                fileCluster = metadata.clusters[cluster];
                ((FAT.Data.File)data.clusters[cluster]).data = Encoding.UTF8.GetBytes(packets.Dequeue());

                fileCluster.available = false;

                if (packets.Count > 0)
                {
                    ClusterMetadata? nextCluster = metadata.clusters.Find(x => x.available == true);

                    if (nextCluster == null)
                    {
                        nextCluster = new ClusterMetadata();
                        metadata.clusters.Add(nextCluster);
                        data.clusters.Add(new FAT.Data.File(clusterSize));
                    }

                    fileCluster.next = metadata.clusters.IndexOf(nextCluster);
                    fileCluster.end = false;

                    cluster = fileCluster.next;
                }
                else
                {
                    fileCluster.next = -1;
                    fileCluster.end = true;
                }
            }

            metadata.bootCode.recalculateMagicNumber(this);
            return true;
        }

        public string catFile(string name, string path)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];
            string content = "";

            int cluster = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return "";

            if (directoryCluster == -1) cluster = metadata.rootDirectory.entries.Find(x => x.name == fileName && x.type == fileType).startingCluster;
            else cluster = ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Find(x => x.name == fileName && x.type == fileType).startingCluster;

            while (!metadata.clusters[cluster].end)
            {
                content += ((FAT.Data.File)data.clusters[cluster]).ToString();
                if (!metadata.clusters[cluster].end) cluster = metadata.clusters[cluster].next;
            }

            content += ((FAT.Data.File)data.clusters[cluster]).ToString();

            return content;
        }

        public bool fileExists(string name, string path)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];
            bool returnValue = true;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) { Console.Write(Bold().Red().Text("Could not find the file \"" + path + "/" + name + "\"\n")); return false; }

            if (directoryCluster == -1) returnValue = metadata.rootDirectory.entries.Exists(x => x.name == fileName && x.type == fileType);
            else returnValue = ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Exists(x => x.name == fileName && x.type == fileType);

            if (!returnValue) Console.Write(Bold().Red().Text("Could not find the file \"" + path + "/" + name + "\"\n"));
            return returnValue;
        }
    }
}
