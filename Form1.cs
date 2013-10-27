using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net;

namespace BibtexManager
{
    public partial class Form1 : Form
    {

        string _myFilename = null;

        public Form1()
        {
            InitializeComponent();
        }

        Dictionary<string, string> _bibTexFields = new Dictionary<string, string>();

        HttpListener _httplistener = new HttpListener();

        System.Threading.Thread thrHttpListening;

        private void httpListening()
        {
            if (_httplistener.Prefixes.Count == 0) _httplistener.Prefixes.Add("http://127.0.0.1:3982/addnote/");
            _httplistener.Start();
            while (this.Visible)
            {
                HttpListenerContext context = _httplistener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                string data = request.QueryString["data"];
                if (data != null) parseFile(data);
                HttpListenerResponse response = context.Response;
                // Construct a response. 
                string responseString = "200 OK";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }
            _httplistener.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            //frmTools tools = new frmTools(); tools.Show();
            
            this.Text = Properties.Resources.softwareName;
            string[] fields = Properties.Resources.fields.Split(';');
            foreach (string field in fields)
            {
                string[] cols = field.Split('=');
                if (cols.Length < 2) continue;
                _bibTexFields.Add(cols[0], cols[1]);
                if (cols[0] == "type")
                {
                    var colcombo = new DataGridViewComboBoxColumn();
                    colcombo.HeaderText = cols[1]; colcombo.Name = cols[0];
                    foreach (var item in articleToolStripMenuItem.Items)
                    {
                        colcombo.Items.Add(item);
                    }
                    colcombo.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                    dgv.Columns.Add(colcombo);
                }
                else
                {
                    dgv.Columns.Add(cols[0], cols[1]);
                }
            }

            if (Program.args.Length > 0)
            {
                if (System.IO.File.Exists(Program.args[0]))
                {
                    _myFilename = Program.args[0];
                    parseFile();
                }
            }
            else
            {
                if (System.IO.File.Exists(Properties.Settings.Default.RecentLib))
                {
                    _myFilename = Properties.Settings.Default.RecentLib;
                    parseFile();
                }
            }
            
            thrHttpListening = new System.Threading.Thread(new System.Threading.ThreadStart(httpListening));
            // thrHttpListening.Start();

            Hotkey.Regist(this.Handle, 0, Keys.F10, new Hotkey.HotKeyCallBackHanlder(()=>{
                string str = Clipboard.GetText().ToString();
                if (str.StartsWith("@"))
                {
                    parseFile(str); Hotkey.SetForegroundWindow(this.Handle);
                }
                else
                {
                    SendKeys.SendWait("^c");
                    System.Threading.Thread.Sleep(500);
                    str = Clipboard.GetText().ToString();
                    System.Diagnostics.Process.Start("http://scholar.google.com/scholar?q=" + str);
                }
            }));
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            Hotkey.ProcessHotKey(m);
        }

        private IEnumerable<string> getEntryStrings(DataGridViewRowCollection rows)
        {
            foreach (DataGridViewRow row in rows)
            {
                if ("" + row.Cells["type"].Value == "") continue;
                string entry = "@" + row.Cells["type"].Value + "{" + row.Cells["key"].Value;
                foreach (string key in _bibTexFields.Keys)
                {
                    if (key == "key" || key == "type") continue;
                    string val = "" + row.Cells[key].Value;
                    if (val.Length == 0) continue;
                    entry += "," + Environment.NewLine + key + " = {" + val + "}";
                }
                entry += Environment.NewLine + "}" + Environment.NewLine;
                yield return entry;
            }
        }

        private void saveFile()
        {
            if (_myFilename == null) return;
            using (StreamWriter sw = new StreamWriter(_myFilename))
            {
                foreach(string entry in getEntryStrings(dgv.Rows)) {
                    sw.WriteLine(entry);
                }
            }
            Properties.Settings.Default.RecentLib = _myFilename;
            Properties.Settings.Default.Save();
        }

        private void parseFile(string content = null)
        {
            if (_myFilename == null && content == null) return;

            if (content == null) content = File.ReadAllText(_myFilename);
            string field = "", value = "";
            Regex regComment = new Regex(@"\%.*", RegexOptions.Compiled);
            content = regComment.Replace(content, "");
            Regex regSpace = new Regex(@"\s+", RegexOptions.Compiled);
            content = regSpace.Replace(content, " ");
            Dictionary<string, string> row = new Dictionary<string, string>();

            int inBracket = 0; bool fieldNameFinished = false;
            for (int i = 0; i < content.Length; ++i)
            {
                switch (content[i])
                {
                    case '@':
                        if (inBracket > 0) continue;
                        field = "type"; value = ""; fieldNameFinished = true;
                        break;
                    case '{':
                        if (inBracket == 0)
                        {
                            parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
                            field = "key"; value = ""; fieldNameFinished = true;
                        }
                        ++inBracket;
                        break;
                    case '}':
                        --inBracket;
                        if (inBracket == 0)
                        {
                            parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
                            int row_id = dgv.Rows.Add();
                            foreach (string key in row.Keys)
                            {
                                dgv.Rows[row_id].Cells[key].Value = row[key];
                            }
                            row.Clear();
                        }
                        break;
                    case '=':
                        if (inBracket > 1 || fieldNameFinished) value += content[i];
                        else fieldNameFinished = true;
                        break;
                    case '\\':
                        ++i;
                        switch (content[i])
                        {
                            case '\\':
                                if (fieldNameFinished) value += Environment.NewLine;
                                break;
                            default:
                                value += '\\' + content[i];
                                break;
                        }
                        break;
                    case ',':
                        parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
                        break;
                    default:
                        if (fieldNameFinished) value += content[i]; else field += content[i];
                        break;
                }
            }
        }

        private void parseSetField(ref string field, ref string value, ref bool fieldNameFinished, ref Dictionary<string,string> row)
        {
            value = value.Trim(); field = field.Trim();
            if (field == "type") value = value[0].ToString().ToUpper() + value.ToLower().Substring(1);
            if (_bibTexFields.ContainsKey(field))
                if (!row.ContainsKey(field)) row.Add(field, value);
                else row[field] = value;
            fieldNameFinished = false; value = ""; field = "";
                        
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
            _myFilename = sfd.FileName;
            saveFile();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
            _myFilename = ofd.FileName;

            this.Text = Properties.Resources.softwareName + _myFilename;
            parseFile();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_myFilename == null)
            {
                saveAsToolStripMenuItem_Click(sender, e);
                return;
            }
            saveFile();
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count < 1) return;
            List<DataGridViewCellCollection> rows = new List<DataGridViewCellCollection>();
            for (int i = 0; i < dgv.SelectedRows.Count; ++i)
                rows.Add(dgv.SelectedRows[i].Cells);
            foreach (var row in rows)
            {
                var r = new DataGridViewRow();
                foreach(DataGridViewCell c in row)
                    r.Cells.Add(c);
                dgv.Rows.Add();
            }
        }

        private void addBibTexCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new dlgAddNewSource();
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            string source = dlg.GetSource();
            parseFile(source);
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

        private const string BOOK_REF_TXT = "{author}：《{title}》，{publisher}，{year}年：第-页。";
        private const string ARTICLE_REF_TXT = "{author}：{title}，《{journal}》，{year}（{number}）：第{pages}页。";
        private const string BOOK_REF_TXT_EN = "{author} ({year}). {title}. {publisher}: pp. -.";
        private const string ARTICLE_REF_TXT_EN = "{author} ({year}). {title}. {journal}. {volume} ({number}): pp. {pages}.";

        private string formatByDictionary(string format, Func<string, string> lookup)
        {
            Regex reg = new Regex(@"\{(\w+)\}");
            return reg.Replace(format, new MatchEvaluator((Match m)=>{return lookup(m.Groups[1].ToString());}));
        }

        private void copyFullReferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedCells.Count <= 0) return;
            try
            {
                var row = dgv.Rows[dgv.SelectedCells[0].RowIndex];
                bool isChinese = !char.IsLetter(row.Cells["title"].Value.ToString()[0]);
                string txt = "";
                switch (row.Cells["type"].Value.ToString().ToLower())
                {
                    case "book":
                        txt = BOOK_REF_TXT;
                        if (!isChinese) txt = BOOK_REF_TXT_EN;
                        break;
                    case "article":
                    default:
                        txt = ARTICLE_REF_TXT;
                        if (!isChinese) txt = ARTICLE_REF_TXT_EN;
                        break;
                }
                txt = formatByDictionary(txt, new Func<string, string>((x) => { return row.Cells[x].Value.ToString(); }));
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
                dgv.Rows.RemoveAt(dgv.SelectedCells[0].RowIndex);
            }
            catch (Exception) { }
        }

        private void articleToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
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

        private void searchStrip_TextChanged(object sender, EventArgs e)
        {
        }
    }
}
