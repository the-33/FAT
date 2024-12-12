using System;
using System.Collections.Generic;
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
        public Func<string?[], Fat, string> cdExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> lsExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> pwdExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> mkdirExecution = (args, fat) =>
        {
            //string regexPattern = @"^[a-zA-Z0-9_-]+$";
            //if (!Regex.IsMatch(name, regexPattern)) Console.WriteLine(Bold().Red().Text("Names can only contain alpanumeric characters and '-' or '_'."));

            return "";
        };

        public Func<string?[], Fat, string> rmdirExecution = (args, fat) =>
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

        public Func<string?[], Fat, string> rmExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> cpExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> mvExecution = (args, fat) =>
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

        public Func<string?[], Fat, string> touchExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> fileExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> findExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> locateExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> catExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> grepExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> nanoExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> helpExecution = (args, fat) =>
        {
            return "";
        };

        public Func<string?[], Fat, string> exitExecution = (args, fat) =>
        {
            Console.WriteLine(Dim().Text("Exiting..."));
            Environment.Exit(0);
            return "";
        };

        public Func<string?[], Fat, string> clsExecution = (args, fat) =>
        {
            Console.Clear();
            return "";
        };

        public Func<string?[], Fat, string> echoExecution = (args, fat) =>
        {
            return "";
        };
    }
}
