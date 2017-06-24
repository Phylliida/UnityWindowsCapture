using System;
using System.Runtime.InteropServices;

namespace WindowRenderCS
{
    public class WindowRenderer
    {

        [DllImport("RenderWindowsPlugin")]
        public static extern int InitDeskDupl(IntPtr dummyTexture, int outputNum);

        [DllImport("RenderWindowsPlugin")]
        public static extern void CleanupDeskDupl(int outputNum);

        [DllImport("RenderWindowsPlugin")]
        public static extern void GetDesktopFrame(IntPtr dummyTexture, int outputNum, out int width, out int height, byte[] data, int lenData, int timeoutInMillis);

        [DllImport("RenderWindowsPlugin")]
        public static extern void SetDebugFunction(IntPtr fp);

    }
}