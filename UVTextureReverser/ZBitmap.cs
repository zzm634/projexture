using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace UVTextureReverser {
    /// <summary>
    /// ZBitmap is a wrapper class that to convert and manipulate 32-bit RGBA images. It can load and save images in PNG, BMP, TIFF, TGA and DDS formats.
    /// 
    /// It is not intended to be fast.
    /// 
    /// ZBitmaps can be used in two ways: as normal images, and as "projection maps". As a projection map, pixel values take on a different meaning. For pixels with an alpha value of 254 or less, the pixel is considered a color pixel and is not used for projection. For pixels with an alpha value of 255, the color values are used to store the X/Y coordinates of the projection destination for a color mapped to this pixel. 
    /// 
    /// When being used as a projection map, for coordinates, the X and Y values are always from 0 to 4095, and should be scaled down based on the target texture size
    /// </summary>
    public class ZBitmap {

        private readonly System.Drawing.Bitmap bitmap;

        /// <summary>
        /// Generates an independent copy of this ZBitmap
        /// </summary>
        /// <returns></returns>
        public ZBitmap clone() {
            return new ZBitmap(this.bitmap);
        }

        // X is red + low 4 bits of blue
        // Y is green + high 4 bits of blue
        public static int getX(Color pixel) {
            return ((pixel.G & 0xFF) << 4) | (pixel.R & 0xF);
        }

        public static Color setX(Color pixel, int x) {
            return Color.FromArgb(pixel.A,
                (pixel.R & 0xF0) | (x & 0xF),
                (x >> 4) & 0xFF,
                pixel.B);
        }

        public static int getY(Color pixel) {
            return ((pixel.B & 0xFF) << 4) | ((pixel.R >> 4) & 0xF);
        }

        public static Color setY(Color pixel, int y) {
            return Color.FromArgb(pixel.A,
                (pixel.R & 0xF) | ((y & 0xF) << 4),
                pixel.G,
                (y >> 4) & 0xFF);
        }

        /// <summary>
        /// The width of this bitmap, in pixels
        /// </summary>
        public int width {
            get {
                return bitmap.Width;
            }
        }

        /// <summary>
        /// The height of this bitmap, in pixels;
        /// </summary>
        public int height {
            get {
                return bitmap.Height;
            }
        }

        /// <summary>
        /// Creates a new, uninitialized ZBitmap of the given width and height.
        /// 
        /// The value of its contents are undefined.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public ZBitmap(int width, int height) {
            this.bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        public ZBitmap(System.Drawing.Bitmap b) {
            this.bitmap = b.Clone(new Rectangle(0, 0, (int)b.Width, (int)b.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Creates a new bitmap of the given dimensions, where the color of each pixel is determined by the masks.
        /// 
        /// If the x-coordinate of a pixel has a non-zero value when bitwise AND'd with the xMask, or the Y coordinate of a pixel does the same with the yMask, the pixel will be the matchColor. Otherwise, it will be the noMatchColor.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="xMask">a bit pattern that</param>
        /// <param name="yMask"></param>
        /// <param name="matchColor"></param>
        /// <param name="noMatchColor"></param>
        /// <returns></returns>
        public static ZBitmap mask(int width, int height, int xMask, int yMask, Color matchColor, Color noMatchColor) {
            ZBitmap b = new ZBitmap(width, height);

            for (int x = 0; x < b.width; x++) {
                for (int y = 0; y < b.height; y++) {
                    if ((x & xMask) != 0 ||
                        (y & yMask) != 0) {
                        b.setPixel(x, y, matchColor);
                    } else {
                        b.setPixel(x, y, noMatchColor);
                    }
                }
            }

            return b;
        }

        /// <summary>
        /// Creates a square bitmap of the given width consisting of all black pixels
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static ZBitmap black(int size) {
            return fill(size, size, Color.Black);
        }

        /// <summary>
        /// Creates a ZBitmap of the given size that is completely filled with the given pixel value.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fill"></param>
        /// <returns></returns>
        public static ZBitmap fill(int width, int height, Color fill) {
            return mask(width, height, 0, 0, fill, fill);
        }

        /// <summary>
        /// Creates a square bitmap of the given width consisting of all white pixels
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static ZBitmap white(int size) {
            return fill(size, size, Color.White);
        }


        /// <summary>
        /// Creates a square, horizontal mask (vertical stripes) with the given bit in the X coordinate acting as the masking value
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static ZBitmap hMask(int size, int m) {
            return mask(size, size, m, 0, Color.White, Color.Black);
        }


        /// <summary>
        /// Creates a square, vertical mask (horizontal stripes) with the given bit in the Y coordinate acting as the masking value
        /// </summary>
        /// <param name="size"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static ZBitmap vMask(int size, int m) {
            return mask(size, size, 0, m, Color.White, Color.Black);
        }

        public ZBitmap crop(Rectangle location) {
            Bitmap b = new Bitmap(location.Width, location.Height);
            Graphics g = Graphics.FromImage(b);
            g.DrawImage(this.bitmap, new Rectangle(0, 0, location.Width, location.Height), location, GraphicsUnit.Pixel);
            return new ZBitmap(b);
        }

        /// <summary>
        /// Creates a new bitmap captures from the screen
        /// </summary>
        /// <returns></returns>
        public static ZBitmap screenshot() {

            int screenWidth = (int)System.Windows.SystemParameters.VirtualScreenWidth;
            int screenHeight = (int)System.Windows.SystemParameters.VirtualScreenHeight;

            System.Drawing.Bitmap screenshot = new Bitmap(screenWidth, screenHeight);
            Graphics g = Graphics.FromImage(screenshot);

            int x = (int)System.Windows.SystemParameters.VirtualScreenLeft;
            int y = (int)System.Windows.SystemParameters.VirtualScreenTop;

            g.CopyFromScreen(x, y, 0, 0, screenshot.Size);

            return new ZBitmap(screenshot);
        }


        private static readonly Regex sdiBitmapReads = new Regex(".*\\.((png)|(tiff)|(bmp)|(jpg)|(jpeg))$");

        /// <summary>
        /// Loads a bitmap from the given file path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ZBitmap fromFile(String path) {
            // BitmapSource can do PNG and TIFF
            // Pfim does TGA and DDS

            Debug.Assert(path != null);

            if (sdiBitmapReads.IsMatch(path.ToLower())) {
                return new ZBitmap(new Bitmap(path));
            }

            if (path.ToLower().EndsWith(".tga")) {
                TgaSharp.TGA t = new(path);
                return new ZBitmap(t.ToBitmap());
            }

            Debug.Assert(false);
            return null;
        }

        public enum FileFormat {
            PNG,
            TIFF,
            BMP,
            TGA,
            DDS,
            AUTO
        }

        public void toFile(String path, FileFormat format = FileFormat.AUTO) {
            using (var fileStream = new FileStream(path, FileMode.Create)) {
                if (path.ToLower().EndsWith(".png") || format == FileFormat.PNG) {

                    this.bitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                } else if (path.ToLower().EndsWith(".tiff") || format == FileFormat.TIFF) {
                    this.bitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Tiff);
                } else if (path.ToLower().EndsWith(".bmp") || format == FileFormat.BMP) {
                    this.bitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Bmp);
                } else if (path.ToLower().EndsWith(".tga") || format == FileFormat.TGA) {
                    TgaSharp.TGA t = new(this.bitmap);
                    t.Save(fileStream);
                } else if (path.ToLower().EndsWith(".dds") || format == FileFormat.DDS) {
                    // TODO
                }
            }
        }



        public void setPixel(uint x, uint y, Color value) {
            if (x >= this.width || y >= this.height) return;

            this.bitmap.SetPixel((int)x, (int)y, value);
        }

        public void setPixel(int x, int y, Color value) {
            if (x >= this.width || y >= this.height) return;

            this.bitmap.SetPixel(x, y, value);
        }

        public Color getPixel(uint x, uint y) {
            return this.bitmap.GetPixel((int)x, (int)y);
        }

        public Color getPixel(int x, int y) {
            return this.bitmap.GetPixel(x, y);
        }

        /// <summary>
        /// Creates a new ZBitmap by overlaying this image with another and applying the given combination function to generate the values in the new image.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="combiner"></param>
        /// <returns></returns>
        public ZBitmap overlay(ZBitmap other, Func<Color, Color, Color> combiner) {
            Debug.Assert(this.width == other.width);
            Debug.Assert(this.height == other.height);
            Debug.Assert(combiner != null);

            ZBitmap combined = new ZBitmap(this.width, this.height);

            for (uint x = 0; x < this.width; ++x) {
                for (uint y = 0; y < this.height; ++y) {
                    combined.setPixel(x, y, combiner(this.getPixel(x, y), other.getPixel(x, y)));
                }
            }

            return combined;
        }

        private static int clamp(int v) {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }

        public ZBitmap add(ZBitmap other) {



            return overlay(other, (p1, p2) => {
                return Color.FromArgb(p1.A, clamp(p1.R + p2.R), clamp(p1.G + p2.G), clamp(p1.B + p2.B));
            });
        }

        public ZBitmap subtract(ZBitmap other) {
            return overlay(other, (p1, p2) => {
                return Color.FromArgb(p1.A, clamp(p1.R - p2.R), clamp(p1.G - p2.G), clamp(p1.B - p2.B));
            });
        }

        public ZBitmap multiply(float value) {
            ZBitmap other = new ZBitmap(this.width, this.height);
            return overlay(other, (p1, p2) => {
                return Color.FromArgb(p1.A,
                    clamp((int)(p1.R * value)),
                     clamp((int)(p1.G * value)),
                      clamp((int)(p1.B * value)));
            });
        }


        public ZBitmap min(ZBitmap other) {
            return overlay(other, (p1, p2) => {
                return Color.FromArgb(p1.A,
                    Math.Min(p1.R, p2.R),
                    Math.Min(p1.G, p2.G),
                    Math.Min(p1.B, p2.B));
            });
        }

        public ZBitmap max(ZBitmap other) {
            return overlay(other, (p1, p2) => {
                return Color.FromArgb(p1.A,
                    Math.Max(p1.R, p2.R),
                    Math.Max(p1.G, p2.G),
                    Math.Max(p1.B, p2.B));
            });
        }

        // Normal alpha blending (overwrite)
        public ZBitmap layer(ZBitmap other) {
            return overlay(other, (p1, p2) => {
                // new alpha = original alpha + other alpha/255;
                // new colors = (original color * (255-new alpha) + other color*new_alpha) / 255
                return Color.FromArgb(clamp(p1.A + (p2.A / 255)),
                    (p1.R * (255 - p2.A) + p2.R * p2.A) / 255,
                    (p1.G * (255 - p2.A) + p2.G * p2.A) / 255,
                    (p1.B * (255 - p2.A) + p2.B * p2.A) / 255);
            });
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
        public ZBitmap project(ZBitmap projection, int textureSize) {
            Debug.Assert(this.width == projection.width);
            Debug.Assert(this.height == projection.height);

            int width = (1 << textureSize);
            int height = width;

            // maximum texture size is 11, which is 4096x4096
            // projection destinations need to be shifted if they are smaller than this

            // create destination image and initialize all of it to zero (zero alpha, black)
            ZBitmap texture = ZBitmap.fill(width, height, Color.FromArgb(0, 0, 0, 0));

            // Uses the R channel to count the number of color samples that have been written to a target pixel. Used for color averaging
            ZBitmap sampleCount = ZBitmap.fill(width, height, Color.FromArgb(0, 0, 0, 0));


            // iterate over image and copy to destination texture where pixels match

            for (int x = 0; x < this.width; x++) {
                for (int y = 0; y < this.height; y++) {
                    // the pixel to project onto the texture
                    Color imagePixel = projection.getPixel(x, y);

                    // the pixel that determines the destination to copy to
                    Color mapPixel = this.getPixel(x, y);

                    if (mapPixel.A == 255) {
                        int tx = getX(mapPixel) >> (12 - textureSize);
                        int ty = getY(mapPixel) >> (12 - textureSize);

                        // For the color, do a weighted average of all channels using the alpha to figure out the total weight
                        int numSamples = sampleCount.getPixel(tx, ty).R;

                        Color texturePixel = texture.getPixel(tx, ty);
                        double aWeightedG = ((double)texturePixel.G * (double)texturePixel.A * numSamples) + (double)imagePixel.G * (double)imagePixel.A;
                        double aWeightedB = ((double)texturePixel.B * (double)texturePixel.A * numSamples) + (double)imagePixel.B * (double)imagePixel.A;
                        double aWeightedR = ((double)texturePixel.R * (double)texturePixel.A * numSamples) + (double)imagePixel.R * (double)imagePixel.A;

                        double totalAlpha = ((double)texturePixel.A * numSamples) + imagePixel.A;

                        double newR, newG, newB, newA;
                        if (totalAlpha == 0)
                        {
                            newR = 0;
                            newG = 0;
                            newB = 0;
                            newA = 0;
                        }
                        else
                        {
                             newR = aWeightedR / totalAlpha;
                             newG = aWeightedG / totalAlpha;
                             newB = aWeightedB / totalAlpha;

                            // new alpha is a weighed average of the old alphas
                             newA = ((texturePixel.A * numSamples) + imagePixel.A) / (numSamples + 1);

                        }
                        Color projectedPixel = Color.FromArgb((int)newA, (int)newR, (int)newG, (int)newB);

                        texture.setPixel(tx, ty, projectedPixel);

                        // there really shouldn't be more than 256 pixels mapping to the same square, but if there is, we could always make this larger using more channels
                        if (numSamples < 255)
                        {
                            numSamples++;
                        }
                        sampleCount.setPixel(tx, ty, Color.FromArgb(0, numSamples, 0, 0));

                        /*
                        Color texturePixel = texture.getPixel(tx, ty);
                        Color newTexturePixel = Color.FromArgb(texturePixel.A + 1,
                            weightedAverage(texturePixel.R, texturePixel.A, projectedPixel.R, 1),
                            weightedAverage(texturePixel.G, texturePixel.A, projectedPixel.G, 1),
                            weightedAverage(texturePixel.B, texturePixel.A, projectedPixel.B, 1));
                        
                        texture.setPixel(tx, ty, newTexturePixel);
                        */

                    }
                }
            }

            return texture;
        }

        public static ZBitmap randomSquiggles(int width, int height, uint squiggles = 10) {
            System.Drawing.Bitmap b = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(b);
            g.FillRectangle(Brushes.Transparent, new Rectangle(0, 0, width, height));

            Random rand = new Random();

            for (uint i = 0; i < squiggles; ++i) {
                // pick a random color
                Color randomColor = Color.FromArgb(255, rand.Next(0, 256), rand.Next(0, 256), rand.Next(0, 256));

                Pen pen = new Pen(new SolidBrush(randomColor));
                pen.Width = 64.0f;
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                // draw a random line
                g.DrawLine(pen, rand.Next(0, width), rand.Next(0, height), rand.Next(0, width), rand.Next(0, height));
            }

            return new ZBitmap(b);
        }

        public static int weightedAverage(int v1, int c1, int v2, int c2) {
            long count = c1 + c2;
            long total = (v1 * c1) + (v2 * c2);
            return (int)(total / count);
        }

        public System.Windows.Media.ImageSource toImageSource() {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(this.bitmap.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(this.bitmap.Width, this.bitmap.Height));
        }

        public static void executeCoordinateTests() {

            Color c = Color.FromArgb(0, 0, 0);

            for(int x=0; x<4096; x++) {
                int y = ZBitmap.getY(c);
                c = ZBitmap.setX(c, x);
                Debug.Assert(ZBitmap.getX(c) == x);
                Debug.Assert(ZBitmap.getY(c) == y);
            }

            for (int y = 0; y < 4096; y++) {
                int x = ZBitmap.getX(c);
                c =ZBitmap.setY(c, y);
                Debug.Assert(ZBitmap.getX(c) == x);
                Debug.Assert(ZBitmap.getY(c) == y);
            }

        }
    }
}