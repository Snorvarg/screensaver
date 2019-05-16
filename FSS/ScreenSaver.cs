using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Gma.System.MouseKeyHook;
using CefSharp.WinForms;
using CefSharp;
using System.IO;
using Microsoft.Win32;

namespace FSS
{
    // Stuff used:
    // https://www.c-sharpcorner.com/UploadFile/f9f215/how-to-minimize-your-application-to-system-tray-in-C-Sharp/
    // https://github.com/gmamaladze/globalmousekeyhook
    // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/how-to-make-thread-safe-calls-to-windows-forms-controls
    // https://github.com/cefsharp/CefSharp
    // 
    // Various sources for getting the loading of local files to work:
    //  OLD VERSION: https://thechriskent.com/2014/04/21/use-local-files-in-cefsharp/
    //  https://stackoverflow.com/questions/28697613/working-with-locally-built-web-page-in-cefsharp
    //  https://stackoverflow.com/questions/28697613/working-with-locally-built-web-page-in-cefsharp/47805353#47805353
    //  https://github.com/cefsharp/CefSharp/blob/ce38ed81f07213d4f9ae13c154801f82779e3818/CefSharp/CefCustomScheme.cs
    //  https://bbonczek.github.io/jekyll/update/2018/04/24/serve-content-in-cef-without-http-server.html
    //
    // Start when windows starts:
    //  https://stackoverflow.com/questions/674628/how-do-i-set-a-program-to-launch-at-startup
    // 
    // Installer: 
    // https://marketplace.visualstudio.com/items?itemName=visualstudioclient.MicrosoftVisualStudio2017InstallerProjects
    // https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net

    // TODO: https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-prepare
    //  <-Specifikt: Läs inte in filer från Current Working Directory, det kanske inte fungerar.. Förstår inte exakt vad de menar.

    // NOTE: Se projektet WappPackagingProject, och guiden https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net
    // 
    // Right-click your project name in Solution Explorer and choose Store->Associate App with the Store.
    //  Once that is done, select Store->Create App Packages.
    //  This will, after some initial fiddling, start the Windows App Certification Kit, which is creating a big heap
    //  of more work for you. :-) 
    //
    // About the Package.appxmanifest and the automatic image creation:
    //   It's not always working, generating images larger than allowed. The solution is to manually decrease the file size, 
    //   they are png, so create a "Indexed color" version replacing the file. I guess decreasing the resolution is not an option.
    // 
    // Måste fixas: 
    //      The binary Gma.System.MouseKeyHook.dll is built in debug mode.
    //      File libcef.dll contains a reference to a "Launch Process" related API kernel32.dll!CreateProcessA
    //      File Fancy Screen Saver\FSS.exe contains a reference to a "Launch Process" related API System.Diagnostics.Process.Start
    //      ...mfl! Det är 10 olika sådana här fel. 

    // During development and testing: 
    //   "Sideload apps" must be enabled in windows 10 settings, 'For developers' section in 'Update & Security'.
    // Funkade inte, men innehåller mer läsvärt:
    //  https://stackoverflow.com/questions/23812471/installing-appx-without-trusted-certificate

    public enum States
    {
        None,
        Settings,       // När man startar upp första gången samt från system tray.
        Minimized,
        Running         // Skärmsläckaren har startat.
    }

    public partial class ScreenSaver : Form
    {
        protected IKeyboardMouseEvents m_GlobalHook;
        protected DateTime lastChange = DateTime.Now;
        protected System.Timers.Timer aTimer;

        private delegate void SafeCallDelegate();

        protected States State = States.None;
        protected string StartupArgs = "";

        protected string regPath = "HKEY_CURRENT_USER\\Software\\MadskullCreations\\";
        protected string appName = "FancyScreenSaver";

        public double startScreenSaverAfterMinutes = 1;
        public bool startWhenWindowsStart = true;

        protected ChromiumWebBrowser browser;

        public ScreenSaver(string arg)
        {
            InitializeComponent();

            // When starting for the first time these values don't yet exist, so create them.
            CreateRegistryEntry();

            FetchSettingsFromRegistry();

#if DEBUG
            this.TopMost = false;
            this.WindowState = FormWindowState.Normal;
#else
            this.TopMost = true;
            this.WindowState = FormWindowState.Maximized;
#endif

            CefSharp.Cef.EnableHighDPISupport();

            MySchemeHandler mySchemeHandler = new MySchemeHandler();

            var settings = new CefSettings();
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "app",     // Make sure any call to "app://local/blargh.html" trigger MySchemeHandler!
                SchemeHandlerFactory = mySchemeHandler,
                IsSecure = true     //treated with the same security rules as those applied to "https" URLs
            });
            CefSharp.Cef.Initialize(settings);

            // browser = new ChromiumWebBrowser("www.google.com");
            browser = new ChromiumWebBrowser("app://local/index.html");
            daPanel.Controls.Add(browser);

            // NOTE: This is fixing a bug in the chromium browser, if its parent window are not shown briefly/codewise, some events does not happen
            // as it appear, resulting in the page not loading as expected.
            this.Show();

            StartupArgs = arg;
            // With or without any arguments, this will transit the screensaver into one of its states.
            SetStartupArgs(StartupArgs);

            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += ATimer_Elapsed;
            aTimer.Interval = 5000;
            aTimer.Enabled = true;

            // When the app are in the background it listens for keyboard and mouse changes. 
            // If no changes has happened for a time, the aTimer might decide to popup the screensaver window.
            SubscribeToMouseKeyboardEvents();
        }

        /**
         * Handle the app args given at startup. (This is almost always "/m" as when windows starts and boot up the screensaver)
         */
        protected void SetStartupArgs(string arg)
        {
#if DEBUG
            //arg = "/m";
#endif

            StartupArgs = arg;

            switch (StartupArgs)
            {
                case "/m":
                    {
                        // Start minimized, as when windows starts.
                        SetSState(States.Minimized);
                        break;
                    }
                case "/f":
                    {
                        // Start in fullscreen mode. Like, only in debug mode.
                        SetSState(States.Running);
                        break;
                    }
                default:
                    {
                        // No argument given, so startup in settings-mode.
                        SetSState(States.Settings);
                        break;
                    }
            }
        }

        /**
         * Transit from current state into the newState, if the transition are allowed.
         */
        public void SetSState(States newState)
        {
            switch(State)
            {
                case States.None:
                    // App has just started. It can transit into any state.
                    if (newState == States.Running)
                    {
                        TransitIntoRunning();
                    }
                    else if (newState == States.Settings)
                    {
                        TransitIntoSettings();
                    }
                    else if (newState == States.Minimized)
                    {
                        TransitIntoMinimized(false); // Don't show notification bubble during startup.
                    }
                    break;
                case States.Minimized:
                    // App is minimized. It can go fullscreen or open its setting-screen.
                    if (newState == States.Running)
                    {
                        TransitIntoRunning();
                    }
                    else if (newState == States.Settings)
                    {
                        TransitIntoSettings();
                    }
                    break;
                case States.Settings:
                    // From settings it can minimize or go fullscreen.
                    if (newState == States.Running)
                    {
                        TransitIntoRunning();
                    }
                    else if (newState == States.Minimized)
                    {
                        TransitIntoMinimized();
                    }
                    break;
                case States.Running:
                    // From fullscreen it can minimize or show settings.
                    if (newState == States.Settings)
                    {
                        TransitIntoSettings();
                    }
                    else if (newState == States.Minimized)
                    {
                        TransitIntoMinimized();
                    }
                    break;
            }

            SetMessage(State.ToString());
        }

        protected void TransitIntoMinimized(bool showNotification = true)
        {
            // Change WindowState, then hide it.
            this.WindowState = FormWindowState.Minimized;
            Hide();

            if (showNotification)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(1000);
            }

            State = States.Minimized;
        }
        protected void TransitIntoRunning()
        {
            // Reload page to start/restart the screensaver code.
            // Fixed: Main reason for doing this is; if we come from settings form, the page has not been loaded properly.
            //       It seem to be a bug in browser, it need it's parent window to be visible, if only for a brief period.
            //browser.Load("app://local/index.html");

            // Note: Important to show the window before changing WindowState.
            this.Show();
#if DEBUG
            this.WindowState = FormWindowState.Normal;
#else
            this.WindowState = FormWindowState.Maximized;
#endif

            notifyIcon1.Visible = false;

            State = States.Running;
        }
        protected void TransitIntoSettings()
        {
            // Change WindowState, then hide it.
            this.WindowState = FormWindowState.Minimized;
            Hide();

            SettingsForm sform = new SettingsForm(this);
            sform.Show();

            notifyIcon1.Visible = false;

            State = States.Settings;
        }

        /**
         * Adds (or removes) the app to the "run at startup of windows" registry key.
         * 
         */
        public void SetStartupInRegistry(bool doStartup)
        {
            startWhenWindowsStart = doStartup;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if(doStartup)
            {
                // It should start minimized when windows starts.
                rk.SetValue(appName, Application.ExecutablePath + " /m");

                string keyName = regPath + appName;
                Registry.SetValue(keyName, "startWhenWindowsStart", 1);
            }
            else
            {
                rk.DeleteValue(appName, false);

                string keyName = regPath + appName;
                Registry.SetValue(keyName, "startWhenWindowsStart", 0);
            }
        }
        public void SetMinutesInRegistry(int minutes)
        {
            startScreenSaverAfterMinutes = minutes;

            string keyName = regPath + appName;
            Registry.SetValue(keyName, "minutesBeforeStart", minutes);
        }

        /**
         * Make sure a registry entry exists for the screen saver.
         * 
         */
        protected void CreateRegistryEntry()
        {
            string keyName = regPath + appName;
            string valueName = "Coffe";

            object val = Registry.GetValue(keyName, valueName, null);

            if (val == null)
            {
                // The coffe is missing, which is critical. Restore order. (app has probably just got installed, so set default values)
                Registry.SetValue(keyName, valueName, "always");
                Registry.SetValue(keyName, "minutesBeforeStart", 10);
                Registry.SetValue(keyName, "startWhenWindowsStart", 1);

                SetStartupInRegistry(true);
            }
        }
        protected void FetchSettingsFromRegistry()
        {
            string keyName = regPath+ appName;
            startScreenSaverAfterMinutes = (int)Registry.GetValue(keyName, "minutesBeforeStart", 10);
            int slorf = (int)Registry.GetValue(keyName, "startWhenWindowsStart", 1);

            startWhenWindowsStart = false;
            if (slorf == 1)
                startWhenWindowsStart = true;
        }

        private void ATimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(State == States.Minimized)
            {
                DateTime someTimeAgo = DateTime.Now;
                someTimeAgo = someTimeAgo.Subtract(TimeSpan.FromSeconds(startScreenSaverAfterMinutes * 60));
                if(lastChange < someTimeAgo)
                {
                    ShowScreenSaver();
                }
            }
        }

        // Invoking since we call this from a timer, which is another thread.
        protected void ShowScreenSaver()
        {
            if (this.InvokeRequired)
            {
                var d = new SafeCallDelegate(ShowScreenSaver);
                Invoke(d, new object[] {});
            }
            else
            {
                Console.WriteLine("Pickaboo!");

                SetSState(States.Running);
            }
        }

        protected void SetMessage(string message)
        {
            this.lblMessage.Text = message;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Unsubscribe();
        }

        protected void SubscribeToMouseKeyboardEvents()
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            m_GlobalHook = Hook.GlobalEvents();

            m_GlobalHook.MouseDownExt += GlobalHookMouseDownExt;
            m_GlobalHook.KeyPress += GlobalHookKeyPress;
        }

        public void Unsubscribe()
        {
            m_GlobalHook.MouseDownExt -= GlobalHookMouseDownExt;
            m_GlobalHook.KeyPress -= GlobalHookKeyPress;
                        //It is recommened to dispose it
            m_GlobalHook.Dispose();
        }

        private void GlobalHookKeyPress(object sender, KeyPressEventArgs e)
        {
            //Console.WriteLine("KeyPress: \t{0}", e.KeyChar);
            lastChange = DateTime.Now;
        }

        private void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        {
            //Console.WriteLine("MouseDown: \t{0}; \t System Timestamp: \t{1}", e.Button, e.Timestamp);

            // uncommenting the following line will suppress the middle mouse button click
            // if (e.Buttons == MouseButtons.Middle) { e.Handled = true; }

            lastChange = DateTime.Now;
        }

        /**
         * The notify icon in windows bottom-right notification area got double-clicked. Show the settings window.
         */
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SetSState(States.Settings);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure? Minimize to system tray to keep screensaver running.", "Click Yes to quit",MessageBoxButtons.YesNo);

            if (confirmResult != DialogResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            SetSState(States.Minimized);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            SetSState(States.Settings);
        }
    }

    /**
     * Extracts "index.html" and try to load the file from the local folder when the uri looks like "app://local/index.html".
     * 
     */
    public class MySchemeHandler : ISchemeHandlerFactory
    {
        public MySchemeHandler()
        {
        }

        private string get_content(Uri uri, out string extension)
        {
            var path = uri.LocalPath.Substring(1);
            path = string.IsNullOrWhiteSpace(path) ? "" : path;

            string appPath = Path.GetDirectoryName(Application.ExecutablePath);
            string fullPath = Path.Combine(appPath, path);

            extension = Path.GetExtension(path);

            return File.ReadAllText(fullPath);
        }

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            var uri = new Uri(request.Url);
            return ResourceHandler.FromString(get_content(uri, out var extension), extension);
        }
    }

    // Not used.
    public class MyResourceHandlerFactory : IResourceHandlerFactory
    {
        public bool HasHandlers
        {
            get
            {
                return true;
            }
        }

        public IResourceHandler GetResourceHandler(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)
        {
            return new MyResourceHandler();
        }
    }

    // ProcessRequest() returns true, and that makes the magic.
    public class MyResourceHandler : IResourceHandler
    {
        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public bool CanGetCookie(Cookie cookie)
        {
            throw new NotImplementedException();
        }

        public bool CanSetCookie(Cookie cookie)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void GetResponseHeaders(IResponse response, out long responseLength, out string redirectUrl)
        {
            throw new NotImplementedException();
        }

        public bool ProcessRequest(IRequest request, ICallback callback)
        {
            return true;
        }

        public bool ReadResponse(Stream dataOut, out int bytesRead, ICallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
