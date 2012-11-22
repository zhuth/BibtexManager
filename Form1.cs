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
    }
}
