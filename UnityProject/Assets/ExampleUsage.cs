using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WinCapture;

public class ExampleUsage : MonoBehaviour {

    public Shader windowShader;
    TexturesHolder texturesHolder;

    public Dictionary<IntPtr, GameObject> windowObjects;

	// Use this for initialization
	void Start () {
        windowObjects = new Dictionary<IntPtr, GameObject>();
        texturesHolder = new TexturesHolder();
        texturesHolder.OnAddWindow += OnAddWindow;
        texturesHolder.OnRemoveWindow += OnRemoveWindow;

        lastUpdateTime = Time.time;
    }


    void OnAddWindow(IntPtr hwnd)
    {
        if (!windowObjects.ContainsKey(hwnd))
        {
            GameObject windowObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Win32Types.WindowInfo windowInfo = new Win32Types.WindowInfo(hwnd);
            windowObject.name = windowInfo.title;
            windowObject.transform.GetComponent<Renderer>().material = new Material(windowShader);
            windowObject.transform.localEulerAngles = new Vector3(90, 0, 0);
            windowObjects[hwnd] = windowObject;
        }
    }

    void OnRemoveWindow(IntPtr hwnd)
    {
        if (windowObjects.ContainsKey(hwnd))
        {
            GameObject windowObjectRemoving = windowObjects[hwnd];
            Destroy(windowObjectRemoving);
            windowObjects.Remove(hwnd);
        }
    }

    float lastUpdateTime;
    public float fps = 30;
    public float windowScale = 0.001f;
	// Update is called once per frame
	void Update () {

        if (Time.time - lastUpdateTime < 1.0/fps)
        {
            return;
        }
        lastUpdateTime = Time.time;
        texturesHolder.Update();

        foreach(KeyValuePair<IntPtr, Texture2D> window in texturesHolder.textures)
        {
            IntPtr hwnd = window.Key;
            Texture2D texture = window.Value;
            if (windowObjects.ContainsKey(hwnd))
            {
                GameObject windowObject = windowObjects[hwnd];
                windowObject.GetComponent<Renderer>().material.mainTexture = texture;
                Win32Types.WindowInfo windowInfo = new Win32Types.WindowInfo(hwnd);
                float windowWidth = windowInfo.windowRect.Width * windowScale;
                float windowHeight = windowInfo.windowRect.Height * windowScale;
                windowObject.transform.localScale = new Vector3(windowWidth, 0.01f, windowHeight);
            }
        }
    }
}
