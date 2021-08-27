using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UVTextureReverser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ImagePreview.Source = ZBitmap.vMask(512,7).toImageSource();
        }

        private void Button_LoadImage_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog ofd = new();

            ofd.Filter = "Image Files|*.png;*.bmp";
            if (ofd.ShowDialog() == true)
            {
                String fileToOpen = ofd.FileName;

                loadImage(fileToOpen);
            }
        }

        private void loadImage(String imagePath)
        {

            ImagePreview.Source = ZBitmap.fromFile(imagePath).toImageSource();
        }

        private void Button_LoadMask_Click(object sender, RoutedEventArgs e)
        {
            int mask = (int)MaskSlider.Value;
            bool horiz = HorizCheckbox.IsChecked == true;

            ZBitmap scan;
            if (horiz)
            {
                scan = ZBitmap.hMask(512, mask);
            }
            else
            {
                scan = ZBitmap.vMask(512, mask);
            }
            ImagePreview.Source = scan.toImageSource();

        }

        private void Screenshot_Click(object sender, RoutedEventArgs e)
        {
            ImagePreview.Source = ZBitmap.screenshot().toImageSource();
        }
    }

    public class ScanBitmap
    {
        public static BitmapSource black(int size)
        {
            return generate(size, (x, y) => false);
        }

        public static BitmapSource white(int size)
        {
            return generate(size, (x, y) => true);
        }

        public static BitmapSource hmask(int size, int mask)
        {
            return generate(size, (x, y) => (x & mask) != 0);
        }

        public static BitmapSource vmask(int size, int mask)
        {
            return generate(size, (x, y) => (y & mask) != 0);
        }

        /// <summary>
        /// Creates and initializes a new bitmap of the given size, using the given function to determine which pixels should be white or black based on their coordinates
        /// </summary>
        /// <param name="size"></param>
        /// <param name="pixelColorByCoordinates"></param>
        private static WriteableBitmap generate(int size, Func<int, int, bool> pixelColorByCoordinates)
        {
            WriteableBitmap bitmap = new(size, size, 96, 96, PixelFormats.Bgra32, null);

            byte[] blackPixel = { 0, 0, 0, 255 }; // B G R A
            byte[] whitePixel = { 255, 255, 255, 255 }; // B G R A

            for (int x = 0; x < size; ++x)
            {
                for (int y = 0; y < size; ++y)
                {
                    Int32Rect pixel = new Int32Rect(x, y, 1, 1);
                    bitmap.WritePixels(pixel, pixelColorByCoordinates(x, y) ? whitePixel : blackPixel, 4, 0);
                }
            }

            return bitmap;
        }
    }
}
