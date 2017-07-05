using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace SimpleWebBrowser
{



    public class BrowserUI : MonoBehaviour
    {
        [SerializeField]
        public Canvas MainCanvas = null;
        [SerializeField]
        public InputField UrlField;
        [SerializeField]
        public Image Background;
        [SerializeField]
        public Button Back;
        [SerializeField]
        public Button Forward;


        [HideInInspector] public bool KeepUIVisible = false;


        public void InitPrefabLinks()
        {
            //3D
            if (MainCanvas == null)
                MainCanvas = gameObject.GetComponent<Canvas>();

            if (UrlField == null)
                UrlField = gameObject.transform.FindChild("UrlField").GetComponent<InputField>();
            if (Background == null)
            {
                //2d
                Background = gameObject.GetComponent<Image>();
                //3d
                if (Background == null)
                    Background = gameObject.transform.FindChild("Background").gameObject.GetComponent<Image>();
            }
            if (Back == null)
                Back = gameObject.transform.FindChild("Back").gameObject.GetComponent<Button>();
            if (Forward == null)
                Forward = gameObject.transform.FindChild("Forward").gameObject.GetComponent<Button>();
        }




        public void Show()
        {
            UrlField.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
            UrlField.placeholder.gameObject.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
            UrlField.textComponent.gameObject.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
            Back.gameObject.SetActive(true);
            Forward.gameObject.SetActive(true);
            Background.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
        }

        public void Hide()
        {
            if (!KeepUIVisible)
            {
                if (!UrlField.isFocused)
                {
                    UrlField.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
                    UrlField.placeholder.gameObject.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
                    UrlField.textComponent.gameObject.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
                    Back.gameObject.SetActive(false);
                    Forward.gameObject.SetActive(false);
                    Background.GetComponent<CanvasRenderer>().SetAlpha(0.0f);
                }
                else
                {
                    Show();
                }
            }
        }




        void Update()
        {
            if (UrlField.isFocused && !KeepUIVisible)
            {
                Show();
            }
        }


    }
}
