using System.Windows;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;
using System;

namespace MapDownloader
{
    /// <summary>
    /// Interaction logic for StartupWindow.xaml
    /// </summary>
    public partial class StartupWindow
    {
        private WebClient client = new WebClient();
        private List<string> serverMapLists = new List<string>();
        private List<string> serverFastDLs = new List<string>();
        private List<string> serverAppIDs = new List<string>();
        private List<string> serverMapDirectories = new List<string>();

        public StartupWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string json = client.DownloadString("https://raw.githubusercontent.com/Vauff/MapDownloader/master/servers.json");

            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;
                JsonElement serversElement = root.GetProperty("servers");

                foreach (JsonElement server in serversElement.EnumerateArray())
                {
                    ServerListBox.Items.Add(server.GetProperty("name").GetString());
                    serverMapLists.Add(server.GetProperty("mapList").GetString());
                    serverFastDLs.Add(server.GetProperty("fastDL").GetString());
                    serverAppIDs.Add(server.GetProperty("appID").GetString());
                    serverMapDirectories.Add(server.GetProperty("mapsDirectory").GetString());
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.Equals(FastDLUrlTextBox.Text, "") || String.Equals(FastDLUrlTextBox.Text, ""))
            {
                MessageBox.Show("You must select a server!", "Error");
                return;
            }

            Global.fastdlUrl = FastDLUrlTextBox.Text;
            Global.maplistUrl = MapUrlTextBox.Text;
            Global.appID = serverAppIDs[ServerListBox.SelectedIndex];
            Global.mapsDirectory = serverMapDirectories[ServerListBox.SelectedIndex];
            

             Hide();
             MainWindow frmMain = new MainWindow();
             frmMain.Closed += (s, args) => this.Close();
             frmMain.Show();
        }

        private void ServerListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            MapUrlTextBox.Text = serverMapLists[ServerListBox.SelectedIndex];
            FastDLUrlTextBox.Text = serverFastDLs[ServerListBox.SelectedIndex];
            MapUrlTextBox.SelectionStart = 0;
            FastDLUrlTextBox.SelectionStart = 0;
        }
    }
}
