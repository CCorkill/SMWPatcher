using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using WindowsInput;
using HtmlAgilityPack;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SMWPatcher
{
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        //List of hackInfo retrieved from the cache file
        List<html.HackInfo> cache = new List<html.HackInfo>();

        //List of hackInfo retrieved from search results
        List<html.HackInfo> foundItems = new List<html.HackInfo>();

        html.HackInfo hackInfo = new html.HackInfo();
        int currentPage = 1;
        int finalPage = 0;
        string smwPath = "";
        string destinationPath = "";

        //Search mode indicates whether a search is taking place
        //If a search is taking place, the list of hack infos referenced
        //is "foundItems" instead of "cache"
        bool searchMode = false;

        public MainWindow()
        {
            //Do NOT use GenerateCache() unless absolutely necessary
            //GenerateCache()
            InitializeComponent();
            LoadCache();
            UpdateCache();
            GetFinalPage();
            GenerateList();
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!searchMode)
            try
            {
                if (listBox.SelectedIndex > -1)
                {
                    infoTextBlock.Text =
                        "Created By: " + cache[(currentPage-1)*50+listBox.SelectedIndex].author +
                        "\n\nHack Type: " + cache[(currentPage - 1) * 50 + listBox.SelectedIndex].type +
                        "\n\nNumber of Exits: " + cache[(currentPage - 1) * 50 + listBox.SelectedIndex].exits;

                    hackDescription.Text = cache[(currentPage - 1) * 50 + listBox.SelectedIndex].description;
                }
            }
            catch
            {
                infoTextBlock.Text =
                        "Created By: " +
                        "\n\nHack Type: " +
                        "\n\nNumber of Exits: ";
                hackDescription.Text = "";
            }
            else
            {
                try
                {
                    if (listBox.SelectedIndex > -1)
                    {
                        infoTextBlock.Text =
                            "Created By: " + foundItems[listBox.SelectedIndex].author +
                            "\n\nHack Type: " + foundItems[listBox.SelectedIndex].type +
                            "\n\nNumber of Exits: " + foundItems[listBox.SelectedIndex].exits;

                        hackDescription.Text = foundItems[listBox.SelectedIndex].description;
                    }
                }
                catch
                {
                    infoTextBlock.Text =
                        "Created By: " +
                        "\n\nHack Type: " +
                        "\n\nNumber of Exits: ";
                    hackDescription.Text = "";
                }
            }
        }


        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();

            var client = new WebClient();
            string dlPath;

            //Downloads the hack's zip file
            if (!searchMode)
            {
                client.DownloadFile("https:" + cache[(currentPage - 1) * 50 + listBox.SelectedIndex].downloadURL, "hack.zip");
            }
            else
            {
                client.DownloadFile("https:" + foundItems[listBox.SelectedIndex].downloadURL, "hack.zip");
            }
            //Extracts the zip into a new hack folder
            System.IO.Compression.ZipFile.ExtractToDirectory("hack.zip", "./hack");

            //Locates the patch (in case it's in a subfolder)
            string fullPath = findPatch("./hack");

            //Opens flips.exe, the patcher
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "./resources/flips.exe";
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            Process p = Process.Start(info);

            //Simulates keyboard input into flips
            InputSimulator sim = new InputSimulator();
            SetForegroundWindow(p.MainWindowHandle);
            Thread.Sleep(50);
            sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
            Thread.Sleep(50);
            sim.Keyboard.TextEntry(fullPath);
            
            
            sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
            Thread.Sleep(50);
            sim.Keyboard.TextEntry(smwPath);
            sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
            Thread.Sleep(50);
            if (!searchMode)
            { 
                foreach(var c in System.IO.Path.GetInvalidFileNameChars())
                {
                    cache[(currentPage - 1) * 50 + listBox.SelectedIndex].title = cache[(currentPage - 1) * 50 + listBox.SelectedIndex].title.Replace(c, ' ');
                }
                dlPath = destinationPath + "\\" + cache[(currentPage - 1) * 50 + listBox.SelectedIndex].title + ".smc";
                sim.Keyboard.TextEntry(dlPath);
            }
            else
            {
                foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                {
                    foundItems[listBox.SelectedIndex].title = foundItems[listBox.SelectedIndex].title.Replace(c, ' ');
                }
                dlPath = destinationPath + "\\" + foundItems[listBox.SelectedIndex].title + ".smc";
                sim.Keyboard.TextEntry(dlPath);
            }
            sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
            Thread.Sleep(1500);
            p.Kill(); //flips closed
            Thread.Sleep(100);

            //Clean up the patching directory
            System.IO.File.Delete("hack.zip");
            System.IO.Directory.Delete("./hack", true);

            //Download complete
            windows.DownloadConfirm dlConfirm = new windows.DownloadConfirm();
            dlConfirm.Show();
        }

        //Navigates to previous listBox page
        private void listBoxPrev_Click(object sender, RoutedEventArgs e)
        {
            if(currentPage == 1)
            {
                currentPage = finalPage;
            }
            else
            {
                currentPage--;
            }
            GenerateList();
        }

        //Navigates to next listBox page
        private void listBoxNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == finalPage)
            {
                currentPage = 1;
            }
            else
            {
                currentPage++;
            }
            GenerateList();
        }

        private void LoadConfig()
        {
            string[] config = new string[2];
            config = System.IO.File.ReadAllLines("./resources/config.txt");
            for (int i = 0; i < 2; i++)
            {
                string[] configSplit = config[i].Split('"');
                config[i] = configSplit[1];
            }
            smwPath = config[0];
            destinationPath = config[1];

            //This check prevents problems with using the root of a drive as your destination
            //This check prevents problems with using the root of a drive as your destination
            if (destinationPath.EndsWith("\\"))
            {
                destinationPath = destinationPath.Remove(destinationPath.Length - 1);
            }
        }

        //Locates the hack patch, in case it's in a subfolder
        string findPatch(string directory)
        {
            string fullPath = "";
            try
            {
                fullPath = System.IO.Path.GetFullPath(System.IO.Directory.GetFiles(directory, "*.bps")[0]);
            }
            catch
            {
                string[] subFolders = System.IO.Directory.GetDirectories(directory);
                for(int i = 0; i < subFolders.Length; i++)
                {
                    fullPath = findPatch(subFolders[i]);
                }
            }
            return fullPath;
        }

        //Checks if the local cache file matches the SMWCentral's most recent files
        //If not, it adds all hacks not currently included into the cache file
        //If the cache file is very old, this function can take several minutes
        private void UpdateCache()
        {
            List<html.HackInfo> newHacks = new List<html.HackInfo>();
            List<string> urls = new List<string>();
            List<string> titles = new List<string>();
            html.HtmlParser parser = new html.HtmlParser();

            int titleIndex = -1;

            int i = 1;

            while (titleIndex < 0)
            {
                string response = parser.CallUrl("https://www.smwcentral.net/?p=section&s=smwhacks&n=" + i);

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
                foreach (var hackURL in hackURLs)
                {
                    urls.Add("https://www.smwcentral.net" + WebUtility.HtmlDecode(hackURL.GetAttributeValue("href", "")));
                    titles.Add(WebUtility.HtmlDecode(hackURL.InnerText));
                }

                if(titles.Contains(cache[0].title))
                {
                    titleIndex = titles.IndexOf(cache[0].title);
                }

                i++;
            }

            for (i = 0; i < titleIndex; i++)
            {
                try
                {
                    hackInfo = parser.ParseInfo(urls[i]);
                    hackInfo.title = titles[i];
                    foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                    {
                        hackInfo.title = hackInfo.title.Replace(c, ' ');
                    }
                    newHacks.Add(hackInfo);
                }
                catch { }
            }

            newHacks.AddRange(cache);

            string jsonString = JsonSerializer.Serialize(newHacks);
            System.IO.File.WriteAllText("./resources/cache.json", jsonString);

            LoadCache();
        }

        //To be used only if the cache file gets deleted
        //or to replace a cache that contains errors
        private void GenerateCache()
        {
            List<string> urls = new List<string>();
            List<string> titles = new List<string>();
            List<html.HackInfo> hackList = new List<html.HackInfo>();
            html.HtmlParser parser = new html.HtmlParser();

            string response = parser.CallUrl("https://www.smwcentral.net/?p=section&s=smwhacks&n=0");
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);
            var pageNumbers = htmlDoc.DocumentNode.Descendants("a")
            .Where(node => node.GetAttributeValue("title", "").Contains("Go to page "));

            foreach (var page in pageNumbers)
            {
                finalPage = Convert.ToInt32(page.InnerHtml);
            }

            for(int i = 1; i <= finalPage; i++)
            {
                response = parser.CallUrl("https://www.smwcentral.net/?p=section&s=smwhacks&n=" + i);

                //Generate HTML doc from URL and parse only sections containing hack links
                htmlDoc.LoadHtml(response);
                var hackURLs = htmlDoc.DocumentNode.Descendants("a")
                    //Checks that the link is not from a Tip section of the page
                    .Where(node => !node.ParentNode.GetAttributeValue("class", "").Contains("rope"))
                    //Grabs urls of hacks based on their url containing "details"
                    .Where(node => node.GetAttributeValue("href", "").Contains("details"))
                    //Checks that smwcentral is not contained
                    .Where(node => !node.GetAttributeValue("href", "").Contains("smwcentral"));

                //Add URLs to hacks and the names of hacks into HackList object
                foreach (var hackURL in hackURLs)
                {
                    urls.Add("https://www.smwcentral.net" + WebUtility.HtmlDecode(hackURL.GetAttributeValue("href", "")));
                    titles.Add(WebUtility.HtmlDecode(hackURL.InnerText));
                }
            }

            for (int i = 0; i < urls.Count; i++)
            {
                try
                {
                    hackInfo = parser.ParseInfo(urls[i]);
                    hackInfo.title = titles[i];
                    foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                    {
                        hackInfo.title = hackInfo.title.Replace(c, ' ');
                    }
                    hackList.Add(hackInfo);
                }
                catch { }
            }

            string jsonString = JsonSerializer.Serialize(hackList);
            System.IO.File.WriteAllText("./resources/cache.json", jsonString);
        }

        private void LoadCache()
        {
            string jsonString = System.IO.File.ReadAllText("./resources/cache.json");
            cache = JsonSerializer.Deserialize<List<html.HackInfo>>(jsonString);
        }

        //Generates the list of items in the listBox and displays them.
        //Displays the first entry's hack info by default upon loading the list.
        private void GenerateList()
        {
            listBox.Items.Clear();

            for (int i = (currentPage-1)*50; i < currentPage*50 && i < cache.Count; i++)
            {
                listBox.Items.Add(cache[i].title);
            }

            infoTextBlock.Text =
                "Created By: " + cache[(currentPage - 1) * 50].author +
                "\n\nHack Type: " + cache[(currentPage - 1) * 50].type +
                "\n\nNumber of Exits: " + cache[(currentPage - 1) * 50].exits;

            hackDescription.Text = cache[(currentPage - 1) * 50].description;

            pageNumbers.Text = currentPage + " / " + finalPage;
            listBox.SelectedIndex = 0;
        }

        //Calculation to find final page of browser
        private int GetFinalPage()
        {
            float pageCalc = (float)cache.Count / 50;
            finalPage = Convert.ToInt32(pageCalc);

            return finalPage;
        }

        //Search operation
        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            searchMode = true;

            listBoxPrev.Visibility = Visibility.Hidden;
            listBoxNext.Visibility = Visibility.Hidden;
            pageNumbers.Visibility = Visibility.Hidden;

            foundItems.Clear();
            listBox.Items.Clear();

            //Generate the list of found items
            for (int i = 0; i < cache.Count; i++)
            {
                //Checks authors, titles, and hack type for substring from search box (case insensitive)
                if(cache[i].author.IndexOf(searchBox.Text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   cache[i].title.IndexOf(searchBox.Text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                   cache[i].type.IndexOf(searchBox.Text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    foundItems.Add(cache[i]);
                }
            }

            if (foundItems.Count > 0)
            {
                //Populate the listBox with the found items
                for (int i = 0; i < foundItems.Count; i++)
                {
                    listBox.Items.Add(foundItems[i].title);
                }

                infoTextBlock.Text =
                    "Created By: " + foundItems[0].author +
                    "\n\nHack Type: " + foundItems[0].type +
                    "\n\nNumber of Exits: " + foundItems[0].exits;

                hackDescription.Text = foundItems[0].description;
            }
            else
            {
                listBox.Items.Add("No Results Were Found");

                infoTextBlock.Text =
                    "Created By: " +
                    "\n\nHack Type: " +
                    "\n\nNumber of Exits: ";

                hackDescription.Text = "";
            }
        }
        private void searchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                searchButton_Click(sender, e);
            }
        }

        private void searchBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (searchBox.Text == "Search...")
            {
                searchBox.Text = "";
            }
        }

        private void clearSearch_Click(object sender, RoutedEventArgs e)
        {
            searchMode = false;
            searchBox.Text = "";
            GenerateList();

            listBoxPrev.Visibility = Visibility.Visible;
            listBoxNext.Visibility = Visibility.Visible;
            pageNumbers.Visibility = Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            windows.SettingsPage settings = new windows.SettingsPage();
            settings.Show();
        }
    }
}
