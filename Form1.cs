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
                    foreach (var item in Properties.Resources.types.Split(';'))
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
            
            // Hotkey
            W32ApiHelper.Regist(this.Handle, 0, Keys.F10, new W32ApiHelper.HotKeyCallBackHanlder(()=>{
                if (this.Handle == W32ApiHelper.GetForegroundWindow()) { addBibTexCodeToolStripMenuItem_Click(null, null); return; }
                string str = Clipboard.GetText().ToString();
                if (string.IsNullOrEmpty(str)) return;
                if (str[0] == '@' || str[0] == '%')
                {
                    parseFile(str); W32ApiHelper.SetForegroundWindow(this.Handle);
                }
                else
                {
                    SendKeys.SendWait("^c");
                    System.Threading.Thread.Sleep(750);
                    str = Clipboard.GetText().ToString();
                    System.Diagnostics.Process.Start(string.Format(Properties.Settings.Default.DefaultSearchEngine, str));
                }
            }));
        }

        private void parseEndNoteFile(string content = null)
        {
            // fields: key=Key;type=类型;author=作者;title=著作名称;journal=期刊;volume=卷;year=年份;month=月份;number=编号;pages=页码;booktitle=书名;chapter=章;publisher=出版商;address=地址;annote=注解;crossref=交叉引用;date=日期;doi=DOI;edition=版次;editor=编者;eprint=电子出版物;howpublished=出版方式;institution=机构;note=备注;organization=组织;school=院校;series=系列;url=URL;
            // types:  Article;Book;Booklet;Conference;Inbook;Incollection;Inproceedings;Manual;Mastersthesis;Misc;Phdthesis;Proceedings;Techreport;Unpublished
            Dictionary<string, string> row = new Dictionary<string,string>();
            string field = "";

            foreach (string l in content.Split('%'))
            {
                bool fieldNameFinished = true;
                string line = l.Trim();
                if (string.IsNullOrEmpty(line) || !line.Contains(' ')) continue;
                string value = line.Substring(2).Trim();
                if (string.IsNullOrEmpty(value)) continue;
                switch (line[0])
                {
                    case '0':
                        field = "type";
                        switch (value.Substring(0, 3).ToLower())
                        {
                            case "the":
                                value = "phdthesis"; break;
                            case "jou":
                                value = "article"; break;
                            case "boo":
                                value = "book"; break;
                        }
                        break;
                    case 'A':
                        field = "author"; break;
                    case 'T':
                        field = "title"; break;
                    case 'I':                        
                        field = "institution"; break;
                    case 'D':
                        field = "year"; break;
                    case 'N':
                        field = "number"; while (value.StartsWith("0")) value = value.Substring(1); break;
                    case 'P':
                        field = "pages"; if (value.Contains('+')) value = value.Substring(0, value.IndexOf('+')); break;
                    case 'J':
                        field = "journal"; break;
                    case '9':
                        if (value == "硕士" || value.ToLower()[0] == 'm')
                        {
                            field = "type"; value = "mastersthesis";
                            parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
                        } break;
                    case 'W':
                        // commit this row
                        field = "key"; value = row["author"] + row["year"] + row["title"];
                        parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
                        if (row.ContainsKey("institution"))
                        {
                            field = "publisher"; value = row["institution"];
                            parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
                        }
                        int row_id = dgv.Rows.Add();
                        foreach (string key in row.Keys)
                        {
                            dgv.Rows[row_id].Cells[key].Value = row[key];
                        }
                        row.Clear();
                        break;
                }
                parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            W32ApiHelper.ProcessHotKey(m);
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
            if (content[0] == '@')
            {
                Regex regComment = new Regex(@"\%.*", RegexOptions.Compiled);
                content = regComment.Replace(content, "");
            }
            Regex regSpace = new Regex(@"\s+", RegexOptions.Compiled);
            content = regSpace.Replace(content, " ");
            Dictionary<string, string> row = new Dictionary<string, string>();

            if (content.StartsWith("%"))
            {
                // EndNote File
                parseEndNoteFile(content);
                return;
            }

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
                        if (inBracket > 1 && fieldNameFinished) value += content[i];
                        else parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
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
                else if (field == "author") row[field] += " and " + value;
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
                txt = formatByDictionary(txt, new Func<string, string>((x) => {
                    if (row.Cells[x].Value == null) return "";
                    string r = row.Cells[x].Value.ToString();
                    if (x == "pages") return r.Replace("--", "-");
                    if (x == "author") return r.Replace(" and ", isChinese ? "，" : ", ");
                    return r;
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

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new dlgPreferences().ShowDialog();
        }
    }
}
