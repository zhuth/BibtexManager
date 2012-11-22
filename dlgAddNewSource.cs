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
    public partial class dlgAddNewSource : Form
    {
        public dlgAddNewSource()
        {
            InitializeComponent();
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        }

        public string GetSource()
        {
            return textBox1.Text;
        }

        private void dlgAddNewSource_Load(object sender, EventArgs e)
        {
            this.textBox1.Text = Clipboard.GetText();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
