using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BibtexManager
{
    public partial class frmTools : Form
    {
        public frmTools()
        {
            InitializeComponent();
        }

        private void frmTools_Load(object sender, EventArgs e)
        {
            txtProcName.Text = Properties.Settings.Default.TexEditorProcName;
        }

        private void SendTextToWindow(string text)
        {
            IntPtr texWindow = Program.ProcessEx.GetMainWindowHandle(txtProcName.Text);
            if (texWindow.Equals(IntPtr.Zero)) return;
            Program.ProcessEx.SetActiveWindow(texWindow);
            Program.ProcessEx.SendChars(texWindow, text);
        }

        private void txtProcName_Leave(object sender, EventArgs e)
        {
            Properties.Settings.Default.TexEditorProcName = txtProcName.Text;
            Properties.Settings.Default.Save();
        }

        private void frmTools_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
