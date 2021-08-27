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
    /// Interaction logic for StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }

        private void NewScan_Click(object sender, RoutedEventArgs e)
        {
            ScanStartWindow ssw = new ScanStartWindow();
            this.Close();
            ssw.Show();
        }

        private void NewProjection_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Projecture Map|*.png";
            if (ofd.ShowDialog() == true)
            {
                ZBitmap projectionMap = ZBitmap.fromFile(ofd.FileName);
                ProjectionWindow pw = new ProjectionWindow(projectionMap);
                this.Close();
                pw.Show();
            }

        }
    }


}
