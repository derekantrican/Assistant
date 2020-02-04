using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assistant
{
    public class ReminderCommand : Command
    {
        public ReminderCommand() : base()
        {
            ReminderMethods = new List<Command>();
        }

        public List<Command> ReminderMethods { get; set; }

        public override void RunCommand(string remainingInput = "")
        {
            Dictionary<Command, string> reminderMethods = new Dictionary<Command, string>();
            foreach (Command remMethod in ReminderMethods)
            {
                if (remMethod is GmailCommand)
                    reminderMethods.Add(remMethod, "Gmail (" + (remMethod as GmailCommand).To + ")");
                else if (remMethod is GoogleCalendarEventCommand)
                    reminderMethods.Add(remMethod, "GoogleCalendarEvent (" + (remMethod as GoogleCalendarEventCommand).Calendar + ")");
                else
                    reminderMethods.Add(remMethod, remMethod.GetType().ToString());
            }

            int reminderIndex = Common.GetIndexFromList(reminderMethods.Values.ToList());
            Command reminderMethod = reminderMethods.Keys.ElementAt(reminderIndex);
            reminderMethod.RunCommand(remainingInput);
        }
    }
}
