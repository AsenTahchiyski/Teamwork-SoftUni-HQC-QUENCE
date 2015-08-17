//Downloaded from Visual C# Kicks - http://www.vcskicks.com/
namespace Nikse.SubtitleEdit.Logic
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// This class provides a faster way to work with bitmaps compared to the standard.
    /// </summary>
    unsafe public class FastBitmap
    {
        private PixelData* pixelData = null;
        private readonly Bitmap workingBitmap;
        private int width;
        private BitmapData bitmapData;
        private Byte* pBase = null;

        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>
        /// Holds the pixel data (byte values for blue, green, red and alpha channel). Overrides ToString to provide pixel data values in order Alpha, Red, Green, Blue.
        /// </summary>
        private struct PixelData
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;

            public override string ToString()
            {
                return "(" + Alpha + ", " + Red + ", " + Green + ", " + Blue + ")";
            }
        }

        /// <summary>
        /// Creates image in FastBitmap format using a regular bitmap image.
        /// </summary>
        /// <param name="inputBitmap">Input bitmap image.</param>
        public FastBitmap(Bitmap inputBitmap)
        {
            workingBitmap = inputBitmap;

            if (workingBitmap.PixelFormat != PixelFormat.Format32bppArgb &&
                Environment.OSVersion.Version.Major < 6 && 
                Configuration.Settings.General.SubtitleFontName == Utilities.WinXP2KUnicodeFontName) // 6 == Vista/Win2008Server/Win7
            { // WinXp Fix
                var newBitmap = new Bitmap(workingBitmap.Width, workingBitmap.Height, PixelFormat.Format32bppArgb);

                for (int y = 0; y < workingBitmap.Height; y++)
                {
                    for (int x = 0; x < workingBitmap.Width; x++)
                    {
                        newBitmap.SetPixel(x, y, workingBitmap.GetPixel(x, y));
                    }
                }

                workingBitmap = newBitmap;
            }

            Width = inputBitmap.Width;
            Height = inputBitmap.Height;
        }

        /// <summary>
        /// Locks the bitmap image to avoid any outside changes to the file we're currently working on.
        /// </summary>
        public void LockImage()
        {
            var bounds = new Rectangle(Point.Empty, workingBitmap.Size);

            width = bounds.Width * sizeof(PixelData);
            if (width%4 != 0)
            {
                width = 4 * (width / 4 + 1);
            }

            //Lock Image
            bitmapData = workingBitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            pBase = (Byte*)bitmapData.Scan0.ToPointer();
        }

        /// <summary>
        /// Provides the specified pixel data.
        /// </summary>
        /// <param name="x">X coordinate of the pixel.</param>
        /// <param name="y">Y coordinate of the pixel.</param>
        /// <returns>Pixel data in ARGB format.</returns>
        public Color GetPixel(int x, int y)
        {
            pixelData = (PixelData*)(pBase + y * width + x * sizeof(PixelData));
            return Color.FromArgb(pixelData->Alpha, pixelData->Red, pixelData->Green, pixelData->Blue);
        }

        /// <summary>
        /// Provides pixel data for the next pixel.
        /// </summary>
        /// <returns>Pixel data in ARGB format.</returns>
        public Color GetPixelNext()
        {
            pixelData++;
            return Color.FromArgb(pixelData->Alpha, pixelData->Red, pixelData->Green, pixelData->Blue);
        }

        /// <summary>
        /// Sets the color of a specified pixel.
        /// </summary>
        /// <param name="x">X coordinate of the target pixel.</param>
        /// <param name="y">Y coordinate of the target pixel.</param>
        /// <param name="color">New pixel color to set.</param>
        public void SetPixel(int x, int y, Color color)
        {
            var data = (PixelData*)(pBase + y * width + x * sizeof(PixelData));
            data->Alpha = color.A;
            data->Red = color.R;
            data->Green = color.G;
            data->Blue = color.B;
        }

        /// <summary>
        /// Sets the color of a pixel sequence simultaneously.
        /// </summary>
        /// <param name="x">X coordinate of the first pixel.</param>
        /// <param name="y">Y coordinate of the first pixel.</param>
        /// <param name="color">New pixel color to set.</param>
        /// <param name="length">Number of pixels.</param>
        public void SetPixel(int x, int y, Color color, int length)
        {
            var data = (PixelData*)(pBase + y * width + x * sizeof(PixelData));
            for (int i = 0; i < length; i++)
            {
                data->Alpha = color.A;
                data->Red = color.R;
                data->Green = color.G;
                data->Blue = color.B;
                data++;
            }
        }

        /// <summary>
        /// Provides the image in Bitmap format.
        /// </summary>
        /// <returns>Image in System.Drawing.Bitmap format.</returns>
        public Bitmap GetBitmap()
        {
            return workingBitmap;
        }

        /// <summary>
        /// Unlocks the image after we're done using it.
        /// </summary>
        public void UnlockImage()
        {
            workingBitmap.UnlockBits(bitmapData);
            bitmapData = null;
            pBase = null;
        }
    }
}