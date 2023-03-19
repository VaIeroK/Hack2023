using Octokit;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SQLiteConnection sqlite_conn;
            sqlite_conn = DB.CreateConnection();

            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");

            new GitFetcher().FlushToDB(sqlite_conn, "VaIeroK", "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", new List<string>() { "ui" });
            DB.RunQuery(sqlite_conn, $"DROP TABLE GitHubUsers;");

            foreach (string file in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "data"), "*.json"))
            {
                new TelegramFetcher().FlushToDB(sqlite_conn, file);
                CalculateState.CreateBD(sqlite_conn, CalculateState.Calculate(sqlite_conn, $"Telegram_{Path.GetFileNameWithoutExtension(file)}"), $"Telegram_{Path.GetFileNameWithoutExtension(file)}_Ui");
                DB.RunQuery(sqlite_conn, $"DROP TABLE Telegram_{Path.GetFileNameWithoutExtension(file)};");
            }

            Console.WriteLine("End");

            sqlite_conn.Close();
        }
    }
}
