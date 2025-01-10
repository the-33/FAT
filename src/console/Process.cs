using Crayon;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Terminal
{
    /*  comandos:
     *      alarm
     *      fork
     *      exec
     *      signal
     *      kill
     *      raise
     *      wait
     *      exit
     *      sleep
     *  if while
     *  crear funciones
     *  señales:
     *      sigterm
     *      sigalrm
     *      sigusr1
     *      sigusr2
     */

    public class Process
    {
        public List<string>? instructions { get; set; }
        public int programCounter { get; set; }
        public int pid { get; set; }
        public int? parent { get; set; }
        public string programName { get; set; }

        string wD;
        bool exit;

        Dictionary<string, int> functions;

        public bool dead { get; set; }
        Thread execution;

        public Process(int pid, List<string>? instructions, string programName, int? parent, string wD)
        {
            dead = false;
            this.pid = pid;
            this.instructions = instructions;

            if (instructions != null) { for (int i = 0; i < instructions.Count; i++) while (instructions[i].StartsWith("\t")) { instructions[i].Remove(0, 1); } }

            programCounter = 0;
            this.programName = programName;
            this.parent = parent;

            functions = new();

            exit = false;
            this.wD = wD;
        }

        public int Execute(string[]? args)
        {
            if (instructions == null) return 0;

            bool openedFunction = false;
            int i = 0;

            foreach (string line in instructions)
            {
                if (line.StartsWith("Function "))
                {
                    Match match = Regex.Match(line, @"^Function\s+([\w_][\w\d_]*)$");
                    if (match.Success)
                    {
                        if (openedFunction) throw new Exception($"Syntax error \"the function {functions.Last().Key} is never closed.\" in line {functions.Last().Value}");
                        openedFunction = true;
                        functions.Add(match.Groups[1].Value, i + 1);
                    }
                    else throw new Exception($"Syntax error \"Function not declared properly\" in line {functions.Last().Value}");
                    i++;
                }
                else if (line == "EndFunction")
                {
                    if (openedFunction) openedFunction = false;
                    else throw new Exception($"Syntax error \"Function is closed but it was never opened in the first place.\" in line {functions.Last().Value}");
                }
                else if (line.StartsWith("if "))
                {

                }
                else if (line.StartsWith("while "))
                {

                }
            }
            if (openedFunction) throw new Exception($"Syntax error \"the function {functions.Last().Key} is never closed.\" in line {functions.Last().Value}");

            if (!functions.ContainsKey("main")) throw new Exception($"Syntax error \"the code lacks a 'main' Function\"");

            Exception? threadException = null;
            try
            {
                execution = new Thread(() =>
                {
                    try
                    {
                        executeFunction("main");
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                });
                execution.Start();
                execution.Join();
                if (threadException != null)
                {
                    throw threadException;
                }
                return 0;
            }
            catch (ThreadInterruptedException e)
            {
                throw e;
            }
            catch (Exception e) { throw new Exception($"Syntax error \"{e.Message}\" in line {programCounter}"); }
        }

        public string Execute(Command c, string[]? args, string wD)
        {
            string output = "";
            Exception? threadException = null;
            try
            {
                execution = new Thread(() =>
                {
                    try
                    {
                        output = c.execute(args, wD);
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                });
                execution.Start();
                execution.Join();
                if (threadException != null)
                {
                    throw threadException;
                }
                return output;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public bool Execute(List<string> chainedCommands, List<List<string>?> chainedArgs, List<string> chains)
        {
            bool success = true;
            Exception? threadException = null;

            try
            {
                execution = new Thread(() =>
                {
                    try
                    {
                        string? previousOutput = null;

                        for (int i = 0; i < chainedCommands.Count() && success; i++)
                        {


                            if (previousOutput != null && previousOutput != "")
                            {
                                if (chainedArgs[i] == null) chainedArgs[i] = new();
                                chainedArgs[i].Insert(0, previousOutput);
                            }

                            (success, previousOutput) = Program.cM.executeLine(chainedCommands[i], chainedArgs[i], wD, chains[i] != "|", pid);
                            if (Program.cM.exit) return;
                            if (chains[i] != "|") previousOutput = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                });
                execution.Start();
                execution.Join();
                if (threadException != null)
                {
                    throw threadException;
                }
                return success;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void kill()
        {
            execution.Interrupt();
        }

        private void executeFunction(string name)
        {
            if (instructions == null) return;

            programCounter = functions[name];

            bool ret = false;

            Match match;

            while (!exit && !ret)
            {
                string instruction = instructions[programCounter];

                if (instruction == "exit") { exit = true; continue; }
                if (instruction == "return") { ret = true; continue; }
                if (instruction == "EndFunction") { ret = true; continue; }
                if (functions.ContainsKey(instruction))
                {
                    int prevProgramCounter = programCounter;
                    executeFunction(instruction);
                    programCounter = prevProgramCounter+1;
                    continue;
                }

                Program.cM.execute(instruction, wD);
                programCounter++;
            }
        }

        public override string ToString()
        {
            return $"{pid}\t{programName}";
        }
    }
}
