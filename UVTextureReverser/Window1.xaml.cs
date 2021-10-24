using System.Windows;

namespace UVTextureReverser {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ScanDebugWindow : Window {

        public ScanDebugWindow() {
            InitializeComponent();
        }

        public void update(ISTextureScan ts) {
            this.Black.Source = ts.getBlackScan()?.toImageSource();
            this.White.Source = ts.getWhiteScan()?.toImageSource();
            this.Map.Source = ts.getMap()?.toImageSource();
        }
    }
}
