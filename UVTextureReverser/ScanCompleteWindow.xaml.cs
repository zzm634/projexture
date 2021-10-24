using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Windows;
using System.Windows.Controls;
using Color = SixLabors.ImageSharp.PixelFormats.Bgra32;
using Point = SixLabors.ImageSharp.Point;

namespace UVTextureReverser {
    /// <summary>
    /// Interaction logic for ScanCompleteWindow.xaml
    /// </summary>
    public partial class ScanCompleteWindow : Window {

        private readonly ISTextureScan scan;

        public ScanCompleteWindow(ISTextureScan scan) {
            InitializeComponent();

            this.scan = scan;

            this.Image.ImageSource = scan.getProjectionMap().toImageSource();
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Projexture Map|*.png";
            if (sfd.ShowDialog() == true) {
                this.scan.getProjectionMap().toFile(sfd.FileName);
                ProjectionWindow pw = new ProjectionWindow(sfd.FileName, scan.getTexturePath());
                this.Close();
                pw.Show();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
            (new StartWindow()).Show();
        }

        private void Rescan_Click(object sender, RoutedEventArgs e) {
            this.Close();
            (new ScanWindow(this.scan.restart())).Show();
        }
    }
}
