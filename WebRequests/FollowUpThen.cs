using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRequests
{
    public class FollowUp
    {
        public DateTime Time { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
    public class FollowUpThen
    {
        public static List<FollowUp> GetFollowUps(string username, string password)
        {
            List<FollowUp> result = new List<FollowUp>();
            var loginUrl = "https://www.followupthen.com/login";

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--headless");

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            try
            {

                using (var driver = new ChromeDriver(service, options))
                {
                    driver.Navigate().GoToUrl(loginUrl);
                    var usernameField = driver.FindElementByName("email");
                    var passwordField = driver.FindElementByName("password");
                    var loginButton = driver.FindElementByXPath("//*[@type='submit']");

                    usernameField.SendKeys(username);
                    passwordField.SendKeys(password);
                    loginButton.Click();

                    //Scroll the page to dynamically load all the followups
                    var followUps = driver.FindElementsByXPath("//*[@class='row_container ng-scope']").ToList();
                    while (followUps.Count == 0)
                        followUps = driver.FindElementsByXPath("//*[@class='row_container ng-scope']").ToList();

                    var footer = driver.FindElementByTagName("footer");
                    int followUpCount = 0;
                    while (followUpCount != followUps.Count)
                    {
                        followUpCount = followUps.Count;
                        Actions actions = new Actions(driver);
                        actions.MoveToElement(footer);
                        actions.Perform();
                        System.Threading.Thread.Sleep(500); //Wait for page to load the new followups
                        followUps = driver.FindElementsByXPath("//*[@class='row_container ng-scope']").ToList();
                    }

                    //Get the followups
                    foreach (var followUp in followUps)
                    {
                        FollowUp newFollowUp = new FollowUp();

                        var titleElement = followUp.FindElement(By.XPath(".//*[@class='subject ng-binding']"));
                        newFollowUp.Title = titleElement.Text;

                        var timeElement = followUp.FindElement(By.XPath(".//*[@class='fut_time ng-scope ng-binding']"));
                        DateTime followUpTime;
                        DateTime.TryParseExact(timeElement.Text, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out followUpTime);
                        if (followUpTime == DateTime.MinValue)
                            DateTime.TryParseExact(timeElement.Text, "ddd, MMM d H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out followUpTime);

                        if (followUpTime == DateTime.MinValue)
                            DateTime.TryParseExact(timeElement.Text, "ddd, MMM d, yyyy H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out followUpTime);

                        newFollowUp.Time = followUpTime;

                        //newFollowUp.Url //Because of the way followupthen.com works, this is currently not supported
                        result.Add(newFollowUp);
                    }
                }
            }
            catch
            {

            }

            return result;
        }
    }
}
