using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    static public class DB
    {
        static public SQLiteConnection CreateConnection()
        {
            SQLiteConnection sqlite_conn;
            sqlite_conn = new SQLiteConnection("Data Source=database.db; Version=3; New=True; Compress=True; ");
            try
            {
                sqlite_conn.Open();
            }
            catch
            {
                Console.WriteLine("Can't open db");
            }
            Console.WriteLine("SQL Connection created!");
            return sqlite_conn;
        }

        static public SQLiteCommand RunQuery(SQLiteConnection conn, string query)
        {
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = query;
            sqlite_cmd.ExecuteNonQuery();

            return sqlite_cmd;
        }

        static public List<List<string>> ReadData(SQLiteConnection conn, string table_name, int values_count, string condition = null)
        {
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = $"SELECT * FROM {table_name}{(condition != null ? " WHERE " + condition : "")}";
            sqlite_datareader = sqlite_cmd.ExecuteReader();

            List<List<string>> Data = new List<List<string>>();

            while (sqlite_datareader.Read())
            {
                List<string> Values = new List<string>();
                for (int i = 0; i < values_count; i++)
                    Values.Add(sqlite_datareader.GetString(i));

                Data.Add(Values);
            }

            return Data;
        }
    }
}
