using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using WinCapture;

namespace WinCapture
{
    public class WindowGetterWorker
    {
        public System.Object windowLock;

        Dictionary<string, IntPtr> tempWindowsFoundSoFar;
        Dictionary<string, bool> shouldKeep;

        List<IntPtr> thingsToAdd;
        List<IntPtr> thingsToRemove;

        public List<IntPtr> myThingsToAdd;
        public List<IntPtr> myThingsToRemove;

        public IntPtr taskbarHandle;

        public WindowGetterWorker()
        {
            this.windowLock = new System.Object();
            tempWindowsFoundSoFar = new Dictionary<string, IntPtr>(100);

            thingsToAdd = new List<IntPtr>(100);
            thingsToRemove = new List<IntPtr>(100);

            myThingsToAdd = new List<IntPtr>(100);
            myThingsToRemove = new List<IntPtr>(100);

            shouldKeep = new Dictionary<string, bool>(100);

            taskbarHandle = Win32Funcs.FindWindow("Shell_TrayWnd", null);

            mapToString = new Dictionary<int, string>();

            lastTimeRan = Time.time;
        }

        public ManualResetEvent amWaiting;


        // For some reason if you convert IntPtrs to strings a lot then things slow down so I do this which helps
        public Dictionary<Int32, string> mapToString;
        public string IntPtrToString(IntPtr ptr)
        {
            int ptrAsInt = ptr.ToInt32();
            if (mapToString.ContainsKey(ptrAsInt))
            {
            }
            else
            {
                mapToString[ptrAsInt] = ptr.ToString();

            }
            return mapToString[ptrAsInt];
        }

        public float lastTimeRan;

        public void DoWork()
        {
            thingsToAdd.Clear();
            thingsToRemove.Clear();

            shouldKeep.Clear();
            AddWindows();

            if (thingsToAdd.Count > 0 || thingsToRemove.Count > 0 || shouldKeep.Count != tempWindowsFoundSoFar.Count)
            {
                lock (windowLock)
                {
                    for (int i = 0; i < thingsToAdd.Count; i++)
                    {
                        string intPtrString = IntPtrToString(thingsToAdd[i]);
                        tempWindowsFoundSoFar[intPtrString] = thingsToAdd[i];
                        shouldKeep[intPtrString] = true;
                        myThingsToAdd.Add(thingsToAdd[i]);
                    }

                    foreach (KeyValuePair<string, IntPtr> windowFound in tempWindowsFoundSoFar)
                    {
                        if (!shouldKeep.ContainsKey(windowFound.Key))
                        {
                            thingsToRemove.Add(windowFound.Value);
                        }
                    }

                    for (int i = 0; i < thingsToRemove.Count; i++)
                    {
                        tempWindowsFoundSoFar.Remove(IntPtrToString(thingsToRemove[i]));
                        myThingsToRemove.Add(thingsToRemove[i]);
                    }
                }
            }
        }
        public void AddWindows()
        {
            Win32Funcs.EnumWindows(new Win32Funcs.EnumWindowsProc(LoopThroughWindows), IntPtr.Zero);
        }

        private IntPtr GetLastVisibleActivePopUpOfWindow(IntPtr window)
        {
            IntPtr lastPopUp = Win32Funcs.GetLastActivePopup(window);
            if (Win32Funcs.IsWindowVisible(lastPopUp))
                return lastPopUp;
            else if (lastPopUp == window)
                return IntPtr.Zero;
            else
                return GetLastVisibleActivePopUpOfWindow(lastPopUp);
        }


        public bool IsAltTabWindow(IntPtr window, string windowTitle)
        {
            if (windowTitle == "Jump List")
            {
                return false;
            }

            if (windowTitle == "TaskListThumbnailWnd")
            {
                return false;
            }

            if (windowTitle == "Unity Personal (64bit) - yams.unity - Floating Windows - PC, Mac & Linux Standalone <DX11>" || windowTitle == "Multiscreens" || windowTitle == "UnityContainerWndClass")
            {
                return true;
            }

            //if (windowTitle == 
            if (window == taskbarHandle)
            {
                return true;
            }
            //if (window == Win32funcs.GetShellWindow())   //Desktop
            //	return false;

            //http://stackoverflow.com/questions/210504/enumerate-windows-like-alt-tab-does
            //http://blogs.msdn.com/oldnewthing/archive/2007/10/08/5351207.aspx
            //1. For each visible window, walk up its owner chain until you find the root owner. 
            //2. Then walk back down the visible last active popup chain until you find a visible window.
            //3. If you're back to where you're started, (look for exceptions) then put the window in the Alt+Tab list.
            IntPtr root = Win32Funcs.GetAncestor(window, Win32Consts.GetAncestorFlags.GetRootOwner);

            if (GetLastVisibleActivePopUpOfWindow(root) == window)
            {
                Win32Types.WindowInfo wi = new Win32Types.WindowInfo(window);

                if (wi.className == "#32768" ||
                    wi.className == "Shell_TrayWnd" || //Windows taskbar
                    wi.className == "DV2ControlHost" || //Windows startmenu, if open
                    (wi.className == "Button" && wi.title == "Start") || //Windows startmenu-button.
                    wi.className == "MsgrIMEWindowClass" || //Live messenger's notifybox i think
                    wi.className == "SysShadow" || //Live messenger's shadow-hack
                    wi.className.StartsWith("WMP9MediaBarFlyout"))//WMP's "now playing" taskbar-toolbar
                {
                    return false;
                }

                return true;
            }
            return false;
        }

        public bool isDialogBox(IntPtr hwnd, string windowTitle)
        {
            if (windowTitle == "Jump List")
            {
                return true;
            }
            if (hwnd == taskbarHandle)
            {
                return false;
            }

            if (windowTitle == "Program Manager")
            {
                return false;
            }


            if (!Win32Funcs.IsWindow(hwnd))
            {
                return false;
            }

            return true;
        }


        public bool LoopThroughWindows(IntPtr hWnd, IntPtr lParam)
        {
            string hwndString = IntPtrToString(hWnd);



            StringBuilder sb = new StringBuilder(10000);
            Win32Funcs.GetClassName(hWnd, sb, sb.Capacity);
            string windowTitle = sb.ToString();

            bool isAltTab = IsAltTabWindow(hWnd, windowTitle);

            if (isAltTab)
            {
                if (!tempWindowsFoundSoFar.ContainsKey(hwndString))
                {
                    thingsToAdd.Add(hWnd);
                }

                shouldKeep[hwndString] = true;
                return true;
            }



            bool isDialog = isDialogBox(hWnd, windowTitle);
            if (isDialog)
            {
                shouldKeep[hwndString] = true;
                if (Win32Funcs.IsWindowVisible(hWnd))
                {
                    if (!tempWindowsFoundSoFar.ContainsKey(hwndString))
                    {
                        thingsToAdd.Add(hWnd);
                    }
                }

                return true;
            }


            if (tempWindowsFoundSoFar.ContainsKey(hwndString))
            {
                thingsToRemove.Add(hWnd);
                shouldKeep[hwndString] = true;
                return true;
            }

            return true;
        }
    }
}