using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal
{
    public class Task
    {
        public List<string> instructions { get; set; }
        public int programCounter {  get; set; }
        public int pid { get; set; }
        public string programName { get; set; }

        public Task(int pid, List<string> instructions, string programName)
        {
            this.pid = pid;
            this.instructions = instructions;
            programCounter = 0;
            this.programName = programName;
        }

        public bool Execute()
        {
            return true;
        }
    }
}
