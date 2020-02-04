using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assistant
{
    public class LaunchCommand : Command
    {
        public string LaunchPath { get; set; }
        public string Parameters { get; set; }

        public override void RunCommand(string remainingInput = "")
        {
            UriBuilder pathOrURL = new UriBuilder(Common.ProcessVariables(LaunchPath, remainingInput));

            if (pathOrURL.Scheme == "file")
                Common.StartProcess(pathOrURL.Uri.LocalPath, string.IsNullOrEmpty(Parameters) ? "" : Parameters);
            else
                Common.StartProcess(pathOrURL.Uri.AbsoluteUri, string.IsNullOrEmpty(Parameters) ? "" : Parameters);
        }
    }
}
