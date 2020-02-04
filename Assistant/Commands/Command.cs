using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assistant
{
    public abstract class Command
    {
        public bool ExitAfterExecution { get; set; }

        public string CommandHelp { get; set; }

        public abstract void RunCommand(string remainingInput = "");
    }
}
