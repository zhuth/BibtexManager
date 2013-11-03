using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BibtexManager
{
    /// <summary>
    /// 解析和导出 BibTex 格式和 EndNote 格式。
    /// </summary>
    public class BibHelper
    {
        Dictionary<string, string> _bibTexFields = new Dictionary<string, string>();

        /// <summary>
        /// 建立一个 BibHelper 类实例。
        /// </summary>
        public BibHelper()
        {            
            string[] fields = Properties.Resources.fields.Split(';');
            foreach (string field in fields)
            {
                string[] cols = field.Split('=');
                if (cols.Length < 2) continue;
                _bibTexFields.Add(cols[0], cols[1]);
            }
        }

        /// <summary>
        /// BibTex 的字段列表。
        /// </summary>
        public Dictionary<string, string> BibTexFields
        {
            get
            {
                return _bibTexFields;
            }
        }
        
        /// <summary>
        /// 从 DGV 行中导出 Bib 格式
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        private string getEntryString(DataGridViewRow row)
        {
            if ("" + row.Cells["type"].Value == "") return "";
            string entry = "@" + row.Cells["type"].Value + "{" + row.Cells["key"].Value;
            foreach (string key in _bibTexFields.Keys)
            {
                if (key == "key" || key == "type") continue;
                string val = "" + row.Cells[key].Value;
                if (val.Length == 0) continue;
                entry += "," + Environment.NewLine + key + " = {" + val + "}";
            }
            entry += Environment.NewLine + "}" + Environment.NewLine;
            return entry;
        }

        /// <summary>
        /// 导出 BibTex 文件
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="rows"></param>
        public void SaveBibFile(string filename, DataGridViewRowCollection rows)
        {
            if (filename == null) return;
            using (StreamWriter sw = new StreamWriter(filename))
            {
                foreach (DataGridViewRow r in rows)
                {
                    sw.WriteLine(getEntryString(r));
                }
            }
        }

        public void SaveBibFile(string filename, DataGridViewSelectedRowCollection rows)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                foreach (DataGridViewRow r in rows)
                {
                    sw.WriteLine(getEntryString(r));
                }
            }
        }
        /// <summary>
        /// 解析 BibTex 或 EndNote 文件
        /// </summary>
        /// <param name="content">以 @ 起始则是 BibTex，以 % 起始的是 EndNote</param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, string>> ParseFile(string content)
        {
            if (content == null) yield break;
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
                foreach(var p in ParseEndNoteFile(content)) yield return p;
                yield break;
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
                            yield return row;
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

        /// <summary>
        /// 解析设置字段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="fieldNameFinished"></param>
        /// <param name="row"></param>
        private void parseSetField(ref string field, ref string value, ref bool fieldNameFinished, ref Dictionary<string, string> row)
        {
            value = value.Trim(); field = field.Trim();
            if (field == "type") value = value[0].ToString().ToUpper() + value.ToLower().Substring(1);
            if (_bibTexFields.ContainsKey(field))
                if (!row.ContainsKey(field)) row.Add(field, value);
                else if (field == "author") row[field] += " and " + value;
                else row[field] = value;
            fieldNameFinished = false; value = ""; field = "";
        }

        /// <summary>
        /// 解析 EndNote 格式信息
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, string>> ParseEndNoteFile(string content)
        {
            // fields: key=Key;type=类型;author=作者;title=著作名称;journal=期刊;volume=卷;year=年份;month=月份;number=编号;pages=页码;booktitle=书名;chapter=章;publisher=出版商;address=地址;annote=注解;crossref=交叉引用;date=日期;doi=DOI;edition=版次;editor=编者;eprint=电子出版物;howpublished=出版方式;institution=机构;note=备注;organization=组织;school=院校;series=系列;url=URL;
            // types:  Article;Book;Booklet;Conference;Inbook;Incollection;Inproceedings;Manual;Mastersthesis;Misc;Phdthesis;Proceedings;Techreport;Unpublished
            Dictionary<string, string> row = new Dictionary<string, string>();
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
                        yield return row;
                        row.Clear();
                        break;
                }
                parseSetField(ref field, ref value, ref fieldNameFinished, ref row);
            }
        }

        /// <summary>
        /// 字符串格式化工具
        /// </summary>
        /// <param name="format"></param>
        /// <param name="lookup"></param>
        /// <returns></returns>
        public string FormatByDictionary(string format, Func<string, string> lookup)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"\{(\w+)\}");
            return reg.Replace(format, new System.Text.RegularExpressions.MatchEvaluator((System.Text.RegularExpressions.Match m) => { return lookup(m.Groups[1].ToString()); }));
        }

    }
}
