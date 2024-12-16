using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FAT.Data;
using FAT.MetaData;
using Microsoft.VisualBasic.FileIO;
using static Crayon.Output;

namespace FAT
{
    /*TODO:
     * Mostrar metadatos DONE
     * 
     * Crear directorio DONE
     * Eliminar directorio DONE
     * Mover directorio DONE
     * Copiar directorio DONE
     * Mostrar contenido directorio DONE
     * 
     * Crear archivo DONE
     * Eliminar archivo DONE
     * Mover archivo DONE
     * Copiar archivo DONE
     * Sobreescribir archivo DONE
     * Escribir en archivo DONE
     * Mostrar contenido de archivo DONE
     */

    public class Fat
    {
        public struct Metadata
        {
            public BootCode bootCode { get; set; }
            public string fatCopyPath {  get; set; }
            public List<ClusterMetadata> clusters { get; set; }
            public RootDirectory rootDirectory { get; set; }

            public Metadata(string fatCopyPath = "")
            {
                bootCode = new BootCode();
                this.fatCopyPath = fatCopyPath;
                clusters = new List<ClusterMetadata>();
                rootDirectory = new RootDirectory();
            }

            public override string ToString()
            {
                string fullFatCopyPath = "";

                using (Process process = new Process()) // Obtiene el nombre de usuario y el nombre del equipo
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = @"/c cd";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    StreamReader reader = process.StandardOutput;
                    string output = reader.ReadToEnd();

                    fullFatCopyPath = output.Replace("\n", "") + "/" + fatCopyPath;
                }

                string returnStr = "";

                returnStr += "[Boot Code]\n\n" + bootCode + "\n\n";
                returnStr += "[Fat Copy Path] " + fullFatCopyPath + "\n\n";
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
        };

        public struct Entry
        {
            public string name { get; set; }
            public string type { get; set; }
            public int startingCluster { get; set; }

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

        public Fat(int clusterSize, string fatCopyPath)
        {
            metadata = new Metadata(fatCopyPath);
            data = new Data();
            this.clusterSize = clusterSize;
        }

        public void showMetadata()
        {
            Console.Write(metadata);
        }

        private int findDirectoryCluster(string path)
        {
            path = path.Replace("C:/", "");
            string[] routing = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            int cluster = -1;

            try
            {
                if (routing.Length > 0)
                {
                    cluster = metadata.rootDirectory.entries.IndexOf(metadata.rootDirectory.entries.Find(e => e.name == routing[0]));
                    if (cluster == -1) throw new Exception(Bold().Red().Text("Could not find " + path + "\n"));
                    for (int i = 1; i < routing.Length; i++)
                    {
                        FAT.Data.Directory d = (FAT.Data.Directory)data.clusters[cluster];
                        cluster = d.entries.IndexOf(d.entries.Find(e => e.name == routing[i]));
                        if (cluster == -1) throw new Exception(Bold().Red().Text("Could not find " + path + "\n"));
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
            
            return true;
        }

        public bool removeDirectory(string path, string name = "") 
        {
            int cluster = -1;
            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            if(name != "")
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
            else ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Add(new Entry(newName, "", directoryEntry.startingCluster));

            return true;
        }

        public bool copyDirectory(string path, string name, string newPath, string newName = "")
        {
            if (newName == "") newName = name;

            int directoryCluster = findDirectoryCluster(path + "/" + name);

            if (directoryCluster == -2) return false;
            if (directoryCluster == -1)
            {
                Console.WriteLine(Bold().Red().Text("You can not copy the root directory"));
                return false;
            }

            bool success = addDirectory(newPath, newName);

            if (!success)
            {
                Console.WriteLine(Bold().Red().Text("Could not copy the directory " + path + "/" + name + " to " + newPath + "/" + newName + "\n"));
                return false;
            }

            foreach (Entry e in ((FAT.Data.Directory)data.clusters[directoryCluster]).entries)
            {
                if (e.type == "") copyDirectory(path + "/" + name, e.name, newPath + "/" + newName, e.name);
                else copyFile(path + "/" + name, e.name, newPath + "/" + newName, e.name);
            }

            return true;
        }

        public string listDirectory(string path)
        {
            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return "";

            if (directoryCluster == -1) return metadata.rootDirectory.ToString();
            else return ((FAT.Data.Directory)data.clusters[directoryCluster]).ToString();
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
                Console.WriteLine(Bold().Red().Text("Could not find the file " + path + "/" + name + "/n"));
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
            else ((FAT.Data.Directory)data.clusters[directoryCluster]).entries.Add(new Entry(newfileName, newfileType, fileEntry.startingCluster));

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

            bool success = addFile(newName, newPath);

            if (!success)
            {
                Console.WriteLine(Bold().Red().Text("Could not copy the file " + path + "/" + name + " to " + newPath + "/" + newName + "\n"));
                return false;
            }

            success = writeToFile(newName, newPath, catFile(name, path), true);

            if (!success)
            {
                Console.WriteLine(Bold().Red().Text("Could not copy the file " + path + "/" + name + " to " + newPath + "/" + newName + "\n"));
                removeFile(newName, newPath);
                return false;
            }

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

            for (int i = 0, j = 0; i<content.Length; i++, j++)
            {
                packet += content[i];
                if (packet.Length == clusterSize || (i+1) == content.Length)
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
                Console.WriteLine(Bold().Red().Text("Could not find the file " + path + "/" + name + "/n"));
                return false;
            }

            ClusterMetadata fileCluster;

            while (packets.Count > 0) 
            {
                fileCluster = metadata.clusters[cluster];
                ((FAT.Data.File)data.clusters[cluster]).data = Encoding.UTF8.GetBytes(packets.Dequeue());

                fileCluster.available = false;

                if(packets.Count > 0)
                {
                    ClusterMetadata? nextCluster = metadata.clusters.Find(x => x.available == true);

                    if(nextCluster == null)
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
                if(!metadata.clusters[cluster].end) cluster = metadata.clusters[cluster].next;
            }

            content += ((FAT.Data.File)data.clusters[cluster]).ToString();

            return content;
        }
    }
}
