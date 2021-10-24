using System;
using System.Windows;

namespace UVTextureReverser {
    /// <summary>
    /// Interaction logic for ScanStartWindow.xaml
    /// </summary>
    public partial class ScanStartWindow : Window {
        public ScanStartWindow() {
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e) {
            // Generate a test image based on the specific size, and save it
            int textureResolution = Int32.Parse(this.TextureResolutionCombo.SelectedValue.ToString());
            int scanResolution = Int32.Parse(this.ScanResolutionCombo.SelectedValue.ToString());
            scanResolution = Math.Min(textureResolution, scanResolution);
            String texturePath = this.TexturePath.Text;

            //   Random r = new();
            //    System.Drawing.Color color = System.Drawing.Color.FromArgb(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256));

            //   ZBitmap testImage = ZBitmap.fill(textureResolution, textureResolution, color);


            // generate a mask texture at the maximum scan resolution in X and Y directions
            // ISBitmap testMask = ISBitmap.checkerboard(textureResolution, scanResolution, System.Drawing.Color.Black, System.Drawing.Color.White);

            ISBitmap testMask = ISBitmap.mask(1 << textureResolution, 1 << textureResolution, 1 << (textureResolution - scanResolution), 0, System.Drawing.Color.Black, System.Drawing.Color.White);

            testMask.toFile(texturePath);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Filter = "Texture File|*.png;*.tga;*.tiff;*.bmp";
            if (sfd.ShowDialog() == true) {
                this.TexturePath.Text = sfd.FileName;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) {
            int textureResolution = Int32.Parse(this.TextureResolutionCombo.SelectedValue.ToString());
            int scanResolution = Int32.Parse(this.ScanResolutionCombo.SelectedValue.ToString());
            scanResolution = Math.Min(textureResolution, scanResolution);
            String texturePath = this.TexturePath.Text;

            ISTextureScan ts = new ISTextureScan(texturePath, textureResolution, scanResolution);

            ScanWindow sw = new ScanWindow(ts);
            this.Close();
            sw.Show();
            sw.updateScanState(true);
        }
    }
}
