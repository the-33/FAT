using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using FAT;
using static Crayon.Output;

namespace Terminal
{
    /*  Esta clase contiene las lambdas que usan cada comando para ejecutarse.
     *  Las lambdas tienen como entrada los argumentos del comando y la fat.
     *  Su salida es una string que sera el error que pueda dar el comando, 
     *  si la ejecucion es correcta el return sera un string vacio.
     *  Cada lambda debe poder gestionar todas las flags posibles del comando
     *  y debe controlar todas las posibles excepciones devolviendo el error como string.
     *  Los comandos gestionaran la fat usando los metodos propios de esta.
     *  
     *  TODO: Implementar todos los comandos una vez tengamos la FAT
     */

    public class Executions
    {
        public static string getRealPath(string path, string wD)
        {
            string realPath = "";
            Stack<string> realPathStack = new Stack<string>();

            if (path.StartsWith(".") || path.StartsWith(".."))
            {
                foreach (string s in wD.Split("/")) realPathStack.Push(s);

                foreach (string s in path.Split("/"))
                {
                    switch (s)
                    {
                        case ".": continue;
                        case "..": realPathStack.Pop(); break;
                        default: realPathStack.Push(s); break;
                    }
                }

                realPath = string.Join('/', realPathStack.ToArray());
            }
            else if (realPath.StartsWith("C:/")) realPath = path;
            else realPath = wD + "/" + path;

            return realPath;
        }

        public Func<string?[], Fat, string, string> cdExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> lsExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> pwdExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> mkdirExecution = (args, fat, wD) =>
        {
            //string regexPattern = @"^[a-zA-Z0-9_-]+$";
            //if (!Regex.IsMatch(name, regexPattern)) Console.WriteLine(Bold().Red().Text("Names can only contain alpanumeric characters and '-' or '_'."));

            return "";
        };

        public Func<string?[], Fat, string, string> rmdirExecution = (args, fat, wD) =>
        {
            //if (name.Contains('*'))
            //{
            //    string regexPattern = "^" + Regex.Escape(name).Replace("\\*", ".*") + "$";

            //    if (directoryCluster == -1)
            //    {
            //        foreach (Entry e in metadata.RootDirectory.Entries)
            //        {
            //            if (Regex.IsMatch(e.Name, regexPattern) && e.Type == "") removeDirectory(path, e.Name);
            //        }
            //    }
            //    else
            //    {
            //        foreach (Entry e in ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries)
            //        {
            //            if (Regex.IsMatch(e.Name, regexPattern) && e.Type == "") removeDirectory(path, e.Name);
            //        }
            //    }
            //}

            return "";
        };

        public Func<string?[], Fat, string, string> rmExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> cpExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> mvExecution = (args, fat, wD) =>
        {
            //if (name.Contains('*'))
            //{
            //    if (newName != "") Console.WriteLine(Bold().Red().Text("You can't move more than one directory to another and assign them the same name."));

            //    string regexPattern = "^" + Regex.Escape(name).Replace("\\*", ".*") + "$";

            //    if (directoryCluster == -1)
            //    {
            //        foreach (Entry e in metadata.RootDirectory.Entries)
            //        {
            //            if (Regex.IsMatch(e.Name, regexPattern) && e.Type == "") moveDirectory(path, e.Name, newPath);
            //        }
            //    }
            //    else
            //    {
            //        foreach (Entry e in ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries)
            //        {
            //            if (Regex.IsMatch(e.Name, regexPattern) && e.Type == "") moveDirectory(path, e.Name, newPath);
            //        }
            //    }

            //    return true;
            //}

            //if (name.Contains('*'))
            //{
            //    if (newName != "") Console.WriteLine(Bold().Red().Text("You can't move more than one file to the same directory and assign them the same name"));

            //    string regexPattern = "^" + Regex.Escape(name).Replace("\\*", ".*") + "$";

            //    if (directoryCluster == -1)
            //    {
            //        foreach (Entry e in metadata.RootDirectory.Entries)
            //        {
            //            if (Regex.IsMatch(e.Name + "." + e.Type, regexPattern)) moveFile(path, e.Name, newPath);
            //        }
            //    }
            //    else
            //    {
            //        foreach (Entry e in ((FAT.Data.Directory)data.Clusters[directoryCluster]).Entries)
            //        {
            //            if (Regex.IsMatch(e.Name + "." + e.Type, regexPattern)) moveFile(path, e.Name, newPath);
            //        }
            //    }
            //}

            return "";
        };

        public Func<string?[], Fat, string, string> touchExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> fileExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> findExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> locateExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> catExecution = (args, fat, wD) =>
        {
            bool showEnds = false;
            bool showTabs = false;
            bool squeezeBlank = false;
            bool number = false;

            List<string[]> files = new List<string[]>();

            foreach(string arg in args)
            {
                switch(arg)
                {
                    case "-A":
                    case "--show-all":
                        showEnds = true;
                        showTabs = true;
                        break;
                    case "-E":
                    case "--show-ends":
                        showEnds = true;
                        break;
                    case "-n":
                    case "--number":
                        number = true;
                        break;
                    case "-s":
                    case "--squeeze-blank":
                        squeezeBlank = true;
                        break;
                    case "-T":
                    case "--show-tabs":
                        showTabs = true;
                        break;
                    default:
                        if (arg.StartsWith("-") || arg.StartsWith("--")) throw new Exception($"");
                        string name = arg.Split('/').Last();
                        string path = getRealPath(arg.Replace(name, ""), wD);
                        if (fat.fileExists(name, path)) files.Add(new string[] { name, path });
                        else throw new Exception($"");
                        break;
                }
            }

            string output = "";

            foreach(string[] file in files)
            {
                output += fat.catFile(file[0], file[1]);
            }

            return output;
        };

        public Func<string?[], Fat, string, string> grepExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> nanoExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> helpExecution = (args, fat, wD) =>
        {
            return "";
        };

        public Func<string?[], Fat, string, string> exitExecution = (args, fat, wD) =>
        {
            Console.WriteLine(Dim().Text("Exiting..."));
            throw new Exception("[EXIT]");
            return "";
        };

        public Func<string?[], Fat, string, string> clsExecution = (args, fat, wD) =>
        {
            Console.Clear();
            return "";
        };

        public Func<string?[], Fat, string, string> echoExecution = (args, fat, wD) =>
        {
            return "";
        };
    }
}
