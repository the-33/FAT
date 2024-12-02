using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT;
using static Crayon.Output;

namespace Terminal
{
    /*  Esta clase maneja todos los comandos usando un diccionario
     *  Cuando se ejecuta un comando se llama a la funcion de ejecucion de ese comando usando la key del diccionario
     *  
     *  TODO: Agregar la descripcion a todos los archivos '.comm' que se encuentran en 'bin/Debug/net6.0/commands' usando el formato establecido en 'doc/FORMAT.html'
     */

    public class ConsoleManager
    {
        const string descPath = "./commands/"; // Carpeta donde estan los archivos de descripcion de los comandos
        const string descFileType = ".comm"; // Extension de archivo de descripcion de los comandos

        public Dictionary<string, Command> path { get; }
        public Executions exec = new();

        public ConsoleManager()
        {
            // Para saber que hace cada comando consultar https://en.wikibooks.org/wiki/Linux_Guide/Linux_commands
            // O escribir "comando" --help en una maquina linux, si tienes windows consultar https://apps.microsoft.com/detail/9pdxgncfsczv?rtc=1&hl=es-es&gl=ES

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
            };
        }

        public void execute(string command, string?[] args, Fat fat)
        {
            if (path.ContainsKey(command)) path[command].execute(args, fat);
            else Console.WriteLine(Red().Bold().Text("The command " + command + " was not found, to get a list of all avaliable commands type 'help'"));
        }
    }
}
