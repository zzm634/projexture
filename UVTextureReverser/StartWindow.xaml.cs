using Microsoft.Win32;
using System.Windows;

namespace UVTextureReverser {
    /// <summary>
    /// Interaction logic for StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window {
        public StartWindow() {
            InitializeComponent();
        }

        private void NewScan_Click(object sender, RoutedEventArgs e) {
            ScanStartWindow ssw = new ScanStartWindow();
            this.Close();
            ssw.Show();
        }

        private void NewProjection_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Projecture Map|*.png";
            if (ofd.ShowDialog() == true) {
                ProjectionWindow pw = new ProjectionWindow(ofd.FileName);
                this.Close();
                pw.Show();
            }

        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
