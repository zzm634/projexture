using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Windows;
using System.Windows.Controls;
using Color = SixLabors.ImageSharp.PixelFormats.Bgra32;
using Image = SixLabors.ImageSharp.Image;

namespace UVTextureReverser {
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class ISTests : Window {
        Image<Color> top = null, bottom = null, right = null;

        private static Image<Color> load() {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.png;*.tga;*.bmp;";
            if (ofd.ShowDialog() == true) {
                return Image.Load<Color>(ofd.FileName);
            }

            return null;
        }

        private static void save(Image<Color> image) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Images|*.png;*.tga;*.bmp;";
            if (sfd.ShowDialog() == true) {
                image.Save(sfd.FileName);
            }
        }
        private void LoadTop_Click(object sender, RoutedEventArgs e) {
            top = load() ?? top;
            applyTransform();
        }
        private void LoadBottom_Click(object sender, RoutedEventArgs e) {
            bottom = load() ?? bottom;
            applyTransform();
        }

        private void SSTop_Click(object sender, RoutedEventArgs e) {
            top = ISBitmap.screenshot().getRawImage();
            applyTransform();
        }

        private void SSBottom_Click(object sender, RoutedEventArgs e) {
            bottom = ISBitmap.screenshot().getRawImage();
            applyTransform();
        }

        private void TBSwap_Click(object sender, RoutedEventArgs e) {
            Image<Color> t = top;
            top = bottom;
            bottom = t;
            applyTransform();
        }

        private void CopyTop_Click(object sender, RoutedEventArgs e) {
            if (right != null) {
                top = right.Clone();
            }

            applyTransform();
        }

        private void CopyBottom_Click(object sender, RoutedEventArgs e) {
            if (right != null) {
                bottom = right.Clone();
            }

            applyTransform();
        }

        private void TransformMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            applyTransform();
        }

        public ISTests() {
            InitializeComponent();
        }

        private void applyTransform() {
            switch (this.TransformMode.SelectedValue) {
                case "MaxBrightness":
                    if (top != null) {
                        right = top.Clone();
                        right.Mutate(x => {
                            x.Brightness(255.0f);
                        });
                    }
                    break;
                case "Add":
                    if (top != null && bottom != null) {
                        right = top.Clone();
                        right.Mutate(x => {
                            x.DrawImage(bottom, SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Add, 1.0f);
                        });
                    }
                    break;
                case "Subtract":
                    if (top != null && bottom != null) {
                        right = top.Clone();
                        right.Mutate(x => {
                            x.DrawImage(bottom, SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Subtract, 1.0f);
                        });
                    }
                    break;
                case "ColorizeGreen":
                    if (top != null) {
                        ISBitmap isTop = new ISBitmap(top);
                        isTop.colorize(new Color(0, 255, 0));
                        right = isTop.getRawImage();
                    }
                    break;
                case "Equals":
                    if (top != null && bottom != null) {
                        ISBitmap isTop = new ISBitmap(top, false);
                        ISBitmap isBottom = new ISBitmap(bottom, false);
                        right = isTop.equals(isBottom).getRawImage();
                    }
                    break;
                case "Invert":
                    if (top != null) {
                        ISBitmap isTop = new ISBitmap(top);
                        isTop.invert();
                        right = isTop.getRawImage();
                    }
                    break;
                case "TransparencyMask":
                    if (top != null && bottom != null) {
                        ISBitmap isTop = new ISBitmap(top);
                        ISBitmap isBottom = new ISBitmap(bottom, false);
                        isTop.mask(isBottom);
                        right = isTop.getRawImage();
                    }

                    break;

            }

            if (top != null) {
                this.TopImage.Source = ISBitmap.toImageSource(top);
            }
            if (bottom != null) {
                this.BottomImage.Source = ISBitmap.toImageSource(bottom);
            }
            if (right != null) {
                this.RightImage.Source = ISBitmap.toImageSource(right);
            }
        }
    }
}
