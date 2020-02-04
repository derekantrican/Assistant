using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assistant
{
    public class GoogleCalendarEventCommand : Command
    {        
        public string Title { get; set; }
        public string StartTime { get; set; }
        public string Duration { get; set; }
        public string Calendar { get; set; }

        public override void RunCommand(string remainingInput = "")
        {
            string title = Common.ProcessVariables(Title);

            DateTime reminderStartTime = DateTime.Parse(Common.ProcessVariables(StartTime));
            DateTime reminderEndTime = reminderStartTime.AddMinutes(Convert.ToInt32(Duration));
            string reminderCalendar = Calendar;

            Functions.CreateGoogleCalendarEvent(title, reminderStartTime, reminderEndTime, reminderCalendar);
        }
    }
}
