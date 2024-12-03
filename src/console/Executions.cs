using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT;

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
            return "";
        };

        public Func<string?[], Fat, string> rmdirExecution = (args, fat) =>
        {
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
            Environment.Exit(0);
            return "";
        };

        public Func<string?[], Fat, string> clsExecution = (args, fat) =>
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = @"/c cls";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.Start();
            }
            Thread.Sleep(100);
            return "";
        };

        public Func<string?[], Fat, string> echoExecution = (args, fat) =>
        {
            Environment.Exit(0);
            return "";
        };
    }
}
