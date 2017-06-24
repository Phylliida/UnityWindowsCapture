using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;
namespace WinCapture
{
    public class TexturesHolder
    {
        public Dictionary<IntPtr, WindowCapture> windowCapturers;


        public delegate void AddWindow(IntPtr hwnd);

        public delegate void RemoveWindow(IntPtr hwnd);

        public event AddWindow OnAddWindow;
        public event RemoveWindow OnRemoveWindow;


        WindowsHolder windowsHolder;
        // Apply WinCapture/WindowShader shader to any resulting textures
        public TexturesHolder()
        {
            windowsHolder = new WindowsHolder();
            windowsHolder.OnAddWindow += OnAddWindowFound;
            windowsHolder.OnRemoveWindow += OnRemoveWindowFound;

            windowCapturers = new Dictionary<IntPtr, WindowCapture>();

            List<Win32Types.DisplayInfo> monitorInfos = Win32Funcs.GetDisplays();

            for (int i = 0; i < monitorInfos.Count; i++)
            {
                windowCapturers[monitorInfos[i].hwnd] = new WindowCapture(monitorInfos[i].hwnd, true);
            }

            textures = new Dictionary<IntPtr, Texture2D>();
        }

        void OnAddWindowFound(System.IntPtr hwnd)
        {
            Win32Types.WindowInfo windowInfo = new Win32Types.WindowInfo(hwnd);
            windowCapturers[hwnd] = new WindowCapture(hwnd, false);

            if (OnAddWindow != null)
            {
                OnAddWindow(hwnd);
            }
        }

        void OnRemoveWindowFound(System.IntPtr hwnd)
        {
            Win32Types.WindowInfo windowInfo = new Win32Types.WindowInfo(hwnd);
            windowCapturers.Remove(hwnd);

            if (OnRemoveWindow != null)
            {
                OnRemoveWindow(hwnd);
            }
        }

        public Dictionary<IntPtr, Texture2D> textures;

        public void Update()
        {
            windowsHolder.UpdateWindows();

            WindowCapture.SetCursorInfo();

            foreach (KeyValuePair<IntPtr, WindowCapture> windowCapturePair in windowCapturers)
            {
                IntPtr hwnd = windowCapturePair.Key;
                WindowCapture windowCapture = windowCapturePair.Value;
                bool didChange;
                Texture2D screenTexture = GetScreenTexture(windowCapture, out didChange);

                if (didChange)
                {
                    textures[hwnd] = screenTexture;
                }
            }
        }


        public Texture2D GetScreenTexture(WindowCapture windowCapture, out bool didChange)
        {
            didChange = false;
            int numBytesPerRow;
            byte[] bitmapBytes = windowCapture.GetWindowContents(out numBytesPerRow);
            if (bitmapBytes != null)
            {
                if (windowCapture.windowTexture == null)
                {
                    windowCapture.windowTexture = new Texture2D(windowCapture.windowWidth, windowCapture.windowHeight, TextureFormat.RGB24, false);
                    didChange = true;
                }

                if (windowCapture.actualColorBuffer == null || windowCapture.actualColorBuffer.Length != windowCapture.windowWidth * windowCapture.windowHeight * 3)
                {
                    windowCapture.actualColorBuffer = new byte[windowCapture.windowWidth * windowCapture.windowHeight * 3];
                }

                int actualNumBytesPerRow = windowCapture.windowWidth * 3;
                int curOffsetInSrc = 0;
                int curOffsetInRes = 0;
                for (int y = 0; y < windowCapture.windowHeight; y++)
                {
                    Buffer.BlockCopy(bitmapBytes, curOffsetInSrc, windowCapture.actualColorBuffer, curOffsetInRes, actualNumBytesPerRow);
                    curOffsetInSrc += numBytesPerRow;
                    curOffsetInRes += actualNumBytesPerRow;
                }



                windowCapture.windowTexture.LoadRawTextureData(windowCapture.actualColorBuffer);
                windowCapture.windowTexture.Apply();

            }

            if (windowCapture.windowTexture != null && windowCapture.windowTexture.width != 0 && windowCapture.windowTexture.height != 0)
            {
                return windowCapture.windowTexture;
            }

            return null;
        }
    }
}