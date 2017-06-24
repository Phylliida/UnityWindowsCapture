using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinCapture
{
    // Much of this is from pinvoke.net
    public class Win32Funcs
    {


        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref Win32Types.BitmapInfo pbmi,
            uint pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();

        /// <summary>
        ///        Creates a memory device context (DC) compatible with the specified device.
        /// </summary>
        /// <param name="hdc">A handle to an existing DC. If this handle is NULL,
        ///        the function creates a memory DC compatible with the application's current screen.</param>
        /// <returns>
        ///        If the function succeeds, the return value is the handle to a memory DC.
        ///        If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.
        /// </returns>
        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Win32Types.RECT lpRect);

        /// <summary>Selects an object into the specified device context (DC). The new object replaces the previous object of the same type.</summary>
        /// <param name="hdc">A handle to the DC.</param>
        /// <param name="hgdiobj">A handle to the object to be selected.</param>
        /// <returns>
        ///   <para>If the selected object is not a region and the function succeeds, the return value is a handle to the object being replaced. If the selected object is a region and the function succeeds, the return value is one of the following values.</para>
        ///   <para>SIMPLEREGION - Region consists of a single rectangle.</para>
        ///   <para>COMPLEXREGION - Region consists of more than one rectangle.</para>
        ///   <para>NULLREGION - Region is empty.</para>
        ///   <para>If an error occurs and the selected object is not a region, the return value is <c>NULL</c>. Otherwise, it is <c>HGDI_ERROR</c>.</para>
        /// </returns>
        /// <remarks>
        ///   <para>This function returns the previously selected object of the specified type. An application should always replace a new object with the original, default object after it has finished drawing with the new object.</para>
        ///   <para>An application cannot select a single bitmap into more than one DC at a time.</para>
        ///   <para>ICM: If the object being selected is a brush or a pen, color management is performed.</para>
        /// </remarks>
        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        /// <summary>
        ///    Performs a bit-block transfer of the color data corresponding to a
        ///    rectangle of pixels from the specified source device context into
        ///    a destination device context.
        /// </summary>
        /// <param name="hdc">Handle to the destination device context.</param>
        /// <param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
        /// <param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
        /// <param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
        /// <param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
        /// <param name="hdcSrc">Handle to the source device context.</param>
        /// <param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
        /// <param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
        /// <param name="dwRop">A raster-operation code.</param>
        /// <returns>
        ///    <c>true</c> if the operation succeedes, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, Win32Consts.TernaryRasterOperations dwRop);

        public static bool GetScreenRect(out Win32Types.RECT result)
        {
            return Win32Funcs.GetWindowRect(Win32Funcs.GetDesktopWindow(), out result);
        }

        public static string GetWindowName(IntPtr hwnd)
        {
            int len = Win32Funcs.GetWindowTextLength(hwnd);
            StringBuilder sb = new StringBuilder(len + 1);
            Win32Funcs.GetWindowText(hwnd, sb, sb.Capacity);

            return sb.ToString();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        // From http://stackoverflow.com/questions/4627376/use-native-hbitmap-in-c-sharp-while-preserving-alpha-channel-transparency
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "GetObject")]
        public static extern int GetObjectBitmap(IntPtr hObject, int nCount, ref Win32Types.BITMAP lpObject);


        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /// <summary>Deletes the specified device context (DC).</summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <returns><para>If the function succeeds, the return value is nonzero.</para><para>If the function fails, the return value is zero.</para></returns>
        /// <remarks>An application must not delete a DC whose handle was obtained by calling the <c>GetDC</c> function. Instead, it must call the <c>ReleaseDC</c> function to free the DC.</remarks>
        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        public static extern bool DeleteDC([In] IntPtr hdc);


        /// <summary>Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object. After the object is deleted, the specified handle is no longer valid.</summary>
        /// <param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
        /// <returns>
        ///   <para>If the function succeeds, the return value is nonzero.</para>
        ///   <para>If the specified handle is not valid or is currently selected into a DC, the return value is zero.</para>
        /// </returns>
        /// <remarks>
        ///   <para>Do not delete a drawing object (pen or brush) while it is still selected into a DC.</para>
        ///   <para>When a pattern brush is deleted, the bitmap associated with the brush is not deleted. The bitmap must be deleted independently.</para>
        /// </remarks>
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);




















        ////// All of these are to get info about monitors ///////


        delegate int MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Win32Types.RECT lprcMonitor, IntPtr dwData);

        public delegate int EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hDCMonitor, ref Win32Types.RECT lprcMonitor, IntPtr dwData);



        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref Win32Types.MonitorInfoEx lpmi);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref Win32Types.MonitorInfo lpmi);


        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point p);


        /// <summary>
        ///     Retrieves a handle to the top-level window whose class name and window name match the specified strings. This
        ///     function does not search child windows. This function does not perform a case-sensitive search. To search child
        ///     windows, beginning with a specified child window, use the
        ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms633500%28v=vs.85%29.aspx">FindWindowEx</see>
        ///     function.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633499%28v=vs.85%29.aspx for FindWindow
        ///     information or https://msdn.microsoft.com/en-us/library/windows/desktop/ms633500%28v=vs.85%29.aspx for
        ///     FindWindowEx
        ///     </para>
        /// </summary>
        /// <param name="lpClassName">
        ///     C++ ( lpClassName [in, optional]. Type: LPCTSTR )<br />The class name or a class atom created by a previous call to
        ///     the RegisterClass or RegisterClassEx function. The atom must be in the low-order word of lpClassName; the
        ///     high-order word must be zero.
        ///     <para>
        ///     If lpClassName points to a string, it specifies the window class name. The class name can be any name
        ///     registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names.
        ///     </para>
        ///     <para>If lpClassName is NULL, it finds any window whose title matches the lpWindowName parameter.</para>
        /// </param>
        /// <param name="lpWindowName">
        ///     C++ ( lpWindowName [in, optional]. Type: LPCTSTR )<br />The window name (the window's
        ///     title). If this parameter is NULL, all window names match.
        /// </param>
        /// <returns>
        ///     C++ ( Type: HWND )<br />If the function succeeds, the return value is a handle to the window that has the
        ///     specified class name and window name. If the function fails, the return value is NULL.
        ///     <para>To get extended error information, call GetLastError.</para>
        /// </returns>
        /// <remarks>
        ///     If the lpWindowName parameter is not NULL, FindWindow calls the <see cref="M:GetWindowText" /> function to
        ///     retrieve the window name for comparison. For a description of a potential problem that can arise, see the Remarks
        ///     for <see cref="M:GetWindowText" />.
        /// </remarks>
        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Retrieves the handle to the ancestor of the specified window. 
        /// </summary>
        /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved. 
        /// If this parameter is the desktop window, the function returns NULL. </param>
        /// <param name="flags">The ancestor to be retrieved.</param>
        /// <returns>The return value is the handle to the ancestor window.</returns>
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetAncestor(IntPtr hwnd, Win32Consts.GetAncestorFlags flags);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);



        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        // From http://blog.strixcode.com/2011/01/how-to-find-taskbar-button-position-on.html
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);


        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// From http://www.pinvoke.net/default.aspx/user32/EnumDisplayMonitors.html
        /// Returns the information about Displays using the Win32 functions
        /// </summary>
        /// <returns>collection of Display Info</returns>
        public static List<Win32Types.DisplayInfo> GetDisplays()
        {
            List<Win32Types.DisplayInfo> displayList = new List<Win32Types.DisplayInfo>();

            Win32Funcs.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Win32Types.RECT lprcMonitor, IntPtr dwData)
                {
                    Win32Types.MonitorInfo mi = new Win32Types.MonitorInfo();
                    mi.cbSize = Marshal.SizeOf(mi);
                    bool success = Win32Funcs.GetMonitorInfo(hMonitor, ref mi);
                    if (success)
                    {
                        Win32Types.DisplayInfo di = new Win32Types.DisplayInfo();
                        di.ScreenWidth = (mi.rcMonitor.Right - mi.rcMonitor.Left).ToString();
                        di.ScreenHeight = (mi.rcMonitor.Bottom - mi.rcMonitor.Top).ToString();
                        di.MonitorArea = mi.rcMonitor;
                        di.WorkArea = mi.rcWork;
                        di.Availability = mi.dwFlags.ToString();
                        di.hwnd = hMonitor;
                        displayList.Add(di);
                    }
                    return 1;
                }, IntPtr.Zero);
            return displayList;
        }


        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr hIcon);


        /// <summary>Must initialize cbSize</summary>
        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(ref Win32Types.CursorInfo pci);


        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon, out Win32Types.IconInfo piconinfo);

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconIndirect([In] ref Win32Types.IconInfo piconinfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon,
                              int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw,
                              int diFlags);


        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(Win32Consts.SystemMetric smIndex);


        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Win32Types.PointL lpPoint);


        // From https://code.msdn.microsoft.com/windowsapps/Enumerate-top-level-9aa9d7c1
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Delegate for the EnumChildWindows method
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="parameter">Caller-defined variable; we use it for a pointer to our list</param>
        /// <returns>True to continue enumerating, false to bail.</returns>
        public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);
    }
}