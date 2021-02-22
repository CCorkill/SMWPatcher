using System;
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
        
        /*
        //Parses information from the SMW Hack directory and places it into the HackList object (hack urls and titles)
        public HackList ParseList(string url)
        {
            HackList list = new HackList();

            string response = CallUrl(url);

            //Generate HTML doc from URL and parse only sections containing hack links
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);
            var hackURLs = htmlDoc.DocumentNode.Descendants("a")
                //Checks that the link is not from a Tip section of the page
                .Where(node => !node.ParentNode.GetAttributeValue("class", "").Contains("rope"))
                //Grabs urls of hacks based on their url containing "details"
                .Where(node => node.GetAttributeValue("href", "").Contains("details"))
                //Checks that smwcentral is not contained
                .Where(node => !node.GetAttributeValue("href", "").Contains("smwcentral"));

            //Add URLs to hacks and the names of hacks into HackList object
            foreach(var hackURL in hackURLs)
            {
                list.urls.Add("https://www.smwcentral.net" + WebUtility.HtmlDecode(hackURL.GetAttributeValue("href", "")));
                list.titles.Add(WebUtility.HtmlDecode(hackURL.InnerText));
            }

            //Return the HackList
            return list;
        }
        */

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

        List<string> GetScreenshots(string url)
        {
            List<string> screenshots = new List<string>();

            //Never got this working

            return screenshots;
        }
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
