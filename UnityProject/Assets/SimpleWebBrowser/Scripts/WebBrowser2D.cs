using System;
using UnityEngine;
using System.Collections;
using System.Text;
//using System.Diagnostics;
using MessageLibrary;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleWebBrowser
{




    public class WebBrowser2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler
    {

        #region General

        [Header("General settings")] public int Width = 1024;

        public int Height = 768;

        public string MemoryFile = "MainSharedMem";

        public bool RandomMemoryFile = true;

        [Range(8000f, 9000f)] public int Port = 8885;

        public bool RandomPort = true;

        public string InitialURL = "http://www.google.com";

        public bool EnableWebRTC = false;

        [Multiline]
        public string JSInitializationCode = "";

        #endregion


        [Header("2D setup")]
        [SerializeField]
        public RawImage Browser2D = null;


        [Header("UI settings")]
        [SerializeField]
        public BrowserUI mainUIPanel;

        public bool KeepUIVisible = false;

        [Header("Dialog settings")]
        [SerializeField]
        public GameObject DialogPanel;
        [SerializeField]
        public Text DialogText;
        [SerializeField]
        public Button OkButton;
        [SerializeField]
        public Button YesButton;
        [SerializeField]
        public Button NoButton;
        [SerializeField]
        public InputField DialogPrompt;

        //dialog states - threading
        private bool _showDialog = false;
        private string _dialogMessage = "";
        private string _dialogPrompt = "";
        private DialogEventType _dialogEventType;
        //query - threading
        private bool _startQuery = false;
        private string _jsQueryString = "";

        //status - threading
        private bool _setUrl = false;
        private string _setUrlString = "";

        //input
        //private GraphicRaycaster _raycaster;
        //private StandaloneInputModule _input;

        #region JS Query events

        public delegate void JSQuery(string query);

        public event JSQuery OnJSQuery;

        #endregion


        private Material _mainMaterial;





        private BrowserEngine _mainEngine;



        private bool _focused = false;


        private int posX = 0;
        private int posY = 0;

        private Camera _mainCamera;

        #region Initialization

        //why Unity does not store the links in package?
        void InitPrefabLinks()
        {
            if (Browser2D == null)
                Browser2D = gameObject.GetComponent<RawImage>();
            if (mainUIPanel == null)
                mainUIPanel = gameObject.transform.FindChild("MainUI").gameObject.GetComponent<BrowserUI>();
            if (DialogPanel == null)
                DialogPanel = gameObject.transform.FindChild("MessageBox").gameObject;
            if (DialogText == null)
                DialogText = DialogPanel.transform.FindChild("MessageText").gameObject.GetComponent<Text>();
            if (OkButton == null)
                OkButton = DialogPanel.transform.FindChild("OK").gameObject.GetComponent<Button>();
            if (YesButton == null)
                YesButton = DialogPanel.transform.FindChild("Yes").gameObject.GetComponent<Button>();
            if (NoButton == null)
                NoButton = DialogPanel.transform.FindChild("No").gameObject.GetComponent<Button>();
            if (DialogPrompt == null)
                DialogPrompt = DialogPanel.transform.FindChild("Prompt").gameObject.GetComponent<InputField>();

        }


        void Awake()
        {
            _mainEngine = new BrowserEngine();

            if (RandomMemoryFile)
            {
                Guid memid = Guid.NewGuid();
                MemoryFile = memid.ToString();
            }
            if (RandomPort)
            {
                System.Random r = new System.Random();
                Port = 8000 + r.Next(1000);
            }
            
            _mainEngine.InitPlugin(Width, Height, MemoryFile, Port, InitialURL,EnableWebRTC);
            //run initialization
            if (JSInitializationCode.Trim() != "")
                _mainEngine.RunJSOnce(JSInitializationCode);
        }


        void Start()
        {
            InitPrefabLinks();
            mainUIPanel.InitPrefabLinks();

            _mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

            Browser2D.texture = _mainEngine.BrowserTexture;
            Browser2D.uvRect = new Rect(0f, 0f, 1f, -1f);






            // _mainInput = MainUrlInput.GetComponent<Input>();
            mainUIPanel.KeepUIVisible = KeepUIVisible;
            if (!KeepUIVisible)
                mainUIPanel.Hide();

            //attach dialogs and querys
            _mainEngine.OnJavaScriptDialog += _mainEngine_OnJavaScriptDialog;
            _mainEngine.OnJavaScriptQuery += _mainEngine_OnJavaScriptQuery;
            _mainEngine.OnPageLoaded += _mainEngine_OnPageLoaded;

            DialogPanel.SetActive(false);



        }

        private void _mainEngine_OnPageLoaded(string url)
        {
            _setUrl = true;
            _setUrlString = url;
        }

        #endregion

        #region Queries and dialogs

        //make it thread-safe
        private void _mainEngine_OnJavaScriptQuery(string message)
        {
            _jsQueryString = message;
            _startQuery = true;
        }

        public void RespondToJSQuery(string response)
        {
            _mainEngine.SendQueryResponse(response);
        }

        private void _mainEngine_OnJavaScriptDialog(string message, string prompt, DialogEventType type)
        {
            _showDialog = true;
            _dialogEventType = type;
            _dialogMessage = message;
            _dialogPrompt = prompt;

        }

        private void ShowDialog()
        {

            switch (_dialogEventType)
            {
                case DialogEventType.Alert:
                {
                    DialogPanel.SetActive(true);
                    OkButton.gameObject.SetActive(true);
                    YesButton.gameObject.SetActive(false);
                    NoButton.gameObject.SetActive(false);
                    DialogPrompt.text = "";
                    DialogPrompt.gameObject.SetActive(false);
                    DialogText.text = _dialogMessage;
                    break;
                }
                case DialogEventType.Confirm:
                {
                    DialogPanel.SetActive(true);
                    OkButton.gameObject.SetActive(false);
                    YesButton.gameObject.SetActive(true);
                    NoButton.gameObject.SetActive(true);
                    DialogPrompt.text = "";
                    DialogPrompt.gameObject.SetActive(false);
                    DialogText.text = _dialogMessage;
                    break;
                }
                case DialogEventType.Prompt:
                {
                    DialogPanel.SetActive(true);
                    OkButton.gameObject.SetActive(false);
                    YesButton.gameObject.SetActive(true);
                    NoButton.gameObject.SetActive(true);
                    DialogPrompt.text = _dialogPrompt;
                    DialogPrompt.gameObject.SetActive(true);
                    DialogText.text = _dialogMessage;
                    break;
                }
            }
            _showDialog = false;
        }

        public void DialogResult(bool result)
        {
            DialogPanel.SetActive(false);
            _mainEngine.SendDialogResponse(result, DialogPrompt.text);

        }

        public void RunJavaScript(string js)
        {
            _mainEngine.SendExecuteJSEvent(js);
        }

        #endregion

        #region UI

        public void OnNavigate()
        {
            // MainUrlInput.isFocused
            _mainEngine.SendNavigateEvent(mainUIPanel.UrlField.text, false, false);

        }

        public void GoBackForward(bool forward)
        {
            if (forward)
                _mainEngine.SendNavigateEvent("", false, true);
            else
                _mainEngine.SendNavigateEvent("", true, false);
        }

        #endregion




        #region Events 

        public void OnPointerEnter(PointerEventData data)
        {
            _focused = true;
            mainUIPanel.Show();
            StartCoroutine("TrackPointer");
        }

        public void OnPointerExit(PointerEventData data)
        {
            _focused = false;
            mainUIPanel.Hide();
            StopCoroutine("TrackPointer");
        }

        //tracker
        IEnumerator TrackPointer()
        {
            var _raycaster = GetComponentInParent<GraphicRaycaster>();
            var _input = FindObjectOfType<StandaloneInputModule>();

            if (_raycaster != null && _input != null && _mainEngine.Initialized)
            {
                while (Application.isPlaying)
                {
                    Vector2 localPos = GetScreenCoords(_raycaster, _input);

                    int px = (int) localPos.x;
                    int py = (int) localPos.y;

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
                        _mainEngine.SendMouseEvent(msg);
                    }

                    yield return 0;
                }
            }
            //  else
            //      UnityEngine.Debug.LogWarning("Could not find GraphicRaycaster and/or StandaloneInputModule");
        }

        public void OnPointerDown(PointerEventData data)
        {

            if (_mainEngine.Initialized)
            {
                var _raycaster = GetComponentInParent<GraphicRaycaster>();
                var _input = FindObjectOfType<StandaloneInputModule>();
                Vector2 pixelUV = GetScreenCoords(_raycaster, _input);

                switch (data.button)
                {
                    case PointerEventData.InputButton.Left:
                    {
                        SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Left,
                            MouseEventType.ButtonDown);
                        break;
                    }
                    case PointerEventData.InputButton.Right:
                    {
                        SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Right,
                            MouseEventType.ButtonDown);
                        break;
                    }
                    case PointerEventData.InputButton.Middle:
                    {
                        SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Middle,
                            MouseEventType.ButtonDown);
                        break;
                    }
                }


            }

        }




        public void OnPointerUp(PointerEventData data)
        {

            if (_mainEngine.Initialized)
            {
                var _raycaster = GetComponentInParent<GraphicRaycaster>();
                var _input = FindObjectOfType<StandaloneInputModule>();

                Vector2 pixelUV = GetScreenCoords(_raycaster, _input);

                switch (data.button)
                {
                    case PointerEventData.InputButton.Left:
                    {
                        SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Left, MouseEventType.ButtonUp);
                        break;
                    }
                    case PointerEventData.InputButton.Right:
                    {
                        SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Right,
                            MouseEventType.ButtonUp);
                        break;
                    }
                    case PointerEventData.InputButton.Middle:
                    {
                        SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Middle,
                            MouseEventType.ButtonUp);
                        break;
                    }
                }


            }

        }



        #endregion

        #region Helpers

        private Vector2 GetScreenCoords(GraphicRaycaster ray, StandaloneInputModule input)
        {

            Vector2 localPos; // Mouse position  
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, Input.mousePosition,
                ray.eventCamera, out localPos);

            // local pos is the mouse position.
            RectTransform trns = transform as RectTransform;
            localPos.y = trns.rect.height - localPos.y;
            //Debug.Log("x:"+localPos.x+",y:"+localPos.y);

            //now recalculate to texture
            localPos.x = (localPos.x*Width)/trns.rect.width;
            localPos.y = (localPos.y*Height)/trns.rect.height;

            return localPos;

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
            _mainEngine.SendMouseEvent(msg);
        }

        private void ProcessScrollInput(int px, int py)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            scroll = scroll*_mainEngine.BrowserTexture.height;

            int scInt = (int) scroll;

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

                _mainEngine.SendMouseEvent(msg);
            }
        }

        #endregion

        // Update is called once per frame
        void Update()
        {

            _mainEngine.UpdateTexture();

            #region 2D mouse

            if (Browser2D != null)
            {
                //GetScreenCoords(true);
            }


            #endregion

            //Dialog
            if (_showDialog)
            {
                ShowDialog();
            }

            //Query
            if (_startQuery)
            {
                _startQuery = false;
                if (OnJSQuery != null)
                    OnJSQuery(_jsQueryString);
            }

            //Status
            if (_setUrl)
            {
                _setUrl = false;
                mainUIPanel.UrlField.text = _setUrlString;

            }



            if (_focused && !mainUIPanel.UrlField.isFocused) //keys
            {
                foreach (char c in Input.inputString)
                {
                    _mainEngine.SendCharEvent((int) c, KeyboardEventType.CharKey);


                }
                ProcessKeyEvents();


            }

        }

        #region Keys

        private void ProcessKeyEvents()
        {
            foreach (KeyCode k in Enum.GetValues(typeof (KeyCode)))
            {
                CheckKey(k);
            }

        }

        private void CheckKey(KeyCode code)
        {
            if (Input.GetKeyDown(code))
                _mainEngine.SendCharEvent((int) code, KeyboardEventType.Down);
            if (Input.GetKeyUp(KeyCode.Backspace))
                _mainEngine.SendCharEvent((int) code, KeyboardEventType.Up);
        }

        #endregion

        void OnDisable()
        {
            _mainEngine.Shutdown();
        }


    }
}