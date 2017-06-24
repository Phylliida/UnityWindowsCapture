using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WinCapture;

public class ExampleUsage : MonoBehaviour {

    public Shader windowShader;
    WindowCaptureManager captureManager;

    public Dictionary<IntPtr, WindowCapture> windowsRendering;
    public Dictionary<IntPtr, GameObject> windowObjects;

    // Use this for initialization
    void Start () {
        windowsRendering = new Dictionary<IntPtr, WindowCapture>();
        windowObjects = new Dictionary<IntPtr, GameObject>();
        captureManager = new WindowCaptureManager();
        captureManager.OnAddWindow += OnAddWindow;
        captureManager.OnRemoveWindow += OnRemoveWindow;
        lastUpdateTime = Time.time;
        lastPollWindowsTime = Time.time;
    }
    

    bool IsGoodWindow(WindowCapture window)
    {
        Debug.Log("Saw window: " + window.windowInfo.title);
        // You can stick whatever logic or names you want here for windows you want to keep to render

        string windowLowerTitle = window.windowInfo.title.ToLower();
        if (windowLowerTitle.Contains("desktop"))
        {
            return true;
        }
        return false;
    }

    void OnAddWindow(WindowCapture window)
    {
        if (!windowsRendering.ContainsKey(window.hwnd) && IsGoodWindow(window))
        {
            GameObject windowObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            windowObject.name = window.windowInfo.title;
            windowObject.transform.GetComponent<Renderer>().material = new Material(windowShader);
            windowObject.transform.localEulerAngles = new Vector3(90, 0, 0);
            windowsRendering[window.hwnd] = window;
            windowObjects[window.hwnd] = windowObject;
        }
    }

    void OnRemoveWindow(WindowCapture window)
    {
        Debug.Log("removed " + window.windowInfo.title);
        if (windowsRendering.ContainsKey(window.hwnd))
        {
            GameObject windowObjectRemoving = windowObjects[window.hwnd];
            Destroy(windowObjectRemoving);
            windowObjects.Remove(window.hwnd);
            windowsRendering.Remove(window.hwnd);
        }
    }

    float lastUpdateTime;
    float lastPollWindowsTime;

    public float captureRateFps = 30;
    public float windowPollPerSecond = 2;
    public float windowScale = 0.001f;
    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastUpdateTime < 1.0f / captureRateFps)
        {
            return;
        }
        else
        {
            lastUpdateTime = Time.time;
        }

        // Gets information about position and icon of the cursor so you can render it onto the captured surfaces
        WindowCapture.UpdateCursorInfo();

        foreach (IntPtr key in windowsRendering.Keys)
        {
            WindowCapture window = windowsRendering[key];
            GameObject windowObject = windowObjects[key];

            bool didChange;
            Texture2D windowTexture = window.GetWindowTexture(out didChange);
            if (didChange)
            {
                windowObject.GetComponent<Renderer>().material.mainTexture = windowTexture;
            }
            windowObject.transform.localScale = new Vector3(window.windowWidth* windowScale, 0, window.windowHeight * windowScale);
        }

        if (Time.time - lastPollWindowsTime < 1.0f / windowPollPerSecond)
        {
            return;
        }
        else
        {
            lastPollWindowsTime = Time.time;
            // calls OnAddWindow or OnRemoveWindow above if any windows have been added or removed
            captureManager.Poll();
        }
    }
}
