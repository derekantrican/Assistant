using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Assistant
{
    public class GmailCommand : Command
    {
        public string To { get; set; }
        public string Subject { get; set; }

        public override void RunCommand(string remainingInput = "")
        {
            string gmailTo;
            if (string.IsNullOrWhiteSpace(To))
            {
                Console.Write("To: ");
                gmailTo = Console.ReadLine();
            }
            else
                gmailTo = To;

            gmailTo = Common.ProcessVariables(gmailTo);

            if (gmailTo == "me" || gmailTo == "self")
                gmailTo = Functions.GetUserEmailAddress();

            string gmailSubject;
            if (string.IsNullOrWhiteSpace(Subject))
            {
                Console.Write("Subject: ");
                gmailSubject = Console.ReadLine();
            }
            else
                gmailSubject = Subject;

            gmailSubject = Common.ProcessVariables(gmailSubject);

            Functions.SendGmail(gmailTo, gmailSubject, ""); //Todo: Support "body"
        }
    }
}
