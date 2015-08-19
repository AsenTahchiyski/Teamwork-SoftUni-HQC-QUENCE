namespace Nikse.SubtitleEdit.Logic.VobSub
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// Subtitle Picture - see http://www.mpucoder.com/DVD/spu.html for more info
    /// </summary>
    public class SubPicture
    {
        public readonly int SubPictureDateSize;

        public TimeSpan Delay;

        public Rectangle ImageDisplayArea;

        private readonly byte[] data;

        private readonly int pixelDataAddressOffset;

        private readonly int startDisplayControlSequenceTableAddress;

        public SubPicture(byte[] data)
        {
            this.data = data;
            this.SubPictureDateSize = Helper.GetEndianWord(this.data, 0);
            this.startDisplayControlSequenceTableAddress = Helper.GetEndianWord(this.data, 2);
            this.ParseDisplayControlCommands(false, null, null, false);
        }

        /// <summary>
        /// For SP packet with DVD subpictures
        /// </summary>
        /// <param name="data">Byte data buffer</param>
        /// <param name="startDisplayControlSequenceTableAddress">Address of first control sequence in data</param>
        /// <param name="pixelDataAddressOffset">Bitmap pixel data address offset</param>
        public SubPicture(byte[] data, int startDisplayControlSequenceTableAddress, int pixelDataAddressOffset)
        {
            this.data = data;
            this.SubPictureDateSize = this.data.Length;
            this.startDisplayControlSequenceTableAddress = startDisplayControlSequenceTableAddress;
            this.pixelDataAddressOffset = pixelDataAddressOffset;
            this.ParseDisplayControlCommands(false, null, null, false);
        }

        public int BufferSize
        {
            get
            {
                return this.data.Length;
            }
        }

        public bool Forced { get; private set; }

        /// <summary>
        /// Generates the current subtitle image
        /// </summary>
        /// <param name="colorLookupTable">The Color LookUp Table (CLUT), if null then only the four colors are used (should contain 16 elements if not null)</param>
        /// <param name="background">Background color</param>
        /// <param name="pattern">Color</param>
        /// <param name="emphasis1">Color</param>
        /// <param name="emphasis2">Color</param>
        /// <param name="useCustomColors">Use custom colors instead of lookup table</param>
        /// <returns>Subtitle image</returns>
        public Bitmap GetBitmap(List<Color> colorLookupTable, Color background, Color pattern, Color emphasis1, Color emphasis2, bool useCustomColors)
        {
            var fourColors = new List<Color> { background, pattern, emphasis1, emphasis2 };
            return this.ParseDisplayControlCommands(true, colorLookupTable, fourColors, useCustomColors);
        }

        private Bitmap ParseDisplayControlCommands(bool createBitmap, List<Color> colorLookUpTable, List<Color> fourColors, bool useCustomColors)
        {
            this.ImageDisplayArea = new Rectangle();
            Bitmap bmp = null;
            var displayControlSequenceTableAddresses = new List<int>();
            int imageTopFieldDataAddress = 0;
            int imageBottomFieldDataAddress = 0;
            bool bitmapGenerated = false;
            double largestDelay = -999999;
            int displayControlSequenceTableAddress = this.startDisplayControlSequenceTableAddress - this.pixelDataAddressOffset;
            int lastDisplayControlSequenceTableAddress = 0;
            displayControlSequenceTableAddresses.Add(displayControlSequenceTableAddress);
            int commandIndex = 0;
            while (displayControlSequenceTableAddress > lastDisplayControlSequenceTableAddress && 
                displayControlSequenceTableAddress + 1 < this.data.Length && commandIndex < this.data.Length)
            {
                int delayBeforeExecute = Helper.GetEndianWord(this.data, displayControlSequenceTableAddress + this.pixelDataAddressOffset);
                commandIndex = displayControlSequenceTableAddress + 4 + this.pixelDataAddressOffset;
                if (commandIndex >= this.data.Length)
                {
                    break; // invalid index
                }

                int command = this.data[commandIndex];
                int numberOfCommands = 0;
                while (command != (int)DisplayControlCommand.End && numberOfCommands < 1000 && commandIndex < this.data.Length)
                {
                    numberOfCommands++;
                    switch (command)
                    {
                        case (int)DisplayControlCommand.ForcedStartDisplay: // 0
                            this.Forced = true;
                            commandIndex++;
                            break;
                        case (int)DisplayControlCommand.StartDisplay: // 1
                            commandIndex++;
                            break;
                        case (int)DisplayControlCommand.StopDisplay: // 2
                            this.Delay = TimeSpan.FromMilliseconds(((delayBeforeExecute << 10) + 1023) / 90.0);
                            if (createBitmap && this.Delay.TotalMilliseconds > largestDelay)
                            {
                                // in case of more than one images, just use the one with the largest display time
                                largestDelay = this.Delay.TotalMilliseconds;
                                if (bmp != null)
                                {
                                    bmp.Dispose();
                                }

                                bmp = this.GenerateBitmap(this.ImageDisplayArea, imageTopFieldDataAddress, imageBottomFieldDataAddress, fourColors);
                                bitmapGenerated = true;
                            }

                            commandIndex++;
                            break;
                        case (int)DisplayControlCommand.SetColor: // 3
                            if (colorLookUpTable != null && fourColors.Count == 4)
                            {
                                byte[] imageColor = { this.data[commandIndex + 1], this.data[commandIndex + 2] };
                                if (!useCustomColors)
                                {
                                    SetColor(fourColors, 3, imageColor[0] >> 4, colorLookUpTable);
                                    SetColor(fourColors, 2, imageColor[0] & Helper.B00001111, colorLookUpTable);
                                    SetColor(fourColors, 1, imageColor[1] >> 4, colorLookUpTable);
                                    SetColor(fourColors, 0, imageColor[1] & Helper.B00001111, colorLookUpTable);
                                }
                            }

                            commandIndex += 3;
                            break;
                        case (int)DisplayControlCommand.SetContrast: // 4
                            if (colorLookUpTable != null && fourColors.Count == 4)
                            {
                                var imageContrast = new[] { this.data[commandIndex + 1], this.data[commandIndex + 2] };
                                if (imageContrast[0] + imageContrast[1] > 0)
                                {
                                    SetTransparency(fourColors, 3, (imageContrast[0] & 0xF0) >> 4);
                                    SetTransparency(fourColors, 2, imageContrast[0] & Helper.B00001111);
                                    SetTransparency(fourColors, 1, (imageContrast[1] & 0xF0) >> 4);
                                    SetTransparency(fourColors, 0, imageContrast[1] & Helper.B00001111);
                                }
                            }

                            commandIndex += 3;
                            break;
                        case (int)DisplayControlCommand.SetDisplayArea: // 5
                            if (this.data.Length > commandIndex + 6)
                            {
                                int startingX = (this.data[commandIndex + 1] << 8 | this.data[commandIndex + 2]) >> 4;
                                int endingX = (this.data[commandIndex + 2] & Helper.B00001111) << 8 | this.data[commandIndex + 3];
                                int startingY = (this.data[commandIndex + 4] << 8 | this.data[commandIndex + 5]) >> 4;
                                int endingY = (this.data[commandIndex + 5] & Helper.B00001111) << 8 | this.data[commandIndex + 6];
                                this.ImageDisplayArea = new Rectangle(startingX, startingY, endingX - startingX, endingY - startingY);
                            }

                            commandIndex += 7;
                            break;
                        case (int)DisplayControlCommand.SetPixelDataAddress: // 6
                            imageTopFieldDataAddress = Helper.GetEndianWord(this.data, commandIndex + 1) + this.pixelDataAddressOffset;
                            imageBottomFieldDataAddress = Helper.GetEndianWord(this.data, commandIndex + 3) + this.pixelDataAddressOffset;
                            commandIndex += 5;
                            break;
                        case (int)DisplayControlCommand.ChangeColorAndContrast: // 7
                            commandIndex++;

                            // int parameterAreaSize = (int)Helper.GetEndian(_data, commandIndex, 2);
                            if (commandIndex + 1 < this.data.Length)
                            {
                                int parameterAreaSize = this.data[commandIndex + 1]; // this should be enough??? (no larger than 255 bytes)
                                if (colorLookUpTable != null)
                                {
                                    // TODO: Set fourColors
                                }

                                commandIndex += parameterAreaSize;
                            }
                            else
                            {
                                commandIndex++;
                            }

                            break;
                        default:
                            commandIndex++;
                            break;
                    }

                    if (commandIndex >= this.data.Length)
                    {
                        // in case of bad files...
                        break;
                    }

                    command = this.data[commandIndex];
                }

                lastDisplayControlSequenceTableAddress = displayControlSequenceTableAddress;
                displayControlSequenceTableAddress = this.pixelDataAddressOffset == -4 ? Helper.GetEndianWord(this.data, commandIndex + 3) : Helper.GetEndianWord(this.data, displayControlSequenceTableAddress + 2);
            }

            if (createBitmap && !bitmapGenerated)
            {
                // StopDisplay not needed (delay will be zero - should be just before start of next subtitle)
                bmp = this.GenerateBitmap(this.ImageDisplayArea, imageTopFieldDataAddress, imageBottomFieldDataAddress, fourColors);
            }

            return bmp;
        }

        private static void SetColor(List<Color> fourColors, int fourColorIndex, int clutIndex, List<Color> colorLookUpTable)
        {
            if (clutIndex >= 0 && clutIndex < colorLookUpTable.Count && fourColorIndex >= 0)
            {
                fourColors[fourColorIndex] = colorLookUpTable[clutIndex];
            }
        }

        private static void SetTransparency(List<Color> fourColors, int fourColorIndex, int alpha)
        {
            // alpha: 0x0 = transparent, 0xF = opaque (in C# 0 is fully transparent, and 255 is fully opaque so we have to multiply by 17)
            if (fourColorIndex >= 0)
            {
                fourColors[fourColorIndex] = Color.FromArgb(alpha * 17, fourColors[fourColorIndex].R, fourColors[fourColorIndex].G, fourColors[fourColorIndex].B);
            }
        }

        private Bitmap GenerateBitmap(Rectangle imageDisplayArea, int imageTopFieldDataAddress, int imageBottomFieldDataAddress, List<Color> fourColors)
        {
            if (imageDisplayArea.Width <= 0 || imageDisplayArea.Height <= 0)
            {
                return new Bitmap(1, 1);
            }

            var bmp = new Bitmap(imageDisplayArea.Width + 1, imageDisplayArea.Height + 1);
            if (fourColors[0] != Color.Transparent)
            {
                var gr = Graphics.FromImage(bmp);
                gr.FillRectangle(new SolidBrush(fourColors[0]), new Rectangle(0, 0, bmp.Width, bmp.Height));
                gr.Dispose();
            }

            var fastBmp = new FastBitmap(bmp);
            fastBmp.LockImage();
            GenerateBitmap(this.data, fastBmp, 0, imageTopFieldDataAddress, fourColors, 2);
            GenerateBitmap(this.data, fastBmp, 1, imageBottomFieldDataAddress, fourColors, 2);
            Bitmap cropped = CropBitmapAndUnlok(fastBmp, fourColors[0]);
            bmp.Dispose();
            return cropped;
        }

        private static Bitmap CropBitmapAndUnlok(FastBitmap bmp, Color backgroundColor)
        {
            int y = 0;
            int x;
            Color c = backgroundColor;
            int backgroundArgb = backgroundColor.ToArgb();

            // Crop top
            while (y < bmp.Height && IsBackgroundColor(c, backgroundArgb))
            {
                c = bmp.GetPixel(0, y);
                if (IsBackgroundColor(c, backgroundArgb))
                {
                    for (x = 1; x < bmp.Width; x++)
                    {
                        c = bmp.GetPixelNext();
                        if (c.A != 0 && c.ToArgb() != backgroundArgb)
                        {
                            break;
                        }
                    }
                }

                if (IsBackgroundColor(c, backgroundArgb))
                {
                    y++;
                }
            }

            int minY = y;
            if (minY > 3)
            {
                minY -= 3;
            }
            else
            {
                minY = 0;
            }

            // Crop left
            x = 0;
            c = backgroundColor;
            while (x < bmp.Width && IsBackgroundColor(c, backgroundArgb))
            {
                for (y = minY; y < bmp.Height; y++)
                {
                    c = bmp.GetPixel(x, y);
                    if (!IsBackgroundColor(c, backgroundArgb))
                    {
                        break;
                    }
                }

                if (IsBackgroundColor(c, backgroundArgb))
                {
                    x++;
                }
            }

            int minX = x;
            if (minX > 3)
            {
                minX -= 3;
            }
            else
            {
                minX -= 0;
            }

            // Crop bottom
            y = bmp.Height - 1;
            c = backgroundColor;
            while (y > minY && IsBackgroundColor(c, backgroundArgb))
            {
                c = bmp.GetPixel(0, y);
                if (IsBackgroundColor(c, backgroundArgb))
                {
                    for (x = 1; x < bmp.Width; x++)
                    {
                        c = bmp.GetPixelNext();
                        if (!IsBackgroundColor(c, backgroundArgb))
                        {
                            break;
                        }
                    }
                }

                if (IsBackgroundColor(c, backgroundArgb))
                {
                    y--;
                }
            }

            int maxY = y + 7;
            if (maxY >= bmp.Height)
            {
                maxY = bmp.Height - 1;
            }

            // Crop right
            x = bmp.Width - 1;
            c = backgroundColor;
            while (x > minX && IsBackgroundColor(c, backgroundArgb))
            {
                for (y = minY; y < bmp.Height; y++)
                {
                    c = bmp.GetPixel(x, y);
                    if (!IsBackgroundColor(c, backgroundArgb))
                    {
                        break;
                    }
                }

                if (IsBackgroundColor(c, backgroundArgb))
                {
                    x--;
                }
            }

            int maxX = x + 7;
            if (maxX >= bmp.Width)
            {
                maxX = bmp.Width - 1;
            }

            bmp.UnlockImage();
            Bitmap bmpImage = bmp.GetBitmap();
            if (bmpImage.Width > 1 && bmpImage.Height > 1 && maxX - minX > 0 && maxY - minY > 0)
            {
                Bitmap bmpCrop = bmpImage.Clone(new Rectangle(minX, minY, maxX - minX, maxY - minY), bmpImage.PixelFormat);
                return bmpCrop;
            }

            return (Bitmap)bmpImage.Clone();
        }

        private static bool IsBackgroundColor(Color c, int backgroundArgb)
        {
            return c.A == 0 || c.ToArgb() == backgroundArgb;
        }

        public static void GenerateBitmap(byte[] data, FastBitmap bmp, int startY, int dataAddress, List<Color> fourColors, int addY)
        {
            int index = 0;
            bool onlyHalf = false;
            int y = startY;
            int x = 0;
            int colorZeroValue = fourColors[0].ToArgb();
            while (y < bmp.Height && dataAddress + index + 2 < data.Length)
            {
                int runLength;
                int color;
                bool restOfLine;
                index += DecodeRle(dataAddress + index, data, out color, out runLength, ref onlyHalf, out restOfLine);
                if (restOfLine)
                {
                    runLength = bmp.Width - x;
                }

                Color c = fourColors[color]; // set color via the four colors
                for (int i = 0; i < runLength; i++, x++)
                {
                    if (x >= bmp.Width - 1)
                    {
                        if (y < bmp.Height && x < bmp.Width && c != fourColors[0])
                        {
                            bmp.SetPixel(x, y, c);
                        }

                        if (onlyHalf)
                        {
                            onlyHalf = false;
                            index++;
                        }

                        x = 0;
                        y += addY;
                        break;
                    }

                    if (y < bmp.Height && c.ToArgb() != colorZeroValue)
                    {
                        bmp.SetPixel(x, y, c);
                    }
                }
            }
        }

        private static int DecodeRle(int index, byte[] data, out int color, out int runLength, ref bool onlyHalf, out bool restOfLine)
        {
            // Value      Bits   n=length, c=color
            // 1-3        4      nncc               (half a byte)
            // 4-15       8      00nnnncc           (one byte)
            // 16-63     12      0000nnnnnncc       (one and a half byte)
            // 64-255    16      000000nnnnnnnncc   (two bytes)
            // When reaching EndOfLine, index is byte aligned (skip 4 bits if necessary)
            restOfLine = false;
            byte b1 = data[index];
            byte b2 = data[index + 1];

            if (onlyHalf)
            {
                byte b3 = data[index + 2];
                b1 = (byte)(((b1 & Helper.B00001111) << 4) | ((b2 & Helper.B11110000) >> 4));
                b2 = (byte)(((b2 & Helper.B00001111) << 4) | ((b3 & Helper.B11110000) >> 4));
            }

            if (b1 >> 2 == 0)
            {
                runLength = (b1 << 6) | (b2 >> 2);
                color = b2 & Helper.B00000011;
                if (runLength != 0)
                {
                    return 2;
                }

                // rest of line + skip 4 bits if Only half
                restOfLine = true;
                if (!onlyHalf)
                {
                    return 2;
                }

                onlyHalf = false;
                return 3;
            }

            if (b1 >> 4 == 0)
            {
                runLength = (b1 << 2) | (b2 >> 6);
                color = (b2 & Helper.B00110000) >> 4;
                if (onlyHalf)
                {
                    onlyHalf = false;
                    return 2;
                }

                onlyHalf = true;
                return 1;
            }

            if (b1 >> 6 == 0)
            {
                runLength = b1 >> 2;
                color = b1 & Helper.B00000011;
                return 1;
            }

            runLength = b1 >> 6;
            color = (b1 & Helper.B00110000) >> 4;

            if (onlyHalf)
            {
                onlyHalf = false;
                return 1;
            }

            onlyHalf = true;
            return 0;
        }

        private enum DisplayControlCommand
        {
            ForcedStartDisplay = 0, 

            StartDisplay = 1, 

            StopDisplay = 2, 

            SetColor = 3, 

            SetContrast = 4, 

            SetDisplayArea = 5, 

            SetPixelDataAddress = 6, 

            ChangeColorAndContrast = 7, 

            End = 0xFF, 
        }
    }
}