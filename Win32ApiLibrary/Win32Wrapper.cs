using Windows.Win32;
using Windows.Win32.Foundation;

namespace Win32ApiLibrary
{
    public class Win32Wrapper
    {
        public static IntPtr GetWindowHwnd(Point point)
        {
            return PInvoke.WindowFromPoint(point);
        }

        public static unsafe string GetWindowName(IntPtr hwnd)
        {
            Span<char> name = stackalloc char[PInvoke.GetWindowTextLength((HWND)hwnd) + 1];
            fixed (char* pName = name)
            {
                PInvoke.GetWindowText((HWND)hwnd, pName, name.Length);
            }

            return name.ToString();
        }

        public static unsafe uint GetProcessIdByHwnd(IntPtr hWnd)
        {
            uint result;
            PInvoke.GetWindowThreadProcessId((HWND)hWnd, &result);
            return result;
        }

        public static unsafe string GetWindowClassName(IntPtr hwnd)
        {
            Span<char> name = stackalloc char[256];
            fixed (char* pName = name)
            {
                name = name.Slice(0, PInvoke.GetClassName((HWND)hwnd, pName, 256) + 1);
            }

            return name.ToString();
        }
    }
}