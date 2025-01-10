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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Security.Cryptography;

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
        public List<Process> taskList { get; set; }
        public Dictionary<string, string> envVars {  get; set; }
        private int pidCounter;
        public bool exit {  get; set; }

        public const string EXECUTABLE_FILE_EXTENSION = ".sh";

        ConsoleCancelEventHandler? cancelHandler;

public ConsoleManager(string descPath, string descFileType)
        {
            // Para saber que hace cada comando consultar https://en.wikibooks.org/wiki/Linux_Guide/Linux_commands
            // O escribir "comando" --help en una maquina linux, si tienes windows consultar https://apps.microsoft.com/detail/9pdxgncfsczv?rtc=1&hl=es-es&gl=ES

            exit = false;

            exec = new();

            taskList = new() { 
                new(0, new(), "Terminal.exe", null, "C:/") 
            };
            pidCounter = 1;
            cancelHandler = null;
            
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
                { "echo", new Command(descPath + "echo" + descFileType, exec.echoExecution) }, // Muestra texto por pantalla
                { "set", new Command(descPath + "set" + descFileType, exec.setExecution) }, // Setea una variable de entorno
                { "kill", new Command(descPath + "kill" + descFileType, exec.killExecution) }, // Mata una tarea
                { "ps", new Command(descPath + "ps" + descFileType, exec.psExecution) }, // Muestra los procesos en ejecucion
                { "sleep", new Command(descPath + "sleep" + descFileType, exec.sleepExecution) }, // Muestra los procesos en ejecucion
            };

            string? computerName, userName;

            using (System.Diagnostics.Process process = new System.Diagnostics.Process()) // Obtiene el nombre de usuario y el nombre del equipo
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = @"/c whoami";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                StreamReader reader = process.StandardOutput;
                string output = reader.ReadToEnd();

                computerName = output.Split('\\')[0];
                userName = output.Split('\\')[1].Substring(0, output.Split('\\')[1].Length - 2);
            }

            if (computerName == "null") computerName = "PC";
            if (userName == null) userName = "user";

            envVars = new Dictionary<string, string>()
            {
                { "PWD", "C:/" },
                { "HOME", "C:/" },
                { "USER", userName },
                { "HOSTNAME", computerName }
            };

            System.Threading.Tasks.Task.Run(() =>
            {
                while(true)
                {
                    if (taskList.Exists(x => x.dead == true)) taskList.Remove(taskList.Find(x => x.dead == true));
                    Thread.Sleep(100);
                }
            });
        }

        public void execute(string s, string wD)
        {
            List<string> lines = new();
            List<string> l = new();

            if (s.Contains(";")) lines = s.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList();
            else lines.Add(s);

            if(l.Count > 0) { lines.Add(string.Join(" ", l)); l.Clear(); }

            foreach(string line in lines)
            {
                if (line.Contains("&&") || line.Contains("|"))
                {
                    List<string> chainedLines = new();
                    List<string> chains = new();
                    bool background = false;

                    foreach(string lPart in line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (lPart == "&&" || lPart == "|")
                        {
                            if (l.Last() == "&") { Console.Write($"-Terminal: Unexpected token '&' before '{lPart}'"); return; }
                            if (l.Exists(x => x == ">") && lPart == "|") { Console.Write($"-Terminal: Unexpected token '>' before '|'"); return; }
                            if (l.Exists(x => x == ">>") && lPart == "|") { Console.Write($"-Terminal: Unexpected token '>>' before '|'"); return; }
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
                    else { Console.Write($"-Terminal: Unexpected token '{line.Split(" ", StringSplitOptions.RemoveEmptyEntries).Last()}' at the end"); return; }

                    chainedLines = line.Split(new char[] { '&', '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    List<string> chainedCommands = new();
                    List<List<string>?> chainedArgs = new();
                    
                    foreach(string chainedLine in chainedLines)
                    {
                        (string command, List<string>? args) = processCommand(chainedLine);

                        chainedCommands.Add(command);
                        chainedArgs.Add(args);
                    }

                    bool success = true;
                    string? previousOutput = null;
                    chains.Add("end");

                    int pid = pidCounter;
                    pidCounter++;
                    taskList.Add(new(pid, null, line, 0, wD));

                    if (background)
                    {
                        Console.Write($"[1] {pid}");

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                success = taskList.Find(x => x.pid == pid).Execute(chainedCommands, chainedArgs, chains);

                                if (success) Console.Write($"\n[1]+  Done\t\t\t{line}");
                                else Console.Write($"\n[1]+  Exit 1\t\t\t{line}");

                                taskList.Find(x => x.pid == pid).dead = true;
                            }
                            catch (ThreadInterruptedException)
                            {
                                taskList.Find(x => x.pid == pid).dead = true;
                                Console.Write($"\n[1]+  Terminated\t\t\t{line}");
                                return; 
                            }
                        });
                        return;
                    }
                    else
                    {
                        cancelHandler = (object? sender, ConsoleCancelEventArgs e) =>
                        {
                            e.Cancel = true;
                            var process = taskList.Find(x => x.pid == pid);
                            if (process != null)
                            {
                                process.kill(); // Solo mata el proceso si existe
                            }
                        };
                        Console.CancelKeyPress += cancelHandler;

                        try
                        {
                            taskList.Find(x => x.pid == pid).Execute(chainedCommands, chainedArgs, chains);
                            taskList.Find(x => x.pid == pid).dead = true;
                        }
                        catch (ThreadInterruptedException) 
                        {
                            if (cancelHandler != null)
                            {
                                Console.CancelKeyPress -= cancelHandler;
                                cancelHandler = null;
                            }
                            taskList.Find(x => x.pid == pid).dead = true; return;
                        }

                        if (cancelHandler != null)
                        {
                            Console.CancelKeyPress -= cancelHandler;
                            cancelHandler = null;
                        }
                        return;
                    }
                }
                else
                {
                    executeLine(line, wD);
                }
            }
        }

        public (bool success, string output) executeLine(string command, List<string>? args, string wD, bool writeOutput = true, int parent = 0)
        {
            bool background = false;
            bool redirectOutput = false;
            string? outputRedirectionPath = null;
            string? outputRedirectionName = null;
            bool overwrite = false;

            if (args != null && args.Last() == "&")
            {
                background = true;
                args.Remove(args.Last());
            }

            if (args != null && args.Exists(x => x == ">"))
            {
                string name, path;
                (name, path) = getRealPath(args[args.IndexOf(">") + 1], wD);

                if (Program.fat.fileExists(name, path))
                {
                    redirectOutput = true;
                    outputRedirectionPath = path;
                    outputRedirectionName = name;
                    overwrite = true;
                }
                else
                {
                    if (Program.fat.addFile(name, path))
                    {
                        redirectOutput = true;
                        outputRedirectionPath = path;
                        outputRedirectionName = name;
                        overwrite = true;
                    }
                    else { Console.Write($"Could not create file {args.IndexOf(">") + 1}"); return (false, ""); }
                }

                args.RemoveAt(args.IndexOf(">") + 1);
                args.RemoveAt(args.IndexOf(">"));
            }

            if (args != null && args.Exists(x => x == ">>"))
            {
                if (redirectOutput)
                {
                    if (args.IndexOf(">") > args.IndexOf(">>")) { Console.Write($"-Terminal: unexpected token '>'"); return (false, ""); }
                    else { Console.Write($"-Terminal: unexpected token '>>'"); return (false, ""); }
                }

                string name, path;
                (name, path) = getRealPath(args[args.IndexOf(">>") + 1], wD);

                if (Program.fat.fileExists(name, path))
                {
                    redirectOutput = true;
                    outputRedirectionPath = path;
                    outputRedirectionName = name;
                }
                else
                {
                    if (Program.fat.addFile(name, path))
                    {
                        redirectOutput = true;
                        outputRedirectionPath = path;
                        outputRedirectionName = name;
                    }
                    else { Console.Write($"Could not create file {args.IndexOf(">") + 1}"); return (false, ""); }
                }

                args.RemoveAt(args.IndexOf(">>") + 1);
                args.RemoveAt(args.IndexOf(">>"));
            }

            if (path.ContainsKey(command))
            {
                int pid = pidCounter;
                pidCounter++;
                taskList.Add(new(pid, null, command, parent, wD));

                if (background)
                {
                    Console.Write($"[1] {pid}");

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        bool success;
                        string output;

                        string[]? argsArray = null;
                        if (args != null) argsArray = args.ToArray();
                        if (args == null) args = new();

                        try
                        {
                            if (!redirectOutput) (success, output) = executeCommand(command, argsArray, pid, wD, writeOutput);
                            else
                            {
                                (success, output) = executeCommand(command, argsArray, pid, wD, false);
                                if (success) Program.fat.writeToFile(outputRedirectionName, outputRedirectionPath, "\n" + output, overwrite);
                            }

                            if (success) Console.Write($"\n[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                            else Console.Write($"\n[1]+  Exit 1\t\t\t{command} {string.Join(' ', args)}");
                        }
                        catch (ThreadInterruptedException) { Console.Write($"\n[1]+  Terminated\t\t\t{command} {string.Join(' ', args)}"); }
                    });
                    return (true, "");
                }
                else
                {
                    bool success;
                    string output;

                    string[]? argsArray = null;
                    if (args != null) argsArray = args.ToArray();

                    cancelHandler = (object? sender, ConsoleCancelEventArgs e) =>
                    {
                        e.Cancel = true;
                        var process = taskList.Find(x => x.pid == pid);
                        if (process != null)
                        {
                            process.kill(); // Solo mata el proceso si existe
                        }
                    };
                    Console.CancelKeyPress += cancelHandler;

                    try
                    {
                        if (!redirectOutput) (success, output) = executeCommand(command, argsArray, pid, wD, writeOutput);
                        else
                        {
                            (success, output) = executeCommand(command, argsArray, pid, wD, false);
                            if (success) Program.fat.writeToFile(outputRedirectionName, outputRedirectionPath, "\n" + output, overwrite);
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (cancelHandler != null)
                        {
                            Console.CancelKeyPress -= cancelHandler;
                            cancelHandler = null;
                        }
                        return (false, "");
                    }

                    if (cancelHandler != null)
                    {
                        Console.CancelKeyPress -= cancelHandler;
                        cancelHandler = null;
                    }

                    return (success, output);
                }
            }
            else
            {
                string name, path;
                (name, path) = getRealPath(command, wD);

                if (command.EndsWith(EXECUTABLE_FILE_EXTENSION) && Program.fat.fileExists(name, path))
                {
                    int pid = pidCounter;
                    pidCounter++;
                    taskList.Add(new(pid, Program.fat.catFile(name, path).Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(), name, parent, path));

                    if (background)
                    {
                        Console.Write($"[1] {pid}");

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            bool success;
                            int exitCode;

                            string[]? argsArray = null;
                            if (args != null) argsArray = args.ToArray();
                            if (args == null) args = new();

                            try
                            {
                                (success, exitCode) = executeFile(name, path, pid, argsArray, wD);

                                if (success) Console.Write($"\n[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                                else Console.Write($"\n[1]+  Exit {exitCode}\t\t\t{command} {string.Join(' ', args)}");
                            }
                            catch (ThreadInterruptedException) { Console.Write($"\n[1]+  Terminated\t\t\t{command} {string.Join(' ', args)}"); }
                        });
                        return (true, "");
                    }
                    else
                    {
                        bool success;
                        int exitCode;

                        string[]? argsArray = null;
                        if (args != null) argsArray = args.ToArray();

                        cancelHandler = (object? sender, ConsoleCancelEventArgs e) =>
                        {
                            e.Cancel = true;
                            var process = taskList.Find(x => x.pid == pid);
                            if (process != null)
                            {
                                process.kill(); // Solo mata el proceso si existe
                            }
                        };
                        Console.CancelKeyPress += cancelHandler;

                        try
                        {
                            (success, exitCode) = executeFile(name, path, pid, argsArray, wD);
                        }
                        catch (ThreadInterruptedException)
                        {
                            if (cancelHandler != null)
                            {
                                Console.CancelKeyPress -= cancelHandler;
                                cancelHandler = null;
                            }
                            return (false, "");
                        }

                        if (cancelHandler != null)
                        {
                            Console.CancelKeyPress -= cancelHandler;
                            cancelHandler = null;
                        }

                        return (success, "");
                    }
                }
                else 
                { 
                    if(args != null) Console.Write($"{command} {string.Join(" ", args)}: command not found, to get a list of all avaliable commands type 'help'");
                    else Console.Write($"{command}: command not found, to get a list of all avaliable commands type 'help'");
                    return (false, ""); 
                }
            }
        }

        public (bool success, string output) executeLine(string s, string wD, bool writeOutput = true, int parent = 0)
        {
            bool background = false;
            bool redirectOutput = false;
            string? outputRedirectionPath = null;
            string? outputRedirectionName = null;
            bool overwrite = false;

            (string command, List<string>? args) = processCommand(s);

            if (args != null && args.Last() == "&")
            {
                background = true;
                args.Remove(args.Last());
            }

            if (args != null && args.Exists(x => x == ">"))
            {
                string name, path;
                (name, path) = getRealPath(args[args.IndexOf(">") + 1], wD);

                if (Program.fat.fileExists(name, path))
                {
                    redirectOutput = true;
                    outputRedirectionPath = path;
                    outputRedirectionName = name;
                    overwrite = true;
                }
                else
                {
                    if (Program.fat.addFile(name, path))
                    {
                        redirectOutput = true;
                        outputRedirectionPath = path;
                        outputRedirectionName = name;
                        overwrite = true;
                    }
                    else { Console.Write($"Could not create file {args.IndexOf(">") + 1}"); return (false, ""); }
                }

                args.RemoveAt(args.IndexOf(">") + 1);
                args.RemoveAt(args.IndexOf(">"));
            }

            if (args != null && args.Exists(x => x == ">>"))
            {
                if(redirectOutput)
                {
                    if(args.IndexOf(">") > args.IndexOf(">>")) { Console.Write($"-Terminal: unexpected token '>'"); return (false, ""); }
                    else { Console.Write($"-Terminal: unexpected token '>>'"); return (false, ""); }
                }

                string name, path;
                (name, path) = getRealPath(args[args.IndexOf(">>") + 1], wD);

                if (Program.fat.fileExists(name, path))
                {
                    redirectOutput = true;
                    outputRedirectionPath = path;
                    outputRedirectionName = name;
                }
                else
                {
                    if (Program.fat.addFile(name, path))
                    {
                        redirectOutput = true;
                        outputRedirectionPath = path;
                        outputRedirectionName = name;
                    }
                    else { Console.Write($"Could not create file {args.IndexOf(">") + 1}"); return (false, ""); }
                }

                args.RemoveAt(args.IndexOf(">>") + 1);
                args.RemoveAt(args.IndexOf(">>"));
            }

            if (path.ContainsKey(command))
            {
                int pid = pidCounter;
                pidCounter++;
                taskList.Add(new(pid, null, command, parent, wD));

                if (background)
                {
                    Console.Write($"[1] {pid}");

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        bool success;
                        string output;

                        string[]? argsArray = null;
                        if (args != null) argsArray = args.ToArray();
                        if (args == null) args = new();

                        try
                        {
                            if (!redirectOutput) (success, output) = executeCommand(command, argsArray, pid, wD, writeOutput);
                            else
                            {
                                (success, output) = executeCommand(command, argsArray, pid, wD, false);
                                if (success) Program.fat.writeToFile(outputRedirectionName, outputRedirectionPath, "\n" + output, overwrite);
                            }

                            if (success) Console.Write($"\n[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                            else Console.Write($"\n[1]+  Exit 1\t\t\t{command} {string.Join(' ', args)}");
                        }
                        catch (ThreadInterruptedException) { Console.Write($"\n[1]+  Terminated\t\t\t{command} {string.Join(' ', args)}"); }
                    });
                    return (true, "");
                }
                else
                {
                    bool success;
                    string output;

                    string[]? argsArray = null;
                    if(args != null) argsArray = args.ToArray();

                    cancelHandler = (object? sender, ConsoleCancelEventArgs e) =>
                    {
                        e.Cancel = true;
                        var process = taskList.Find(x => x.pid == pid);
                        if (process != null)
                        {
                            process.kill(); // Solo mata el proceso si existe
                        }
                    };
                    Console.CancelKeyPress += cancelHandler;

                    try
                    {
                        if (!redirectOutput) (success, output) = executeCommand(command, argsArray, pid, wD, writeOutput);
                        else
                        {
                            (success, output) = executeCommand(command, argsArray, pid, wD, false);
                            if (success) Program.fat.writeToFile(outputRedirectionName, outputRedirectionPath, "\n" + output, overwrite);
                        }
                    }
                    catch (ThreadInterruptedException) 
                    {
                        if (cancelHandler != null)
                        {
                            Console.CancelKeyPress -= cancelHandler;
                            cancelHandler = null;
                        }
                        return (false, ""); 
                    }

                    if (cancelHandler != null)
                    {
                        Console.CancelKeyPress -= cancelHandler;
                        cancelHandler = null;
                    }

                    return (success, output);
                }
            }
            else
            {
                string name, path;
                (name, path) = getRealPath(command, wD);

                if (command.EndsWith(EXECUTABLE_FILE_EXTENSION) && Program.fat.fileExists(name, path))
                {
                    int pid = pidCounter;
                    pidCounter++;
                    taskList.Add(new(pid, Program.fat.catFile(name, path).Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(), name, parent, path));

                    if (background)
                    {
                        Console.Write($"[1] {pid}");

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            bool success;
                            int exitCode;

                            string[]? argsArray = null;
                            if (args != null) argsArray = args.ToArray();
                            if (args == null) args = new();

                            try
                            {
                                (success, exitCode) = executeFile(name, path, pid, argsArray, wD);

                                if (success) Console.Write($"\n[1]+  Done\t\t\t{command} {string.Join(' ', args)}");
                                else Console.Write($"\n[1]+  Exit {exitCode}\t\t\t{command} {string.Join(' ', args)}");
                            }
                            catch (ThreadInterruptedException) { Console.Write($"\n[1]+  Terminated\t\t\t{command} {string.Join(' ', args)}"); }
                        });
                        return (true, "");
                    }
                    else
                    {
                        bool success;
                        int exitCode;

                        string[]? argsArray = null;
                        if (args != null) argsArray = args.ToArray();

                        cancelHandler = (object? sender, ConsoleCancelEventArgs e) =>
                        {
                            e.Cancel = true;
                            var process = taskList.Find(x => x.pid == pid);
                            if (process != null)
                            {
                                process.kill(); // Solo mata el proceso si existe
                            }
                        };
                        Console.CancelKeyPress += cancelHandler;

                        try
                        {
                            (success, exitCode) = executeFile(name, path, pid, argsArray, wD);
                        }
                        catch (ThreadInterruptedException) 
                        {
                            if (cancelHandler != null)
                            {
                                Console.CancelKeyPress -= cancelHandler;
                                cancelHandler = null;
                            }
                            return (false, "");
                        }

                        if (cancelHandler != null)
                        {
                            Console.CancelKeyPress -= cancelHandler;
                            cancelHandler = null;
                        }

                        return (success, "");
                    }
                }
                else { Console.Write($"{s}: command not found, to get a list of all avaliable commands type 'help'"); return (false, ""); }
            }
        }

        private (bool sucess, string output) executeCommand(string command, string[]? args, int pid, string wD, bool writeOutput)
        {
            string output;

            try
            {
                output = taskList.Find(x => x.pid == pid).Execute(path[command], args, wD);
                taskList.Find(x => x.pid == pid).dead = true;
            }
            catch (ThreadInterruptedException e) { taskList.Find(x => x.pid == pid).dead = true; throw e; }
            catch (Exception e)
            {
                taskList.Find(x => x.pid == pid).dead = true;
                Console.Write($"{command}: {e.Message}\nTry '{command} --help' for more information.");
                return (false, "");
            }

            if(writeOutput) if (output != "") Console.Write(output);
            return (true, output);
        }

        private (bool sucess, int exitCode) executeFile(string name, string path, int pid, string[]? args, string wD)
        {
            int exitCode;

            try
            {
                exitCode = taskList.Find(x => x.pid == pid).Execute(args);
                taskList.Find(x => x.pid == pid).dead = true;
            }
            catch (ThreadInterruptedException e) { taskList.Find(x => x.pid == pid).dead = true; throw e; }
            catch (Exception e)
            {
                taskList.Find(x => x.pid == pid).dead = true;
                Console.Write($"{path}/{name}: {e.Message}");
                return (false, 1);
            }

            return (true, exitCode);
        }

        private (string command, List<string>? args) processCommand(string line)
        {
            string s = line;
            MatchCollection matches = Regex.Matches(s, @"\$(.*?)\$");

            foreach (Match match in matches)
            {
                string variable = match.Groups[1].Value; // Captures the text inside $ $
                string value = "";
                if (envVars.ContainsKey(variable)) value = envVars[variable];
                s.Replace($"${variable}$", value);
            }

            string command;
            if (s.Contains(" ")) command = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            else command = s;
            List<string>? args = null;
            string argsString = "";
            string arg = "";

            if (s.Length > command.Length)
            {
                argsString = s.Substring(command.Length+1);
            }

            if (argsString != "")
            {
                args = new();
                arg = "";
                bool ignoreSpaces = false;

                foreach (char c in argsString)
                {
                    if (c == '\"') if (!ignoreSpaces) ignoreSpaces = true; else { ignoreSpaces = false; args.Add(arg); arg = ""; }
                    else if (c == ' ') if (ignoreSpaces) arg += c; else { if (arg != "") args.Add(arg); arg = ""; }
                    else arg += c;
                }
            }

            if (arg != "") args.Add(arg); 
            arg = "";

            return (command, args);
        }

        private (string name, string path) getRealPath(string path, string wD)
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
    }
}
