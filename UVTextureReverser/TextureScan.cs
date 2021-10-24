using System;
using System.Diagnostics;
using System.Drawing;

namespace UVTextureReverser {
    /// <summary>
    /// TextureScan holds the in-progress data and state for a texture scan.
    /// </summary>
    public class TextureScan {

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

        public ZBitmap getBlackScan() {
            if (this.blackScan != null) {
                return this.blackScan.clone();
            } else {
                return null;
            }
        }

        public ZBitmap getWhiteScan() {
            if (this.whiteScan != null) {
                return this.whiteScan.clone();
            } else {
                return null;
            }
        }

        public ZBitmap getMap() {
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
        public TextureScan restart() {
            return new TextureScan(texturePath, textureSize, minScanDepth);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="texturePath"></param>
        /// <param name="textureSize">The size of the texture to generate (log 2)</param>
        /// <param name="scanDepth">the pixel depth of a scan to do (log 2). For maximum scan resolution, scanDepth == textureSize</param>
        public TextureScan(String texturePath, int textureSize, int scanDepth) {
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
        private ZBitmap blackScan = null;

        /// <summary>
        /// A screenshot of the model when an all white texture was shown.
        /// </summary>
        private ZBitmap whiteScan = null;

        /// <summary>
        /// The current projection map.
        /// - An alpha channel of 0 indicates that a pixel is not mappable (non-zero is mappable)
        /// </summary>
        private ZBitmap projectionMap = null;

        /// <summary>
        /// Starts at 1. Black scan first, then white, then masks.
        /// </summary>
        /// <returns></returns>
        public int getCurrentStepNumber() {
            if (blackScan == null)
                return 1;

            if (whiteScan == null)
                return 2;

            return 3;

        }

        public int getTotalStepsCount() {
            // black scan, white scan, then scanDepth+1 for horizontal and vertical
            return 999;
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
        public ZBitmap getCurrentScanTexture() {
            if (blackScan == null) {
                return ZBitmap.black(1 << textureSize);
            } else if (whiteScan == null) {
                return ZBitmap.white(1 << textureSize);
            } else {
                // the scan texture

                int textureMask = 1 << currentScanDepth;
                if (scanningVertical) {
                    return ZBitmap.vMask(1 << textureSize, textureMask);
                } else {
                    return ZBitmap.hMask(1 << textureSize, textureMask);
                }
            }
        }

        private Color getCurrentMaskAdditionColor() {
            // the coordinate masks are always 0-4095, regardless of the texture size or scan depth
            // the current scan depth goes form 0 to "scanDepth", so the mask should be 1 << (currentScanDepth + (11 - scanDepth)
            //
            // probably

            int additionMask = 1 << (12 - (textureSize - currentScanDepth));

            if (scanningVertical) {
                return ZBitmap.setY(Color.Black, additionMask);
            } else {
                return ZBitmap.setX(Color.Black, additionMask);
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
            ZBitmap scanTexture = getCurrentScanTexture();

            if (scanTexture != null) {
                scanTexture.toFile(this.texturePath);
            }
        }

        private readonly bool testImageTransforms = false;

        /// <summary>
        /// Updates the current scan mapping with a screenshot of the model wearing the current scan texture
        /// </summary>
        /// <param name="scanImage"></param>
        public void addScan(ZBitmap scanImage) {
            Debug.Assert(this.blackScan == null || (
                this.blackScan.width == scanImage.width && this.blackScan.height == scanImage.height));

            if (blackScan == null) {
                blackScan = scanImage;
            } else if (whiteScan == null) {
                whiteScan = scanImage;

                // use a black image for the projection map so that the coordinate values are all zeroed out.
                projectionMap = ZBitmap.fill(blackScan.width, blackScan.height, System.Drawing.Color.Black);
            } else if (currentScanDepth >= minScanDepth) {


                if (!scanningVertical) {
                    updateProjectionMap(this.projectionMap, scanImage, false);

                    if (currentScanDepth == minScanDepth) {
                        scanningVertical = true;
                        currentScanDepth = textureSize;
                    }

                } else {

                    // Update the projection Y coordinate
                    updateProjectionMap(this.projectionMap, scanImage, true);

                }

                currentScanDepth--;
            }
        }

        /// <summary>
        /// Updates the projection map using ZBitmap image transform operations, rather than a per-pixel operation
        /// </summary>
        /// <param name="scanImage"></param>
        /// <param name="mask"></param>
        /// <param name="vertical"></param>
        private ZBitmap updateProjectionMapTransforms(ZBitmap projMap, ZBitmap scanImage, int mask, bool vertical) {
            // compare against the white mask to see which sections of the image are black
            ZBitmap whiteSections = scanImage.subtract(this.blackScan).multiply(255);
            // sections of the image that matched the white stripes will be 255, white stripes will be 0

            // sections of the image that matched the white stripes should have this color added to it
            Color maskAdditionColor = vertical ? ZBitmap.setY(Color.Black, mask) : ZBitmap.setX(Color.Black, mask);

            // create an image where the white stripes have the additioncolor
            ZBitmap additionMask = whiteSections.min(ZBitmap.fill(scanImage.width, scanImage.height, maskAdditionColor));

            // add mask to projection map
            return projMap.add(additionMask);
        }

        /// <summary>
        /// Modifies the given projection map with the new scan
        /// </summary>
        /// <param name="projMap"></param>
        /// <param name="scanImage"></param>
        /// <param name="mask"></param>
        /// <param name="vertical"></param>
        private void updateProjectionMap(ZBitmap projMap, ZBitmap scanImage, bool vertical) {
            Debug.Assert(projectionMap != null);

            int xmin = 0;
            int xmax = ((int)projMap.width - 1);
            int ymin = 0;
            int ymax = ((int)projMap.height - 1);

            Color additionMask = getCurrentMaskAdditionColor();

            for (int x = xmin; x <= xmax; x++) {
                for (int y = ymin; y <= ymax; y++) {
                    Color projPixel = projMap.getPixel(x, y);
                    Color scanPixel = scanImage.getPixel(x, y);
                    Color blackPixel = blackScan.getPixel(x, y);
                    Color whitePixel = whiteScan.getPixel(x, y);

                    // map pixels with an alpha less than 255 are non-mappable
                    if (projPixel.A == 255) {
                        if (scanPixel.Equals(blackPixel) == scanPixel.Equals(whitePixel)) {
                            // if it matches both black and white, or doesn't match either, it can't be mapped. It should only match one of them.
                            projPixel = Color.Transparent;
                        } else {
                            if (scanPixel.Equals(whitePixel)) {

                                projPixel = Color.FromArgb(
                                    projPixel.R + additionMask.R,
                                    projPixel.G + additionMask.G,
                                    projPixel.B + additionMask.B);
                            }
                        }
                        // update the pixel value
                        projMap.setPixel(x, y, projPixel);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current generated projection map.
        /// 
        /// In the returned projection, an alpha value of 255 indicates a normal pixel. Any other alpha value indicates a projection coordinate pixel.
        /// </summary>
        /// <returns></returns>
        public ZBitmap getProjectionMap(bool crop = false) {
            if (blackScan == null) {
                return null;
            }

            // use the black scan image as the base and copy over any pixels from the current projection map that have a non-zero alpha value
            ZBitmap map = this.blackScan.clone();

            int xmin = -1;
            int xmax = -1;
            int ymin = -1;
            int ymax = -1;

            if (this.projectionMap != null) {
                for (int x = 0; x < projectionMap.width; x++) {
                    for (int y = 0; y < projectionMap.height; y++) {
                        Color projectionPixel = this.projectionMap.getPixel(x, y);
                        if (projectionPixel != Color.Transparent) {
                            map.setPixel(x, y, Color.FromArgb(254, projectionPixel));

                            if (xmin == -1 || xmin > x) {
                                xmin = x;
                            }
                            if (xmax == -1 || xmax < x) {
                                xmax = x;
                            }
                            if (ymin == -1 || ymin > y) {
                                ymin = x;
                            }
                            if (ymax == -1 || ymax < y) {
                                ymax = y;
                            }


                        }
                    }
                }
            }

            if (crop) {
                return map.crop(new Rectangle(xmin, ymin, (xmax - xmin), (ymax - ymin)));
            }

            return map;
        }

    }
}
