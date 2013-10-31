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
    public partial class dlgPreferences : Form
    {
        public dlgPreferences()
        {
            InitializeComponent();
        }

        private void dlgOptions_Load(object sender, EventArgs e)
        {
            pgd.SelectedObject = Properties.Settings.Default;
        }
        
        private void pgd_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Properties.Settings.Default.GetType().GetProperty(e.ChangedItem.Label).SetValue(Properties.Settings.Default, e.ChangedItem.Value, null);
        }

        private void dlgOptions_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
