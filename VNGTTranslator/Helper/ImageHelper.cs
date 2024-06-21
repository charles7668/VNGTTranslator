using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Win32ApiLibrary;

namespace VNGTTranslator.Helper
{
    public static class ImageHelper
    {
        private static Bitmap Binarize(Bitmap input, byte threshold)
        {
            int width = input.Width;
            int height = input.Height;
            BitmapData data = input.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p = (byte*)data.Scan0;
                int offset = data.Stride - (width * 4);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte gray = (byte)(((p[2] * 19595) + (p[1] * 38469) + (p[0] * 7472)) >> 16);
                        byte colorValue = gray >= threshold ? (byte)255 : (byte)0;
                        p[0] = p[1] = p[2] = colorValue;
                        p += 4;
                    }

                    p += offset;
                }
            }

            input.UnlockBits(data);
            return input;
        }

        /// <summary>
        /// capture window screenshot
        /// </summary>
        /// <returns></returns>
        public static Bitmap? CaptureFullWindow()
        {
            if (Screen.PrimaryScreen == null) return null;
            int w = Screen.PrimaryScreen.Bounds.Width;
            int h = Screen.PrimaryScreen.Bounds.Height;

            var bitmap = new Bitmap(w, h);
            var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(w, h));
            graphics.Dispose();

            return bitmap;
        }

        public static Bitmap CaptureWindowByHandle(IntPtr hwnd)
        {
            return ScreenHelper.GetWindowCapture(hwnd);
        }

        public static Bitmap? GetWindowRectCapture(IntPtr handle, Rectangle rec, bool isAllWin)
        {
            if (rec.Width == 0 || rec.Height == 0)
                return null;

            using Bitmap? img = isAllWin ? CaptureFullWindow() : ScreenHelper.GetWindowCapture(handle);
            return img?.Clone(rec, img.PixelFormat);
        }

        public static BitmapImage? ImageToBitmapImage(Image? bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);

            stream.Position = 0;
            var result = new BitmapImage();
            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.StreamSource = stream;
            result.EndInit();
            result.Freeze();
            return result;
        }


        /// <summary>
        /// OTSU algorithm for find binarization threshold
        /// </summary>
        /// <returns></returns>
        public static Bitmap OTSUAlgorithm(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            byte[] pixelCountsInGrayValue = new byte[256];

            BitmapData imageData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* p = (byte*)imageData.Scan0;
                int offset = imageData.Stride - (width * 4);
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        pixelCountsInGrayValue[p[0]]++;
                        p += 4;
                    }

                    p += offset;
                }

                image.UnlockBits(imageData);
            }

            double totalPixelCount = 0;
            double weightedPixelSum = 0;
            for (int i = 0; i < 256; i++)
            {
                weightedPixelSum += i * pixelCountsInGrayValue[i];
                totalPixelCount += pixelCountsInGrayValue[i];
            }

            double maxVariance = -1.0;
            double countForegroundPixels = 0;
            double sumForeground = 0;
            byte threshold = 0;
            for (int i = 0; i < 256; i++)
            {
                countForegroundPixels += pixelCountsInGrayValue[i];
                double countBackgroundPixels = totalPixelCount - countForegroundPixels;
                if (countBackgroundPixels == 0)
                {
                    break;
                }

                sumForeground += i * pixelCountsInGrayValue[i];
                double sumBackground = weightedPixelSum - sumForeground;
                double meanForeground = sumForeground / countForegroundPixels;
                double meanBackground = sumBackground / countBackgroundPixels;
                double betweenClassVariance = (countForegroundPixels * meanForeground * meanForeground) +
                                              (countBackgroundPixels * meanBackground * meanBackground);

                if (betweenClassVariance > maxVariance)
                {
                    maxVariance = betweenClassVariance;
                    threshold = (byte)i;
                }
            }

            return Binarize(image, threshold);
        }
    }
}