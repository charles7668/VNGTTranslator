using System.Drawing.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.Xps;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Win32ApiLibrary
{
    public static class ScreenHelper
    {
        /// <summary>
        /// get screenshot base on window handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static Bitmap GetWindowCapture(IntPtr handle)
        {
            PInvoke.ShowWindow((HWND)handle, SHOW_WINDOW_CMD.SW_SHOWNA);
            // get the size
            PInvoke.GetWindowRect((HWND)handle, out RECT windowRect);
            var bmp = new Bitmap(windowRect.Width, windowRect.Height, PixelFormat.Format32bppArgb);
            var gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();
            // For param3, use 2 for DirectX windows. Note that 2 is not defined in the Win32 API.
            // Reference: https://stackoverflow.com/questions/891345/get-a-screenshot-of-a-specific-application
            PInvoke.PrintWindow((HWND)handle, (HDC)hdcBitmap, (PRINT_WINDOW_FLAGS)2);
            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();
            return bmp;
        }
    }
}