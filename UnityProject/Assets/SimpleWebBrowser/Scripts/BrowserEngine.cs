using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using MessageLibrary;
using SharedMemory;

using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace SimpleWebBrowser
{



    public class BrowserEngine
    {
        private TcpClient _clientSocket;

        private SharedArray<byte> _mainTexArray;

        private Process _pluginProcess;

        private static Object sPixelLock;

        public Texture2D BrowserTexture = null;
        public bool Initialized = false;


        private bool _needToRunOnce = false;
        private string _runOnceJS = "";

        //Image buffer
        private byte[] _bufferBytes = null;
        private long _arraySize = 0;

        //TCP buffer
        const int READ_BUFFER_SIZE = 2048;
        private byte[] readBuffer = new byte[READ_BUFFER_SIZE];

        #region Status events

        public delegate void PageLoaded(string url);

        public event PageLoaded OnPageLoaded;

        #endregion


        #region Settings

        public int kWidth = 512;
        public int kHeight = 512;

        private string _sharedFileName;
        private int _port;
        private string _initialURL;
        private bool _enableWebRTC;

        #endregion

        #region Dialogs

        public delegate void JavaScriptDialog(string message, string prompt, DialogEventType type);

        public event JavaScriptDialog OnJavaScriptDialog;

        #endregion

        #region JSQuery

        public delegate void JavaScriptQuery(string message);

        public event JavaScriptQuery OnJavaScriptQuery;

        #endregion



        #region Init

        //A really hackish way to avoid thread error. Should be better way
        public bool ConnectTcp(out TcpClient tcp)
        {
            TcpClient ret = null;
            try
            {
                ret = new TcpClient("127.0.0.1", _port);
            }
            catch (Exception ex)
            {
                tcp = null;
                return false;
            }

            tcp = ret;
            return true;

        }


        public void InitPlugin(int width, int height, string sharedfilename, int port, string initialURL,bool enableWebRTC)
        {

            //Initialization (for now) requires a predefined path to PluginServer,
            //so change this section if you move the folder
            //Also change the path in deployment script.

#if UNITY_EDITOR_64
            string PluginServerPath = Application.dataPath + @"\SimpleWebBrowser\PluginServer\x64";
#else
#if UNITY_EDITOR_32
        string PluginServerPath = Application.dataPath + @"\SimpleWebBrowser\PluginServer\x86";
#else


        //HACK
        string AssemblyPath=System.Reflection.Assembly.GetExecutingAssembly().Location;
        //log this for error handling
        Debug.Log("Assembly path:"+AssemblyPath);

        AssemblyPath = Path.GetDirectoryName(AssemblyPath); //Managed
      
        AssemblyPath = Directory.GetParent(AssemblyPath).FullName; //<project>_Data
        AssemblyPath = Directory.GetParent(AssemblyPath).FullName;//required

        string PluginServerPath=AssemblyPath+@"\PluginServer";
#endif
#endif



            Debug.Log("Starting server from:" + PluginServerPath);

            kWidth = width;
            kHeight = height;



            _sharedFileName = sharedfilename;
            _port = port;
            _initialURL = initialURL;
            _enableWebRTC = enableWebRTC;

            if (BrowserTexture == null)
                BrowserTexture = new Texture2D(kWidth, kHeight, TextureFormat.BGRA32, false);



            sPixelLock = new object();


            string args = BuildParamsString();

            bool connected = false;
            while (!connected)
            {
                try
                {
                    _pluginProcess = new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            WorkingDirectory = PluginServerPath,
                            FileName = PluginServerPath + @"\SharedPluginServer.exe",
                            Arguments = args

                        }
                    };



                    _pluginProcess.Start();
                    Initialized = false;
                }
                catch (Exception ex)
                {
                    //log the file
                    Debug.Log("FAILED TO START SERVER FROM:" + PluginServerPath + @"\SharedPluginServer.exe");
                    throw;
                }

                connected = ConnectTcp(out _clientSocket);
            }



        }

        private string BuildParamsString()
        {
            string ret = kWidth.ToString() + " " + kHeight.ToString() + " ";
            ret = ret + _initialURL + " ";
            ret = ret + _sharedFileName + " ";
            ret = ret + _port.ToString();

            if (_enableWebRTC)
                ret = ret + " 1";
            else
                ret = ret + " 0";

            return ret;
        }

        #endregion



        #region SendEvents

        public void SendNavigateEvent(string url, bool back, bool forward)
        {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.Navigate,
                GenericType = BrowserEventType.Generic,
                NavigateUrl = url
            };

            if (back)
                ge.Type = GenericEventType.GoBack;
            else if (forward)
                ge.Type = GenericEventType.GoForward;

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = BrowserEventType.Generic
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }
        }

        public void SendShutdownEvent()
        {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.Shutdown,
                GenericType = BrowserEventType.Generic
            };

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = BrowserEventType.Generic
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }

        }

        public void SendDialogResponse(bool ok, string dinput)
        {
            DialogEvent de = new DialogEvent()
            {
                GenericType = BrowserEventType.Dialog,
                success = ok,
                input = dinput
            };

            EventPacket ep = new EventPacket
            {
                Event = de,
                Type = BrowserEventType.Dialog
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();

            //
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }

        }

        public void SendQueryResponse(string response)
        {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.JSQueryResponse,
                GenericType = BrowserEventType.Generic,
                JsQueryResponse = response
            };

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = BrowserEventType.Generic
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            //
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }
        }

        public void SendCharEvent(int character, KeyboardEventType type)
        {

            KeyboardEvent keyboardEvent = new KeyboardEvent()
            {
                Type = type,
                Key = character
            };
            EventPacket ep = new EventPacket()
            {
                Event = keyboardEvent,
                Type = BrowserEventType.Keyboard
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }
        }

        public void SendMouseEvent(MouseMessage msg)
        {

            EventPacket ep = new EventPacket
            {
                Event = msg,
                Type = BrowserEventType.Mouse
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }

        }

        public void SendExecuteJSEvent(string js)
        {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.ExecuteJS,
                GenericType = BrowserEventType.Generic,
                JsCode = js
            };

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = BrowserEventType.Generic
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }

        }

        public void SendPing()
        {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.Navigate, //could be any
                GenericType = BrowserEventType.Ping,

            };

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = BrowserEventType.Ping
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            //
            lock (_clientSocket.GetStream())
            {
                _clientSocket.GetStream().Write(b, 0, b.Length);
            }
        }



        #endregion


        #region Helpers

        /// <summary>
        /// Used to run JS on initialization, for example, to set CSS
        /// </summary>
        /// <param name="js">JS code</param>
       public void RunJSOnce(string js )
        {
            _needToRunOnce = true;
            _runOnceJS = js;
        }

        #endregion

        



        public void UpdateTexture()
        {

            if (Initialized)
            {


                if (_bufferBytes == null)
                {
                    long arraySize = _mainTexArray.Length;
                    Debug.Log("Memory array size:" + arraySize);
                    _bufferBytes = new byte[arraySize];
                }
                _mainTexArray.CopyTo(_bufferBytes, 0);

                lock (sPixelLock)
                {

                    BrowserTexture.LoadRawTextureData(_bufferBytes);
                    BrowserTexture.Apply();

                }

                SendPing();

                //execute run-once functions
                if (_needToRunOnce)
                {
                    SendExecuteJSEvent(_runOnceJS);
                    _needToRunOnce = false;
                }
            }
            else
            {
                try
                {
                    //GetProcesses does not work for x86.
                    //so we have just to wait while the plugin started.



                    //string processName = _pluginProcess.ProcessName;//could be InvalidOperationException
                    //foreach (System.Diagnostics.Process clsProcess in System.Diagnostics.Process.GetProcesses())
                    //{
                    // if (clsProcess.ProcessName.Contains(processName)) //HACK: on x86 we have: <process name> (32 bit) 
                    {
                        Thread.Sleep(200); //give it some time to initialize
                        try
                        {


                            //Connect
                           // _clientSocket = new TcpClient("127.0.0.1", _port);
                            //start listen
                            _clientSocket.GetStream()
                                .BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);

                            //init memory file
                            _mainTexArray = new SharedArray<byte>(_sharedFileName);

                            Initialized = true;
                        }
                        catch (Exception ex)
                        {
                            //SharedMem and TCP exceptions
                            Debug.Log("Exception on init:" + ex.Message + ".Waiting for plugin server");
                        }



                    }
                    //}
                }
                catch (Exception)
                {

                    //InvalidOperationException
                }

            }
        }

        //Receiver
        private void StreamReceiver(IAsyncResult ar)
        {
            int BytesRead;

            try
            {
                // Ensure that no other threads try to use the stream at the same time.
                lock (_clientSocket.GetStream())
                {
                    // Finish asynchronous read into readBuffer and get number of bytes read.
                    BytesRead = _clientSocket.GetStream().EndRead(ar);
                }
                MemoryStream mstr = new MemoryStream(readBuffer);
                BinaryFormatter bf = new BinaryFormatter();
                EventPacket ep = bf.Deserialize(mstr) as EventPacket;
                if (ep != null)
                {
                    //main handlers
                    if (ep.Type == BrowserEventType.Dialog)
                    {
                        DialogEvent dev = ep.Event as DialogEvent;
                        if (dev != null)
                        {
                            if (OnJavaScriptDialog != null)
                                OnJavaScriptDialog(dev.Message, dev.DefaultPrompt, dev.Type);
                        }
                    }
                    if (ep.Type == BrowserEventType.Generic)
                    {
                        GenericEvent ge = ep.Event as GenericEvent;
                        if (ge != null)
                        {
                            if (ge.Type == GenericEventType.JSQuery)
                            {
                                if (OnJavaScriptQuery != null)
                                    OnJavaScriptQuery(ge.JsQuery);
                            }
                        }

                        if (ge.Type == GenericEventType.PageLoaded)
                        {
                            if (OnPageLoaded != null)
                                OnPageLoaded(ge.NavigateUrl);
                        }
                    }
                }
                lock (_clientSocket.GetStream())
                {
                    // Start a new asynchronous read into readBuffer.
                    _clientSocket.GetStream()
                        .BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error reading from socket,waiting for plugin server to start...");
            }
        }


        public void Shutdown()
        {
            SendShutdownEvent();
            _clientSocket.Close();
        }
    }
}