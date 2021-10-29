using System;
using System.Threading;
using System.Windows;

namespace UVTextureReverser {
    /// <summary>
    /// Interaction logic for ScanWindow.xaml
    /// </summary>
    public partial class ScanWindow : Window {

        private ISTextureScan scan;
        public ScanWindow(ISTextureScan scan) {
            InitializeComponent();
            this.scan = scan;
//            debugWindow = new ScanDebugWindow();
//            debugWindow.Show();
        }

        private ScanDebugWindow debugWindow;

        private static void textureScanNextStep(Object ow) {
            ScanWindow w = (ScanWindow)ow;

            // Take a screenshot and update the scan
            w.scan.addScan(ISBitmap.screenshot());

            w.Dispatcher.Invoke(() => {
                w.workerFinished();
            });
        }

        private void workerFinished() {
            if (!this.scan.done()) {
                this.updateScanState(true);
            } else {
                // open texture scan save window and close this one
                ScanCompleteWindow scw = new ScanCompleteWindow(this.scan);
                this.Close();
                scw.Show();
            }

            this.NextButton.IsEnabled = true;
        }


        public void updateScanState(bool writeTexture) {
            this.Title = String.Format("Projexture - Scanning ({0} of {1})", scan.getCurrentStepNumber(), scan.getTotalStepsCount());
            this.scanStatus.Content = "Scanning: " + scan.getStepDescription();
            if (writeTexture) {
                this.scan.updateScanTexture();
            }
            if (debugWindow != null) {
                debugWindow.update(this.scan);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) {
            this.NextButton.IsEnabled = false;

            Thread worker = new Thread(textureScanNextStep);
            worker.Start(this);

            /*
            // Take a screenshot and update the scan
            this.scan.addScan(ZBitmap.screenshot());

            if(!this.scan.done())
            {
                this.updateScanState(true);
            } else
            {
                // open texture scan save window and close this one
                ZBitmap map = this.scan.getProjectionMap(true);
                ScanCompleteWindow scw = new ScanCompleteWindow(map);
                this.Close();
                scw.Show();
            }
            this.NextButton.IsEnabled = true;
            */
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {

        }
    }
}
