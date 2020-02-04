using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Assistant
{
    class Program
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;

        static void Main(string[] args)
        {
            List<string> argsList = args.ToList();
            if (argsList.Find(p => p.Contains("alertMessage")) != null)
            {
                ShowWindow(GetConsoleWindow(), SW_HIDE);
                string title = "Reminder";
                if (argsList.Find(p => p.Contains("alertTitle")) != null)
                    title = argsList.Find(p => p.Contains("alertTitle")).Split('=')[1];

                string message = argsList.Find(p => p.Contains("alertMessage")).Split('=')[1];

                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1); //Todo: make a custom form so this can be shown on the active screen
                return;
            }

            Common.CheckPathsExist();
            Common.ReadSettings();

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true
                }
            };

            proc.Start();

            IntPtr handle = FindWindowByCaption(IntPtr.Zero, Console.Title);
            SetForegroundWindow(handle);

            while (true)
            {
                Console.Write(">> ");
                var line = Console.ReadLine();
                FindMatchingCommand(line);
            }
        }

        private static void FindMatchingCommand(string input)
        {
            bool helpRequested = false;
            input = input.ToLower();

            Match help = Regex.Match(input, @"\s?\S+\s?");
            while (help.Success)
            {
                if (help.Value.Trim() == "?")
                    helpRequested = true;

                help = help.NextMatch();
            }

            Command matchingCommand = null;
            string matchingTrigger = "";
            foreach (KeyValuePair<List<string>, Command> command in Common.Commands)
            {
                int numberOfTriggerWordsMatched = 0;
                foreach (string trigger in command.Key) //This might be able to be simplified in the future
                {
                    if (Regex.Match(input, @"(^|\s+)" + trigger + @"(\s+|$)").Success && Regex.Matches(trigger, @"\w+").Count > numberOfTriggerWordsMatched)
                    {
                        matchingCommand = command.Value;
                        matchingTrigger = trigger;
                        numberOfTriggerWordsMatched = Regex.Matches(trigger, @"\w+").Count;
                    }
                }
            }

            if (matchingCommand != null)
            {
                if (helpRequested)
                    Console.WriteLine(matchingCommand.CommandHelp);
                else
                {
                    matchingCommand.RunCommand(input.Replace(matchingTrigger, "").Trim());

                    if (matchingCommand.ExitAfterExecution)
                        Environment.Exit(0);
                }

                return;
            }

            if (helpRequested)
            {
                foreach (KeyValuePair<List<string>, Command> command in Common.Commands)
                {
                    Console.Write(string.Join(",", command.Key));

                    if (!Common.Commands.Last().Equals(command))
                        Console.Write(',');
                }

                Console.Write(Environment.NewLine);
            }
            else
                Console.WriteLine("No matching command");
        }
    }
}
