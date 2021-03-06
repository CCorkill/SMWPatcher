﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NHtmlUnit;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SMWPatcher.html
{
    public class HtmlParser
    {
        public string CallUrl(string fullUrl)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(fullUrl).Result;
            HttpContent content = response.Content;
            string result = content.ReadAsStringAsync().Result;
            return result;
        }

        //Assembles all hack information one function at a time
        public HackInfo ParseInfo(string url)
        {
            HackInfo info = new HackInfo();

            string response = CallUrl(url);
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);

            int nodeModifier = GetNodeModifier(htmlDoc);
            HtmlNodeCollection childNodes = GetChildNodes(htmlDoc);

            //info.imageURLs = GetScreenshots(url);
            info.rating = GetRating(htmlDoc);
            info.author = GetAuthor(nodeModifier, childNodes);
            info.type = GetHackType(nodeModifier, childNodes);
            info.exits = GetExits(nodeModifier, childNodes);
            info.description = GetDescription(nodeModifier, childNodes);
            info.downloadURL = GetDownloadURL(nodeModifier, childNodes);

            return info;
        }

        //Gets the modifier for html doc nodes based on existence of version history or "discretion advised"
        //nodes existing in html doc, shifting other nodes from their normal locations
        int GetNodeModifier(HtmlDocument htmlDoc)
        {
            int nodeModifier = 0;
            if (htmlDoc.ParsedText.Contains("Version History"))
            {
                nodeModifier = nodeModifier + 2;
            }
            if (htmlDoc.ParsedText.Contains("Discretion"))
            {
                nodeModifier = nodeModifier + 4;
            }

            return nodeModifier;
        }

        //Gets all relevant child nodes from the htmlDoc, to be parsed for hack info
        HtmlNodeCollection GetChildNodes(HtmlDocument htmlDoc)
        {
            HtmlNode tableRoot = htmlDoc.DocumentNode.SelectSingleNode("//td[@class=\"pad text\"]/table[@class=\"generic\"]");
            HtmlNodeCollection childNodes = tableRoot.ChildNodes;

            return childNodes;
        }

        //Wasn't able to get this working cleanly
        /*
        public List<string> GetScreenshots(string url)
        {
            List<string> screenshots = new List<string>();

            IWebDriver driver;

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");
            driver = new ChromeDriver(chromeOptions);
            driver.Url = url;

            var listContainer = driver.FindElements(By.XPath("//*[@id=\"screenshotListContainer\"]/*"));
            driver.FindElements(By.XPath("//*[@id=\"screenshotContainer\"]"));
            
            string[] protocols = { "TLSv1.2", "TLSv1.1", "TLSv1" };

            NHtmlUnit.WebClient client = new NHtmlUnit.WebClient();
            client.Options.UseInsecureSsl = true;
            client.Options.SSLClientProtocols = protocols;
            client.Options.JavaScriptEnabled = true;

            NHtmlUnit.Html.HtmlPage page = client.GetHtmlPage(url);

            java.net.URL requestURL = new java.net.URL(url);
            NHtmlUnit.WebRequest webRequest = new NHtmlUnit.WebRequest(requestURL);
            NHtmlUnit.WebResponse response = client.LoadWebResponse(webRequest);
            string wait = "";


            //Never got this working

            return screenshots;
        }
        */
        float GetRating(HtmlDocument htmlDoc)
        {
            float rating = 0;
            string ratingString = "";

            //Not all hacks have a rating, so this checks if there is one and then marks if it isn't rated with a -1
            try
            {
                ratingString = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"star_average\"]").InnerText;
                rating = float.Parse(ratingString.Split(' ')[0]);
            }
            catch
            {
                rating = -1;
            }

            return rating;
        }

        //From here on, the text retrieval follows a specific pattern of nodes

        //Node 11 = Author
        //Node 19 = Hack Type
        //Node 17 = Exits
        //Node 21 = Description
        //Node 27 = Download Link

        //Then we access ChildNode[3] of those nodes for the information we need, doing a little manipulation as needed also
        //A nodemodifier is also added to the initial node pattern, since things like Version History and Discretion Advisories
        //can mess up where the nodes normally are

        string GetAuthor(int nodeModifier, HtmlNodeCollection childNodes)
        {
            string author;

            //Checks to see if the author is listed
            //If the check fails, it means the file was uploaded anonymously
            try
            {
                 author = childNodes[11 + nodeModifier].ChildNodes[3].ChildNodes[1].InnerText;
            }
            catch
            {
                 author = "Anonymous";
            }
           
            return author;
        }

        string GetHackType(int nodeModifier, HtmlNodeCollection childNodes)
        {
            string hackType = childNodes[19+nodeModifier].ChildNodes[3].InnerText
                .Replace("\n", "")
                .Replace("\t", "");

            return hackType;
        }

        string GetExits(int nodeModifier, HtmlNodeCollection childNodes)
        {
            string exits = childNodes[17+nodeModifier].ChildNodes[3].InnerText
                .Replace("\n", "")
                .Replace("\t", "");

            return exits;
        }

        string GetDescription(int nodeModifier, HtmlNodeCollection childNodes)
        {
            string description = childNodes[21+nodeModifier].ChildNodes[3].InnerText
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("<br>", "");

            return description;
        }

        string GetDownloadURL(int nodeModifier, HtmlNodeCollection childNodes)
        {
            string downloadURL = WebUtility.HtmlDecode(childNodes[27+nodeModifier].ChildNodes[3].ChildNodes[1].GetAttributeValue("href",""));
            return downloadURL;
        }

    }
}
