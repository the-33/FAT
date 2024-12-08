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
        const int clusterSize = 30;

        private struct Metadata
        {
            BootCode BootCode { get; set; }
            public string FatCopyPath {  get; set; }
            public List<ClusterMetadata> Clusters { get; set; }
            public RootDirectory RootDirectory { get; set; }

            public Metadata(string fatCopyPath = "")
            {
                BootCode = new BootCode();
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
                    process.StartInfo.Arguments = @"/c cd";
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
        };

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
                if (Type == "") return "📁 " + Name;
                else return "📄 " + Name + "." + Type;
            }
        };

        private Metadata metadata;
        private Data data;

        public Fat(string fatCopyPath = "")
        {
            metadata = new Metadata(fatCopyPath);
            data = new Data();
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
                    cluster = metadata.RootDirectory.Entries.IndexOf(metadata.RootDirectory.Entries.Find(e => e.Name == routing[0]));
                    if (cluster == -1) throw new Exception(Bold().Red().Text("Could not find " + path + "\n"));
                    for (int i = 1; i < routing.Length; i++)
                    {
                        FAT.Data.Directory d = (FAT.Data.Directory)data.Clusters[cluster];
                        cluster = d.Entries.IndexOf(d.Entries.Find(e => e.Name == routing[i]));
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
            ClusterMetadata? cluster = metadata.Clusters.Find(x => x.Available == true);

            if (cluster == null)
            {
                cluster = new ClusterMetadata();
                metadata.Clusters.Add(cluster);
                data.Clusters.Add(new FAT.Data.Directory());
            }
            else
            {
                data.Clusters[metadata.Clusters.IndexOf(cluster)] = new FAT.Data.Directory();
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
                    if (e.Type == "") removeDirectory(path + "/" + name, e.Name);
                    else removeFile(path + "/" + name, e.Name + "." + e.Type);
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
                        if (e.Type == "") removeDirectory(path + "/" + name, e.Name);
                        else removeFile(path + "/" + name, e.Name + "." + e.Type);
                    }
                }
                else
                {
                    foreach (Entry e in ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries)
                    {
                        if (e.Type == "") removeDirectory(path + "/" + name, e.Name);
                        else removeFile(path + "/" + name, e.Name + "." + e.Type);
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

            foreach (Entry e in ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries)
            {
                if (e.Type == "") copyDirectory(path + "/" + name, e.Name, newPath + "/" + newName, e.Name);
                else copyFile(path + "/" + name, e.Name, newPath + "/" + newName, e.Name);
            }

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
            string fileName = name.Split('.')[0];

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            ClusterMetadata? cluster = metadata.Clusters.Find(x => x.Available == true);

            if (cluster == null)
            {
                cluster = new ClusterMetadata();
                metadata.Clusters.Add(cluster);
                data.Clusters.Add(new FAT.Data.File(clusterSize));
            }
            else
            {
                data.Clusters[metadata.Clusters.IndexOf(cluster)] = new FAT.Data.File(clusterSize);
            }

            cluster.Available = false;
            cluster.End = true;
            cluster.Next = -1;

            if (directoryCluster == -1) metadata.RootDirectory.Entries.Add(new Entry(fileName, fileType, metadata.Clusters.IndexOf(cluster)));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new Entry(fileName, fileType, metadata.Clusters.IndexOf(cluster)));

            return true;
        }

        public bool removeFile(string name, string path)
        {
            string fileType = name.Split('.')[1];
            string fileName = name.Split('.')[0];

            int cluster = -1;

            int directoryCluster = findDirectoryCluster(path);

            if (directoryCluster == -2) return false;

            if (directoryCluster == -1) cluster = metadata.RootDirectory.Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;
            else cluster = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;

            if (cluster == -1)
            {
                Console.WriteLine(Bold().Red().Text("Could not find the file " + path + "/" + name + "/n"));
                return false;
            }

            while (!metadata.Clusters[cluster].End)
            {
                metadata.Clusters[cluster].Available = true;
                if (!metadata.Clusters[cluster].End) cluster = metadata.Clusters[cluster].Next;
            }

            metadata.Clusters[cluster].Available = true;

            if (directoryCluster == -1) metadata.RootDirectory.Entries.Remove(metadata.RootDirectory.Entries.Find(x => x.Name == fileName && x.Type == fileType));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Remove(((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == fileName && x.Type == fileType));

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
                fileEntry = metadata.RootDirectory.Entries.Find(x => x.Name == fileName && x.Type == fileType);
                metadata.RootDirectory.Entries.Remove(fileEntry);
            }
            else
            {
                fileEntry = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == fileName && x.Type == fileType);
                ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Remove(fileEntry);
            }

            if (newDirectoryCluster == -1) metadata.RootDirectory.Entries.Add(new Entry(newfileName, newfileType, fileEntry.StartingCluster));
            else ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Add(new Entry(newfileName, newfileType, fileEntry.StartingCluster));

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

            if (directoryCluster == -1) cluster = metadata.RootDirectory.Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;
            else cluster = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;

            if (cluster == -1)
            {
                Console.WriteLine(Bold().Red().Text("Could not find the file " + path + "/" + name + "/n"));
                return false;
            }

            ClusterMetadata fileCluster;

            while (packets.Count > 0) 
            {
                fileCluster = metadata.Clusters[cluster];
                ((FAT.Data.File)data.Clusters[cluster]).Data = Encoding.UTF8.GetBytes(packets.Dequeue());

                fileCluster.Available = false;

                if(packets.Count > 0)
                {
                    ClusterMetadata? nextCluster = metadata.Clusters.Find(x => x.Available == true);

                    if(nextCluster == null)
                    {
                        nextCluster = new ClusterMetadata();
                        metadata.Clusters.Add(nextCluster);
                        data.Clusters.Add(new FAT.Data.File(clusterSize));
                    }

                    fileCluster.Next = metadata.Clusters.IndexOf(nextCluster);
                    fileCluster.End = false;

                    cluster = fileCluster.Next;
                }
                else
                {
                    fileCluster.Next = -1;
                    fileCluster.End = true;
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

            if (directoryCluster == -1) cluster = metadata.RootDirectory.Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;
            else cluster = ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries.Find(x => x.Name == fileName && x.Type == fileType).StartingCluster;

            while (!metadata.Clusters[cluster].End)
            {
                content += ((FAT.Data.File)data.Clusters[cluster]).ToString();
                if(!metadata.Clusters[cluster].End) cluster = metadata.Clusters[cluster].Next;
            }

            content += ((FAT.Data.File)data.Clusters[cluster]).ToString();

            return content;
        }
    }
}
