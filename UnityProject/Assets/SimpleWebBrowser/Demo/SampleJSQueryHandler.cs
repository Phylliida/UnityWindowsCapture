using UnityEngine;
using System.Collections;
using SimpleWebBrowser;

public class SampleJSQueryHandler : MonoBehaviour {

    public WebBrowser MainBrowser;

    void Start()
    {
        MainBrowser.OnJSQuery += MainBrowser_OnJSQuery;
    }

    private void MainBrowser_OnJSQuery(string query)
    {
        Debug.Log("Javascript query:" + query);
        MainBrowser.RespondToJSQuery("My response: OK");
    }
}
