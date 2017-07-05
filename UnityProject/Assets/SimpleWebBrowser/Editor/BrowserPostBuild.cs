using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class BrowserPostBuild
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string path)
    {
        Debug.Log("Post Build:"+Path.GetDirectoryName(path));
       // File.
      

        switch (target)
        {
            case BuildTarget.StandaloneWindows64:
            {
                    if (!Directory.Exists(Path.GetDirectoryName(path) + @"\PluginServer"))
                        Directory.CreateDirectory(Path.GetDirectoryName(path) + @"\PluginServer");
                    
                string[] files=Directory.GetFiles(Application.dataPath+ @"\SimpleWebBrowser\PluginServer\x64");
                foreach (var file in files)
                {
                    if (!file.Contains("meta"))
                    {
                            if(!File.Exists(Path.GetDirectoryName(path) + @"\PluginServer\" + Path.GetFileName(file)))
                        FileUtil.CopyFileOrDirectory(file, Path.GetDirectoryName(path) + @"\PluginServer\"+Path.GetFileName(file));
                    }
                }
                    Directory.CreateDirectory(Path.GetDirectoryName(path) + @"\PluginServer\locales");
                    files = Directory.GetFiles(Application.dataPath + @"\SimpleWebBrowser\PluginServer\x64\locales");
                    foreach (var file in files)
                    {
                        if (!file.Contains("meta"))
                        {
                            if (!File.Exists(Path.GetDirectoryName(path) + @"\PluginServer\locales\" + Path.GetFileName(file)))
                                FileUtil.CopyFileOrDirectory(file, Path.GetDirectoryName(path) + @"\PluginServer\locales\" + Path.GetFileName(file));
                        }
                    }

                    break;
            }
            case BuildTarget.StandaloneWindows:
            {
                    if (!Directory.Exists(Path.GetDirectoryName(path) + @"\PluginServer"))
                        Directory.CreateDirectory(Path.GetDirectoryName(path) + @"\PluginServer");

                    string[] files = Directory.GetFiles(Application.dataPath + @"\SimpleWebBrowser\PluginServer\x86");
                    foreach (var file in files)
                    {
                        if (!file.Contains("meta"))
                        {
                            if (!File.Exists(Path.GetDirectoryName(path) + @"\PluginServer\" + Path.GetFileName(file)))
                                FileUtil.CopyFileOrDirectory(file, Path.GetDirectoryName(path) + @"\PluginServer\" + Path.GetFileName(file));
                        }
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path) + @"\PluginServer\locales");
                    files = Directory.GetFiles(Application.dataPath + @"\SimpleWebBrowser\PluginServer\x86\locales");
                    foreach (var file in files)
                    {
                        if (!file.Contains("meta"))
                        {
                            if (!File.Exists(Path.GetDirectoryName(path) + @"\PluginServer\locales\" + Path.GetFileName(file)))
                                FileUtil.CopyFileOrDirectory(file, Path.GetDirectoryName(path) + @"\PluginServer\locales\" + Path.GetFileName(file));
                        }
                    }
                    break;
            }
            default:
                Debug.LogError("Web browser is not supported on this platform!");
                break;
        }
    }
}
