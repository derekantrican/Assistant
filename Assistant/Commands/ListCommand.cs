using Google.Apis.Calendar.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assistant
{
    public class ListCommand : Command
    {
        public enum ListType
        {
            Files,
            Gmail,
            GoogleCalendar,
            FollowUpThen
        }

        public ListCommand() : base()
        {
            Calendars = new List<string>();
        }

        public ListType ListAction { get; set; }
        public bool ActOnListItems { get; set; }
        public int ReturnAmount { get; set; }

        #region ListDirectory
        public string Folder { get; set; }
        public string FileType { get; set; }
        public bool ReturnNamesWithExtensions { get; set; }
        public bool IncludeSubFolders { get; set; }
        #endregion ListDirectory

        #region ListGmail
        public string Query { get; set; }
        #endregion ListGmail

        #region ListGoogleCalendar
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> Calendars { get; set; }
        #endregion ListGoogleCalendar

        public override void RunCommand(string remainingInput = "")
        {
            if (ActOnListItems)
            {
                switch (ListAction)
                {
                    case ListType.Files:
                        string directory = Folder;
                        string fileType = FileType;
                        bool returnNamesWithExtensions = ReturnNamesWithExtensions;
                        bool includeSubfolders = IncludeSubFolders;
                        Dictionary<FileInfo, string> files = Functions.ListFiles(directory, fileType, returnNamesWithExtensions, includeSubfolders);
                        string selectedFileName = Common.GetSelectionFromList(files.Values.ToList());
                        FileInfo selectedFile = files.Where(p => p.Value == selectedFileName).First().Key;
                        Common.StartProcess(selectedFile.FullName);
                        break;
                    case ListType.Gmail:
                        string query = Common.ProcessVariables(Query, remainingInput);
                        int numberOfThreadsToReturn = ReturnAmount;
                        Dictionary<Google.Apis.Gmail.v1.Data.Thread, string> threadPairs = Functions.GetGmailThreads(query, numberOfThreadsToReturn);
                        int selectedThreadIndex = Common.GetIndexFromList(threadPairs.Values.ToList());
                        Google.Apis.Gmail.v1.Data.Thread selectedThread = threadPairs.Keys.ElementAt(selectedThreadIndex);
                        Common.StartProcess(@"https://mail.google.com/mail/u/0/#inbox/" + selectedThread.Id); //Open the email in the default browser
                        break;
                    case ListType.GoogleCalendar:
                        List<string> calendarStrings = new List<string>();
                        foreach (string calendar in Calendars)
                            calendarStrings.Add(calendar);

                        string startDateString = Common.ProcessVariables(StartDate);
                        string endDateString = Common.ProcessVariables(EndDate);

                        DateTime startDate = DateTime.Parse(startDateString);
                        DateTime endDate = DateTime.Parse(endDateString);
                        int numberOfEventsToReturn = Convert.ToInt32(ReturnAmount);
                        Dictionary<Event, string> eventPairs = Functions.GetGoogleCalendarEvents(calendarStrings, startDate, endDate, numberOfEventsToReturn);
                        int selectedEventIndex = Common.GetIndexFromList(eventPairs.Values.ToList());
                        Event selectedEvent = eventPairs.Keys.ElementAt(selectedEventIndex);
                        Common.StartProcess(selectedEvent.HtmlLink); //Open the event in the default browser
                        break;
                }
            }
            else
            {
                switch (ListAction)
                {
                    case ListType.Files:
                        string directory = Folder;
                        string fileType = FileType;
                        bool returnNamesWithExtensions = ReturnNamesWithExtensions;
                        bool includeSubfolders = IncludeSubFolders;
                        Dictionary<FileInfo, string> files = Functions.ListFiles(directory, fileType, returnNamesWithExtensions, includeSubfolders);
                        Common.DisplayList(files.Values.ToList(), true);
                        break;
                    case ListType.Gmail:
                        string query = Common.ProcessVariables(Query, remainingInput);
                        int numberOfThreadsToReturn = ReturnAmount;
                        Dictionary<Google.Apis.Gmail.v1.Data.Thread, string> threadPairs = Functions.GetGmailThreads(query, numberOfThreadsToReturn);
                        Common.DisplayList(threadPairs.Values.ToList(), true);
                        break;
                    case ListType.GoogleCalendar:
                        List<string> calendarStrings = new List<string>();
                        foreach (string calendar in Calendars)
                            calendarStrings.Add(calendar);

                        string startDateString = Common.ProcessVariables(StartDate);
                        string endDateString = Common.ProcessVariables(EndDate);

                        DateTime startDate = DateTime.Parse(startDateString);
                        DateTime endDate = DateTime.Parse(endDateString);
                        int returnAmount = ReturnAmount;
                        Dictionary<Event, string> eventPairs = Functions.GetGoogleCalendarEvents(calendarStrings, startDate, endDate, returnAmount);
                        Common.DisplayList(eventPairs.Values.ToList(), true);
                        break;
                }
            }
        }
    }
}
