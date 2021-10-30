using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.Drawing;
using Color = System.Drawing.Color;
using ISColor = SixLabors.ImageSharp.PixelFormats.Bgra32;

namespace UVTextureReverser {
    /// <summary>
    /// TextureScan holds the in-progress data and state for a texture scan.
    /// </summary>
    public class ISTextureScan {

        // Maximum projection resolution is 12 bits for each coordinate, so 4096 bits.
        // Maximum texture size then is 12 (4096)
        // Scan resolution starts at the highest bit then, and counts down (11 to 0)
        // "Scan Depth" though, can be equal to the texture size (12)
        // if scan depth is less than the texture size, it means we stop scanning at a lower 

        // current scan depth is the index of the bit we are trying to determine

        // so "current scan depth" should always start at (texture size -1) and count down to (texture size - scan depth) (inclusive)
        // the generated texture mask can be (1 << current scan depth)
        // but the projection map mask should always start from 4096,
        // so it needs to be (1 << (12 - (scan depth - current scan depth)))

        // The projection mapping should always be at the highest resolution possible, even if the scan resolution was lower.

        public ISBitmap getBlackScan() {
            if (this.blackScan != null) {
                return this.blackScan.clone();
            } else {
                return null;
            }
        }

        public ISBitmap getWhiteScan() {
            if (this.whiteScan != null) {
                return this.whiteScan.clone();
            } else {
                return null;
            }
        }

        public ISBitmap getMap() {
            if (this.projectionMap != null) {
                return this.projectionMap.clone();
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a copy of this TextureScan with the state reset to the beginning
        /// </summary>
        /// <returns></returns>
        public ISTextureScan restart() {
            return new ISTextureScan(texturePath, textureSize, minScanDepth);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="texturePath"></param>
        /// <param name="textureSize">The size of the texture to generate (log 2)</param>
        /// <param name="scanDepth">the pixel depth of a scan to do (log 2). For maximum scan resolution, scanDepth == textureSize</param>
        public ISTextureScan(String texturePath, int textureSize, int scanDepth) {
            Debug.Assert(texturePath != null);

            this.texturePath = texturePath;
            this.textureSize = textureSize;
            this.minScanDepth = textureSize - scanDepth;
            this.currentScanDepth = textureSize - 1;
        }

        /// <summary>
        /// The location to write scan textures to.
        /// </summary>
        private readonly String texturePath;

        public String getTexturePath() {
            return this.texturePath;
        }

        /// <summary>
        /// The size (log_2) of the scan texture to write to.
        /// </summary>
        private readonly int textureSize;

        /// <summary>
        /// the resolution of the scan to perform.
        /// 
        /// Maximum texture size is 4096 (12). The scan should start at the highest bit of the texture resolution (1 << (textureSize-1))
        /// </summary>
        private readonly int minScanDepth;

        /// <summary>
        /// currentScanDepth starts at the largest value and counts down
        /// 
        /// </summary>
        private int currentScanDepth;

        private bool scanningVertical = false;

        /// <summary>
        /// A screenshot of the model when an all black texture was shown.
        /// </summary>
        private ISBitmap blackScan = null;

        /// <summary>
        /// A screenshot of the model when an all white texture was shown.
        /// </summary>
        private ISBitmap whiteScan = null;

        /// <summary>
        /// The current projection map.
        /// - An alpha channel of 0 indicates that a pixel is not mappable (non-zero is mappable)
        /// </summary>
        private ISBitmap projectionMap = null;

        /// <summary>
        /// Starts at 1. Black scan first, then white, then masks.
        /// </summary>
        /// <returns></returns>
        public int getCurrentStepNumber() {
            if (blackScan == null)
                return 1;

            if (whiteScan == null)
                return 2;

            if(this.scanningVertical) {
                return 2 + (textureSize-minScanDepth) + (textureSize-currentScanDepth);
            } else {
                return 2 + (textureSize - currentScanDepth);
            }
            

        }

        public int getTotalStepsCount() {
            return 2 + ((textureSize-minScanDepth) * 2);
        }

        public string getStepDescription() {
            String textureName;
            if (blackScan == null) {
                textureName = "All Black";
            } else if (whiteScan == null) {
                textureName = "All White";
            } else if (scanningVertical == false) {
                textureName = "Horizontal " + (1 << currentScanDepth);
            } else {
                textureName = "Vertical " + (1 << currentScanDepth);
            }

            return String.Format("{0} ({1} of {2})", textureName, getCurrentStepNumber(), getTotalStepsCount());
        }

        /// <summary>
        /// Returns the current texture needed to scan the next stage of the model, or null if the scan is complete.
        /// </summary>
        public ISBitmap getCurrentScanTexture() {
            if (blackScan == null) {
                return new ISBitmap(1 << textureSize, 1 << textureSize, Color.Black);
            } else if (whiteScan == null) {
                return new ISBitmap(1 << textureSize, 1 << textureSize, Color.White);
            } else {
                // the scan texture

                int textureMask = 1 << currentScanDepth;
                if (scanningVertical) {
                    return ISBitmap.mask(1 << textureSize, 1 << textureSize, 0, textureMask, Color.White, Color.Black);
                } else {
                    return ISBitmap.mask(1 << textureSize, 1 << textureSize, textureMask, 0, Color.White, Color.Black);
                }
            }
        }

        private void addProjectionUsingEquals(ISBitmap scan, Color additionMask) {
            // see which pixels exactly matched the white and black images
            ISBitmap matchedWhite = scan.equals(whiteScan);

            // mask out the pixels that didn't match either image
            ISBitmap matchedNeither = scan.equals(blackScan);
            matchedNeither.add(matchedWhite);
            matchedNeither.invert();


            // max out the color values on the pixels that matched none, so that future adds do nothing.
            // Sorry corner pixel.
            this.projectionMap.add(matchedNeither);


            // For all the ones that matched the white bars exactly, add that color to the projection map
            ISBitmap colorized = matchedWhite.clone();
            colorized.colorize(additionMask);
            this.projectionMap.add(colorized);
        }

        private void addProjectionUsingHardlight(ISBitmap scanImage, Color additionMask) {
            // Alternative, better solution for pixels that are sorta inbetween
            // 1) subtract scan from white, then invert to get whiter pixels where they match the white image
            // 2) subtract black from scan to get blacker pixels where it matches the black image
            // 3) draw (2) on top of (1) using "hard light" blend mode to get a black/white contrasted image where blacker pixels more closely matched the black scan, and whiter pixels more closely matched the white scan
            // 4) apply a binarization filter onto it to maximize the contrast
            // 5) colorize the image with the addition mask

            ISColor additionMaskIsColor = ISBitmap.c(additionMask);

            var blackerPixels = scanImage.getRawImage().Clone();
            blackerPixels.Mutate(x => {
                // (2)
                x.DrawImage(this.blackScan.getRawImage(), SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Subtract, 1.0f);
            });

            var whiterPixels = this.whiteScan.getRawImage().Clone();
            whiterPixels.Mutate(x => {
                // (1)
                x.DrawImage(scanImage.getRawImage(), SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Subtract, 1.0f);
                x.Invert();
                // (3)
                x.DrawImage(blackerPixels, SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.HardLight, 1.0f);
                // (4) + (5)
                x.BinaryThreshold(0.5f, additionMaskIsColor, ISBitmap.c(Color.Black));
            });

            this.projectionMap.add(new ISBitmap(whiterPixels));
        }


        private readonly float matchMargin = 0.50f;

        private void addProjectionUsingManual(ISBitmap scan, Color additionMask) {

            var distanceFromBlack = scan.getRawImage().Clone();
            distanceFromBlack.Mutate(x => {
                x.DrawImage(this.blackScan.getRawImage(), SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Subtract, 1.0f);
            });

            var distanceFromWhite = this.whiteScan.getRawImage().Clone();
            distanceFromWhite.Mutate(x => {
                x.DrawImage(scan.getRawImage(), SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Subtract, 1.0f);
            });

            var possibleSpan = this.whiteScan.getRawImage().Clone();
            possibleSpan.Mutate(x => {
                x.DrawImage(this.blackScan.getRawImage(), SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Subtract, 1.0f);
            });

            var unmappable = ISBitmap.c(Color.White);

            for(int x=0; x < this.projectionMap.width; x++) {
                for(int y=0; y<this.projectionMap.height; y++) {
                    // should all be greyscale, so color doesn't matter
                    int blackDist = distanceFromBlack[x, y].R;
                    int whiteDist = distanceFromWhite[x, y].R;
                    int span = possibleSpan[x, y].R;

                    if(span < 16) {
                        // difference betwen black and white pixels is too small
                        this.projectionMap.getRawImage()[x, y] = unmappable;
                        continue;
                    }

                    float distFromBlack = ((float)blackDist) / ((float)span);
                    float distFromWhite = ((float)whiteDist) / ((float)span);

                    // check to see if the match is too close to call;
                    if(Math.Abs(distFromBlack - distFromWhite) <= matchMargin) {
                        // pixels are too close to call, this one is unmappable.
                        this.projectionMap.getRawImage()[x, y] = unmappable;
                        continue;

                    } else if(whiteDist < blackDist) {
                        // if the pixel is closer to white than black, add it to the mask

                        ISColor projPixel = this.projectionMap.getRawImage()[x, y];
                        projPixel.R = (byte)Math.Min(255, projPixel.R + additionMask.R);
                        projPixel.G = (byte)Math.Min(255, projPixel.G + additionMask.G);
                        projPixel.B = (byte)Math.Min(255, projPixel.B + additionMask.B);
                        this.projectionMap.getRawImage()[x, y] = projPixel;
                    } 

                }
            }
        }

        // X is red + low 4 bits of blue
        // Y is green + high 4 bits of blue
        public static int getX(Color pixel)
        {
            return ((pixel.G & 0xFF) << 4) | (pixel.R & 0xF);
        }

        public static Color setX(Color pixel, int x)
        {
            return Color.FromArgb(pixel.A,
                (pixel.R & 0xF0) | (x & 0xF),
                (x >> 4) & 0xFF,
                pixel.B);
        }

        public static int getY(Color pixel)
        {
            return ((pixel.B & 0xFF) << 4) | ((pixel.R >> 4) & 0xF);
        }

        public static Color setY(Color pixel, int y)
        {
            return Color.FromArgb(pixel.A,
                (pixel.R & 0xF) | ((y & 0xF) << 4),
                pixel.G,
                (y >> 4) & 0xFF);
        }


        private Color getCurrentMaskAdditionColor() {
            // the coordinate masks are always 0-4095, regardless of the texture size or scan depth
            // the current scan depth goes form 0 to "scanDepth", so the mask should be 1 << (currentScanDepth + (11 - scanDepth)
            //
            // probably

            int additionMask = 1 << (12 - (textureSize - currentScanDepth));

            if (scanningVertical) {
                return setY(Color.Black, additionMask);
            } else {
                return setX(Color.Black, additionMask);
            }
        }

        public bool done() {
            return blackScan != null &&
                whiteScan != null &&
                scanningVertical &&
                currentScanDepth < minScanDepth;
        }

        /// <summary>
        /// Writes the next scan texture needed to the disk so that it may be applied to the model.
        /// </summary>
        public void updateScanTexture() {
            ISBitmap scanTexture = getCurrentScanTexture();

            if (scanTexture != null) {
                scanTexture.toFile(this.texturePath);
            }
        }

        private readonly bool testImageTransforms = false;



        /// <summary>
        /// Updates the current scan mapping with a screenshot of the model wearing the current scan texture
        /// </summary>
        /// <param name="scanImage"></param>
        public void addScan(ISBitmap scanImage) {
            Debug.Assert(this.blackScan == null || (
                this.blackScan.width == scanImage.width && this.blackScan.height == scanImage.height));

            // convert all scans to greyscale, for simplicity
            var scanBw = scanImage.getRawImage().Clone();
            scanBw.Mutate(x => {
                x.Grayscale();
            });
            scanImage = new ISBitmap(scanBw);

            if (blackScan == null) {
                blackScan = scanImage;
            } else if (whiteScan == null) {
                whiteScan = scanImage;

                // start the projection map out as an image with white pixels where the black and white scans matched, because these areas will be unscannable
                projectionMap = blackScan.equals(whiteScan);

            } else if (currentScanDepth >= minScanDepth) {
                Color additionMask = this.getCurrentMaskAdditionColor();

                this.addProjectionUsingManual(scanImage, additionMask);

                if (!scanningVertical) {

                    if (currentScanDepth == minScanDepth) {
                        scanningVertical = true;
                        currentScanDepth = textureSize;
                    }
                }

                currentScanDepth--;
            }
        }


        /// <summary>
        /// Returns the current generated projection map.
        /// 
        /// In the returned projection, an alpha value of 255 indicates a projection pixel. An alpha value of 254 is a normal display pixel.
        /// </summary>
        /// <returns></returns>
        public ISBitmap getProjectionMap() {
            if (blackScan == null) {
                return null;
            }

            // use the black scan image as the base and copy over any pixels from the current projection map.
            // Pixels in the projection map that aren't mappable will have an alpha value of 0, while mappable
            // pixels should be 255, so we should be able to make the final projection using a simple overlay.
            
            /* this didn't work
             var map = this.blackScan.getRawImage().Clone();

             if (this.projectionMap == null) {
                 // no projection has occurred yet, so just alphaise it;
                 map.Mutate(x => { x.Opacity(254.0f / 255.0f); });
                 return new ISBitmap(map);
             }
             */

            var map = (new ISBitmap(this.blackScan.width, this.blackScan.height, Color.Transparent)).getRawImage();

            /*
            map.Mutate(x => {
                x.Opacity(254.0f / 255.0f)
                .DrawImage(this.projectionMap.getRawImage(),
                SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Normal,
                1.0f);
            });
            */

            var pmap = this.projectionMap.getRawImage();
            // copy pixels from projection except where mapping is totally white
            for(int x=0; x<map.Width; x++) {
                for(int y=0; y<map.Height; y++) {
                    var p = pmap[x, y];
                    
                    // at some point, I may use the alpha channel of the projection to determine validity, instead of it just being totally white
                    if (p.A == 255 && !(p.R == 255 && p.B == 255 && p.G == 255)) {
                        // copy the projection map pixel over top the map
                        map[x, y] = p;
                    }
                }
            }

            return new ISBitmap(map);
        }
    }
}
