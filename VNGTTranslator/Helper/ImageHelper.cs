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

        public static unsafe bool CompareImage(Bitmap? bmp1, Bitmap? bmp2)
        {
            if (bmp1 == null || bmp2 == null)
                return false;
            if (bmp1.Width != bmp2.Width || bmp1.Height != bmp2.Height)
            {
                return false;
            }

            BitmapData bmpData1 = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height),
                ImageLockMode.ReadOnly, bmp1.PixelFormat);
            BitmapData bmpData2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height),
                ImageLockMode.ReadOnly, bmp2.PixelFormat);

            try
            {
                int bytesPerPixel = Image.GetPixelFormatSize(bmp1.PixelFormat) / 8;
                int heightInPixels = bmp1.Height;
                int widthInBytes = bmp1.Width * bytesPerPixel;

                byte* ptr1 = (byte*)bmpData1.Scan0;
                byte* ptr2 = (byte*)bmpData2.Scan0;

                for (int y = 0; y < heightInPixels; y++)
                {
                    byte* row1 = ptr1 + (y * bmpData1.Stride);
                    byte* row2 = ptr2 + (y * bmpData2.Stride);

                    for (int x = 0; x < widthInBytes; x++)
                    {
                        if (row1[x] != row2[x])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                bmp1.UnlockBits(bmpData1);
                bmp2.UnlockBits(bmpData2);
            }
        }
    }
}