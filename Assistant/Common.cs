using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Diagnostics;
using Google.Apis.Gmail.v1;
using Google.Apis.Calendar.v3.Data;
using Assistant;
using System.Runtime.InteropServices;

namespace Assistant
{
    public static class Common
    {
        private static string userSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Assistant");
        private static string commandsPath = Path.Combine(userSettings, "Commands.xml");

        public static Dictionary<List<string>, Command> Commands = new Dictionary<List<string>, Command>();

        public static void CheckPathsExist()
        {
            if (!Directory.Exists(userSettings))
                Directory.CreateDirectory(userSettings);

            if (!File.Exists(commandsPath))
                File.WriteAllText(commandsPath, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Commands/>");
        }

        public static void ReadSettings()
        {
            #region ReadCommands
            XDocument document = XDocument.Load(commandsPath);
            Dictionary<List<string>, Command> temp = new Dictionary<List<string>, Command>();

            List<XElement> commands = document.Descendants("Command").ToList();
            foreach (XElement element in commands)
            {
                //Parse triggers
                List<string> triggers = new List<string>();
                foreach (XElement tElement in element.Descendants("Trigger").ToList())
                {
                    List<XAttribute> phraseAttributes = GetAttributesWithFuzzyName(tElement.Attributes().ToList(), "phrase");
                    triggers.Add(GetFirstValueIfExist(phraseAttributes, true));
                }

                //Parse action
                Command action = null;
                foreach (XElement aElement in element.Descendants("Action").FirstOrDefault().Elements())
                {
                    List<XAttribute> curActionAttributes = aElement.Attributes().ToList();

                    switch (aElement.Name.ToString().ToUpper())
                    {
                        case "GMAIL":
                            List<XAttribute> toAttributes = GetAttributesWithFuzzyName(curActionAttributes, "to");
                            List<XAttribute> subjectAttributes = GetAttributesWithFuzzyName(curActionAttributes, "subject");
                            action = new GmailCommand()
                            {
                                To = GetFirstValueIfExist(toAttributes),
                                Subject = GetFirstValueIfExist(subjectAttributes)
                            };

                            break;
                        case "GOOGLECALENDAREVENT":
                            List<XAttribute> titleAttributes = GetAttributesWithFuzzyName(curActionAttributes, "title");
                            List<XAttribute> startTimeAttributes = GetAttributesWithFuzzyName(curActionAttributes, "startTime");
                            List<XAttribute> durationAttributes = GetAttributesWithFuzzyName(curActionAttributes, "duration");
                            List<XAttribute> calendarAttributes = GetAttributesWithFuzzyName(curActionAttributes, "calendar");
                            action = new GoogleCalendarEventCommand()
                            {
                                Title = GetFirstValueIfExist(titleAttributes, true),
                                StartTime = GetFirstValueIfExist(startTimeAttributes, true),
                                Duration = GetFirstValueIfExist(durationAttributes, true),
                                Calendar = GetFirstValueIfExist(calendarAttributes, true)
                            };

                            break;
                        case "LAUNCH":
                            List<XAttribute> pathAttributes = GetAttributesWithFuzzyName(curActionAttributes, "path");
                            List<XAttribute> parametersAttributes = GetAttributesWithFuzzyName(curActionAttributes, "parameters");
                            action = new LaunchCommand()
                            {
                                LaunchPath = GetFirstValueIfExist(pathAttributes, true),
                                Parameters = GetFirstValueIfExist(parametersAttributes)
                            };

                            break;
                        case "LIST":
                            XElement listAction = aElement.Descendants().FirstOrDefault();
                            List<XAttribute> actUponResultsAttributes = GetAttributesWithFuzzyName(curActionAttributes, "ActUponResults");
                            action = new ListCommand()
                            {
                                ListAction = (ListCommand.ListType)Enum.Parse(typeof(ListCommand.ListType), listAction.Name.ToString(), true),
                                ActOnListItems = Convert.ToBoolean(GetFirstValueIfExist(actUponResultsAttributes, false, "false"))
                            };

                            List<XAttribute> listActionAttributes = listAction.Attributes().ToList();
                            switch (listAction.Name.ToString().ToUpper())
                            {
                                case "FILES":
                                    List<XAttribute> fileTypeAttributes = GetAttributesWithFuzzyName(listActionAttributes, "fileType"); //Todo: in the future support multiple filetypes
                                    (action as ListCommand).FileType = GetFirstValueIfExist(fileTypeAttributes);

                                    List<XAttribute> folderAttributes = GetAttributesWithFuzzyName(listActionAttributes, "folder");
                                    (action as ListCommand).Folder = GetFirstValueIfExist(folderAttributes, true);

                                    List<XAttribute> returnNamesWithExtensionsAttributes = GetAttributesWithFuzzyName(listActionAttributes, "returnNamesWithExtensions");
                                    (action as ListCommand).ReturnNamesWithExtensions = Convert.ToBoolean(GetFirstValueIfExist(returnNamesWithExtensionsAttributes, false, "false"));

                                    List<XAttribute> includeSubFoldersAttributes = GetAttributesWithFuzzyName(listActionAttributes, "includeSubFolders");
                                    (action as ListCommand).IncludeSubFolders = Convert.ToBoolean(GetFirstValueIfExist(includeSubFoldersAttributes, false, "false"));
                                    
                                    break;
                                case "GMAIL":
                                    List<XAttribute> queryAttributes = GetAttributesWithFuzzyName(listActionAttributes, "query");
                                    (action as ListCommand).Query = GetFirstValueIfExist(queryAttributes, false, "in:inbox");

                                    List<XAttribute> returnAmountAttributes = GetAttributesWithFuzzyName(listActionAttributes, "returnAmount");
                                    (action as ListCommand).ReturnAmount = Convert.ToInt32(GetFirstValueIfExist(returnAmountAttributes, false, "10"));

                                    break;
                                case "GOOGLECALENDAR":
                                    List<XAttribute> calendarListAttributes = GetAttributesWithFuzzyName(listActionAttributes, "calendar");
                                    if (calendarListAttributes.Count > 0)
                                    {
                                        foreach (XAttribute calendar in calendarListAttributes)
                                            (action as ListCommand).Calendars.Add(calendar.Value);
                                    }
                                    else
                                        (action as ListCommand).Calendars.Add(""); //If there are no calendars specify, we will default to "" (meaning "all")

                                    List<XAttribute> startDateListAttributes = GetAttributesWithFuzzyName(listActionAttributes, "startDate");
                                    (action as ListCommand).StartDate = GetFirstValueIfExist(startDateListAttributes, true);

                                    List<XAttribute> endDateListAttributes = GetAttributesWithFuzzyName(listActionAttributes, "endDate");
                                    (action as ListCommand).EndDate = GetFirstValueIfExist(endDateListAttributes, false, GetFirstValueIfExist(startDateListAttributes)); //Defaults to the same as "startDate"

                                    List<XAttribute> returnAmountListAttributes = GetAttributesWithFuzzyName(listActionAttributes, "returnAmount");
                                    (action as ListCommand).ReturnAmount = Convert.ToInt32(GetFirstValueIfExist(returnAmountListAttributes, false, "10"));

                                    break;
                            }

                            break;
                        case "REMINDER":
                            action = new ReminderCommand();
                            foreach (XElement reminderMethod in aElement.Descendants().ToList())
                            {
                                List<XAttribute> reminderMethodAttributes = reminderMethod.Attributes().ToList();
                                Command reminder = null;
                                switch (reminderMethod.Name.ToString().ToUpper())
                                {
                                    case "GMAIL":
                                        List<XAttribute> toReminderAttributes = GetAttributesWithFuzzyName(reminderMethodAttributes, "to");
                                        List<XAttribute> subjectReminderAttributes = GetAttributesWithFuzzyName(reminderMethodAttributes, "subject");
                                        reminder = new GmailCommand()
                                        {
                                            To = GetFirstValueIfExist(reminderMethodAttributes, true),
                                            Subject = GetFirstValueIfExist(subjectReminderAttributes, true)
                                        };

                                        break;
                                    case "GOOGLECALENDAREVENT":
                                        List<XAttribute> titleReminderAttributes = GetAttributesWithFuzzyName(reminderMethodAttributes, "title");
                                        List<XAttribute> startTimeReminderAttributes = GetAttributesWithFuzzyName(reminderMethodAttributes, "startTime");
                                        List<XAttribute> durationReminderAttributes = GetAttributesWithFuzzyName(reminderMethodAttributes, "duration");
                                        List<XAttribute> calendarReminderAttributes = GetAttributesWithFuzzyName(reminderMethodAttributes, "calendar");
                                        reminder = new GoogleCalendarEventCommand()
                                        {
                                            Title = GetFirstValueIfExist(titleReminderAttributes, true),
                                            StartTime = GetFirstValueIfExist(startTimeReminderAttributes, true),
                                            Duration = GetFirstValueIfExist(durationReminderAttributes, true),
                                            Calendar = GetFirstValueIfExist(calendarReminderAttributes, true)
                                        };

                                        //Todo: should also be able to accept "endTime" instead of "duration" (BUT NOT BOTH)
                                        break;
                                }
                                (action as ReminderCommand).ReminderMethods.Add(reminder);
                            }
                            //Todo
                            break;
                    }
                }

                List<XAttribute> exitAfterExecutionAttributes = GetAttributesWithFuzzyName(element.Descendants("Action").Attributes().ToList(), "exitAfterExecution");

                if (exitAfterExecutionAttributes.Count > 0)
                    action.ExitAfterExecution = Convert.ToBoolean(GetFirstValueIfExist(exitAfterExecutionAttributes));
                else if (action is ListCommand)
                {
                    ListCommand listCommand = action as ListCommand;

                    if (listCommand.ActOnListItems) //Force an exit after execution if we are acting on the items
                        listCommand.ExitAfterExecution = true;
                    else
                        listCommand.ExitAfterExecution = false;
                }
                else
                    action.ExitAfterExecution = true; //Default to true

                temp.Add(triggers.ToList(), action);
            }

            Commands = temp;
            #endregion ReadCommands
        }

        private static List<XAttribute> GetAttributesWithFuzzyName(List<XAttribute> attributes, string name)
        {
            List<XAttribute> result = new List<XAttribute>();
            name = name.ToLower();

            foreach (XAttribute attribute in attributes)
            {
                if (attribute.Name.ToString().ToLower() == name)
                    result.Add(attribute);
            }

            return result;
        }

        private static string GetFirstValueIfExist(List<XAttribute> foundAttributes, bool throwExceptionIfMissing = false, string defaultValue = "")
        {
            if (foundAttributes.Count() > 0)
                return foundAttributes.First().Value;
            else if (throwExceptionIfMissing)
                throw new Exception("Improperly defined command"); //Todo: in the future, this and "GetAttributesWithFuzzyName" are extension methods on an XElement. That way, the exception can also print out the command that is wrong
            else
                return defaultValue;
        }

        //private static void CreateDefaultXml()
        //{
        //    string xmlContents = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
        //                         "<Settings updated=\"" + DateTime.Now.ToString("%M/%d/yyyy HH:mm") + "\">" + Environment.NewLine +
        //                         "</Settings>";

        //    if (!Directory.Exists(userSettings))
        //        Directory.CreateDirectory(userSettings);

        //    File.WriteAllText(settingsPath, xmlContents);
        //}

        public static string ProcessVariables(string input, string remainingConsoleInput = "")
        {
            if (input == null)
                input = "";

            input = input.Replace("{leftoverInput}", remainingConsoleInput);
            input = input.Replace("{Now}", DateTime.Now.ToString()).Replace("{Today}", DateTime.Now.ToString());

            if (input.Contains("{ReminderSubject}"))
            {
                Console.WriteLine("What is the reminder?");
                input = input.Replace("{ReminderSubject}", Console.ReadLine());
            }

            if (input.Contains("{ReminderTime}"))
            {
                Console.WriteLine("When is the reminder?");
                input = input.Replace("{ReminderTime}", DateTimeStringParser.ParseString(Console.ReadLine()).ToString());
            }

            if (input.Contains("{ReminderTimeDotSeparated}"))
            {
                Console.WriteLine("When is the reminder?");
                input = input.Replace("{ReminderTimeDotSeparated}", DateTimeStringParser.ParseString(Console.ReadLine()).ToString("%H.mm.MMM.%d.yyyy"));
            }

            return input;
        }

        public static void StartProcess(string uri, string parameters = "")
        {
            //Start process via command line so that we aren't attached to the process
            ProcessStartInfo startInfo = new ProcessStartInfo() { WindowStyle = ProcessWindowStyle.Hidden, FileName = "cmd" };
            startInfo.Arguments = $"/C start \"\" \"{uri}\" {parameters}";

            Process startedProcess = Process.Start(startInfo);
            ActivateWindow(startedProcess.MainWindowHandle);
        }

        public static int GetIndexFromList<T>(List<T> listToDisplay)
        {
            for (int i = 0; i < listToDisplay.Count; i++)
                Console.WriteLine("(" + i + ") " + listToDisplay[i].ToString());

            Console.WriteLine("Which # would you like?");
            int selectedIndex = Convert.ToInt32(Console.ReadLine());
            while (selectedIndex < 0 || selectedIndex > listToDisplay.Count - 1)
            {
                Console.WriteLine("That is not one of the options. Which # would you like?");
                selectedIndex = Convert.ToInt32(Console.ReadLine());
            }

            return selectedIndex;
        }

        public static T GetSelectionFromList<T>(List<T> listToDisplay)
        {
            foreach (var item in listToDisplay)
                Console.WriteLine("(" + listToDisplay.IndexOf(item) + ") " + item.ToString());

            Console.WriteLine("Which # would you like?");
            int selectedIndex = Convert.ToInt32(Console.ReadLine());
            while (selectedIndex < 0 || selectedIndex > listToDisplay.Count - 1)
            {
                Console.WriteLine("That is not one of the options. Which # would you like?");
                selectedIndex = Convert.ToInt32(Console.ReadLine());
            }
            return listToDisplay[selectedIndex];
        }

        public static void DisplayList<T>(List<T> listToDisplay, bool withBullets)
        {
            if (withBullets)
            {
                foreach (var item in listToDisplay)
                    Console.WriteLine("\u00B7 " + item);
            }
            else
            {
                foreach (var item in listToDisplay)
                    Console.WriteLine(item);
            }
        }

        #region Focus Window
        public static void ActivateWindow(IntPtr windowHandle)
        {
            // Guard: check if window already has focus.
            if (windowHandle == GetForegroundWindow()) return;
            // Show window maximized.
            ShowWindow(windowHandle, SHOW_MAXIMIZED);
            // Simulate an "ALT" key press.
            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | 0, 0);
            // Simulate an "ALT" key release.
            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | KEYUP, 0);
            SetForegroundWindow(windowHandle);
        }
        private const int ALT = 0xA4;
        private const int EXTENDEDKEY = 0x1;
        private const int KEYUP = 0x2;
        private const int SHOW_MAXIMIZED = 3;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion Focus Window
    }
}
