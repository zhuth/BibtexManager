using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedSQLite;

namespace BibtexManager
{
    /// <summary>
    /// SqlLite Helper Class
    /// </summary>
    class SqliteHelper
    {
        Database _conn = new Database();
        HashSet<string> _cols = new HashSet<string>();
        string _colsList = "";

        /// <summary>
        /// 打开指定的数据库文件
        /// </summary>
        /// <param name="filename"></param>
        public SqliteHelper(string filename)
        {
            _conn.Open(filename);
            foreach (string s in Properties.Resources.fields.Split(';'))
            {
                int eqidx = s.IndexOf('='); if (eqidx < 0) continue;
                string field = s.Substring(0, eqidx);
                _cols.Add(field);
                _colsList += ",`"+ field + "`";
            }
            _colsList = _colsList.Substring(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sql)
        {
            return _conn.ExecuteDML(sql);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, string>> ExecuteQuery(string sql)
        {
            var r = _conn.ExecuteQuery(sql);
            Dictionary<string, string> row = new Dictionary<string, string>();
            foreach (string k in _cols) row.Add(k, "");
            
            while (!r.EndOfFile())
            {
                foreach (string k in _cols) row[k] = r.FieldValue(k);
                yield return row;
                r.NextRow();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CreateTable()
        {
            string sql = "CREATE TABLE bibdb (";
            foreach (string k in _cols) sql += "`" + k + "` varchar(255),";
            sql += " PRIMARY KEY(`key`))";
            ExecuteNonQuery(sql);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        public int Insert(System.Windows.Forms.DataGridViewRow row)
        {
            try
            {
                string sql = "insert into bibdb (" + _colsList + ") values(";
                foreach (string k in _cols)
                {
                    if (row.Cells[k].Value == null) sql += "'',";
                    else sql += "'" + row.Cells[k].Value.ToString().Replace("'", "''") + "',";
                }
                sql = sql.Substring(0, sql.Length - 1) + ")";
                return ExecuteNonQuery(sql);
            }
            catch (System.Exception) { }
            return 0;
        }

        public void Delete(string key)
        {
            ExecuteNonQuery("delete from bibdb where `key` = '" + key + "'");
        }

        /// <summary>
        /// 关闭连接。
        /// </summary>
        public void Close()
        {
            try
            {
                _conn.Close();
            }
            catch { }
        }


        public void Commit()
        {
            _conn.CommitTransaction();
        }

        public void BeginTransaction()
        {
            _conn.BeginTransaction();
        }
    }
}
