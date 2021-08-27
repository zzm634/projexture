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
using System.Threading;

namespace UVTextureReverser
{
    /// <summary>
    /// Interaction logic for ScanWindow.xaml
    /// </summary>
    public partial class ScanWindow : Window
    {

        private TextureScan _scan;
        public TextureScan scan
        {
            get
            {
                return _scan;
            }
            set
            {
                this._scan = value;
                updateScanState(false);
            }
        }

        public ScanWindow()
        {
            InitializeComponent();
            debugWindow = new ScanDebugWindow();
            debugWindow.Show();
        }

        private ScanDebugWindow debugWindow;

        private static void textureScanNextStep(Object ow)
        {
            ScanWindow w = (ScanWindow)ow;

            // Take a screenshot and update the scan
            w.scan.addScan(ZBitmap.screenshot());

            w.Dispatcher.Invoke(() =>
            {
                w.workerFinished();
            });
        }

        private void workerFinished()
        {
            if (!this.scan.done())
            {
                this.updateScanState(true);
            }
            else
            {
                // open texture scan save window and close this one
                ZBitmap map = this.scan.getProjectionMap(true);
                ScanCompleteWindow scw = new ScanCompleteWindow(map);
                this.Close();
                scw.Show();
            }

            this.NextButton.IsEnabled = true;
        }


        public void updateScanState(bool writeTexture)
        {
            this.Title = String.Format("Projexture - Scanning ({0} of {1})", scan.getCurrentStepNumber(), scan.getTotalStepsCount());
            this.scanStatus.Content = "Scanning: " + scan.getStepDescription();
            if(writeTexture)
            {
                this.scan.updateScanTexture();
            }
            if(debugWindow != null)
            {
                debugWindow.update(this.scan);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
