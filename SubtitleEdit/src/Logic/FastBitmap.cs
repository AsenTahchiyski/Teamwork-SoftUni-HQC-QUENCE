//Downloaded from Visual C# Kicks - http://www.vcskicks.com/
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Nikse.SubtitleEdit.Logic
{
    /// <summary>
    /// This class provides a faster way to work with bitmaps compared to the standard.
    /// </summary>
    unsafe public class FastBitmap
    {
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

        public int Width { get; set; }
        public int Height { get; set; }

        private readonly Bitmap _workingBitmap;
        private int _width;
        private BitmapData _bitmapData;
        private Byte* _pBase = null;

        /// <summary>
        /// Creates image in FastBitmap format using a regular bitmap image.
        /// </summary>
        /// <param name="inputBitmap">Input bitmap image.</param>
        public FastBitmap(Bitmap inputBitmap)
        {
            _workingBitmap = inputBitmap;

            if (_workingBitmap.PixelFormat != PixelFormat.Format32bppArgb &&
                Environment.OSVersion.Version.Major < 6 && Configuration.Settings.General.SubtitleFontName == Utilities.WinXP2KUnicodeFontName) // 6 == Vista/Win2008Server/Win7
            { // WinXp Fix
                var newBitmap = new Bitmap(_workingBitmap.Width, _workingBitmap.Height, PixelFormat.Format32bppArgb);
                for (int y = 0; y < _workingBitmap.Height; y++)
                    for (int x = 0; x < _workingBitmap.Width; x++)
                        newBitmap.SetPixel(x, y, _workingBitmap.GetPixel(x, y));
                _workingBitmap = newBitmap;
            }

            Width = inputBitmap.Width;
            Height = inputBitmap.Height;
        }

        /// <summary>
        /// Locks the bitmap image to avoid any outside changes to the file we're currently working on.
        /// </summary>
        public void LockImage()
        {
            var bounds = new Rectangle(Point.Empty, _workingBitmap.Size);

            _width = bounds.Width * sizeof(PixelData);
            if (_width % 4 != 0) _width = 4 * (_width / 4 + 1);

            //Lock Image
            _bitmapData = _workingBitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            _pBase = (Byte*)_bitmapData.Scan0.ToPointer();
        }

        private PixelData* _pixelData = null;

        /// <summary>
        /// Provides the specified pixel data.
        /// </summary>
        /// <param name="x">X coordinate of the pixel.</param>
        /// <param name="y">Y coordinate of the pixel.</param>
        /// <returns>Pixel data in ARGB format.</returns>
        public Color GetPixel(int x, int y)
        {
            _pixelData = (PixelData*)(_pBase + y * _width + x * sizeof(PixelData));
            return Color.FromArgb(_pixelData->Alpha, _pixelData->Red, _pixelData->Green, _pixelData->Blue);
        }

        /// <summary>
        /// Provides pixel data for the next pixel.
        /// </summary>
        /// <returns>Pixel data in ARGB format.</returns>
        public Color GetPixelNext()
        {
            _pixelData++;
            return Color.FromArgb(_pixelData->Alpha, _pixelData->Red, _pixelData->Green, _pixelData->Blue);
        }

        /// <summary>
        /// Sets the color of a specified pixel.
        /// </summary>
        /// <param name="x">X coordinate of the target pixel.</param>
        /// <param name="y">Y coordinate of the target pixel.</param>
        /// <param name="color">New pixel color to set.</param>
        public void SetPixel(int x, int y, Color color)
        {
            var data = (PixelData*)(_pBase + y * _width + x * sizeof(PixelData));
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
            var data = (PixelData*)(_pBase + y * _width + x * sizeof(PixelData));
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
            return _workingBitmap;
        }

        /// <summary>
        /// Unlocks the image after we're done using it.
        /// </summary>
        public void UnlockImage()
        {
            _workingBitmap.UnlockBits(_bitmapData);
            _bitmapData = null;
            _pBase = null;
        }
    }
}