using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FAT;
using static Crayon.Output;
using System.Threading;
using System.Threading.Tasks;
using Crayon;
using System.Windows.Documents;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Terminal
{
    /*  Esta clase maneja todos los comandos usando un diccionario
     *  Cuando se ejecuta un comando se llama a la funcion de ejecucion de ese comando usando la key del diccionario
     *  
     *  TODO: Agregar la descripcion a todos los archivos '.comm' que se encuentran en 'bin/Debug/net6.0/commands' usando el formato establecido en 'doc/FORMAT.html'
     */

    public class ConsoleManager
    {
        private Dictionary<string, Command> path;
        private Executions exec;
        public List<Process> taskList;
        private int pidCounter;
        public bool exit {  get; set; }

        public ConsoleManager(string descPath, string descFileType)
        {
            // Para saber que hace cada comando consultar https://en.wikibooks.org/wiki/Linux_Guide/Linux_commands
            // O escribir "comando" --help en una maquina linux, si tienes windows consultar https://apps.microsoft.com/detail/9pdxgncfsczv?rtc=1&hl=es-es&gl=ES

            exit = false;

            exec = new();

            taskList = new();
            taskList.Add(new(0, new(), "Terminal.exe", null));
            pidCounter = 1;
            
            path = new Dictionary<string, Command>()
            {
                { "cd", new Command(descPath + "cd" + descFileType, exec.cdExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#cd
                { "ls", new Command(descPath + "ls" + descFileType, exec.lsExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#ls
                { "pwd", new Command(descPath + "pwd" + descFileType, exec.pwdExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#pwd
                { "mkdir", new Command(descPath + "mkdir" + descFileType, exec.mkdirExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#mkdir
                { "rmdir", new Command(descPath + "rmdir" + descFileType, exec.rmdirExecution) }, // "rmdir --help" en maquina linux
                { "rm", new Command(descPath + "rm" + descFileType, exec.rmExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#rm
                { "cp", new Command(descPath + "cp" + descFileType, exec.cpExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#rm
                { "mv", new Command(descPath + "mv" + descFileType, exec.mvExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#mv
                { "touch", new Command(descPath + "touch" + descFileType, exec.touchExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_System_Utilities#touch
                { "file", new Command(descPath + "file" + descFileType, exec.fileExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_Analysing#file
                { "find", new Command(descPath + "find" + descFileType, exec.findExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/Finding_Files#find
                { "locate", new Command(descPath + "locate" + descFileType, exec.locateExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/Finding_Files#locate
                { "cat", new Command(descPath + "cat" + descFileType, exec.catExecution) }, // https://en.wikibooks.org/wiki/Guide_to_Unix/Commands/File_Viewing#cat
                { "grep", new Command(descPath + "grep" + descFileType, exec.grepExecution) }, // "grep --help" en maquina linux
                { "nano", new Command(descPath + "nano" + descFileType, exec.nanoExecution) }, // Editor de archivos (SI DA TIEMPO)
                { "help", new Command(descPath + "help" + descFileType, exec.helpExecution) }, // Muestra un listado con todos los comandos disponibles y una breve descripcion de cada uno
                { "cls", new Command(descPath + "cls" + descFileType, exec.clsExecution) }, // Limpia la pantalla
                { "clear", new Command(descPath + "cls" + descFileType, exec.clsExecution) }, // Limpia la pantalla
                { "exit", new Command(descPath + "exit" + descFileType, exec.exitExecution) }, // Detiene la ejecucion del programa
                { "echo", new Command(descPath + "echo" + descFileType, exec.echoExecution) }, // Detiene la ejecucion del programa
            };

            System.Threading.Tasks.Task.Run(() =>
            {
                while(!exit)
                {
                    Process? p = taskList.Find(x => x.dead);
                    if (p != null) taskList.Remove(p);
                }
            });
        }


        public void execute(string s, Fat fat, string wD)
        {
            List<string> lines = new();
            List<string> l = new();

            foreach(string sPart in s.Split(" ", StringSplitOptions.RemoveEmptyEntries))
            {
                if (sPart == ";")
                {
                    lines.Add(string.Join(" ", l));
                    l.Clear();
                }
                else l.Add(sPart);
            }

            if(l.Count > 0) { lines.Add(string.Join(" ", l)); l.Clear(); }

            foreach(string line in lines)
            {
                if (s.Contains("&&") || s.Contains("|"))
                {
                    List<string> chainedLines = new();
                    List<string> chains = new();
                    bool background = false;

                    foreach(string lPart in line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (lPart == "&&" || lPart == "|")
                        {
                            if (l.Last() == "&") { Console.WriteLine($"-Terminal: Unexpected token '&' before '{lPart}'"); return; }
                            chainedLines.Add(string.Join(" ", l));
                            chains.Add(lPart);
                            l.Clear();
                        }
                        else l.Add(lPart);
                    }

                    if (l.Count() > 0)
                    {
                        if (l.Last() == "&")
                        {
                            background = true;
                            l.Remove(l.Last());
                        }

                        chainedLines.Add(string.Join(" ", l));
                    }
                    else { Console.WriteLine($"-Terminal: Unexpected token '{line.Split(" ", StringSplitOptions.RemoveEmptyEntries).Last()}' at the end"); return; }

                    List<string> chainedCommands = new();
                    List<List<string>> chainedArgs = new();

                    foreach(string chainedLine in chainedLines)
                    {
                        string command = chainedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                        List<string> args = new();
                        if (chainedLine.Length > command.Length)
                        {
                            args = ((chainedLine.Substring(command.Length)).Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();
                        }

                        if(!path.ContainsKey(command)) { Console.WriteLine(Red().Bold().Text($"{s}: command not found, to get a list of all avaliable commands type 'help'")); return; }
                        else
                        {
                            string name = command.Split('/').Last();
                            string path = getRealPath(command.Replace(name, ""), wD);

                            if (!command.EndsWith(".sh") || !fat.fileExists(name, path)) { Console.WriteLine(Red().Bold().Text($"{s}: command not found, to get a list of all avaliable commands type 'help'")); return; }
                        }

                        chainedCommands.Add(command);
                        chainedArgs.Add(args);
                    }

                    bool success = true;
                    string? previousOutput = null;
                    chains.Add("end");

                    int pid = pidCounter;
                    pidCounter++;
                    taskList.Add(new(pid, null, line, null));

                    if (background)
                    {
                        Console.WriteLine($"[1] {pid}");

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            for(int i = 0; i<chainedCommands.Count() && success; i++)
                            {
                                if (previousOutput != null) chainedArgs[i].Insert(0, previousOutput);
                                (success, previousOutput) = executeLine(chainedCommands[i], chainedArgs[i], fat, wD, chains[i] != "|", taskList.Find(x => x.pid == pid));
                                if (chains[i] != "|") previousOutput = null;
                            }

                            if (success) Console.WriteLine($"[1]+  Done\t\t\t{line}");
                            else Console.WriteLine($"[1]+  Exit 1\t\t\t{line}");
                            taskList.Find(x => x.pid == pid).dead = true;
                        });
                        return;
                    }
                    else
                    {
                        for (int i = 0; i < chainedCommands.Count() && success; i++)
                        {
                            if (previousOutput != null) chainedArgs[i].Insert(0, previousOutput);
                            (success, previousOutput) = executeLine(chainedCommands[i], chainedArgs[i], fat, wD, chains[i] != "|", taskList.Find(x => x.pid == pid));
                            if (chains[i] != "|") previousOutput = null;
                        }

                        taskList.Find(x => x.pid == pid).dead = true;
                        return;
                    }
                }
                else
                {
                    executeLine(line, fat, wD);
                }
            }
        }

        private (bool success, string output) executeLine(string command, List<string> args, Fat fat, string wD, bool writeOutput = true, Process? parent = null)
        {
            bool background = false;

            if (path.ContainsKey(command))
            {
                int pid = pidCounter;
                pidCounter++;
                taskList.Add(new(pid, null, command, parent));

                if (background)
                {
                    Console.WriteLine($"[1] {pid}");

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        (bool success, string output) = executeCommand(command, args.ToArray(), fat, wD, writeOutput);
                        if (success) Console.WriteLine($"[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                        else Console.WriteLine($"[1]+  Exit 1\t\t\t{command} {string.Join(' ', args)}");
                        taskList.Find(x => x.pid == pid).dead = true;
                    });
                    return (true, "");
                }
                else
                {
                    (bool success, string output) = executeCommand(command, args.ToArray(), fat, wD, writeOutput);
                    taskList.Find(x => x.pid == pid).dead = true;
                    return (success, output);
                }
            }
            else
            {
                string name = command.Split('/').Last();
                string path = getRealPath(command.Replace(name, ""), wD);

                if (command.EndsWith(".sh") && fat.fileExists(name, path))
                {
                    int pid = pidCounter;
                    pidCounter++;
                    taskList.Add(new(pid, fat.catFile(name, path).Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(), name, parent));

                    if (background)
                    {
                        Console.WriteLine($"[1] {pid}");

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            (bool success, string output) = executeFile(name, path, pid, args.ToArray(), wD, writeOutput);
                            if (success) Console.WriteLine($"[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                            else Console.WriteLine($"[1]+  Exit 1\t\t\t{command} {string.Join(' ', args)}");
                            taskList.Find(x => x.pid == pid).dead = true;
                        });
                        return (true, "");
                    }
                    else
                    {
                        (bool success, string output) = executeFile(name, path, pid, args.ToArray(), wD, writeOutput);
                        taskList.Find(x => x.pid == pid).dead = true;
                        return (success, output);
                    }
                }
                else { Console.WriteLine(Red().Bold().Text($"{command} {string.Join(' ', args)}: command not found, to get a list of all avaliable commands type 'help'")); return (false, ""); }
            }
        }

        private (bool success, string output) executeLine(string s, Fat fat, string wD, bool writeOutput = true, Process? parent = null)
        {
            bool background = false;

            string command = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            List<string> args = new();
            if (s.Length > command.Length)
            {
                args = ((s.Substring(command.Length)).Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();
            }

            if (args.Count > 0 && args.Last() == "&")
            {
                background = true;
                args.Remove(args.Last());
            }

            if (path.ContainsKey(command))
            {
                int pid = pidCounter;
                pidCounter++;
                taskList.Add(new(pid, null, command, parent));

                if (background)
                {
                    Console.WriteLine($"[1] {pid}");

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        (bool success, string output) = executeCommand(command, args.ToArray(), fat, wD, writeOutput);
                        if (success) Console.WriteLine($"[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                        else Console.WriteLine($"[1]+  Exit 1\t\t\t{command} {string.Join(' ', args)}");
                        taskList.Find(x => x.pid == pid).dead = true;
                    });
                    return (true, "");
                }
                else
                {
                    (bool success, string output) = executeCommand(command, args.ToArray(), fat, wD, writeOutput);
                    taskList.Find(x => x.pid == pid).dead = true;
                    return (success, output);
                }
            }
            else
            {
                string name = command.Split('/').Last();
                string path = getRealPath(command.Replace(name, ""), wD);

                if (command.EndsWith(".sh") && fat.fileExists(name, path))
                {
                    int pid = pidCounter;
                    pidCounter++;
                    taskList.Add(new(pid, fat.catFile(name, path).Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(), name, parent));

                    if (background)
                    {
                        Console.WriteLine($"[1] {pid}");

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            (bool success, string output) = executeFile(name, path, pid, args.ToArray(), wD, writeOutput);
                            if (success) Console.WriteLine($"[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                            else Console.WriteLine($"[1]+  Exit 1\t\t\t{command} {string.Join(' ', args)}");
                            taskList.Find(x => x.pid == pid).dead = true;
                        });
                        return (true, "");
                    }
                    else
                    {
                        (bool success, string output) = executeFile(name, path, pid, args.ToArray(), wD, writeOutput);
                        taskList.Find(x => x.pid == pid).dead = true;
                        return (success, output);
                    }
                }
                else { Console.WriteLine(Red().Bold().Text($"{s}: command not found, to get a list of all avaliable commands type 'help'")); return (false, ""); }
            }
        }

        private (bool sucess, string output) executeCommand(string command, string[]? args, Fat fat, string wD, bool writeOutput)
        {
            string output = "";

            try
            {
                output = path[command].execute(args, fat, wD);
            }
            catch (Exception e)
            {
                if (e.Message == "[EXIT]") { exit = true; }
                else Console.WriteLine($"{command}: {e.Message}\nTry '{command} --help' for more information.");
                return (false, "");
            }

            if (output != "" && writeOutput) Console.WriteLine(output);
            return (true, output);
        }

        private (bool sucess, string output) executeFile(string name, string path, int pid, string[]? args, string wD, bool writeOutput)
        {
            string output = "";

            try
            {
                output = taskList.Find(x => x.pid == pid).Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{path}/{name}: {e.Message}");
                return (false, "");
            }

            if (output != "" && writeOutput) Console.WriteLine(output);
            return (true, output);
        }

        private string getRealPath(string path, string wD)
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
    }
}
