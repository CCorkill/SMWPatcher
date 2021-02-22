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
using System.Windows.Shapes;
using System.Windows.Forms;

namespace SMWPatcher.windows
{
    public partial class SettingsPage : Window
    {
        public SettingsPage()
        {
            //Loads information from the config file upon startup
            string[] config = new string[2];

            if (System.IO.File.Exists("config.txt"))
            {
                config = ParseConfig(config);
            }
            else
            {
                string[] defaultConfig = { "romlocation:\"\"", "romdestination:\"\"" };
                System.IO.File.WriteAllLines("config.txt", defaultConfig);
            }
            InitializeComponent();
            romLocation.Text = config[0];
            romDestination.Text = config[1];
        }

        //SMW ROM location button click
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Creates OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //Sets filter for file extension
            dlg.Filter = "ROM File (.smc)|*.smc";

            //Displays OpenFileDialog by calling ShowDialog
            Nullable<bool> result = dlg.ShowDialog();

            if(result == true)
            {
                string filename = dlg.FileName;
                romLocation.Text = filename;
            }
        }

        //Output destination button click
        private void destinationBrowse_Click(object sender, RoutedEventArgs e)
        {
            //Creates FolderBrowserDialog
            var folderBrowser = new FolderBrowserDialog();
            //Displays FolderBrowserDialog and tracks its result
            DialogResult result = folderBrowser.ShowDialog();

            //If user clicks "OK" stores the selected filepath in destination textbox
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string filepath = folderBrowser.SelectedPath;
                romDestination.Text = filepath;
            }    
        }

        //OK button click
        private void settingsOkButton_Click(object sender, RoutedEventArgs e)
        {
            string[] config = new string[2];

            //Stream info from textboxes into a file
            config[0] = "romlocation:\"" + romLocation.Text + "\"";
            config[1] = "romdestination:\"" + romDestination.Text + "\"";

            System.IO.File.WriteAllLines("config.txt", config);

            //Close window
            this.Close();
        }

        //Cancel button Click
        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            //Close window
            this.Close();
        }

        //Helper function for parsing the config file
        string[] ParseConfig(string[] config)
        {
            config = System.IO.File.ReadAllLines("config.txt");

            //Loop extracts the contents of the rom location and destination lines
            for(int i = 0; i < 2; i++)
            {
                string[] configSplit = config[i].Split('"');
                config[i] = configSplit[1];
            }

            return config;
        }
    }
}
