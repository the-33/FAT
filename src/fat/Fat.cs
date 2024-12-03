using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using FAT.Data;
using FAT.MetaData;
using Microsoft.VisualBasic.FileIO;

namespace FAT
{
    /*TODO:
     * Mostrar metadatos DONE
     * 
     * Crear directorio DONE
     * Eliminar directorio DONE
     * Mover directorio DONE
     * Copiar directorio
     * Mostrar contenido directorio DONE
     * 
     * Crear archivo DONE
     * Eliminar archivo DONE
     * Mover archivo DONE
     * Copiar archivo
     * Mostrar contenido de archivo DONE
     */

    public class Fat
    {
        const int clusterSize = 3200;

        private struct Metadata
        {
            BootCode BootCode { get; set; }
            public string FatCopyPath {  get; set; }
            public List<ClusterMetadata> Clusters { get; set; }
            public RootDirectory RootDirectory { get; set; }

            public Metadata(string fatCopyPath = "")
            {
                this.BootCode = new BootCode();
                this.FatCopyPath = fatCopyPath;
                Clusters = new List<ClusterMetadata>();
                RootDirectory = new RootDirectory();
            }

            public override string ToString()
            {
                string fullFatCopyPath = "";

                using (Process process = new Process()) // Obtiene el nombre de usuario y el nombre del equipo
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = @"/c pwd";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    StreamReader reader = process.StandardOutput;
                    string output = reader.ReadToEnd();

                    fullFatCopyPath = output.Replace("\n", "") + "/" + FatCopyPath;
                }

                string returnStr = "";

                returnStr += "[Boot Code]\n\n" + BootCode + "\n\n";
                returnStr += "[Fat Copy Path] " + fullFatCopyPath + "\n\n";
                returnStr += "[Clusters]\n\n";

                int i = 0;
                foreach(ClusterMetadata c in Clusters)
                {
                    returnStr += "Cluster " + i + ": " + c + "\n";
                    i++;
                }

                returnStr += "\n\n";

                returnStr += "[Root Directory]\n\nName\t\tType\tStarting Cluster\n----\t\t----\t----------------\n\n";

                foreach(Entry e in RootDirectory.Entries)
                {
                    if(e.Type != "") returnStr += e.Name + "\t\t" + e.Type + "\t" + e.StartingCluster + "\n";
                    else returnStr += e.Name + "\t\tDIR\t" + e.StartingCluster + "\n";
                }

                return returnStr;
            }
        }
        private struct Data
        {
            public List<Cluster> Clusters { get; set; }

            public Data()
            {
                Clusters = new List<Cluster>();
            }
        };

        public struct Entry
        {
            public string Name { get; set; }
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
                if (Type != "") return "📁 " + Name + "." + Type;
                else return "📄 " + Name;
            }
        };

        private Metadata metadata;
        private Data data;

        public Fat()
        {
            metadata = new Metadata();
            data = new Data();
        }

        private int findDirectoryCluster(string path)
        {
            path = path.Replace("C://", "");
            string[] routing = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int cluster = -1;

            try
            {
                if (routing.Length > 0)
                {
                    cluster = metadata.RootDirectory.Entries.IndexOf(metadata.RootDirectory.Entries.Find(e => e.Name == routing[0]));
                    if (cluster == -1) throw new Exception("Could not find specified path");
                    for (int i = 1; i < routing.Length; i++)
                    {
                        FAT.Data.Directory d = (FAT.Data.Directory)data.Clusters[cluster];
                        cluster = d.Entries.IndexOf(d.Entries.Find(e => e.Name == routing[i]));
                        if (cluster == -1) throw new Exception("Could not find specified path");
                    }
                }
            }
            catch (Exception e)
            { 
                Console.WriteLine(e);
                return -2;
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

            if (directoryCluster == -2) return false;

            if (directoryCluster == -1) metadata.RootDirectory.Entries.Add(new Entry(name, "", metadata.Clusters.IndexOf(cluster)));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new Entry(name, "", metadata.Clusters.IndexOf(cluster)));
            
            return true;
        }

        public bool removeDirectory(string path, string name = "") 
        {
            int cluster = -1;
            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            if(name != "")
            {
                if (directoryCluster == -1) cluster = metadata.RootDirectory.Entries.Find(x => x.Name == name).StartingCluster;
                else cluster = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == name).StartingCluster;

                foreach (Entry e in ((FAT.Data.Directory)data.Clusters[cluster]).Entries)
                {
                    if (e.Type == "") removeDirectory(path + "/name", e.Name);
                    else removeFile(path + "/name", e.Name + "." + e.Type);
                }

                if (directoryCluster == -1) metadata.RootDirectory.Entries.Remove(metadata.RootDirectory.Entries.Find(x => x.StartingCluster == cluster));
                else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Remove(((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.StartingCluster == cluster));

                metadata.Clusters[cluster].Available = true;
            }
            else
            {
                if (directoryCluster == -1)
                {
                    foreach (Entry e in metadata.RootDirectory.Entries)
                    {
                        if (e.Type == "") removeDirectory(path + "/name", e.Name);
                        else removeFile(path + "/name", e.Name + "." + e.Type);
                    }
                }
                else
                {
                    foreach (Entry e in ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries)
                    {
                        if (e.Type == "") removeDirectory(path + "/name", e.Name);
                        else removeFile(path + "/name", e.Name + "." + e.Type);
                    }
                }
            }
            return true;
        }

        public bool moveDirectory(string path, string name, string newPath, string newName)
        {
            int directoryCluster = findDirectoryCluster(path);
            int newDirectoryCluster = findDirectoryCluster(newPath);

            if (directoryCluster == -2 || newDirectoryCluster == -2) return false;

            Entry directoryEntry;

            if (directoryCluster == -1)
            {
                directoryEntry = metadata.RootDirectory.Entries.Find(x => x.Name == name);
                metadata.RootDirectory.Entries.Remove(directoryEntry);
            }
            else
            {
                directoryEntry = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == name);
                ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Remove(directoryEntry);
            }

            if (newDirectoryCluster == -1) metadata.RootDirectory.Entries.Add(new Entry(newName, "", directoryEntry.StartingCluster));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new Entry(newName, "", directoryEntry.StartingCluster));

            return true;
        }

        public string listDirectory(string path)
        {
            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return "";

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

            if (directoryCluster == -2) return false;

            if (directoryCluster == -1) metadata.RootDirectory.Entries.Add(new Entry(name, fileType, metadata.Clusters.IndexOf(cluster)));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new Entry(name, fileType, metadata.Clusters.IndexOf(cluster)));

            return true;
        }

        public bool removeFile(string path, string name)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split(".")[0];

            int cluster = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            if (directoryCluster == -1) cluster = metadata.RootDirectory.Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;
            else cluster = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;

            do
            {
                metadata.Clusters[cluster].Available = true;
                cluster = metadata.Clusters[cluster].Next;
            }
            while (!metadata.Clusters[cluster].End);

            return true;
        }

        public bool moveFile(string path, string name, string newPath, string newName)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split(".")[0];

            string newfileType = newName.Split('.')[1];
            string newfileName = newName.Split(".")[0];

            int directoryCluster = findDirectoryCluster(path);
            int newDirectoryCluster = findDirectoryCluster(newPath);

            if (directoryCluster == -2 || newDirectoryCluster == -2) return false;

            Entry fileEntry;

            if (directoryCluster == -1)
            {
                fileEntry = metadata.RootDirectory.Entries.Find(x => x.Name == name);
                metadata.RootDirectory.Entries.Remove(fileEntry);
            }
            else
            {
                fileEntry = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == name);
                ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Remove(fileEntry);
            }

            if (newDirectoryCluster == -1) metadata.RootDirectory.Entries.Add(new Entry(newfileName, newfileType, fileEntry.StartingCluster));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new Entry(newfileName, newfileType, fileEntry.StartingCluster));

            return true;
        }

        public string catFile(string name, string path)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split(".")[0];
            string content = "";

            int cluster = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return "";

            if (directoryCluster == -1) cluster = metadata.RootDirectory.Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;
            else cluster = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;

            do
            {
                content += ((FAT.Data.File)data.Clusters[cluster]).ToString();
                cluster = metadata.Clusters[cluster].Next;
            }
            while (!metadata.Clusters[cluster].End);

            return content;
        }
    }
}
