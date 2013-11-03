using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace BibtexManager
{
    /// <summary>
    /// SqlLite Helper Class
    /// </summary>
    class SqliteHelper
    {
        SQLiteConnection _conn;
        HashSet<string> _cols = new HashSet<string>();
        string _colsList = "";

        /// <summary>
        /// 打开指定的数据库文件
        /// </summary>
        /// <param name="filename"></param>
        public SqliteHelper(string filename)
        {
            _conn = new SQLiteConnection(@"Data Source=" + filename);
            _conn.Open();
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
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, string>> ExecuteQuery(string sql)
        {
            using (var reader = new SQLiteCommand(sql, _conn).ExecuteReader())
            {
                while (reader.Read())
                {
                    Dictionary<string, string> row = new Dictionary<string, string>();
                    foreach (string k in _cols)
                    {
                        row.Add(k, reader[k].ToString());
                    }
                    yield return row;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CreateTable()
        {
            string sql = "CREATE TABLE bibdb (";
            foreach (string k in _cols) sql += "`" + k + "` varchar(255),";
            sql = sql.Substring(0, sql.Length - 1) + ")";
            ExecuteNonQuery(sql);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        public void Insert(System.Windows.Forms.DataGridViewRow row)
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
                ExecuteNonQuery(sql);
            }
            catch (Exception) { }
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
            _conn.Close();
        }

        public void Commit()
        {
            _conn.Close(); _conn.Open();
        }
    }
}
