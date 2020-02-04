using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Assistant
{
    public static class DateTimeStringParser
    {
        private static Dictionary<int, List<string>> monthDictionary = new Dictionary<int, List<string>>()
                        { //Always define these longest to shortest
                            { 1, new List<string>(){ "JANUARY", "JAN" } },
                            { 2, new List<string>(){ "FEBRUARY", "FEB" } },
                            { 3, new List<string>(){ "MARCH", "MAR" } },
                            { 4, new List<string>(){ "APRIL", "APR" } },
                            { 5, new List<string>(){ "MAY" } },
                            { 6, new List<string>(){ "JUNE", "JUN" } },
                            { 7, new List<string>(){ "JULY", "JUL" } },
                            { 8, new List<string>(){ "AUGUST", "AUG" } },
                            { 9, new List<string>(){ "SEPTEMBER", "SEPT", "SEP" } },
                            { 10, new List<string>(){ "OCTOBER", "OCT" } },
                            { 11, new List<string>(){ "NOVEMBER", "NOV" } },
                            { 12, new List<string>(){ "DECEMBER", "DEC" } }
                        };

        private static Dictionary<int, List<string>> daysOfWeekDictionary = new Dictionary<int, List<string>>()
                        { //Always define these longest to shortest
                            { 0, new List<string>(){ "SUNDAY", "SUN" } },
                            { 1, new List<string>(){ "MONDAY", "MON" } },
                            { 2, new List<string>(){ "TUESDAY", "TUES" } },
                            { 3, new List<string>(){ "WEDNESDAY", "WED" } },
                            { 4, new List<string>(){ "THURSDAY", "THURS" } },
                            { 5, new List<string>(){ "FRIDAY", "FRI" } },
                            { 6, new List<string>(){ "SATURDAY", "SAT" } },
                        };
        private static string daysOfWeekGroup = "";

        private static string specialStringGroup = "TODAY|TOMORROW";

        private static List<string> possibleMonthCollisions = new List<string>();
        private static string monthCollisionGroup = "";

        public static DateTime ParseString(string dateTimeString)
        {
            #region InitializeCollisions
            //Initalize "possibleMonthCollisions" list
            possibleMonthCollisions.AddRange(monthDictionary[4]); //Will collide with "A" (short for "AM")
            possibleMonthCollisions.AddRange(monthDictionary[8]); //Will collide with "A" (short for "AM")
            possibleMonthCollisions.AddRange(monthDictionary[3]); //Will collide with "M" (short for "MONTHS")
            possibleMonthCollisions.AddRange(monthDictionary[5]); //Will collide with "M" (short for "MONTHS")
            possibleMonthCollisions.AddRange(monthDictionary[12]); //Will collide with "D" (short for "DAYS")

            monthCollisionGroup = "(" + string.Join("|", possibleMonthCollisions) + ")";
            #endregion InitializeCollisions

            foreach (KeyValuePair<int, List<string>> pair in daysOfWeekDictionary)
                pair.Value.ForEach(p => daysOfWeekGroup += p + "|");

            daysOfWeekGroup = daysOfWeekGroup.Remove(daysOfWeekGroup.LastIndexOf("|"));

            dateTimeString = dateTimeString.ToUpper();

            //Remove all non-word characters from string
            Match nonAlphaNumeric = Regex.Match(dateTimeString, @"[^A-Z0-9]");
            while (nonAlphaNumeric.Success)
            {
                dateTimeString = dateTimeString.Replace(nonAlphaNumeric.Value, "");
                nonAlphaNumeric = nonAlphaNumeric.NextMatch();
            }

            DateTime now = DateTime.Now;
            int curMinute = now.Minute;
            int curHour = now.Hour;
            int curDay = now.Day;
            int curMonth = now.Month;
            int curYear = now.Year;

            DateTime result = now;
            while (result == now)
            {
                try
                {
                    #region DurationMatch
                    string possibleMinDuration = PossibleMinDurationMatch(dateTimeString);
                    string possibleHourDuration = PossibleHourDurationMatch(dateTimeString);
                    string possibleDayDuration = PossibleDayDurationMatch(dateTimeString);
                    string possibleWeekDuration = PossibleWeekDurationMatch(dateTimeString);
                    string possibleMonthDuration = PossibleMonthDurationMatch(dateTimeString);
                    string possibleYearDuration = PossibleYearDurationMatch(dateTimeString);
                    #endregion DurationMatch

                    #region DateMatch
                    string possibleMin = PossibleMinMatch(dateTimeString);
                    string possibleHour = PossibleHourMatch(dateTimeString);

                    string possibleDay, possibleMonth, possibleYear;
                    possibleDay = possibleMonth = possibleYear = "";
                    if (IsDayOfWeekMatch(dateTimeString))
                    {
                        if (!string.IsNullOrEmpty(GetDayOfWeek(dateTimeString)))
                        {
                            DayOfWeek targetDoW = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), GetDayOfWeek(dateTimeString));
                            possibleDay = GetNextDoWInstance(DateTime.Today, targetDoW).Day.ToString();
                            possibleMonth = GetNextDoWInstance(DateTime.Today, targetDoW).Month.ToString();
                            possibleYear = GetNextDoWInstance(DateTime.Today, targetDoW).Year.ToString();
                        }
                    }
                    else if (Regex.IsMatch(dateTimeString, specialStringGroup, RegexOptions.IgnoreCase))
                    {
                        if (Regex.IsMatch(dateTimeString, "TODAY", RegexOptions.IgnoreCase))
                        {
                            possibleDay = DateTime.Today.Day.ToString();
                            possibleMonth = DateTime.Today.Month.ToString();
                            possibleYear = DateTime.Today.Year.ToString();
                        }
                        else if (Regex.IsMatch(dateTimeString, "TOMORROW", RegexOptions.IgnoreCase))
                        {
                            possibleDay = DateTime.Today.AddDays(1).Day.ToString();
                            possibleMonth = DateTime.Today.AddDays(1).Month.ToString();
                            possibleYear = DateTime.Today.AddDays(1).Year.ToString();
                        }
                    }
                    else
                    {
                        possibleDay = PossibleDayMatch(dateTimeString);
                        possibleMonth = PossibleMonthMatch(dateTimeString);
                        possibleYear = PossibleYearMatch(dateTimeString);
                    }
                    #endregion DateMatch

                    #region StringsToInts
                    //Convert all the above strings into integers
                    int minDuration = string.IsNullOrEmpty(possibleMinDuration) ? 0 : Convert.ToInt32(Regex.Match(possibleMinDuration, @"\d+").Value);
                    int hourDuration = string.IsNullOrEmpty(possibleHourDuration) ? 0 : Convert.ToInt32(Regex.Match(possibleHourDuration, @"\d+").Value);
                    int dayDuration = string.IsNullOrEmpty(possibleDayDuration) ? 0 : Convert.ToInt32(Regex.Match(possibleDayDuration, @"\d+").Value);
                    int weekDuration = string.IsNullOrEmpty(possibleWeekDuration) ? 0 : Convert.ToInt32(Regex.Match(possibleWeekDuration, @"\d+").Value);
                    int monthDuration = string.IsNullOrEmpty(possibleMonthDuration) ? 0 : Convert.ToInt32(Regex.Match(possibleMonthDuration, @"\d+").Value);
                    int yearDuration = string.IsNullOrEmpty(possibleYearDuration) ? 0 : Convert.ToInt32(Regex.Match(possibleYearDuration, @"\d+").Value);

                    int min = string.IsNullOrEmpty(possibleMin) ? 0 : Convert.ToInt32(Regex.Match(possibleMin, @"\d+").Value); //Default minute to beginning of hour
                    int hour = string.IsNullOrEmpty(possibleHour) ? 6 : Convert.ToInt32(Regex.Match(possibleHour, @"\d+").Value); //Default hour to 6am (this should come from a setting later)
                    int day = string.IsNullOrEmpty(possibleDay) ? now.Day : Convert.ToInt32(Regex.Match(possibleDay, @"\d+").Value);
                    int month = string.IsNullOrEmpty(possibleMonth) ? now.Month : Convert.ToInt32(Regex.Match(possibleMonth, @"\d+").Value);
                    int year = string.IsNullOrEmpty(possibleYear) ? now.Year : Convert.ToInt32(Regex.Match(possibleYear, @"\d+").Value);
                    #endregion StringsToInts

                    //Choose between "Duration" or "Date" & create new date
                    if (string.IsNullOrEmpty(possibleMinDuration) && string.IsNullOrEmpty(possibleHourDuration) && string.IsNullOrEmpty(possibleDayDuration) &&
                        string.IsNullOrEmpty(possibleWeekDuration) && string.IsNullOrEmpty(possibleMonthDuration) && string.IsNullOrEmpty(possibleYearDuration))
                    {//Therefore, "Date"

                        if (!string.IsNullOrWhiteSpace(GetTimeGroup(dateTimeString)) && !string.IsNullOrWhiteSpace(possibleYear))
                            year = curYear;

                        result = new DateTime(year, month, day, hour, min, 0);

                        while (result < DateTime.Now)
                        {
                            if (string.IsNullOrEmpty(possibleYear))
                                result = result.AddYears(1);
                            else if (string.IsNullOrEmpty(possibleMonth))
                                result = result.AddMonths(1);
                            else
                                result = now; //Change to default for "unclear"
                        }
                    }
                    else if (string.IsNullOrEmpty(possibleMin) && string.IsNullOrEmpty(possibleHour) && string.IsNullOrEmpty(possibleDay) &&
                             string.IsNullOrEmpty(possibleMonth) && string.IsNullOrEmpty(possibleYear))
                    {//Therefore, "Duration"
                        result = result.AddSeconds(result.Second * -1); //Zero-out the seconds
                        result = result.AddMinutes(minDuration);
                        result = result.AddHours(hourDuration);
                        result = result.AddDays(dayDuration);
                        result = result.AddDays(weekDuration * 7);
                        result = result.AddMonths(monthDuration);
                        result = result.AddYears(yearDuration);
                    }
                    else
                    {//There were both "Duration elements" and "Date" elements found
                        result = now;
                        Console.WriteLine("Date was unclear...please try again: ");
                        dateTimeString = Console.ReadLine().ToUpper();
                    }
                }
                catch (Exception ex)
                {
                    result = now;
                    Console.WriteLine("Date was unclear...please try again: ");
                    dateTimeString = Console.ReadLine().ToUpper();
                }
            }

            return result;
        }

        #region DurationMatchMethods
        private static string PossibleMinDurationMatch(string input)
        {
            return Regex.Match(input, @"\d+(MINUTES|MINUTE|MIN)").Value;
        }

        private static string PossibleHourDurationMatch(string input)
        {
            return Regex.Match(input, @"\d+(HOURS|HOUR|H)").Value;
        }

        private static string PossibleDayDurationMatch(string input)
        {
            if (input.IndexOf(Regex.Match(input, @"\d+(DAYS|DAY|D)").Value) != input.IndexOf(Regex.Match(input, @"\d+" + monthCollisionGroup).Value))
                return Regex.Match(input, @"\d+(DAYS|DAY|D)").Value;
            else
                return "";
        }

        private static string PossibleWeekDurationMatch(string input)
        {
            return Regex.Match(input, @"\d+(WEEKS|WEEK|W)").Value;
        }

        private static string PossibleMonthDurationMatch(string input)
        {
            if (input.IndexOf(Regex.Match(input, @"\d+(MONTHS|MONTH|M)").Value) != input.IndexOf(Regex.Match(input, @"\d+" + monthCollisionGroup).Value))
                return Regex.Match(input, @"\d+(MONTHS|MONTH|M)").Value;
            else
                return "";
        }

        private static string PossibleYearDurationMatch(string input)
        {
            return Regex.Match(input, @"\d+(YEARS|YEAR|Y)").Value;
        }
        #endregion DurationMatchMethods

        #region DateMatchMethods
        private static string PossibleMinMatch(string input)
        {
            string timeFromInput = GetTimeGroup(input);
            if (string.IsNullOrEmpty(timeFromInput))
                return "";

            int timeFromInputInt = Convert.ToInt32(timeFromInput);
            if (timeFromInputInt / 100 > 0)
                return (timeFromInputInt % 100).ToString();
            else
                return "0";
        }

        private static string PossibleHourMatch(string input)
        {
            string timeFromInput = GetTimeGroup(input);
            if (string.IsNullOrEmpty(timeFromInput))
                return "";

            int timeFromInputInt = Convert.ToInt32(timeFromInput);
            if (timeFromInputInt / 100 > 0)
                return (timeFromInputInt / 100).ToString();
            else
                return timeFromInput;
        }

        private static string GetTimeGroup(string input)
        {
            //Check for "AM"/"PM" time
            if (Regex.Match(input, @"\d{1,4}(AM|A)").Success &&
                (input.IndexOf(Regex.Match(input, @"\d{1,4}(AM|A)").Value) != input.IndexOf(Regex.Match(input, @"\d{1,4}" + monthCollisionGroup).Value)))
            {
                int time = Convert.ToInt32(Regex.Replace(Regex.Match(input, @"\d{1,4}(AM|A)").Value, @"(AM|A)", ""));
                if (time / 100 > 0)
                {
                    int minute = time % 100;
                    int hour = time / 100;
                    hour = hour == 12 ? 0 : hour;
                    return hour.ToString() + minute.ToString();
                }
                else
                    return time.ToString();
            }
            else if (Regex.Match(input, @"\d{1,4}(PM|P)").Success)
            {
                int time = Convert.ToInt32(Regex.Replace(Regex.Match(input, @"\d{1,4}(PM|P)").Value, @"(PM|P)", ""));
                if (time / 100 > 0)
                {
                    int minute = time % 100;
                    int hour = time / 100;
                    hour = hour == 12 ? 12 : hour + 12; //"+ 12" for PM

                    return hour.ToString() + minute.ToString();
                }
                else
                {
                    time = time == 12 ? 12 : time + 12; //"+ 12" for PM
                    return time.ToString();
                }
            }

            //Check for time at the beginning of input string
            string timeGroup = Regex.Match(input, @"\d{1,4}").Value;
            if (input.IndexOf(timeGroup) == 0 && (string.IsNullOrWhiteSpace(GetDayMonthGroup(input)) || input.IndexOf(GetDayMonthGroup(input)) != 0))
                return timeGroup;

            return "";
        }

        private static bool IsDayOfWeekMatch(string input)
        {
            if (Regex.IsMatch(input, daysOfWeekGroup, RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(input, monthCollisionGroup, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        private static string GetDayOfWeek(string input)
        {
            foreach (KeyValuePair<int, List<string>> pair in daysOfWeekDictionary)
            {
                if (!string.IsNullOrEmpty(pair.Value.Where(p => input.Contains(p)).FirstOrDefault()))
                    return pair.Key.ToString();
            }

            return "";
        }

        private static DateTime GetNextDoWInstance(DateTime startDate, DayOfWeek day)
        {
            int dayNumber = (int)startDate.DayOfWeek;
            int reqDayNumber = (int)day;

            if (dayNumber == reqDayNumber)
                return startDate.AddDays(7); //Don't use the current day as the "next" one
            else if (dayNumber > reqDayNumber)
                return startDate.AddDays(7 - (dayNumber - reqDayNumber));
            else
                return startDate.AddDays(reqDayNumber - dayNumber);
        }

        private static string PossibleDayMatch(string input)
        {
            //Check for "date parts" (ST, ND, RD, TH - as in "1ST")
            List<string> dayParts = new List<string>() { "ST", "ND", "RD", "TH" };
            string dayPart = dayParts.Where(p => input.Contains(p)).FirstOrDefault();
            if (!string.IsNullOrEmpty(dayPart))
                return Regex.Match(input, @"\d{1,2}" + dayPart).Value.Replace(dayPart, "");

            //Check for a date based off a month
            string dayMonthGroup = GetDayMonthGroup(input);
            if (!string.IsNullOrEmpty(dayMonthGroup))
            {
                if (Regex.Match(dayMonthGroup, @"\d{1,2}").Success)
                    return Regex.Match(dayMonthGroup, @"\d{1,2}").Value;
                else
                    return "1"; //Set to the first of the month (if there is no date paired with the month)
            }

            return "";
        }

        private static string PossibleMonthMatch(string input)
        {
            string dayMonthGroup = GetDayMonthGroup(input);
            foreach (KeyValuePair<int, List<string>> pair in monthDictionary)
            {
                if (!string.IsNullOrEmpty(pair.Value.Where(p => dayMonthGroup.Contains(p)).FirstOrDefault()))
                    return pair.Key.ToString();
            }

            return "";
        }

        private static string GetDayMonthGroup(string input)
        {
            foreach (KeyValuePair<int, List<string>> pair in monthDictionary)
            {
                if (!string.IsNullOrEmpty(pair.Value.Where(p => input.Contains(p)).FirstOrDefault()))
                {
                    string monthGroup = "(";
                    foreach (string variation in monthDictionary[pair.Key])
                        monthGroup += variation + "|";

                    monthGroup = monthGroup.Remove(monthGroup.LastIndexOf('|')) + ")";

                    if (Regex.Match(input, @"\d{1,2}" + monthGroup).Success)
                        return Regex.Match(input, @"\d{1,2}" + monthGroup).Value;
                    else if (Regex.Match(input, monthGroup + @"\d{1,2}").Success && !Regex.Match(input, monthGroup + @"\d{4}").Success)
                        return Regex.Match(input, monthGroup + @"\d{1,2}").Value;
                }
            }

            return "";
        }

        private static string PossibleYearMatch(string input)
        {
            Match yearGroup = Regex.Match(input, @"\d{4}");
            while (yearGroup.Success)
            {
                if (input.IndexOf(yearGroup.Value) + yearGroup.Value.Length == input.Length)
                    return yearGroup.Value;

                yearGroup = yearGroup.NextMatch();
            }

            return "";
        }
        #endregion DateMatchMethods
    }
}
