# UnityWindowsCapture
A framework to capture individual windows or the entire desktop in Unity. This is a large part of the code I wrote to make [Multiscreens](http://store.steampowered.com/app/512400).

Right now this uses GDI in an optimized way to capture things on windows 7+. As long as you are careful about your capturing rates you can run at 40-50 fps.

I also have code for using the Desktop Capture API (Windows 8+) in here which seems to be what BigScreen and Virtual Desktop use to capture windows in realtime while running at 90 fps. See DesktopCapture.cs and usage in ExampleUsage.cs

See ExampleUsage.cs in the scene "Test" for an example of how to use this. I'll add a more detailed tutorial later as well.

Edit: I added [this code](https://bitbucket.org/vitaly_chashin/simpleunitybrowser) to this (all credit goes to Vitaly Chashin) which lets you simulate a web browser and capture its contents at a very fast rate, almost as fast as the Windows 8+ capture API. As far as I know this should work on windows 7+. You can have as many as you want at a time, it runs most websites great, and you can have windows with any resolution. [Here is a screen shot of it running](http://i.imgur.com/bYt5ndW.png)

Right now you can't interact with them you can only capture them and change their urls/refresh, but interacting with them is pretty easy to do (I already did it [for my train simulator](https://www.reddit.com/r/oculus/comments/6lbv0b/i_made_a_vr_train_simulator/)) so I'll add that in a day or two. I modified ExampleUsage.cs to show how to use this as well.

To use Chromium Capture you will need to go unzip UnityWindowsCapture\UnityProject\Assets\SimpleWebBrowser\PluginServer\x86\libcef.zip into that x86\ folder, and unzip UnityWindowsCapture\UnityProject\Assets\SimpleWebBrowser\PluginServer\x64\libcef.zip into that x64\ folder, simply because those dlls were too big so github got angry unless I compressed them
