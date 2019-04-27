using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

namespace FSS
{
    public partial class SettingsForm : Form
    {
        ScreenSaver ScreenSaverForm;

        public SettingsForm(ScreenSaver ssf)
        {
            InitializeComponent();

            ScreenSaverForm = ssf;

            chkBoxStartWhenWindowsStarts.Checked = ScreenSaverForm.startWhenWindowsStart;
            numericUpDownMinutesBeforeStart.Value = (int)ScreenSaverForm.startScreenSaverAfterMinutes;
        }

        private void btnEditSource_Click(object sender, EventArgs e)
        {
            string appPath = Path.GetDirectoryName(Application.ExecutablePath);
            string pathToJs = Path.Combine(appPath, "javascript.js");

            System.Diagnostics.Process.Start(pathToJs);
        }

        private void btnStartScreenSaver_Click(object sender, EventArgs e)
        {
            Close();
            ScreenSaverForm.SetSState(States.Running);
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            Close();
            ScreenSaverForm.SetSState(States.Minimized);
        }

        private void numericUpDownMinutesBeforeStart_ValueChanged(object sender, EventArgs e)
        {
            ScreenSaverForm.SetMinutesInRegistry((int)numericUpDownMinutesBeforeStart.Value);
        }

        private void chkBoxStartWhenWindowsStarts_CheckedChanged(object sender, EventArgs e)
        {
            bool startWhenWindowsStart = chkBoxStartWhenWindowsStarts.Checked;

            ScreenSaverForm.SetStartupInRegistry(startWhenWindowsStart);
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.Show();
        }
    }
}
