using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using Google.Apis.Calendar.v3;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using System.IO;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using AE.Net.Mail;
using System.Net.Mail;
using Microsoft.Win32.TaskScheduler;

namespace Assistant
{
    public static class Functions
    {
        static string[] Scopes = { CalendarService.Scope.Calendar, GmailService.Scope.GmailModify };
        static string ApplicationName = "Assistant";
        static string userSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationName);
        static string credentialsPath = Path.Combine(userSettings, "Credentials");
        static string userEmailAddress = "";
        static GmailService gmailService;
        static CalendarService calendarService;

        public static string InputRemainder = "";
        public static string GetUserEmailAddress()
        {
            if (string.IsNullOrEmpty(userEmailAddress))
            {
                AuthorizeGmail();

                UsersResource.GetProfileRequest profileRequest = gmailService.Users.GetProfile("me");
                Profile userProfile = profileRequest.Execute();

                userEmailAddress = userProfile.EmailAddress;
            }

            return userEmailAddress;
        }

        private static void AuthorizeGmail()
        {
            if (gmailService != null)
                return;

            UserCredential credential;
            string clientSecretString = "{\"installed\":" +
                    "{" +
                        "\"client_id\":\"80418664891-qq23ro9799t047oqoa5a0m3mu9oga7d3.apps.googleusercontent.com\"," +
                        "\"project_id\":\"assistant-165119\"," +
                        "\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\"," +
                        "\"token_uri\":\"https://accounts.google.com/o/oauth2/token\"," +
                        "\"auth_provider_x509_cert_url\":\"https://www.googleapis.com/oauth2/v1/certs\"," +
                        "\"client_secret\":\"AjOtj5dNgtHbznG0GyBixWzJ\"," +
                        "\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\",\"http://localhost\"]" +
                    "}" +
                "}";
            byte[] byteArray = Encoding.ASCII.GetBytes(clientSecretString);
            using (var stream = new MemoryStream(byteArray))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credentialsPath, true)).Result;
            }

            gmailService = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        private static void AuthorizeGoogleCalendar()
        {
            if (calendarService != null)
                return;

            UserCredential credential;
            string clientSecretString = "{\"installed\":" +
                    "{" +
                        "\"client_id\":\"80418664891-qq23ro9799t047oqoa5a0m3mu9oga7d3.apps.googleusercontent.com\"," +
                        "\"project_id\":\"assistant-165119\"," +
                        "\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\"," +
                        "\"token_uri\":\"https://accounts.google.com/o/oauth2/token\"," +
                        "\"auth_provider_x509_cert_url\":\"https://www.googleapis.com/oauth2/v1/certs\"," +
                        "\"client_secret\":\"AjOtj5dNgtHbznG0GyBixWzJ\"," +
                        "\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\",\"http://localhost\"]" +
                    "}" +
                "}";
            byte[] byteArray = Encoding.ASCII.GetBytes(clientSecretString);
            using (var stream = new MemoryStream(byteArray))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credentialsPath, true)).Result;
            }

            calendarService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        public static void SendGmail(string to, string subject, string body)
        {
            AuthorizeGmail();

            AE.Net.Mail.MailMessage message = new AE.Net.Mail.MailMessage();
            message.Subject = subject;
            message.Body = body;
            message.To.Add(new MailAddress(to));
            message.From = new MailAddress(GetUserEmailAddress());

            StringWriter msgStr = new StringWriter();
            message.Save(msgStr);

            UsersResource.MessagesResource messageResource = gmailService.Users.Messages;
            UsersResource.MessagesResource.SendRequest sendRequest = messageResource.Send(new Google.Apis.Gmail.v1.Data.Message { Raw = Base64UrlEncode(msgStr.ToString()) }, "me");
            sendRequest.Execute();
        }

        private static string Base64UrlEncode(string input)
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            // Special "url-safe" base64 encode.
            return Convert.ToBase64String(inputBytes)
              .Replace('+', '-')
              .Replace('/', '_')
              .Replace("=", "");
        }

        public static Dictionary<Google.Apis.Gmail.v1.Data.Thread, string> GetGmailThreads(string query, int returnAmount)
        {
            AuthorizeGmail();

            UsersResource.ThreadsResource.ListRequest request = gmailService.Users.Threads.List("me");
            request.Q = query;
            request.MaxResults = returnAmount;
            ListThreadsResponse response = request.Execute();
            List<Google.Apis.Gmail.v1.Data.Thread> threads = response.Threads.ToList();
            List<string> threadSubjects = GetThreadSubjects(threads);

            Dictionary<Google.Apis.Gmail.v1.Data.Thread, string> threadDictionary = new Dictionary<Google.Apis.Gmail.v1.Data.Thread, string>();
            for (int i = 0; i < threads.Count; i++)
                threadDictionary.Add(threads[i], threadSubjects[i]);



            return threadDictionary;
        }

        public static List<string> GetThreadSubjects(List<Google.Apis.Gmail.v1.Data.Thread> threads)
        {
            List<string> threadSubjects = new List<string>();

            foreach (Google.Apis.Gmail.v1.Data.Thread thread in threads)
            {
                UsersResource.ThreadsResource.GetRequest subjectRequest = gmailService.Users.Threads.Get("me", thread.Id);
                threadSubjects.Add(subjectRequest.Execute().Messages.First().Payload.Headers.Where(p => p.Name == "Subject").FirstOrDefault().Value);
            }

            return threadSubjects;
        }

        public static Dictionary<Event, string> GetGoogleCalendarEvents(List<string> calendarNames, DateTime startDate, DateTime endDate, int returnAmount)
        {
            Dictionary<Event, string> eventDictionary = new Dictionary<Event, string>();
            AuthorizeGoogleCalendar();

            CalendarListResource.ListRequest calRequest = calendarService.CalendarList.List();
            calRequest.MinAccessRole = CalendarListResource.ListRequest.MinAccessRoleEnum.Owner;
            CalendarList calendars = calRequest.Execute();

            if (calendarNames == null || calendarNames.Count == 0)
            {
                foreach (CalendarListEntry calendar in calendars.Items)
                {
                    EventsResource.ListRequest eventRequest = calendarService.Events.List(calendar.Id);
                    eventRequest.TimeMin = startDate.Date + new TimeSpan(0, 0, 0); //Force the start to the bottom of the date
                    eventRequest.TimeMax = endDate.Date + new TimeSpan(23, 59, 59); //Make the "endDate" inclusive
                    eventRequest.ShowDeleted = false;
                    eventRequest.SingleEvents = true;
                    eventRequest.MaxResults = returnAmount;
                    eventRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                    Events events = eventRequest.Execute();
                    List<string> eventDisplay = new List<string>();
                    for (var i = 0; i < events.Items.Count; i++)
                    {
                        string eventSummary = events.Items[i].Summary + " (" + events.Items[i].Start.DateTime.Value.ToString("HH:mm") + " - " + events.Items[i].End.DateTime.Value.ToString("HH:mm") + ")";
                        eventDictionary.Add(events.Items[i], eventSummary);
                    }
                }
            }
            else if (calendarNames.Count == 1)
            {
                CalendarListEntry calendar = calendars.Items.Where(p => p.Summary == "derekantrican@gmail.com").FirstOrDefault(); //Todo: get the user's personal calendar from the Google Calendar API? https://developers.google.com/gmail/api/v1/reference/users/getProfile

                EventsResource.ListRequest eventRequest = calendarService.Events.List(calendar.Id);
                eventRequest.TimeMin = startDate.Date + new TimeSpan(0, 0, 0); //Force the start to the bottom of the date
                eventRequest.TimeMax = endDate.Date + new TimeSpan(23, 59, 59); //Make the "endDate" inclusive
                eventRequest.ShowDeleted = false;
                eventRequest.SingleEvents = true;
                eventRequest.MaxResults = returnAmount;
                eventRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                Events events = eventRequest.Execute();
                List<string> eventDisplay = new List<string>();
                for (var i = 0; i < events.Items.Count; i++)
                {
                    string eventSummary = events.Items[i].Summary + " (" + events.Items[i].Start.DateTime.Value.ToString("HH:mm") + " - " + events.Items[i].End.DateTime.Value.ToString("HH:mm") + ")";
                    eventDictionary.Add(events.Items[i], eventSummary);
                }
            }
            else
            {
                foreach (string calendarString in calendarNames)
                {
                    CalendarListEntry calendar = calendars.Items.Where(p => p.Summary == calendarString).FirstOrDefault();

                    EventsResource.ListRequest eventRequest = calendarService.Events.List(calendar.Id);
                    eventRequest.TimeMin = startDate.Date + new TimeSpan(0, 0, 0); //Force the start to the bottom of the date
                    eventRequest.TimeMax = endDate.Date + new TimeSpan(23, 59, 59); //Make the "endDate" inclusive
                    eventRequest.ShowDeleted = false;
                    eventRequest.SingleEvents = true;
                    eventRequest.MaxResults = returnAmount;
                    eventRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                    Events events = eventRequest.Execute();
                    List<string> eventDisplay = new List<string>();
                    for (var i = 0; i < events.Items.Count; i++)
                    {
                        string eventSummary = events.Items[i].Summary + " (" + events.Items[i].Start.DateTime.Value.ToString("HH:mm") + " - " + events.Items[i].End.DateTime.Value.ToString("HH:mm") + ")";
                        eventDictionary.Add(events.Items[i], eventSummary);
                    }
                }
            }

            //Reorganize the dictionary by startDate
            eventDictionary = eventDictionary.OrderBy(p => p.Key.Start.DateTime.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

            return eventDictionary;
        }

        public static void CreateGoogleCalendarEvent(string title, DateTime startTime, DateTime endTime, string calendar)
        {
            AuthorizeGoogleCalendar();

            CalendarListResource.ListRequest calRequest = calendarService.CalendarList.List();
            calRequest.MinAccessRole = CalendarListResource.ListRequest.MinAccessRoleEnum.Owner;
            CalendarList calendars = calRequest.Execute();

            CalendarListEntry reminderCalendar = calendars.Items.Where(p => p.Summary == calendar).FirstOrDefault();

            Event newReminderEvent = new Event() { Summary = title };
            newReminderEvent.Start = new EventDateTime() { DateTime = startTime };
            newReminderEvent.End = new EventDateTime() { DateTime = endTime };

            EventsResource.InsertRequest createRequest = calendarService.Events.Insert(newReminderEvent, reminderCalendar.Id);
            createRequest.Execute();
        }

        public static Dictionary<FileInfo, string> ListFiles(string directory, string fileType, bool returnNamesWithExtensions, bool includeSubfolders)
        {
            Dictionary<FileInfo, string> resultingFiles = new Dictionary<FileInfo, string>();
            string extensionToMatch = Regex.Replace(fileType, @"\W", "");
            List<FileInfo> files = WalkDirectoryTree(new DirectoryInfo(directory), includeSubfolders);
            for (int i = 0; i < files.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(extensionToMatch)) //If a fileType is specified
                {
                    if (Regex.Replace(files[i].Extension, @"\W", "") == extensionToMatch)
                    {
                        if (returnNamesWithExtensions)
                            resultingFiles.Add(files[i], files[i].Name);
                        else
                            resultingFiles.Add(files[i], Path.GetFileNameWithoutExtension(files[i].Name));
                    }

                    continue;
                }

                //If a fileType is not specified
                if (returnNamesWithExtensions)
                    resultingFiles.Add(files[i], files[i].Name);
                else
                    resultingFiles.Add(files[i], Path.GetFileNameWithoutExtension(files[i].Name));
            }

            return resultingFiles;
        }

        private static List<FileInfo> WalkDirectoryTree(DirectoryInfo dirInfo, bool includeSubFolders)
        {
            List<FileInfo> files = null;
            DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = dirInfo.GetFiles("*.*").ToList();
            }
            catch (Exception e)
            {
            }

            if (includeSubFolders)
            {
                // Now find all the subdirectories under this directory.
                subDirs = dirInfo.GetDirectories();

                foreach (DirectoryInfo di in subDirs)
                {
                    // Resursive call for each subdirectory.
                    files.AddRange(WalkDirectoryTree(di, includeSubFolders));
                }
            }

            return files;
        }
    }
}
