using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using WinCapture;

namespace WinCapture
{
    public class WindowsHolder
    {
        public Dictionary<string, IntPtr> windows;

        WindowGetterWorker windowGetter;
        Thread windowGetterThread;

        public WindowsHolder()
        {
            windowGetter = new WindowGetterWorker();
            //windowGetterThread = new Thread(windowGetter.DoWork);
            //windowGetterThread.Start();

            windowTypes = new Dictionary<string, WindowType>();

            mapToString = new Dictionary<int, string>();
            windows = new Dictionary<string, IntPtr>();
        }


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

        public IntPtr rightClickMenuHandle = IntPtr.Zero;

        public delegate void AddWindow(IntPtr hwnd);

        public delegate void AddDialogBox(IntPtr hwnd);

        public delegate void RemoveWindow(IntPtr hwnd);

        public delegate void RemoveDialogBox(IntPtr hwnd);

        public event AddWindow OnAddWindow;
        public event RemoveWindow OnRemoveWindow;

        public event AddDialogBox OnAddDialogBox;
        public event RemoveDialogBox OnRemoveDialogBox;

        public enum WindowType
        {
            Window,
            DialogBox
        }

        public Dictionary<string, WindowType> windowTypes;

        public void UpdateWindows()
        {
            windowGetter.DoWork();
            List<IntPtr> actualThingsToAdd = new List<IntPtr>(10);
            List<IntPtr> actualThingsToRemove = new List<IntPtr>(10);

            lock (windowGetter.windowLock)
            {
                for (int i = 0; i < windowGetter.myThingsToAdd.Count; i++)
                {
                    string curKey = IntPtrToString(windowGetter.myThingsToAdd[i]);
                    if (!(windows.ContainsKey(curKey)))
                    {
                        actualThingsToAdd.Add(windowGetter.myThingsToAdd[i]);
                    }
                }
                for (int i = 0; i < windowGetter.myThingsToRemove.Count; i++)
                {
                    string curKey = IntPtrToString(windowGetter.myThingsToRemove[i]);
                    if (windows.ContainsKey(curKey))
                    {
                        actualThingsToRemove.Add(windowGetter.myThingsToRemove[i]);
                    }
                }

                windowGetter.myThingsToAdd.Clear();
                windowGetter.myThingsToRemove.Clear();

            }
            for (int i = 0; i < actualThingsToAdd.Count; i++)
            {
                //WindowThing windowAdding = null;
                IntPtr hwnd = actualThingsToAdd[i];
                string hwndString = IntPtrToString(hwnd);

                if (windows.ContainsKey(hwndString))
                {
                    continue;
                }

                StringBuilder sb = new StringBuilder(10000);
                Win32Funcs.GetClassName(hwnd, sb, sb.Capacity);
                string windowTitle = sb.ToString().Trim();


                if (windowGetter.IsAltTabWindow(hwnd, windowTitle))
                {
                    windowTypes[hwndString] = WindowType.Window;
                    //Win32window winWindowAdding = new Win32window(hwnd, baseManager);
                    //windowAdding = winWindowAdding;
                    //if (windowAdding.windowInfo.title == "Program Manager" && false)
                    //{
                    //temp windowAdding.windowObject.transform.position = baseManager.riftController.transform.position + baseManager.riftController.transform.forward * 50.0f;
                    //}
                    //else
                    //{
                    //    windowAdding.windowObject.transform.position = new Vector3(-100000, -100000, -100000);
                    //}




                    // Otherwise set it to default where you are looking as is done above once you focus on it


                    if (OnAddWindow != null)
                    {
                        OnAddWindow(hwnd);
                    }

                    windows[hwndString] = hwnd;

                    //bool setPosition = false;
                }
                else if (windowGetter.isDialogBox(hwnd, windowTitle))
                {
                    windowTypes[IntPtrToString(hwnd)] = WindowType.DialogBox;
                    //Win32dialogBox winDiagAdding = new Win32dialogBox(hwnd, baseManager, foregroundWindowThing);
                    //windowAdding = winDiagAdding;
                    if (OnAddDialogBox != null)
                    {
                        //OnAddDialogBox(hwnd, winDiagAdding);
                        OnAddDialogBox(hwnd);
                    }

                    windows[hwndString] = hwnd;
                }
                else
                {
                    continue;
                }
            }

            for (int i = 0; i < actualThingsToRemove.Count; i++)
            {
                IntPtr hwnd = actualThingsToRemove[i];
                string hwndString = IntPtrToString(hwnd);


                if (windows.ContainsKey(hwndString))
                {
                    windows.Remove(hwndString);
                    if (windowTypes[hwndString] == WindowType.Window)
                    {
                        if (OnRemoveWindow != null)
                        {
                            OnRemoveWindow(hwnd);
                        }
                    }
                    else if (windowTypes[hwndString] == WindowType.DialogBox)
                    {
                        if (OnRemoveDialogBox != null)
                        {
                            OnRemoveDialogBox(hwnd);
                        }
                    }


                    /////if (windowRemoving.foregroundWindowThing != null && windowRemoving.foregroundWindowThing.hwnd == windowGetter.taskbarHandle)
                    ////{ 
                    //windowRemoving.numChancesLeft -= 1;
                    //if (windowRemoving.numChancesLeft == 0) {
                    //windows.Remove (hwndString);
                    //windowRemoving.Destroy ();
                    //}
                    ///// }
                    /*
                     else
                     {
                         if (windowRemoving.GetType() == typeof(Win32window))
                         {
                             if (hwnd == BaseManager.foregroundWindow)
                             {
                                 //BaseManager.me.faceMouse.capturingClicks = true;
                             }
                             if (OnRemoveWindow != null)
                             {
                                 OnRemoveWindow(hwnd, (Win32window)windowRemoving);
                             }
                         }
                         else if (windowRemoving.GetType() == typeof(Win32dialogBox))
                         {
                             if (OnRemoveDialogBox != null)
                             {
                                 OnRemoveDialogBox(hwnd, (Win32dialogBox)windowRemoving);
                             }
                         }

                         windows.Remove(hwndString);
                         windowRemoving.Destroy();
                     }
                 */
                }
            }
        }
    }
}