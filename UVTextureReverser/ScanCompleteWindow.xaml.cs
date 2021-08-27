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
using Microsoft.Win32;

namespace UVTextureReverser
{
    /// <summary>
    /// Interaction logic for ScanCompleteWindow.xaml
    /// </summary>
    public partial class ScanCompleteWindow : Window
    {

        private readonly ZBitmap scan;
        public ScanCompleteWindow(ZBitmap scan)
        {
            InitializeComponent();

            this.scan = scan;

            this.CropX.Value = 0;
            this.CropX.Minimum = 0;
            this.CropX.Maximum = scan.width;

            this.CropY.Value = 0;
            this.CropY.Minimum = 0;
            this.CropY.Maximum = scan.height;

            this.CropW.Value = scan.width;
            this.CropW.Maximum = scan.width;
            this.CropW.Minimum = 0;

            this.CropH.Value = scan.height;
            this.CropH.Minimum = 0;
            this.CropH.Maximum = scan.height;

            this.Image.Source = scan.toImageSource();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Projecture Map|*.png";
            if(sfd.ShowDialog() == true)
            {
                ZBitmap cropped = this.getCropped();
                cropped.toFile(sfd.FileName, ZBitmap.FileFormat.PNG);
                ProjectionWindow pw = new ProjectionWindow(cropped);
                this.Close();
                pw.Show();
            }
        }

        private void Crop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.Image.Source = this.getCropped().toImageSource();
        }

        private ZBitmap getCropped()
        {
            return scan.crop(new System.Drawing.Rectangle(CropX.Value.GetValueOrDefault(0), CropY.Value.GetValueOrDefault(0), CropW.Value.GetValueOrDefault(scan.width), CropH.Value.GetValueOrDefault(scan.height)));
        }
    }
}
