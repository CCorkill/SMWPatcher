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

namespace SMWPatcher.windows
{
    public partial class StartPage : Window
    {
        public StartPage()
        {
            InitializeComponent();
        }

        //Start Button Click
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] config = new string[2];
            config = System.IO.File.ReadAllLines("./resources/config.txt");
            for (int i = 0; i < 2; i++)
            {
                string[] configSplit = config[i].Split('"');
                config[i] = configSplit[1];
            }
            if (config[0] == "" || config[1] == "")
            {
                windows.ConfigAlert alert = new windows.ConfigAlert();
                alert.Show();
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }

        //Settings Button Click
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SettingsPage settingsPage = new SettingsPage();
            settingsPage.Show();
        }

        //Exit
        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            System.Windows.Forms.Application.Exit();
        }
    }
}
