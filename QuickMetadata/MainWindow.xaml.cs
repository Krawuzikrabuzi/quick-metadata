using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuickMetadata
{
    public partial class MainWindow : Window
    {
        private static readonly string[] tags =
        [
            "drone-dji:AltitudeType",
            "drone-dji:GpsLatitude",
            "drone-dji:GpsLongitude",
            "drone-dji:AbsoluteAltitude",
            "drone-dji:RelativeAltitude",
            "drone-dji:DroneModel",
            "Date/Time Original",
        ];

        // Kleines Model für die ListBox
        private record FileEntry(string FileName, string FilePath, Dictionary<string, string> Fields);

        public MainWindow()
        {
            InitializeComponent();
            TestExtractor();
        }

        private void TestExtractor()
        {
            string[] filePaths =
            [
                 @"C:\Users\R_Dev\Documents\GitHub\quick-metadata\TestFiles\570211A Panos\570211A Panos\TestPhoto_1.JPG",
                @"C:\Users\R_Dev\Documents\GitHub\quick-metadata\TestFiles\570211A Panos\570211A Panos\TestPhoto_2.JPG",
                @"C:\Users\R_Dev\Documents\GitHub\quick-metadata\TestFiles\570211A Panos\570211A Panos\TestPhoto_3.JPG",
                @"C:\Users\R_Dev\Documents\GitHub\quick-metadata\TestFiles\570211A Panos\570211A Panos\TestPhoto_4.JPG",
                @"C:\Users\R_Dev\Documents\GitHub\quick-metadata\TestFiles\570211A Panos\570211A Panos\TestPhoto_5.JPG",
                @"C:\Users\R_Dev\Documents\GitHub\quick-metadata\TestFiles\570211A Panos\570211A Panos\TestPhoto_6.JPG",
            ];

            var results = Extractor.ExtractFromFiles(filePaths, tags);

            var entries = filePaths
                .Zip(results, (path, fields) => new FileEntry(Path.GetFileName(path), path, fields))
                .ToList();

            lb_FileList.ItemsSource = entries;
            lb_FileList.SelectedIndex = 0;
        }

        private void lb_FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb_FileList.SelectedItem is FileEntry entry)
            {
                lb_ImageName.Content = entry.FileName;
                PopulateFields(entry.Fields);
            }
        }

        private void PopulateFields(Dictionary<string, string> fields)
        {
            tb_DroneModel.Text = fields["drone-dji:DroneModel"];
            tb_DateTime.Text = fields["Date/Time Original"];
            tb_AltitudeType.Text = fields["drone-dji:AltitudeType"];
            tb_AbsoluteAltitude.Text = fields["drone-dji:AbsoluteAltitude"];
            tb_RelativeAltitude.Text = fields["drone-dji:RelativeAltitude"];
            tb_Latitude.Text = fields["drone-dji:GpsLatitude"];
            tb_Longitude.Text = fields["drone-dji:GpsLongitude"];
        }

        private async void TextBox_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !string.IsNullOrEmpty(tb.Text))
            {
                Clipboard.SetText(tb.Text);
                await FlashCopied(tb);
            }
        }

        private async void CoordinatesLabel_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(tb_Latitude.Text) || string.IsNullOrEmpty(tb_Longitude.Text))
                return;

            Clipboard.SetText($"{tb_Longitude.Text}\n{tb_Latitude.Text}");
            await FlashCopied(tb_Latitude, tb_Longitude);
        }

        private async Task FlashCopied(params TextBox[] textBoxes)
        {
            var originals = textBoxes.Select(tb => (bg: tb.Background, fg: tb.Foreground)).ToList();

            foreach (var tb in textBoxes)
            {
                tb.Background = Brushes.LightGreen;
                tb.Foreground = Brushes.Black;
            }

            await Task.Delay(200);

            for (int i = 0; i < textBoxes.Length; i++)
            {
                textBoxes[i].Background = originals[i].bg;
                textBoxes[i].Foreground = originals[i].fg;
            }
        }

    }
}