using System;
using UnityEngine;
using System.Collections;
using System.Text;
//using System.Diagnostics;
using MessageLibrary;
using UnityEngine.UI;
using System.Collections.Generic;
using WinCapture;
using SimpleWebBrowser;

namespace WinCapture
{
    public class ChromiumCapture : IDisposable
    {
        BrowserEngine mainEngine;

        string MemoryFile = "MainSharedMem";

        int Port = 8885;


        public ChromiumCapture(int width, int height, string url, bool enableWebRTC=true)
        {
            mainEngine = new BrowserEngine();

            bool RandomMemoryFile = true;

            if (RandomMemoryFile)
            {
                Guid memid = Guid.NewGuid();
                MemoryFile = memid.ToString();
            }

            bool RandomPort = true;

            if (RandomPort)
            {
                System.Random r = new System.Random();
                Port = 8000 + r.Next(1000);
            }



            mainEngine.InitPlugin(width, height, MemoryFile, Port, url, enableWebRTC);
            //run initialization
            //if (JSInitializationCode.Trim() != "")
            //    mainEngine.RunJSOnce(JSInitializationCode);
        }


        public void SetUrl(string url)
        {
            mainEngine.SendNavigateEvent(url, false, false);
        }

        bool isFirst = true;
        public Texture2D GetChromiumTexture(out bool didChange)
        {
            didChange = false;
            if (isFirst)
            {
                didChange = true;
                isFirst = false;
            }

            mainEngine.UpdateTexture();
            return mainEngine.BrowserTexture;
        }


        public void Dispose()
        {
            Cleanup();
        }

        ~ChromiumCapture()
        {
            Cleanup();
        }

        object cleanupLock = new object();
        bool didCleanup = false;
        void Cleanup()
        {
            lock (cleanupLock)
            {
                if (!didCleanup)
                {
                    didCleanup = true;
                    mainEngine.Shutdown();
                }
            }
        }



        ////// This is all stuff you can use to let the user interact with the chromium window //////
        ////// I'll add an example of how to use it all in a bit                               //////


        /*
        public void UpdateInteract()
        {
            if (focused)
            {
                foreach (char c in Input.inputString)
                {
                    mainEngine.SendCharEvent((int)c, KeyboardEventType.CharKey);
                }
                ProcessKeyEvents();
            }
        }

        private void ProcessKeyEvents()
        {
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                CheckKey(k);
            }
        }

        private void CheckKey(KeyCode code)
        {
            if (Input.GetKeyDown(code))
                mainEngine.SendCharEvent((int)code, KeyboardEventType.Down);
            if (Input.GetKeyUp(KeyCode.Backspace))
                mainEngine.SendCharEvent((int)code, KeyboardEventType.Up);
        }







        // Input stuff


        bool focused = false;
        void OnMouseEnter()
        {
            focused = true;
        }

        void OnMouseExit()
        {
            focused = false;
        }

        void OnMouseDown()
        {
            if (mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                    SendMouseButtonEvent((int)pixelUV.x, (int)pixelUV.y, MouseButton.Left, MouseEventType.ButtonDown);

                }
            }
        }




        void OnMouseUp()
        {
            if (mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                    SendMouseButtonEvent((int)pixelUV.x, (int)pixelUV.y, MouseButton.Left, MouseEventType.ButtonUp);
                }
            }
        }

        int posX, posY;

        void OnMouseOver()
        {
            if (mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                    int px = (int)pixelUV.x;
                    int py = (int)pixelUV.y;

                    ProcessScrollInput(px, py);

                    if (posX != px || posY != py)
                    {
                        MouseMessage msg = new MouseMessage
                        {
                            Type = MouseEventType.Move,
                            X = px,
                            Y = py,
                            GenericType = MessageLibrary.BrowserEventType.Mouse,
                            // Delta = e.Delta,
                            Button = MouseButton.None
                        };

                        if (Input.GetMouseButton(0))
                            msg.Button = MouseButton.Left;
                        if (Input.GetMouseButton(1))
                            msg.Button = MouseButton.Right;
                        if (Input.GetMouseButton(1))
                            msg.Button = MouseButton.Middle;

                        posX = px;
                        posY = py;
                        mainEngine.SendMouseEvent(msg);
                    }

                    //check other buttons...
                    if (Input.GetMouseButtonDown(1))
                        SendMouseButtonEvent(px, py, MouseButton.Right, MouseEventType.ButtonDown);
                    if (Input.GetMouseButtonUp(1))
                        SendMouseButtonEvent(px, py, MouseButton.Right, MouseEventType.ButtonUp);
                    if (Input.GetMouseButtonDown(2))
                        SendMouseButtonEvent(px, py, MouseButton.Middle, MouseEventType.ButtonDown);
                    if (Input.GetMouseButtonUp(2))
                        SendMouseButtonEvent(px, py, MouseButton.Middle, MouseEventType.ButtonUp);
                }
            }

            // Debug.Log(pixelUV);
        }


        private void SendMouseButtonEvent(int x, int y, MouseButton btn, MouseEventType type)
        {
            MouseMessage msg = new MouseMessage
            {
                Type = type,
                X = x,
                Y = y,
                GenericType = MessageLibrary.BrowserEventType.Mouse,
                // Delta = e.Delta,
                Button = btn
            };
            mainEngine.SendMouseEvent(msg);
        }

        private void ProcessScrollInput(int px, int py)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            scroll = scroll * mainEngine.BrowserTexture.height;

            int scInt = (int)scroll;

            if (scInt != 0)
            {
                MouseMessage msg = new MouseMessage
                {
                    Type = MouseEventType.Wheel,
                    X = px,
                    Y = py,
                    GenericType = MessageLibrary.BrowserEventType.Mouse,
                    Delta = scInt,
                    Button = MouseButton.None
                };

                if (Input.GetMouseButton(0))
                    msg.Button = MouseButton.Left;
                if (Input.GetMouseButton(1))
                    msg.Button = MouseButton.Right;
                if (Input.GetMouseButton(1))
                    msg.Button = MouseButton.Middle;

                mainEngine.SendMouseEvent(msg);
            }
        }

        private Vector2 GetScreenCoords()
        {
            RaycastHit hit;
            if (
                !Physics.Raycast(
                    interactCamera.ScreenPointToRay(Input.mousePosition), out hit))
                return new Vector2(-1f, -1f);
            Texture tex = mainEngine.BrowserTexture;


            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x = (1 - pixelUV.x) * tex.width;
            pixelUV.y *= tex.height;
            return pixelUV;
        }
        */
    }
}