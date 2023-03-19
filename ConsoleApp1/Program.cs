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
        private static string[] TelegramDataBases =
        {
            "C:\\hotger.json", "C:\\delivery.json", "C:\\gigi.json", "C:\\hotfix1.json", "C:\\result.json"
        };

        static void Main(string[] args)
        {
            SQLiteConnection sqlite_conn;
            sqlite_conn = DB.CreateConnection();

            new GitFetcher().FlushToDB(sqlite_conn, "VaIeroK", "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", new List<string>() { "ui" });
            GitFetcher.User gituser = new GitFetcher().ReadDB(sqlite_conn, "VaIeroK");

            foreach (string json in TelegramDataBases)
            {
                new TelegramFetcher().FlushToDB(sqlite_conn, json);
                CalculateState.CreateBD(sqlite_conn, CalculateState.Calculate(sqlite_conn, $"Telegram_{Path.GetFileNameWithoutExtension(json)}"), $"Telegram_{Path.GetFileNameWithoutExtension(json)}_Ui");
            }
            
            //GitFetcher.User gituser = new GitFetcher().ReadDB(sqlite_conn, "kcat");
            
            //// Git
            //for (int i = 0; i < gituser.WorkState.Count; i++)
            //    Console.WriteLine($"{new DateTime(gituser.WorkState[i].Year, gituser.WorkState[i].Month, 1).ToShortDateString()}: Проработано дней {gituser.WorkState[i].WorkDays} из {DateTime.DaysInMonth(gituser.WorkState[i].Year, gituser.WorkState[i].Month)}");
            
            Console.WriteLine();

            Console.WriteLine("End");
            Console.ReadKey();

            sqlite_conn.Close();
        }
    }
}
