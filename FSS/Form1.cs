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

    public enum States
    {
        None,
        Settings,       // När man startar upp första gången samt från system tray.
        Minimized,
        Running         // Skärmsläckaren har startat.
    }

    public partial class Form1 : Form
    {
        protected IKeyboardMouseEvents m_GlobalHook;
        protected DateTime lastChange = DateTime.Now;
        protected System.Timers.Timer aTimer;

        private delegate void SafeCallDelegate();

        protected States State = States.None;

        protected double startScreenSaverAfterSeconds = 10;

        protected ChromiumWebBrowser browser;

        public Form1()
        {
            InitializeComponent();

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

            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += ATimer_Elapsed;
            aTimer.Interval = 5000;
            aTimer.Enabled = true;

            Subscribe();

#if DEBUG
            this.WindowState = FormWindowState.Normal;
            this.TopMost = false;
#else
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
#endif

            State = States.Settings;
            SetMessage(State.ToString());
        }

        private void ATimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                DateTime someTimeAgo = DateTime.Now;
                someTimeAgo = someTimeAgo.Subtract(TimeSpan.FromSeconds(startScreenSaverAfterSeconds));
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

                State = States.Running;
                SetMessage(State.ToString());

                this.Show();

#if DEBUG
                this.WindowState = FormWindowState.Normal;
#else
                this.WindowState = FormWindowState.Maximized;
#endif
                notifyIcon1.Visible = false;
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

        protected void Subscribe()
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

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                State = States.Minimized;
                SetMessage(State.ToString());

                Hide();
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(1000);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            State = States.Settings;
            SetMessage(State.ToString());

            Show();

#if DEBUG
            this.WindowState = FormWindowState.Normal;
#else
                this.WindowState = FormWindowState.Maximized;
#endif

            notifyIcon1.Visible = false;
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
            this.WindowState = FormWindowState.Minimized;
        }
    }

    /**
     * Extracts "index.html" and try to load the file from the local folder when the uri looks like "app://local/index.html".
     * 
     */
    public class MySchemeHandler : ISchemeHandlerFactory
    {
        private string scheme, host, folder, default_filename;

        public MySchemeHandler()
        {
            scheme = "";
            host = "";
            folder = "";
            default_filename = "";
        }

        private string get_content(Uri uri, out string extension)
        {
            var path = uri.LocalPath.Substring(1);
            path = string.IsNullOrWhiteSpace(path) ? this.default_filename : path;
            extension = Path.GetExtension(path);
            return File.ReadAllText(Path.Combine(this.folder, path));
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

    // Not used.
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

    /*public class LocalSchemeHandler : ISchemeHandler
    {
        public bool ProcessRequestAsync(IRequest request, ISchemeHandlerResponse response, OnRequestCompletedHandler requestCompletedCallback)
        {
            Uri u = new Uri(request.Url);
            String file = u.Authority + u.AbsolutePath;

            if (File.Exists(file))
            {
                Byte[] bytes = File.ReadAllBytes(file);
                response.ResponseStream = new MemoryStream(bytes);
                switch (Path.GetExtension(file))
                {
                    case ".html":
                        response.MimeType = "text/html";
                        break;
                    case ".js":
                        response.MimeType = "text/javascript";
                        break;
                    case ".png":
                        response.MimeType = "image/png";
                        break;
                    case ".appcache":
                    case ".manifest":
                        response.MimeType = "text/cache-manifest";
                        break;
                    default:
                        response.MimeType = "application/octet-stream";
                        break;
                }
                requestCompletedCallback();
                return true;
            }
            return false;
        }
    }*/
}
