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

namespace FSS
{
    // Stuff used:
    // https://www.c-sharpcorner.com/UploadFile/f9f215/how-to-minimize-your-application-to-system-tray-in-C-Sharp/
    // https://github.com/gmamaladze/globalmousekeyhook
    // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/how-to-make-thread-safe-calls-to-windows-forms-controls
    // https://github.com/cefsharp/CefSharp

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

            var settings = new CefSettings();
            CefSharp.Cef.Initialize(settings);

            browser = new ChromiumWebBrowser("www.google.com");
            daPanel.Controls.Add(browser);

            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += ATimer_Elapsed;
            aTimer.Interval = 5000;
            aTimer.Enabled = true;

            Subscribe();

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
                this.WindowState = FormWindowState.Maximized;
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
            this.WindowState = FormWindowState.Maximized;
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
}
