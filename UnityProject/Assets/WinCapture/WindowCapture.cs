using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace WinCapture
{
    public class WindowCapture : IDisposable
    {
        public Win32Types.WindowInfo windowInfo;
        public IntPtr hwnd;
        IntPtr hdc;
        IntPtr hDest;
        public int windowWidth;
        public int windowHeight;
        IntPtr curRenderingBitmap;
        byte[] bitmapBytes = null;
        bool isDesktop;
        public byte[] actualColorBuffer = null;

        public Texture2D windowTexture;

        public Win32Types.RECT windowRect;

        public bool onlyCaptureMouse;

        public WindowCapture(IntPtr windowHandle, bool isDesktop, bool onlyCaptureMouse=false)
        {
            this.isDesktop = isDesktop;

            this.onlyCaptureMouse = onlyCaptureMouse;

            // windowHandle is your window handle, IntPtr.Zero is the desktop
            hwnd = windowHandle;
            windowInfo = new Win32Types.WindowInfo(hwnd);

            SetupWindowCapture();

            bitmapBytes = null;
        }

        void SetupWindowCapture()
        {
            UpdateCursorInfo();

            // Get the device context
            if (isDesktop)
            {
                hdc = Win32Funcs.GetDC(IntPtr.Zero);
            }
            else
            {
                hdc = Win32Funcs.GetWindowDC(hwnd);
            }

            // Create a device context to use yourself
            hDest = Win32Funcs.CreateCompatibleDC(hdc);

            Win32Types.RECT windowRect;

            if (isDesktop)
            {
                Win32Types.MonitorInfo mi = new Win32Types.MonitorInfo();
                mi.cbSize = Marshal.SizeOf(mi);
                Win32Funcs.GetMonitorInfo(hwnd, ref mi);
                windowRect = mi.rcMonitor;

            }
            else
            {
                // Get the size of the window
                Win32Funcs.GetWindowRect(hwnd, out windowRect);
            }

            windowWidth = windowRect.Width;
            windowHeight = windowRect.Height;

            if (onlyCaptureMouse)
            {
                windowWidth = cursorRect.Width;
                windowHeight = cursorRect.Height;
            }

            // From http://stackoverflow.com/questions/7502588/createcompatiblebitmap-and-createdibsection-memory-dcs
            Win32Types.BitmapInfo bmi = new Win32Types.BitmapInfo();
            bmi.bmiHeader.Init();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(bmi);
            bmi.bmiHeader.biWidth = windowWidth;
            bmi.bmiHeader.biHeight = -windowHeight; // top-down
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 24;
            bmi.bmiHeader.biCompression = Win32Consts.BitmapCompressionMode.BI_RGB;


            IntPtr outBits;
            curRenderingBitmap = Win32Funcs.CreateDIBSection(hdc, ref bmi, (uint)Win32Consts.DIB_Color_Mode.DIB_PAL_COLORS, out outBits, IntPtr.Zero, (uint)0);

        }

        bool firstTime = true;



        public Texture2D GetWindowTexture(out bool didChange)
        {
            byte[] textureBytes = GetAlignedBytes(out didChange);
            if (windowTexture == null || windowWidth != windowTexture.width || windowHeight != windowTexture.height)
            {
                windowTexture = new Texture2D(windowWidth, windowHeight, TextureFormat.RGB24, false);
                didChange = true;
            }
            windowTexture.LoadRawTextureData(textureBytes);
            windowTexture.Apply();
            return windowTexture;
        }
        public byte[] GetAlignedBytes(out bool didChange)
        {
            didChange = false;
            if (firstTime)
            {
                firstTime = false;
                didChange = true;
            }
            int numBytesPerRow;
            WindowCapture.UpdateCursorInfo();
            byte[] bitmapBytes = GetWindowContents(out numBytesPerRow);
            if (bitmapBytes != null)
            {

                if (actualColorBuffer == null || actualColorBuffer.Length != windowWidth * windowHeight * 3)
                {
                    actualColorBuffer = new byte[windowWidth * windowHeight * 3];
                }

                int actualNumBytesPerRow = windowWidth * 3;
                int curOffsetInSrc = 0;
                int curOffsetInRes = 0;
                for (int y = 0; y < windowHeight; y++)
                {
                    Buffer.BlockCopy(bitmapBytes, curOffsetInSrc, actualColorBuffer, curOffsetInRes, actualNumBytesPerRow);
                    curOffsetInSrc += numBytesPerRow;
                    curOffsetInRes += actualNumBytesPerRow;
                }

                return actualColorBuffer;
                
            }

            return null;
            
        }

        public byte[] GetWindowContents(out int numBytesPerRow)
        {
            bitmapBytes = GetWindowBitmapUsingBitBlt(out numBytesPerRow);
            return bitmapBytes;
        }

        public byte[] GetWindowBitmapUsingBitBlt(out int numBytesPerRow)
        {
            windowRect = new Win32Types.RECT();
            bool result;
            if (isDesktop)
            {
                Win32Types.MonitorInfo mi = new Win32Types.MonitorInfo();
                mi.cbSize = Marshal.SizeOf(mi);
                result = Win32Funcs.GetMonitorInfo(hwnd, ref mi);
                if (result)
                {
                    windowRect = mi.rcMonitor;
                }
            }
            else
            {
                // Get the size of the window
                result = Win32Funcs.GetWindowRect(hwnd, out windowRect);
            }

            if (onlyCaptureMouse)
            {
                windowRect = cursorRect;
                result = true;
            }

            if (!result)
            {
                // Failed getting rect
                numBytesPerRow = 0;

                return null;
            }

            // If they resized the window we need to reinit the memory
            if (windowWidth != windowRect.Width || windowHeight != windowRect.Height)
            {
                CleanupWindowCapture();
                SetupWindowCapture();
                windowWidth = windowRect.Width;
                windowHeight = windowRect.Height;
            }



            // Use the previously created device context with the bitmap
            Win32Funcs.SelectObject(hDest, curRenderingBitmap);

            if (onlyCaptureMouse)
            {
                if (isDesktop)
                {
                    Win32Funcs.BitBlt(hDest, 0,0, cursorRect.Width, cursorRect.Height, hdc, windowRect.Left, windowRect.Top, Win32Consts.TernaryRasterOperations.SRCCOPY);
                }
                else
                {
                    Win32Funcs.BitBlt(hDest, cursorRect.Left, cursorRect.Height, cursorRect.Width, cursorRect.Height, hdc, 0,0, Win32Consts.TernaryRasterOperations.SRCCOPY);
                }
                Win32Funcs.DrawIconEx(hDest, 1, 1, cursorHandle, cursorRect.Width, cursorRect.Height, 0, IntPtr.Zero, Win32Consts.DI_NORMAL);
            }
            else
            {
                // Copy from the screen device context to the bitmap device context
                if (isDesktop)
                {
                    Win32Funcs.BitBlt(hDest, 0, 0, windowRect.Width, windowRect.Height, hdc, windowRect.Left, windowRect.Top, Win32Consts.TernaryRasterOperations.SRCCOPY);
                }
                else
                {
                    Win32Funcs.BitBlt(hDest, 0, 0, windowRect.Width, windowRect.Height, hdc, 0, 0, Win32Consts.TernaryRasterOperations.SRCCOPY);
                }
                Win32Funcs.DrawIconEx(hDest, cursorRect.Left, cursorRect.Top, cursorHandle, cursorRect.Width, cursorRect.Height, 0, IntPtr.Zero, Win32Consts.DI_NORMAL);
            }




            Win32Types.BITMAP bitmap = new Win32Types.BITMAP();

            Win32Funcs.GetObjectBitmap(curRenderingBitmap, Marshal.SizeOf(bitmap), ref bitmap);

            numBytesPerRow = bitmap.bmWidthBytes;

            if (bitmapBytes == null || bitmapBytes.Length != bitmap.bmHeight * bitmap.bmWidthBytes)
            {
                bitmapBytes = new byte[bitmap.bmHeight * bitmap.bmWidthBytes];
            }


            if (bitmap.bmBits != IntPtr.Zero)
            {
                Marshal.Copy(bitmap.bmBits, bitmapBytes, 0, bitmapBytes.Length);
            }

            if (bitmapBytes == null || bitmapBytes.Length == 0)
            {
                return null;
            }
            
            return bitmapBytes;
        }
        public static Win32Types.RECT cursorRect;
        public static Point hotspot;
        public static Point iconDims;
        public static IntPtr cursorHandle;
        

        public static void UpdateCursorInfo()
        {
            // TODO - make sure everything here is cleaned up properly?
            // I'm pretty sure it is but it is good to check
            //int x = 0, y = 0;
            //return CaptureCursor (ref x, ref y);

            Win32Types.CursorInfo ci = new Win32Types.CursorInfo();
            ci.cbSize = Marshal.SizeOf(typeof(Win32Types.CursorInfo));

            if (!Win32Funcs.GetCursorInfo(ref ci))
            {
                return;
            }

            // Todo: this will change if cursor icon changes (via http://stackoverflow.com/questions/358527/how-to-tell-if-mouse-pointer-icon-changed?rq=1),
            // So then we can make more expensive and accurate cursor things and only update as needed

            IntPtr cursorPointer = ci.hCursor;
            cursorHandle = cursorPointer;

            int iconWidth = Win32Funcs.GetSystemMetrics(Win32Consts.SystemMetric.SM_CXICON) + 1;
            int iconHeight = Win32Funcs.GetSystemMetrics(Win32Consts.SystemMetric.SM_CYICON) + 1;


            iconDims = new System.Drawing.Point(iconWidth, iconHeight);
            

            Win32Types.IconInfo hotSpotInfo = new Win32Types.IconInfo();
            Win32Funcs.GetIconInfo(cursorPointer, out hotSpotInfo);

            

            //Win32funcs.DrawIcon(hdcBitmap, 1, 1, cursorPointer);


            hotspot = new System.Drawing.Point(hotSpotInfo.xHotspot + 1, hotSpotInfo.yHotspot + 1);


            if (hotSpotInfo.hbmColor != IntPtr.Zero)
            {
                Win32Funcs.DeleteObject(hotSpotInfo.hbmColor);
            }
            if (hotSpotInfo.hbmMask != IntPtr.Zero)
            {
                Win32Funcs.DeleteObject(hotSpotInfo.hbmMask);
            }


            Win32Types.PointL cursorPos;
            Win32Funcs.GetCursorPos(out cursorPos);
            int cursorX = cursorPos.x - hotspot.X;
            int cursorY = cursorPos.y - hotspot.Y;
            cursorRect = new Win32Types.RECT(cursorX, cursorY, cursorX + iconDims.X, cursorY + iconDims.Y);
        }


        public void Dispose()
        {
            CleanupWindowCapture();
        }

        ~WindowCapture()
        {
            Dispose();
        }


        void CleanupWindowCapture()
        {
            if (hdc != IntPtr.Zero)
            {
                Win32Funcs.ReleaseDC(hwnd, hdc);
                hdc = IntPtr.Zero;
            }

            if (hDest != IntPtr.Zero)
            {
                Win32Funcs.DeleteDC(hDest);
                hDest = IntPtr.Zero;
            }

            if (curRenderingBitmap != IntPtr.Zero)
            {
                Win32Funcs.DeleteObject(curRenderingBitmap);
                curRenderingBitmap = IntPtr.Zero;
            }
        }
    }
}
 