using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
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

namespace MapDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private WebClient client { get; } = new WebClient();
        private Queue<string> queue { get; } = new Queue<string>();
        private bool running { get; set; } = false;
        private int processed { get; set; } = 0;
        private int toDownloadCount { get; set; }
        private string currentMap { get; set; }
        private bool currentCompressed { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            client.DownloadFileCompleted += DownloadFinished;
        }

        private void DownloadInfoRichText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private string GetDownloadInfoContents()
        {
            TextRange textRange = new TextRange(
                DownloadInfoRichText.Document.ContentStart,
                DownloadInfoRichText.Document.ContentEnd
            );

            return textRange.Text;
        }

        private void SetProgressBarValue(double value)
        {
            this.DownloadProgressBar.IsIndeterminate = false;
            this.DownloadProgressBar.Value = value;
            this.DownloadProgressBar.ApplyTemplate();
        }

        private void Download()
        {
            if (queue.Count > 0)
            {
                currentMap = queue.Dequeue();

                if (currentMap.StartsWith("$"))
                {
                    currentMap = currentMap.Replace("$", "");
                    currentCompressed = false;
                }
                else
                {
                    currentCompressed = true;
                }

                DownloadInfoRichText.AppendText(Environment.NewLine + "Downloading " + currentMap);

                try
                {
                    if (currentCompressed)
                        client.DownloadFileAsync(new Uri(Global.fastdlUrl + currentMap + ".bsp.bz2"), DirectoryTextBox.Text + currentMap + ".bsp.bz2");
                    else
                        client.DownloadFileAsync(new Uri(Global.fastdlUrl + currentMap + ".bsp"), DirectoryTextBox.Text + currentMap + ".bsp");
                }
                catch (UriFormatException)
                {
                    DownloadInfoRichText.AppendText(Environment.NewLine + "ERROR: Invalid FastDL URL provided");
                    ToggleMode(true);
                }
            }
            else
            {
                if (processed == 1)
                    DownloadInfoRichText.AppendText(Environment.NewLine + "Successfully downloaded/extracted " + processed + " map");
                else
                    DownloadInfoRichText.AppendText(Environment.NewLine + "Successfully downloaded/extracted " + processed + " maps");

                ToggleMode(true);
                BrowseFolderButton.IsEnabled = true;
                DownloadProgressBar.Maximum = processed;

            //    FlashWindow.Flash(this);
            }
        }

        private async void DownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            FileInfo compressedFile = new FileInfo(BrowseFolderButton.Content + currentMap + ".bsp.bz2");

            try
            {
                string x = e.Error.Message;

                DownloadInfoRichText.AppendText(Environment.NewLine + currentMap + " download failed");
            }
            catch (Exception)
            {
                if (currentCompressed)
                {
                    FileStream compressedStream = compressedFile.OpenRead();
                    FileStream decompressedStream = File.Create(GetDownloadInfoContents() + currentMap + ".bsp");

                    DownloadInfoRichText.AppendText(Environment.NewLine + "Extracting " + currentMap);
                    await Task.Run(() => BZip2.Decompress(compressedStream, decompressedStream, true));
                    processed++;
                }
            }

            SetProgressBarValue(( processed / 100 ) / DownloadProgressBar.Maximum);

            if (compressedFile.Exists)
                compressedFile.Delete();

            Download();
        }

        private void btnMain_Click_Download(object sender, EventArgs e)
        {
            ToggleMode(false);
            processed = 0;

            string[] mapList;
            List<string> realMapList = new List<string>();
            List<string> downloadedMapList = new List<string>();
            List<string> toDownloadList = new List<string>();
            FileInfo[] mapFiles;

            DownloadInfoRichText.Document.Blocks.Clear();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
                mapList = client.DownloadString(Global.maplistUrl).Split(',');
            }
            catch (WebException)
            {
                DownloadInfoRichText.AppendText("ERROR: Invalid map list URL provided");
                ToggleMode(true);
                return;
            }

            try
            {
                mapFiles = new DirectoryInfo(DirectoryTextBox.Text).GetFiles("*.bsp");
            }
            catch (Exception)
            {
                DownloadInfoRichText.AppendText("ERROR: Invalid maps directory provided");
                ToggleMode(true);
                return;
            }

            foreach (FileInfo file in mapFiles)
                downloadedMapList.Add(file.Name.Split('.')[0].ToLower());

            foreach (string rawMap in mapList)
            {
                string map = rawMap.Replace("\r\n", "").Replace("\n", "");

                if (!map.Equals(""))
                {
                    realMapList.Add(map);

                    if (!downloadedMapList.Contains(map.Replace("$", "").ToLower()))
                        toDownloadList.Add(map);
                }
            }

            DownloadInfoRichText.AppendText(realMapList.Count + " total maps found in server map list");

            toDownloadCount = toDownloadList.Count;
            DownloadProgressBar.Maximum = toDownloadCount / 100;
            DownloadProgressBar.Value = 0;
        //    DownloadProgressBar.Step = 1;

            if (toDownloadCount != 0)
            {
                if (toDownloadCount == 1)
                    DownloadInfoRichText.AppendText(Environment.NewLine + "Maps directory missing " + toDownloadCount + " map from the map list, marking it for download...");
                else
                    DownloadInfoRichText.AppendText(Environment.NewLine + "Maps directory missing " + toDownloadCount + " maps from the map list, marking them for download...");

                foreach (string map in toDownloadList)
                    queue.Enqueue(map);

                Download();
            }
            else
            {
                DownloadInfoRichText.AppendText(Environment.NewLine + "All maps already downloaded and up to date!");
                ToggleMode(true);
            }
        }

        private void btnMain_Click_Stop(object sender, EventArgs e)
        {
            DownloadInfoRichText.AppendText(Environment.NewLine + "Stop request received, process will stop after the current map is finished");
            DownloadButton.IsEnabled = false;
            queue.Clear();
        }

        private void ToggleMode(bool defaultState)
        {
            if (defaultState)
            {
                DownloadButton.Content = "Download Maps";
                this.DownloadButton.Click -= new RoutedEventHandler(this.btnMain_Click_Stop);
                this.DownloadButton.Click += new RoutedEventHandler(this.btnMain_Click_Download);
            }
            else
            {
                DownloadButton.Content = "Stop";
                this.DownloadButton.Click -= new RoutedEventHandler(this.btnMain_Click_Download);
                this.DownloadButton.Click += new RoutedEventHandler(this.btnMain_Click_Stop);
            }

            running = !defaultState;
        //    btnBrowse.Enabled = defaultState;
        //    txtMapsDir.Enabled = defaultState;
        }

        private void btnMain_Click_Download(object sender, RoutedEventArgs e)
        {

        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (running)
            {
                if (MessageBox.Show("A download process is currently running, are you sure you want to exit?" + Environment.NewLine + Environment.NewLine + "Exiting while a process is running could result in map file corruption", "Exit Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
        //    DirectoryTextBox.Text = Functions.GetMapsDirectory();
        //    DirectoryTextBox.SelectionStart = 0;
        }
    }
}
