using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Win32ApiLibrary
{
    public class ScreenHelper
    {
        /// <summary>
        /// get screenshot base on window handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static Bitmap GetWindowCapture(IntPtr handle)
        {
            // get te hDC of the target window
            HDC hdcSrc = PInvoke.GetWindowDC((HWND)handle);
            PInvoke.ShowWindow((HWND)handle, SHOW_WINDOW_CMD.SW_SHOWNA);
            // get the size
            PInvoke.GetWindowRect((HWND)handle, out RECT windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            HDC hdcDest = PInvoke.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            HBITMAP hBitmap = PInvoke.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            HGDIOBJ hOld = PInvoke.SelectObject(hdcDest, hBitmap);
            // bitblt over
            PInvoke.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, ROP_CODE.SRCCOPY | ROP_CODE.CAPTUREBLT);
            // restore selection
            PInvoke.SelectObject(hdcDest, hOld);
            // clean up
            PInvoke.DeleteDC(hdcDest);
            PInvoke.ReleaseDC((HWND)handle, hdcSrc);
            // get a .NET image object for it
            Bitmap img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            PInvoke.DeleteObject(hBitmap);
            return img;
        }
    }
}