using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace BibtexManager
{
    public partial class Form1 : Form
    {
        BibHelper _bh = new BibHelper();
        SqliteHelper _sh = null;

        private bool _rendering = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = Properties.Resources.softwareName;
            foreach (var pair in _bh.BibTexFields)
            {
                if (pair.Key == "type")
                {
                    var colcombo = new DataGridViewComboBoxColumn();
                    colcombo.HeaderText = pair.Value; colcombo.Name = pair.Key;
                    foreach (var item in Properties.Resources.types.Split(';'))
                    {
                        colcombo.Items.Add(item);
                    }
                    colcombo.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                    dgv.Columns.Add(colcombo);
                }
                else
                {
                    dgv.Columns.Add(pair.Key, pair.Value);
                }
            }

            if (System.IO.File.Exists(Properties.Settings.Default.RecentLib))
            {
                _sh = new SqliteHelper(Properties.Settings.Default.RecentLib);
                refreshRows();
            }

            if (Program.args.Length > 0)
            {
                if (System.IO.File.Exists(Program.args[0]))
                {
                    if (Program.args[0].EndsWith(".bdb"))
                    {
                        _sh = new SqliteHelper(Program.args[0]); refreshRows();
                    }
                    else
                    {
                        _bh.ParseFile(Program.args[0]);
                    }
                }
            }

            // Hotkey
            W32ApiHelper.Regist(this.Handle, 0, Keys.F10, new W32ApiHelper.HotKeyCallBackHanlder(() =>
            {
                try
                {
                    if (this.Handle == W32ApiHelper.GetForegroundWindow()) { addBibTexCodeToolStripMenuItem_Click(null, null); return; }
                    string str = Clipboard.GetText().ToString();
                    if (string.IsNullOrEmpty(str)) return;
                    if (str[0] == '@' || str[0] == '%')
                    {
                        importRecords(str);
                        W32ApiHelper.SetForegroundWindow(this.Handle);
                    }
                    else
                    {
                        SendKeys.SendWait("^c");
                        System.Threading.Thread.Sleep(750);
                        str = Clipboard.GetText().ToString();
                        System.Diagnostics.Process.Start(string.Format(Properties.Settings.Default.DefaultSearchEngine, str));
                    }
                }
                catch (Exception) { }
            }));
        }
        
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            W32ApiHelper.ProcessHotKey(m);
        }

        private int addDgvRow(Dictionary<string, string> row)
        {
            _rendering = true;
            int row_id = dgv.Rows.Add();
            foreach (string key in row.Keys)
            {
                dgv.Rows[row_id].Cells[key].Value = row[key];
            }
            _rendering = false;
            return row_id;
        }

        private void refreshRows()
        {
            _rendering = true;
            if (_sh == null)return;
            dgv.Rows.Clear();
            foreach (var r in _sh.ExecuteQuery("select * from bibdb"))
            {
                addDgvRow(r);
            }
            _rendering = false;
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sfd.Filter = "BibTex File|*.bib";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
            _bh.SaveBibFile(sfd.FileName, dgv.Rows);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_sh != null) _sh.Close();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
            _sh = new SqliteHelper(ofd.FileName); refreshRows();
            Properties.Settings.Default.RecentLib = ofd.FileName;
            Properties.Settings.Default.Save();
        }

        private void addBibTexCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new dlgAddNewSource();
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            string source = dlg.GetSource();
            importRecords(source);
        }

        private void importRecords(string source)
        {
            _sh.BeginTransaction();
            foreach (var r in _bh.ParseFile(source))
            {
                int idx = addDgvRow(r);
                if (_sh != null) if (_sh.Insert(dgv.Rows[idx]) <= 0)
                    {
                        _rendering = true;
                        dgv.Rows.RemoveAt(idx);
                        _rendering = false;
                    }
            }
            _sh.Commit();
        }

        private void copyKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool[] flag = new bool[dgv.Rows.Count];
            string entries = "";
            foreach (DataGridViewCell cell in dgv.SelectedCells)
            {
                if (flag[cell.RowIndex]) continue;
                flag[cell.RowIndex] = true;
                DataGridViewRow row = dgv.Rows[cell.RowIndex];
                entries += "\\cite{" + row.Cells["key"].Value + "}";
            }
            Clipboard.SetText(entries);
        }


        private void copyFullReferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedCells.Count <= 0) return;
            try
            {
                var row = dgv.Rows[dgv.SelectedCells[0].RowIndex];
                bool isChinese = row.Cells["title"].Value.ToString()[0] > 'z';
                string txt = "", mChinese = "", mEnglish = "";
                switch (row.Cells["type"].Value.ToString().ToLower())
                {
                    case "book":
                        txt = Properties.Settings.Default.NonEnglishBookItemFormat;
                        if (!isChinese) txt = Properties.Settings.Default.EnglishBookItemFormat;
                        break;
                    case "mastersthesis":
                    case "phdthesis":
                        mChinese = "博士学位论文"; mEnglish = " PhD Thesis";
                        if (row.Cells["type"].Value.ToString().ToLower()[0] == 'm')
                        {
                            mChinese = "硕士学位论文";
                            mEnglish = "Master's Thesis";
                        }
                        txt = Properties.Settings.Default.NonEnglishBookItemFormat;
                        if (!isChinese) txt = Properties.Settings.Default.EnglishBookItemFormat;
                        txt = txt.Replace("{publisher}", "{institution}" + (isChinese ? mChinese : mEnglish));
                        break;
                    case "article":
                    default:
                        txt = Properties.Settings.Default.NonEnglishArticleItemFormat;
                        if (!isChinese) txt = Properties.Settings.Default.EnglishArticleItemFormat;
                        break;
                }
                txt = _bh.FormatByDictionary(txt, new Func<string, string>((x) => {
                    try
                    {
                        if (row.Cells[x].Value == null) return "";
                        string r = row.Cells[x].Value.ToString();
                        if (x == "pages") return r.Replace("--", "-");
                        if (x == "author") return r.Replace(" and ", isChinese ? "，" : ", ");
                        return r;
                    }
                    catch (Exception) {
                        return "";
                    }
                }));
                Clipboard.SetText(txt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedCells.Count <= 0) return;
            try
            {
                int i = dgv.SelectedCells[0].RowIndex;
                if (dgv.Rows[i].Cells["key"].Value == null) return;
                _sh.Delete(dgv.Rows[i].Cells["key"].Value.ToString());
                dgv.Rows.RemoveAt(i);
            }
            catch (Exception) { }
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            W32ApiHelper.UnregisterHotKey(this.Handle);
        }

        private void searchStrip_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                for (int searchIndex = 0; searchIndex < dgv.Rows.Count; ++searchIndex)
                {
                    bool flag = false;
                    foreach (DataGridViewCell c in dgv.Rows[searchIndex].Cells)
                    {
                        if (c.Value == null) continue;
                        if (c.Value.ToString().Contains(searchStrip.Text)) { flag = true; break; }
                    }
                    dgv.Rows[searchIndex].Selected = flag;
                }
            }
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new dlgPreferences().ShowDialog();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_sh != null) _sh.Close();
            _rendering = true;
            dgv.Rows.Clear();
            sfd.Filter = "Reference Database|*.bdb";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
            _sh = new SqliteHelper(sfd.FileName);
            _sh.CreateTable();
            _rendering = false;
        }

        string oldkey = "";

        private void dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_rendering) return;
            if (dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null) return;
            if (dgv.Rows[e.RowIndex].Cells["key"].Value == null) { dgv.Rows[e.RowIndex].Cells["key"].Value = "tk" + DateTime.Now.GetHashCode().ToString("X"); return; }
            string key = dgv.Rows[e.RowIndex].Cells["key"].Value.ToString();
            string field = dgv.Columns[e.ColumnIndex].Name;
            if (field == "key") key = oldkey;
            string newvalue = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
            string sql = string.Format("update bibdb set {0} = '{1}' where `key` = '{2}'", field, newvalue.Replace("'", "''"), key);
            if (_sh.ExecuteNonQuery(sql) <= 0) _sh.Insert(dgv.Rows[e.RowIndex]);
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (_rendering || _sh == null) return;
            for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; ++i)
            {
                if (dgv.Rows[i].Cells["key"].Value == null || dgv.Rows[i].Cells["key"].Value.ToString() == "") continue;
                _sh.Insert(dgv.Rows[i]);
            }
        }
        
        private void exportSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sfd.Filter = "BibTex File|*.bib";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
            _bh.SaveBibFile(sfd.FileName, dgv.SelectedRows);            
        }

        private void dgv_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            oldkey = "" + dgv.Rows[e.RowIndex].Cells["key"].Value;
        }
    }
}
