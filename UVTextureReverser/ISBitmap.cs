using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tga;
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

namespace UVTextureReverser
{
    public class ISBitmap
    {
        private Image<Color> bitmap;

        public int width
        {
            get
            {
                return bitmap.Width;
            }
        }

        public int height
        {
            get
            {
                return bitmap.Height;
            }
        }

        public ISBitmap clone()
        {
            return new ISBitmap(this.bitmap.Clone());
        }

        public SDIColor getPixel(int x, int y)
        {
            return c(this.bitmap[x, y]);
        }

        public void setPixel(int x, int y, SDIColor c)
        {
            Color p = new Color(c.R, c.B, c.G, c.A);
            this.bitmap[x, y] = p;
        }

        public void setPixel(int x, int y, Color c)
        {
            this.bitmap[x, y] = c;
        }

        public static Color c(SDIColor c)
        {
            return new Color(c.R, c.G, c.B, c.A);
        }

        public static SDIColor c(Color c)
        {
            return SDIColor.FromArgb(c.A, c.R, c.G, c.B);
        }

        // X is red + low 4 bits of blue
        // Y is green + high 4 bits of blue
        public static int getX(Color pixel)
        {
            return ((pixel.G & 0xFF) << 4) | (pixel.R & 0xF);
        }

        public static Color setX(Color pixel, int x)
        {
            return c(SDIColor.FromArgb(pixel.A,
                (pixel.R & 0xF0) | (x & 0xF),
                (x >> 4) & 0xFF,
                pixel.B));
        }

        public static int getY(Color pixel)
        {
            return ((pixel.B & 0xFF) << 4) | ((pixel.R >> 4) & 0xF);
        }

        public static Color setY(Color pixel, int y)
        {
            return c(SDIColor.FromArgb(pixel.A,
                (pixel.R & 0xF) | ((y & 0xF) << 4),
                pixel.G,
                (y >> 4) & 0xFF));
        }

        public System.Windows.Media.ImageSource toImageSource()
        {
            return toImageSource(this.bitmap);
        }

        public ISBitmap(int width, int height, SDIColor fill)
        {
            bitmap = new(width, height, c(fill));
        }

        public ISBitmap(int width, int height, Color fill)
        {
            bitmap = new(width, height, fill);
        }

        public void toFile(String path)
        {
            if (path.ToLower().EndsWith("tga"))
            {
                TgaEncoder enc = new TgaEncoder();
                enc.BitsPerPixel = TgaBitsPerPixel.Pixel32;
                enc.Compression = TgaCompression.None;
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(0); // discard contents of file
                    enc.Encode(this.bitmap, fs);
                }
            }
            else
            {
                this.bitmap.Save(path);
            }
        }

        public static ISBitmap fromFile(String path)
        {
            var image = Image.Load<Color>(path);
            return new ISBitmap(image);
        }

        public ISBitmap(Image<Color> image, bool clone = true)
        {
            this.bitmap = clone ? image.Clone() : image;
        }

        public static ISBitmap screenshot()
        {
            int screenWidth = (int)System.Windows.SystemParameters.VirtualScreenWidth;
            int screenHeight = (int)System.Windows.SystemParameters.VirtualScreenHeight;

            System.Drawing.Bitmap screenshot = new System.Drawing.Bitmap(screenWidth, screenHeight);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(screenshot);

            int x = (int)System.Windows.SystemParameters.VirtualScreenLeft;
            int y = (int)System.Windows.SystemParameters.VirtualScreenTop;

            g.CopyFromScreen(x, y, 0, 0, screenshot.Size);

            return new ISBitmap(screenshot);
        }


        public void colorize(SDIColor col)
        {
            colorize(c(col));
        }
        public void colorize(Color c)
        {
            this.bitmap.Mutate(x =>
            {
                x.DrawImage(new Image<Color>(this.bitmap.Width, this.bitmap.Height, c), PixelColorBlendingMode.Multiply, 1.0f);
            });
        }

        public static System.Windows.Media.ImageSource toImageSource(Image<Color> image)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                image.SaveAsPng(buffer);
                buffer.Seek(0, SeekOrigin.Begin);
                PngBitmapDecoder d = new PngBitmapDecoder(buffer, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return d.Frames[0];
            }
        }

        public void add(ISBitmap other)
        {
            this.bitmap.Mutate(x =>
            {
                x.DrawImage(other.getRawImage(), PixelColorBlendingMode.Add, 1.0f);
            });
        }

        /// <summary>
        /// Converts this image to an all-white image where the alpha channel is the brightnes value of the previous image.
        /// </summary>
        public void greyscaleToAlpha()
        {
            this.bitmap.Mutate(x =>
            {
                x.Grayscale();
                x.ProcessPixelRowsAsVector4(sp =>
                {

                    for (int i = 0; i < sp.Length; ++i)
                    {
                        sp[i].W = sp[i].X;
                        sp[i].X = 255;
                        sp[i].Y = 255;
                        sp[i].Z = 255;
                    }
                });
            });
        }

        public void invert()
        {
            this.bitmap.Mutate(x =>
            {
                x.Invert();
            });
        }

        /// <summary>
        /// Makes sections of this image transparent based on the (greyscale) brightness in the source image.
        /// </summary>
        /// <param name="other"></param>
        public void mask(ISBitmap other)
        {
            this.bitmap.Mutate(x =>
            {
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
        public ISBitmap equals(ISBitmap other)
        {
            // subtract this image from the other image,  Pixels that exactly matched will be totally black in both images

            var black1 = this.bitmap.Clone();
            black1.Mutate(x =>
            {
                x.DrawImage(other.bitmap, PixelColorBlendingMode.Subtract, 1.0f);
            });

            var black = other.bitmap.Clone();
            black.Mutate(x =>
            {
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

        public ISBitmap(System.Drawing.Bitmap sdiBitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                sdiBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                this.bitmap = Image.Load<Color>(ms, new SixLabors.ImageSharp.Formats.Png.PngDecoder());
            }
        }

        /**
         * Modifies this bitmap by searching for transparent pixels that are surrounded by four non-transparent pixels and averages their pixel values to fill in the empty pixel
         */
        public void fillSmallHoles(int times) {


            for(int time = 0; time < times; time++) {
                bool pixelsFixed = false;

                for (int x = 1; x < (this.width - 1); x++)
                {
                    for (int y = 1; y < (this.height - 1); y++)
                    {

                        Color px = this.bitmap[x, y];

                        if (px.A == 0)
                        {
                            Color pu = this.bitmap[x, y - 1];
                            Color pd = this.bitmap[x, y + 1];
                            Color pl = this.bitmap[x - 1, y];
                            Color pr = this.bitmap[x + 1, y];

                            bool pixelFixed = true;

                            // all four
                            if (pu.A != 0 && pd.A != 0 && pl.A != 0 && pr.A != 0)
                            {
                                this.bitmap[x, y] = average(pu, pd, pl, pr);
                            }
                            else if (pu.A != 0 && pd.A != 0)
                            {
                                // top/bottom
                                this.bitmap[x, y] = average(pu, pd);
                            }
                            else if (pl.A != 0 && pr.A != 0)
                            {
                                // left/right
                                this.bitmap[x, y] = average(pl, pr);
                            }
                            else if (pu.A != 0 && pd.A != 0 && pl.A != 0)
                            {
                                this.bitmap[x, y] = average(pu, pd, pl);
                            }
                            else if (pu.A != 0 && pd.A != 0 && pr.A != 0)
                            {
                                this.bitmap[x, y] = average(pu, pd, pr);
                            }
                            else if (pu.A != 0 && pl.A != 0 && pr.A != 0)
                            {
                                this.bitmap[x, y] = average(pu, pl, pr);
                            }
                            else if (pd.A != 0 && pl.A != 0 && pr.A != 0)
                            {
                                this.bitmap[x, y] = average(pd, pl, pr);
                            }
                            else
                            {
                                pixelFixed = false;
                            }

                            pixelsFixed |= pixelFixed;
                        }
                    }
                }

                if (!pixelsFixed)
                    break;
            }
        }

        private static Color average(params Color[] colors)
        {
            long a = 0, r = 0, g = 0, b = 0;
            foreach(Color c in colors)
            {
                a += c.A;
                r += c.R;
                g += c.G;
                b += c.B;
            }

            int n = colors.Length;

            return new Color((byte)(r / n),
                (byte)(g / n),
                (byte)(b / n),
                (byte)(a / n));
        }

        public static ISBitmap checkerboard(int textureSize, int scanSize, SDIColor black, SDIColor white)
        {

            Image<Color> bitmap = new Image<Color>(1 << textureSize, 1 << textureSize, c(black));
            Color w = c(white);
            Color b = c(black);
            // TODO make this faster and better

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    bool h = ((x >> (textureSize - scanSize)) & 1) == 1;
                    bool v = ((y >> (textureSize - scanSize)) & 1) == 1;
                    if (h != v)
                    {
                        bitmap[x, y] = w;
                    }
                    else
                    {
                        bitmap[x, y] = b;
                    }
                }
            }

            return new ISBitmap(bitmap);
        }

        public static ISBitmap mask(int width, int height, int hmask, int vmask, SDIColor matchColor, SDIColor noMatchColor)
        {
            return mask(width, height, hmask, vmask, c(matchColor), c(noMatchColor));
        }
        public static ISBitmap mask(int width, int height, int hmask, int vmask, Color matchColor, Color noMatchColor)
        {
            Image<Color> bitmap = new Image<Color>(width, height, noMatchColor);

            // TODO make this faster and better

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if ((x & hmask) != 0 ||
                        (y & vmask) != 0)
                    {
                        bitmap[x, y] = matchColor;
                    }
                }
            }

            return new ISBitmap(bitmap);
        }

        public Image<Color> getRawImage()
        {
            return this.bitmap;
        }

        /**
         * Clones this bitmap and rescales it to the new size
         */
        public ISBitmap scale(int width, int height)
        {
            return new ISBitmap(this.bitmap.Clone(x =>
            {
                x.Resize(width, height, KnownResamplers.Bicubic);
            }));
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
        public ISBitmap project(ISBitmap projection, int textureSize)
        {
            Debug.Assert(this.width == projection.width);
            Debug.Assert(this.height == projection.height);

            int width = (1 << textureSize);
            int height = width;

            // maximum texture size is 11, which is 4096x4096
            // projection destinations need to be shifted if they are smaller than this

            // create destination image and initialize all of it to zero (zero alpha, black)
            ISBitmap texture = new ISBitmap(width, height, new Color(0, 0, 0, 0));

            // Uses the R channel to count the number of color samples that have been written to a target pixel. Used for color averaging
            ISBitmap sampleCount = new ISBitmap(width, height, new Color(0, 0, 0, 0));


            // iterate over image and copy to destination texture where pixels match

            for (int x = 0; x < this.width; x++)
            {
                for (int y = 0; y < this.height; y++)
                {
                    // the pixel to project onto the texture
                    Color imagePixel = projection.bitmap[x, y];

                    // the pixel that determines the destination to copy to
                    Color mapPixel = this.bitmap[x, y];

                    if (mapPixel.A == 255)
                    {
                        int tx = getX(mapPixel) >> (12 - textureSize);
                        int ty = getY(mapPixel) >> (12 - textureSize);

                        // For the color, do a weighted average of all channels using the alpha to figure out the total weight
                        int numSamples = sampleCount.getPixel(tx, ty).R;

                        Color texturePixel = texture.bitmap[tx, ty];
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
                        Color projectedPixel = c(SDIColor.FromArgb((int)newA, (int)newR, (int)newG, (int)newB));

                        texture.setPixel(tx, ty, projectedPixel);

                        // there really shouldn't be more than 256 pixels mapping to the same square, but if there is, we could always make this larger using more channels
                        if (numSamples < 255)
                        {
                            numSamples++;
                        }
                        sampleCount.setPixel(tx, ty, c(SDIColor.FromArgb(0, numSamples, 0, 0)));

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

    public ISBitmap layer(ISBitmap other)
        {
            Debug.Assert(this.width == other.width);
            Debug.Assert(this.height == other.height);

            ISBitmap combined = this.clone();

            combined.bitmap.Mutate(x =>
            {
                x.DrawImage(other.bitmap, 0.50f);
            });

            return combined;

        }
    }
}
