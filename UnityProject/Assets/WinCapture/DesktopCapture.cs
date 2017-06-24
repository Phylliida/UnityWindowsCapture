using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using WinCapture;

namespace WinCapture
{
    public class DesktopCapture : IDisposable
    {
        bool getDesktop;
        IntPtr testPointer;
        IntPtr renderCallbackPointer;
        public delegate void GLRenderPluginFunc(int eventId);


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugDelegate(string str);

        System.Object isRenderingLock = new System.Object();
        System.Object actuallyGoLock = new System.Object();

        GLRenderPluginFunc pluginFunc;

        bool isRendering = false;

        byte[] desktopFrameData;

        int displayNum;

        public int desktopWidth = 0;
        public int desktopHeight = 0;

        public Texture2D desktopTexture;

        static void DebugFunction(string str)
        {
            Debug.Log("Fromcpp: " + str);
        }


        // Use this for initialization
        public DesktopCapture(int displayNum)
        {
            this.displayNum = displayNum;

            Win32Types.RECT screenRect;
            Win32Funcs.GetScreenRect(out screenRect);

            DebugDelegate debugDelegate = new DebugDelegate(DebugFunction);
            IntPtr intptrDelegate = Marshal.GetFunctionPointerForDelegate(debugDelegate);
            WindowRenderCS.WindowRenderer.SetDebugFunction(intptrDelegate);

            pluginFunc = ActuallyRenderWindows;

            renderCallbackPointer = Marshal.GetFunctionPointerForDelegate(pluginFunc);

            // You can do a try catch here to test if it is supported


            Texture2D dummyTexture = new Texture2D(10, 10);

            WindowRenderCS.WindowRenderer.InitDeskDupl(dummyTexture.GetNativeTexturePtr(), displayNum);


            desktopTexture = new Texture2D(screenRect.Width, screenRect.Height, TextureFormat.ARGB32, false);

            testPointer = desktopTexture.GetNativeTexturePtr();

            getDesktop = true;
        }

        void ActuallyRenderWindows(int eventId)
        {
            if (!getDesktop)
            {
                return;
            }
            lock (isRenderingLock)
            {
                isRendering = true;
            }

            lock (actuallyGoLock)
            {
                if (desktopFrameData == null)
                {
                    desktopFrameData = new byte[128];
                }

                try
                {
                    WindowRenderCS.WindowRenderer.GetDesktopFrame(testPointer, displayNum, out desktopWidth, out desktopHeight, desktopFrameData, desktopFrameData.Length, 0);
                }
                catch
                {

                }
                if (desktopWidth != 0 && desktopHeight != 0)
                {
                    if (desktopWidth * desktopHeight * 4 != desktopFrameData.Length)// || windowCapture.windowWidth != curFrameWidth || windowCapture.windowHeight != curFrameHeight)
                    {
                        desktopFrameData = new byte[desktopWidth * desktopHeight * 4];
                    }
                }
            }


            lock (isRenderingLock)
            {
                isRendering = false;
            }
        }

        public void OnPostRender()
        {
            bool curIsRendering = false;

            lock (isRenderingLock)
            {
                curIsRendering = isRendering;
            }

            if (!curIsRendering)
            {

                GL.IssuePluginEvent(renderCallbackPointer, 1);
            }
        }

        bool firstTime = true;

        // Update is called once per frame
        public Texture2D GetWindowTexture(out bool didChange)
        {
            didChange = false;
            if (firstTime)
            {
                didChange = true;
                firstTime = false;
            }
            if (getDesktop)
            {
                if (desktopFrameData != null && desktopFrameData.Length > 0 && desktopWidth != 0 && desktopHeight != 0)
                {
                    if (desktopTexture == null || desktopTexture.width != desktopWidth || desktopTexture.height != desktopHeight)
                    {
                        //desktopTexture = new Texture2D(curFrameWidth, curFrameHeight, TextureFormat.ARGB32, false);
                        //testPointer = desktopTexture.GetNativeTexturePtr();
                    }
                    desktopTexture.LoadRawTextureData(desktopFrameData);
                    desktopTexture.Apply();
                }
            }
            return desktopTexture;
        }

        bool cleanedUp = false;

        void Cleanup()
        {
            if (!cleanedUp)
            {
                cleanedUp = true;
                WindowRenderCS.WindowRenderer.CleanupDeskDupl(displayNum);
            }
        }

        public void Dispose()
        {
            Cleanup();
        }

        ~DesktopCapture()
        {
            Dispose();
        }
    }
}