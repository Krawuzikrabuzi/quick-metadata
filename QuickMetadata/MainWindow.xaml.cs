using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public MainWindow(string[] args)
        {
            InitializeComponent();

            if (args.Length == 0)
            {
                TestExtractor();
                return;
            }

            if (args.Length == 2 && args[0] == "/folder")
            {
                var jpgFiles = Directory.GetFiles(args[1], "*.jpg", SearchOption.TopDirectoryOnly)
                                            .DistinctBy(f => f.ToLowerInvariant())
                                            .ToArray();
                LoadFiles(jpgFiles);
            }
            else
            {
                LoadFiles(args);
            }


        }

        public void AddFiles(string message)
        {
            string[] newPaths;

            if (message.StartsWith("/folder|"))
            {
                var folder = message["/folder|".Length..];
                newPaths = Directory.GetFiles(folder, "*.jpg", SearchOption.TopDirectoryOnly)
                                    .Concat(Directory.GetFiles(folder, "*.JPG", SearchOption.TopDirectoryOnly))
                                    .ToArray();
            }
            else
            {
                newPaths = [message];
            }

            var existing = lb_FileList.ItemsSource as IEnumerable<FileEntry> ?? [];
            var existingPaths = existing.Select(e => e.FilePath).ToHashSet();
            var toAdd = newPaths.Where(p => !existingPaths.Contains(p)).ToArray();

            if (toAdd.Length == 0) return;

            var results = Extractor.ExtractFromFiles(toAdd, tags);
            var newEntries = toAdd
                .Zip(results, (path, fields) => new FileEntry(Path.GetFileName(path), path, fields))
                .ToList();

            lb_FileList.ItemsSource = existing.Concat(newEntries).ToList();
        }


        private void LoadFiles(string[] filePaths)
        {
            var results = Extractor.ExtractFromFiles(filePaths, tags);
            var entries = filePaths
                .Zip(results, (path, fields) => new FileEntry(Path.GetFileName(path), path, fields))
                .ToList();

            lb_FileList.ItemsSource = entries;
            lb_FileList.SelectedIndex = 0;
        }


        private void TestExtractor()
        {

            string baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));

            string[] filePaths =
            [
                Path.Combine(baseDir, @"TestFiles\570211A Panos\TestPhoto_1.JPG"),
                Path.Combine(baseDir, @"TestFiles\570211A Panos\TestPhoto_2.JPG"),
                Path.Combine(baseDir, @"TestFiles\570211A Panos\TestPhoto_3.JPG"),
                Path.Combine(baseDir, @"TestFiles\570211A Panos\TestPhoto_4.JPG"),
                Path.Combine(baseDir, @"TestFiles\570211A Panos\TestPhoto_5.JPG"),
                Path.Combine(baseDir, @"TestFiles\570211A Panos\TestPhoto_6.JPG"),
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
                LoadPreview(entry.FilePath);
            }
        }


        private static string StripSign(string value) => value.TrimStart('+', '-');

        private static string FormatDateTime(string raw)
        {
            if (DateTime.TryParseExact(raw, "yyyy:MM:dd HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var dt))
                return dt.ToString("dd.MM.yyyy  HH:mm:ss");

            if (DateTime.TryParse(raw, out dt))
                return dt.ToString("dd.MM.yyyy  HH:mm:ss");

            return raw;
        }

        private void PopulateFields(Dictionary<string, string> fields)
        {
            tb_DroneModel.Text = fields["drone-dji:DroneModel"];
            var raw = fields["Date/Time Original"];
            if (DateTime.TryParseExact(raw, "yyyy:MM:dd HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt)
                || DateTime.TryParse(raw, out dt))
            {
                tb_Date.Text = dt.ToString("dd.MM.yyyy");
                tb_Time.Text = dt.ToString("HH:mm:ss");
            }
            else
            {
                tb_Date.Text = raw;
                tb_Time.Text = "";
            }

            tb_AltitudeType.Text = fields["drone-dji:AltitudeType"];
            tb_AbsoluteAltitude.Text = StripSign(fields["drone-dji:AbsoluteAltitude"]);
            tb_RelativeAltitude.Text = StripSign(fields["drone-dji:RelativeAltitude"]);
            tb_Latitude.Text = StripSign(fields["drone-dji:GpsLatitude"]);
            tb_Longitude.Text = StripSign(fields["drone-dji:GpsLongitude"]);
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

        private async void DateTimeLabel_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(tb_Date.Text) || string.IsNullOrEmpty(tb_Time.Text))
                return;

            Clipboard.SetText($"{tb_Date.Text} {tb_Time.Text}");
            await FlashCopied(tb_Date, tb_Time);
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

        private void LoadPreview(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.DecodePixelWidth = 500;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                img_Preview.Source = bitmap;
            }
            catch
            {
                img_Preview.Source = null;
            }
        }


    }
}