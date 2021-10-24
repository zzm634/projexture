using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.ImageSharp.Processing.Processors.Drawing;
using SixLabors.ImageSharp.Processing.Processors.Filters;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Color = SixLabors.ImageSharp.PixelFormats.Bgra32;
using SDIColor = System.Drawing.Color;

namespace UVTextureReverser {
    public class ISBitmap {
        private Image<Color> bitmap;

        public int width {
            get {
                return bitmap.Width;
            }
        }

        public int height {
            get {
                return bitmap.Height;
            }
        }

        public ISBitmap clone() {
            return new ISBitmap(this.bitmap.Clone());
        }

        public SDIColor getPixel(int x, int y) {
            return c(this.bitmap[x, y]);
        }

        public void setPixel(int x, int y, SDIColor c) {
            Color p = new Color(c.R, c.B, c.G, c.A);
            this.bitmap[x, y] = p;
        }

        public void setPixel(int x, int y, Color c) {
            this.bitmap[x, y] = c;
        }

        public static Color c(SDIColor c) {
            return new Color(c.R, c.G, c.B, c.A);
        }

        public static SDIColor c(Color c) {
            return SDIColor.FromArgb(c.A, c.R, c.G, c.B);
        }

        public System.Windows.Media.ImageSource toImageSource() {
            return toImageSource(this.bitmap);
        }

        public ISBitmap(int width, int height, SDIColor fill) {
            bitmap = new(width, height, c(fill));
        }

        public ISBitmap(int width, int height, Color fill) {
            bitmap = new(width, height, fill);
        }

        public void toFile(String path) {
            this.bitmap.Save(path);
        }

        public static ISBitmap fromFile(String path) {
            var image = Image.Load<Color>(path);
            return new ISBitmap(image);
        }

        public ISBitmap(Image<Color> image, bool clone = true) {
            this.bitmap = clone ? image.Clone() : image;
        }

        public static ISBitmap screenshot() {
            int screenWidth = (int)System.Windows.SystemParameters.VirtualScreenWidth;
            int screenHeight = (int)System.Windows.SystemParameters.VirtualScreenHeight;

            System.Drawing.Bitmap screenshot = new System.Drawing.Bitmap(screenWidth, screenHeight);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(screenshot);

            int x = (int)System.Windows.SystemParameters.VirtualScreenLeft;
            int y = (int)System.Windows.SystemParameters.VirtualScreenTop;

            g.CopyFromScreen(x, y, 0, 0, screenshot.Size);

            return new ISBitmap(screenshot);
        }


        public void colorize(SDIColor col) {
            colorize(c(col));
        }
        public void colorize(Color c) {
            this.bitmap.Mutate(x => {
                x.DrawImage(new Image<Color>(this.bitmap.Width, this.bitmap.Height, c), PixelColorBlendingMode.Multiply, 1.0f);
            });
        }

        public static System.Windows.Media.ImageSource toImageSource(Image<Color> image) {
            using (MemoryStream buffer = new MemoryStream()) {
                image.SaveAsPng(buffer);
                buffer.Seek(0, SeekOrigin.Begin);
                PngBitmapDecoder d = new PngBitmapDecoder(buffer, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return d.Frames[0];
            }
        }

        public void add(ISBitmap other) {
            this.bitmap.Mutate(x => {
                x.DrawImage(other.getRawImage(), PixelColorBlendingMode.Add, 1.0f);
            });
        }

        /// <summary>
        /// Converts this image to an all-white image where the alpha channel is the brightnes value of the previous image.
        /// </summary>
        public void greyscaleToAlpha() {
            this.bitmap.Mutate(x => {
                x.Grayscale();
                x.ProcessPixelRowsAsVector4(sp => {

                    for (int i = 0; i < sp.Length; ++i) {
                        sp[i].W = sp[i].X;
                        sp[i].X = 255;
                        sp[i].Y = 255;
                        sp[i].Z = 255;
                    }
                });
            });
        }

        public void invert() {
            this.bitmap.Mutate(x => {
                x.Invert();
            });
        }

        /// <summary>
        /// Makes sections of this image transparent based on the (greyscale) brightness in the source image.
        /// </summary>
        /// <param name="other"></param>
        public void mask(ISBitmap other) {
            this.bitmap.Mutate(x => {
                ISBitmap otherGrey = other.clone();
                otherGrey.greyscaleToAlpha();
                x.DrawImage(otherGrey.getRawImage(), PixelColorBlendingMode.Darken, PixelAlphaCompositionMode.SrcIn, 1.0f);
            });
        }

        /// <summary>
        /// Creates a new ISBitmap where any pixels that exactly match between this image and the given image are white (255,255,255), and any that do not exactly match are black (0,0,0).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public ISBitmap equals(ISBitmap other) {
            // subtract this image from the other image,  Pixels that exactly matched will be totally black in both images

            var black1 = this.bitmap.Clone();
            black1.Mutate(x => {
                x.DrawImage(other.bitmap, PixelColorBlendingMode.Subtract, 1.0f);
            });

            var black = other.bitmap.Clone();
            black.Mutate(x => {
                // and also subtract the other image from this one.
                x.DrawImage(this.bitmap, PixelColorBlendingMode.Subtract, 1.0f)
                // add those two together, and any exactly black pixels will be the ones that matched
                .DrawImage(black1, PixelColorBlendingMode.Add, 1.0f)
                // Max out the brightness so that black pixels stay black
                .Brightness(255.0f)
                // Split the pixels into black and non black, but make the black ones white and vise versa,
                .BinaryThreshold(0.1f, c(SDIColor.Black), c(SDIColor.White));

                // Now we should have an image where any pixels that exactly matched were white, and all others are black.
            });


            return new ISBitmap(black, false);
        }

        public ISBitmap(System.Drawing.Bitmap sdiBitmap) {
            using (MemoryStream ms = new MemoryStream()) {
                sdiBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                this.bitmap = Image.Load<Color>(ms, new SixLabors.ImageSharp.Formats.Png.PngDecoder());
            }
        }

        public static ISBitmap checkerboard(int textureSize, int scanSize, SDIColor black, SDIColor white) {

            Image<Color> bitmap = new Image<Color>(1 << textureSize, 1 << textureSize, c(black));
            Color w = c(white);
            Color b = c(black);
            // TODO make this faster and better

            for (int x = 0; x < bitmap.Width; x++) {
                for (int y = 0; y < bitmap.Height; y++) {
                    bool h = ((x >> (textureSize - scanSize)) & 1) == 1;
                    bool v = ((y >> (textureSize - scanSize)) & 1) == 1;
                    if(h != v) {
                        bitmap[x, y] = w;
                    } else {
                        bitmap[x, y] = b;
                    }
                }
            }

            return new ISBitmap(bitmap);
        }

        public static ISBitmap mask(int width, int height, int hmask, int vmask, SDIColor matchColor, SDIColor noMatchColor) {
            return mask(width, height, hmask, vmask, c(matchColor), c(noMatchColor));
        }
        public static ISBitmap mask(int width, int height, int hmask, int vmask, Color matchColor, Color noMatchColor) {
            Image<Color> bitmap = new Image<Color>(width, height, noMatchColor);

            // TODO make this faster and better

            for (int x = 0; x < bitmap.Width; x++) {
                for (int y = 0; y < bitmap.Height; y++) {
                    if ((x & hmask) != 0 ||
                        (y & vmask) != 0) {
                        bitmap[x, y] = matchColor;
                    }
                }
            }

            return new ISBitmap(bitmap);
        }

        public Image<Color> getRawImage() {
            return this.bitmap;
        }

        /// <summary>
        /// Using this image as a projection mapping, generate a texture of the given width and height by projecting the given image.
        /// 
        /// textureSize is a power of 2 thing. actual image size will be (1 << textureSize)
        /// 
        /// max texture size is 12 (4096x4096)
        /// 
        /// </summary>
        /// <param name="projection"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public ISBitmap project(ISBitmap projection, int textureSize) {

            // should be using ZBitmap for projections (for now)
            Debug.Assert(false);

            Debug.Assert(this.width == projection.width);
            Debug.Assert(this.height == projection.height);

            int width = (1 << textureSize);
            int height = width;

            // maximum texture size is 11, which is 4096x4096
            // projection destinations need to be shifted if they are smaller than this

            // create destination image and initialize all of it to zero (zero alpha, black)
            Image<Color> texture = new Image<Color>(width, height, new Color(0,0,0,0));
            Image<Color> iProjection = projection.getRawImage();
            Image<Color> iMap = this.getRawImage();

            // iterate over image and copy to destination texture where pixels match

            for (int x = 0; x < this.width; x++) {
                for (int y = 0; y < this.height; y++) {
                    // the pixel to project onto the texture
                    Color imagePixel = iProjection[x,y];

                    // the pixel that determines the destination to copy to
                    Color mapPixel = iMap[x, y];

                    if (imagePixel.A != 0 && mapPixel.A == 255) {
                        // ignore alphas of projection pixel for now, because it's complicated
                       

                        int tx = ZBitmap.getX(c(mapPixel)) >> (12 - textureSize);
                        int ty = ZBitmap.getY(c(mapPixel)) >> (12 - textureSize);

                        // TODO better blending, for now just copy color
                        texture[tx, ty] = imagePixel;
                    }
                }
            }

            return new ISBitmap(texture);
        }
    }
}
