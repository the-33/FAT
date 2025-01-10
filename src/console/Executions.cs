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
        private static (string name, string path) getRealPath(string path, string wD)
        {
            string realPath = "";

            Stack<string> realPathStack = new Stack<string>();

            if (path.StartsWith("./") || path.StartsWith("../"))
            {
                foreach (string s in wD.Split("/", StringSplitOptions.RemoveEmptyEntries)) realPathStack.Push(s);

                foreach (string s in path.Split("/", StringSplitOptions.RemoveEmptyEntries))
                {
                    switch (s)
                    {
                        case ".": continue;
                        case "..": realPathStack.Pop(); break;
                        default: realPathStack.Push(s); break;
                    }
                }

                realPath = wD + string.Join('/', realPathStack.ToArray().Reverse());
            }
            else if (realPath.StartsWith("C:/")) realPath = path;
            else realPath = wD + "/" + path;

            string returnName = "";
            string returnPath;

            if (!realPath.EndsWith("/"))
            {
                returnName = realPath.Split("/").Last();
                returnPath = realPath.Remove(realPath.Length - (returnName.Length + 1));
            }
            else returnPath = realPath;

            return (returnName, returnPath);
        }

        public Func<string[]?, string, string> cdExecution = (args, wD) =>
        {
            if (args == null) throw new Exception("No directory specified");
            if (args.Length > 1) throw new Exception("Too many arguments, expected one");

            string name, path;
            (name, path) = getRealPath(args[0], wD);

            if (name.Contains(".") && Program.fat.fileExists(name, path)) throw new Exception($"'{path}/{name}' is a file");
            else if (Program.fat.directoryExists(path + "/" + name)) Program.cM.envVars["PWD"] = path + name;
            else throw new Exception($"{args[0]}: no such file or directory");

            return "";
        };

        public Func<string[]?, string, string> lsExecution = (args, wD) =>
        {
            if (args != null && args.Length > 1) throw new Exception("Too many arguments, expected one");

            string output = "";
            if (args == null)
            {
                output = Program.fat.listDirectory(wD);
            }
            else
            {
                string name, path;
                (name, path) = getRealPath(args[0], wD);

                if (name.Contains(".") && Program.fat.fileExists(name, path)) throw new Exception($"'{path}/{name}' is a file");
                else if (Program.fat.directoryExists(path + "/" + name)) output = Program.fat.listDirectory(path + "/" + name);
                else throw new Exception($"{args[0]}: no such file or directory");
            }

            return output;
        };

        public Func<string[]?, string, string> pwdExecution = (args, wD) =>
        {
            return wD;
        };

        public Func<string[]?, string, string> mkdirExecution = (args, wD) =>
        {
            if (args == null) throw new Exception("No directories to create specified");

            bool verbose = false;
            bool parents = false;

            List<string> directories = new List<string>();

            foreach(string arg in args)
            {
                switch(arg)
                {
                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;
                    case "-p":
                    case "--parents":
                        parents = true;
                        break;
                    default:
                        directories.Add(arg);
                        break;
                }
            }

            foreach(string d in directories)
            {
                if (parents)
                {
                    //Hacer que cree padres
                }
                else
                {
                    string name, path;
                    (name, path) = getRealPath(d, wD);
                    if(Program.fat.directoryExists(path))
                    {
                        string regexPattern = @"^[a-zA-Z0-9_-]+$";
                        if (!Regex.IsMatch(name, regexPattern)) throw new Exception("Directory names can only contain alpanumeric characters and '-' or '_'");
                        Program.fat.addDirectory(name, path);
                        if (verbose) Console.WriteLine($"mkdir: Directory '{name}' created");
                    }
                    else throw new Exception($"cannot create directory '{d}': No such file or directory");
                }
            }

            return "";
        };

        public Func<string[]?, string, string> rmdirExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> rmExecution = (args, wD) =>
        {
            if (args == null) throw new Exception("No files or directories to remove specified");

            bool force = false;
            bool interactive = false;
            bool INTERACTIVE = false;
            bool recursive = false;
            bool dir = false;
            bool verbose = false;

            List<string> files = new List<string>();

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-f":
                    case "--force":
                        force = true;
                        break;
                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;
                    case "-i":
                        interactive = true;
                        break;
                    case "-I":
                        INTERACTIVE = true;
                        break;
                    case "-r":
                    case "-R":
                    case "--recursive":
                        recursive = true;
                        break;
                    case "-d":
                    case "--dir":
                        dir = true;
                        break;
                    default:
                        files.Add(arg);
                        break;
                }
            }

            if (files.Count == 0) throw new Exception("No file(s) specified");

            string destinationName, destinationPath;

            foreach (string file in files)
            {
                string name, path;
                (name, path) = getRealPath(file, wD);

                if (name == "")
                {
                    if (Program.fat.directoryExists(path)) name = "*";
                    else Console.WriteLine($"cat: {path}: No such directory");
                }

                if (name.Contains("*"))
                {
                    string regexPattern = "^" + Regex.Escape(name).Replace("\\*", ".*") + "$";
                    foreach (string fileName in Program.fat.listDirectory(path).Split("   ", StringSplitOptions.RemoveEmptyEntries))
                    {
                        List<string> toRemove = new();
                        if (Regex.IsMatch(fileName, regexPattern))
                        {
                            toRemove.Add(fileName);
                        }

                        string response = "y";
                        if(((toRemove.Count > 3 && INTERACTIVE) || interactive) && !force)
                        {
                            while (response != "y" || response != "n")
                            {
                                Console.WriteLine($"{toRemove.Count} file(s) or directories will be tried to delete, do you want to continue? (y/n)");
                                response = Console.ReadLine();
                            }
                        }

                        if(response == "y")
                        {
                            foreach(string s in toRemove)
                            {
                                if(s.Contains("."))
                                {
                                    Program.fat.removeFile(s, path);
                                }
                                else
                                {
                                    if (Program.fat.listDirectory(path + s).Split("   ", StringSplitOptions.RemoveEmptyEntries).Count() == 0)
                                    {
                                        if (dir) Program.fat.removeDirectory(path, s);
                                        else Console.WriteLine($"Directory {path}/{s} is empty, try using option -d or --dir");
                                    }
                                    else
                                    {
                                        if (recursive) Program.fat.removeDirectory(path, s);
                                        else Console.WriteLine($"Directory {path}/{s} is not empty, try using option -r, -R or --recursive");
                                    }
                                }
                            }
                        }
                    }
                }
                else if (name.Contains("."))
                {
                    string response = "y";
                    if (interactive && !force)
                    {
                        while (response != "y" || response != "n")
                        {
                            Console.WriteLine($"File {name} will be deleted, do you want to continue? (y/n)");
                            response = Console.ReadLine();
                        }
                    }
                    if (response == "y") Program.fat.removeFile(name, path);
                }
                else
                {
                    string response = "y";
                    if (interactive && !force)
                    {
                        while (response != "y" || response != "n")
                        {
                            Console.WriteLine($"Directory {name} will be tried to delete, do you want to continue? (y/n)");
                            response = Console.ReadLine();
                        }
                    }
                    if (response == "y")
                    {
                        if (Program.fat.listDirectory(path + name).Split("   ", StringSplitOptions.RemoveEmptyEntries).Count() == 0)
                        {
                            if (dir) Program.fat.removeDirectory(path, name);
                            else Console.WriteLine($"Directory {path}/{name} is empty, try using option -d or --dir");
                        }
                        else
                        {
                            if (recursive) Program.fat.removeDirectory(path, name);
                            else Console.WriteLine($"Directory {path}/{name} is not empty, try using option -r, -R or --recursive");
                        }
                    }
                }
            }

            return "";
        };

        public Func<string[]?, string, string> cpExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> mvExecution = (args, wD) =>
        {
            if (args == null) throw new Exception("No files or directories to move specified");

            bool force = false;
            bool verbose = false;

            int mode = 0;

            List<string> sources = new List<string>();
            string destination = "";

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-f":
                    case "--force":
                        force = true;
                        break;
                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;
                    default:
                        if (arg == args.Last()) destination = arg;
                        else sources.Add(arg);
                        break;
                }
            }

            if (destination == "") throw new Exception("No destination specified");
            if (sources.Count == 0) throw new Exception("No source(s) specified");

            string destinationName, destinationPath;

            if (!destination.Contains("."))
            {
                mode = 2;
                string name, path;
                (name, path) = getRealPath(destination, wD);

                if(name == "")
                {
                    if (!Program.fat.directoryExists(path)) throw new Exception($"Cannot move files to '{destination}': no such file or directory");
                }
                else
                {
                    if (!Program.fat.directoryExists(path + "/" + name)) throw new Exception($"Cannot move files to '{destination}': no such file or directory");
                }

                destinationName = name;
                destinationPath = path;
            }
            else
            {
                mode = 1;
                string name, path;
                (name, path) = getRealPath(destination, wD);
                if (!Program.fat.directoryExists(path)) throw new Exception($"Cannot move file to '{destination}': no such file or directory");

                destinationName = name;
                destinationPath = path;
            }

            if (mode == 1)
            {
                foreach(string source in sources)
                {
                    string name, path;
                    (name, path) = getRealPath(source, wD);

                    if (name == "")
                    {
                        if (Program.fat.directoryExists(path)) name = "*";
                        else Console.WriteLine($"cat: {path}: No such directory");
                    }

                    if (name.Contains("*"))
                    {
                        string regexPattern = "^" + Regex.Escape(name).Replace("\\*", ".*") + "$";
                        foreach (string fileName in Program.fat.listDirectory(path).Split("   ", StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (Regex.IsMatch(fileName, regexPattern))
                            {
                                if (Program.fat.fileExists(destinationName, destinationPath) && !force)
                                {
                                    string response = "";
                                    while (response != "y" || response != "n")
                                    {
                                        Console.WriteLine($"File '{destinationPath}/{destinationName}' will be overwritten, do you want to continue? (y/n)");
                                        response = Console.ReadLine();
                                    }
                                    if (response == "n") continue;
                                }
                                Program.fat.moveFile(path, fileName, destinationPath, destinationName);
                                if (verbose) Console.WriteLine($"mv: File {fileName} moved");
                            }
                        }
                    }
                    else if (name.Contains(".") && Program.fat.fileExists(name, path))
                    {
                        if (Program.fat.fileExists(destinationName, destinationPath) && !force)
                        {
                            string response = "";
                            while (response != "y" || response != "n")
                            {
                                Console.WriteLine($"File '{destinationPath}/{destinationName}' will be overwritten, do you want to continue? (y/n)");
                                response = Console.ReadLine();
                            }
                            if (response == "n") continue;
                        }
                        Program.fat.moveFile(path, name, destinationPath, destinationName);
                        if (verbose) Console.WriteLine($"mv: File {name} moved");
                    }
                    else
                    {
                        if (Program.fat.directoryExists(path + name)) Console.WriteLine($"cat: {source}: Is a directory");
                        else Console.WriteLine($"cat: {source}: No such file or directory");
                    }
                }
            }
            else if (mode == 2)
            {
                foreach (string source in sources)
                {
                    string name, path;
                    (name, path) = getRealPath(source, wD);

                    if (name == "")
                    {
                        if (Program.fat.directoryExists(path)) name = "*";
                        else Console.WriteLine($"cat: {path}: No such directory");
                    }

                    if (name.Contains("*"))
                    {
                        string regexPattern = "^" + Regex.Escape(name).Replace("\\*", ".*") + "$";
                        foreach (string fileName in Program.fat.listDirectory(path).Split("   ", StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (Regex.IsMatch(fileName, regexPattern))
                            {
                                if (fileName.Contains("."))
                                {
                                    if (Program.fat.fileExists(fileName, destinationPath + "/" + destinationName) && !force)
                                    {
                                        string response = "";
                                        while (response != "y" || response != "n")
                                        {
                                            Console.WriteLine($"File '{destinationPath}/{destinationName}/{fileName}' will be overwritten, do you want to continue? (y/n)");
                                            response = Console.ReadLine();
                                        }
                                        if (response == "n") continue;
                                    }
                                    Program.fat.moveFile(path, fileName, destinationPath + "/" + destinationName);
                                    if (verbose) Console.WriteLine($"mv: File {fileName} moved");
                                }
                                else
                                {
                                    if (Program.fat.directoryExists(destinationPath + "/" + destinationName + "/" + fileName) && !force)
                                    {
                                        string response = "";
                                        while (response != "y" || response != "n")
                                        {
                                            Console.WriteLine($"Directory '{destinationPath}/{destinationName}/{fileName}' will be overwritten, do you want to continue? (y/n)");
                                            response = Console.ReadLine();
                                        }
                                        if (response == "n") continue;
                                    }
                                    Program.fat.moveDirectory(path, fileName, destinationPath + "/" + destinationName);
                                    if (verbose) Console.WriteLine($"mv: Directory {fileName} moved");
                                }
                            }
                        }
                    }
                    else if (name.Contains(".") && Program.fat.fileExists(name, path))
                    {
                        if (Program.fat.fileExists(name, destinationPath + "/" + destinationName) && !force)
                        {
                            string response = "";
                            while (response != "y" || response != "n")
                            {
                                Console.WriteLine($"File '{destinationPath}/{destinationName}/{name}' will be overwritten, do you want to continue? (y/n)");
                                response = Console.ReadLine();
                            }
                        }
                        Program.fat.moveFile(path, name, destinationPath + "/" + destinationName);
                        if (verbose) Console.WriteLine($"mv: File {name} moved");
                    }
                    else if (!name.Contains(".") && Program.fat.directoryExists(name + "/" + path))
                    {
                        if (destinationName == "")
                        {
                            if (Program.fat.directoryExists(destinationPath + "/" + name) && !force)
                            {
                                string response = "";
                                while (response != "y" || response != "n")
                                {
                                    Console.WriteLine($"Directory '{destinationPath}/{name}' will be overwritten, do you want to continue? (y/n)");
                                    response = Console.ReadLine();
                                }
                                if (response == "n") continue;
                            }
                            Program.fat.moveDirectory(path, name, destinationPath);
                            if (verbose) Console.WriteLine($"mv: File {name} moved");
                        }
                        else
                        {
                            if (Program.fat.directoryExists(destinationPath + "/" + destinationName) && !force)
                            {
                                string response = "";
                                while (response != "y" || response != "n")
                                {
                                    Console.WriteLine($"Directory '{destinationPath}/{destinationName}' will be overwritten, do you want to continue? (y/n)");
                                    response = Console.ReadLine();
                                }
                                if (response == "n") continue;
                            }
                            Program.fat.moveDirectory(path, name, destinationPath, destinationName);
                            if (verbose) Console.WriteLine($"mv: File {name} moved");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"cat: {source}: No such file or directory");
                    }
                }
            }

            return "";
        };

        public Func<string[]?, string, string> touchExecution = (args, wD) =>
        {

            return "";
        };

        public Func<string[]?, string, string> fileExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> findExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> locateExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> catExecution = (args, wD) =>
        {
            bool showEnds = false;
            bool showTabs = false;
            bool squeezeBlank = false;
            bool number = false;
            bool numberNonBlank = false;

            List<string> files = new List<string>();

            if(args != null)
            {
                foreach (string arg in args)
                {
                    switch (arg)
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
                        case "-b":
                        case "--number-nonblank":
                            numberNonBlank = true;
                            break;
                        default:
                            if (arg.StartsWith("-") || arg.StartsWith("--")) throw new Exception($"Invalid option '{arg}'");
                            files.Add(arg);
                            break;
                    }
                }
            }

            List<string> inputList = new();
            string input = "";
            string output = "";

            if (files.Count > 0)
            {
                foreach (string file in files)
                {
                    string name, path;
                    (name, path) = getRealPath(file, wD);

                    if (name == "")
                    {
                        if (Program.fat.directoryExists(path)) name = "*";
                        else inputList.Add($"cat: {path}: No such directory");
                    }

                    if (name.Contains("*"))
                    {
                        string regexPattern = "^" + Regex.Escape(name).Replace("\\*", ".*") + "$";
                        foreach (string fileName in Program.fat.listDirectory(path).Split("   ", StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (Regex.IsMatch(fileName, regexPattern))
                            {
                                inputList.Add(Program.fat.catFile(fileName, path));
                            }
                        }
                    }
                    else if (name.Contains(".") && Program.fat.fileExists(name, path)) inputList.Add(Program.fat.catFile(name, path));
                    else
                    {
                        if (Program.fat.directoryExists(path + name)) inputList.Add($"cat: {file}: Is a directory");
                        else inputList.Add($"cat: {file}: No such file or directory");
                    }
                }

                input = string.Join("\n", inputList);

                if (showEnds) input = input.Replace("\n", "$\n");
                if (showTabs) input = input.Replace("\t", "^I");
                if (squeezeBlank) input = string.Join("\n", input.Split("\n", StringSplitOptions.RemoveEmptyEntries));

                if (numberNonBlank)
                {
                    string[] lines = input.Split("\n");

                    if (lines.Length == 0) output = "\t1  " + input;
                    else
                    {
                        int i = 1;
                        List<string> outputList = new();
                        foreach (string line in lines)
                        {
                            if (line != "" && (showEnds && line != "$")) { outputList.Add($"\t{i}  {line}"); i++; }
                            else outputList.Add(line);
                        }
                        output = string.Join("\n", outputList);
                    }
                }
                else if (number)
                {
                    string[] lines = input.Split("\n");

                    if (lines.Length == 0) output = "\t1  " + input;
                    else
                    {
                        List<string> outputList = new();
                        for (int i = 0; i < lines.Length; i++) outputList.Add($"\t{i + 1}  {lines[i]}");
                        output = string.Join("\n", outputList);
                    }
                }
                else output = (input);
            }
            else throw new Exception("No file(s) specified");

            return output;
        };

        public Func<string[]?, string, string> grepExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> nanoExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> helpExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> exitExecution = (args, wD) =>
        {
            Program.cM.exit = true;
            return Dim().Text("Exiting...");
        };

        public Func<string[]?, string, string> clsExecution = (args, wD) =>
        {
            Console.Clear();
            return "";
        };

        public Func<string[]?, string, string> echoExecution = (args, wD) =>
        {
            List<string> outputs = new();

            if (args != null)
            {
                foreach (string arg in args) outputs.Add(arg);
            }

            return string.Join(" ", outputs);
        };

        public Func<string[]?, string, string> setExecution = (args, wD) =>
        {
            return "";
        };

        public Func<string[]?, string, string> psExecution = (args, wD) =>
        {
            if (args != null) throw new Exception("Too many arguments, expected cero");
            Console.WriteLine("PID\tNAME");
            return string.Join("\n", Program.cM.taskList);
        };

        public Func<string[]?, string, string> killExecution = (args, wD) =>
        {
            List<int> pids = new();
            if (args != null)
            {
                foreach (string arg in args)
                {
                    int pid;
                    if (!int.TryParse(arg, out pid)) throw new Exception($"{arg} arguments must be pids");
                    pids.Add(pid);
                }
            }

            if(pids.Contains(0))
            {
                Program.cM.exit = true;
                return Dim().Text("Exiting...");
            }

            foreach (int pid in pids)
            {
                if (!Program.cM.taskList.Exists(x => x.pid == pid)) Console.WriteLine($"-bash: kill: ({pid}) - No such process");
                else
                {
                    Program.cM.taskList.Find(x => x.pid == pid).kill();
                }
            }
            
            return "";
        };

        public Func<string[]?, string, string> sleepExecution = (args, wD) =>
        {
            if (args == null) throw new Exception("Number of seconds to sleep expected");
            if (args.Length > 1) throw new Exception("Too many arguments, expected one");
            int miliseconds = 0;
            if (!int.TryParse(args[0], out miliseconds)) throw new Exception($"'{args[0]}' argument must be a number of seconds");
            Thread.Sleep(miliseconds*1000);
            return "";
        };
    }
}
