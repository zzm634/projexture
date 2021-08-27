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

namespace UVTextureReverser
{
    /// <summary>
    /// Interaction logic for ScanStartWindow.xaml
    /// </summary>
    public partial class ScanStartWindow : Window
    {
        public ScanStartWindow()
        {
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            // Generate a test image based on the specific size, and save it
            int textureResolution = Int32.Parse(this.TextureResolutionCombo.SelectedValue.ToString());
            int scanResolution = Int32.Parse(this.ScanResolutionCombo.SelectedValue.ToString());
            String texturePath = this.TexturePath.Text;

            Random r = new();
            System.Drawing.Color color = System.Drawing.Color.FromArgb(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256));

            ZBitmap testImage = ZBitmap.fill(textureResolution, textureResolution, color);

            testImage.toFile(texturePath);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Filter = "PNG Image|*.png|TGA Image|*.tga|TIFF Image|*.tiff";
            if(sfd.ShowDialog() == true)
            {
                this.TexturePath.Text = sfd.FileName;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            int textureResolution = Int32.Parse(this.TextureResolutionCombo.SelectedValue.ToString());
            int scanResolution = Int32.Parse(this.ScanResolutionCombo.SelectedValue.ToString());
            String texturePath = this.TexturePath.Text;

            TextureScan ts = new TextureScan(texturePath, textureResolution, scanResolution);

            ScanWindow sw = new ScanWindow();
            sw.scan = ts;
            this.Close();
            sw.Show();
            sw.updateScanState(true);
        }
    }
}
