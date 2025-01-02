using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal
{
    public class Process
    {
        public List<string>? instructions { get; set; }
        public int programCounter { get; set; }
        public int pid { get; set; }
        public Process? parent { get; set; }
        public List<Process> sons { get; set; }
        public string programName { get; set; }

        public bool dead { get; set; }

        public Process(int pid, List<string>? instructions, string programName, Process? parent)
        {
            this.pid = pid;
            this.instructions = instructions;
            programCounter = 0;
            this.programName = programName;
            this.parent = parent;
            sons = new List<Process>();
        }

        public string[] Execute(string[]? args)
        {
            bool exit = false;
            string returnValue;

            while(!exit)
            {

            }

            return new string[] { "" };
        }
    }
}
