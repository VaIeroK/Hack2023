using Octokit;
using System.Data.SQLite;

namespace Test
{
    class GitFetcher
    {
        private static readonly object InsertLock = new object();

        public class User
        {
            public string UserName;
            public List<Commit> Commits;
            public List<MonthWorkState> WorkState;
            public User() 
            {
                Commits = new List<Commit>();
                WorkState = new List<MonthWorkState>();
            }
        }

        public class MonthWorkState
        {
            public int Month;
            public int Year;
            public int WorkDays;

            public MonthWorkState(int Month, int Year, int WorkDays)
            {
                this.Month = Month;
                this.Year = Year;
                this.WorkDays = WorkDays;
            }
        }

        public class Commit
        {
            public string CommitDescr;
            public DateTime CommitDate;

            public Commit(string CommitDescr, DateTime CommitDate)
            {
                this.CommitDate = CommitDate;
                this.CommitDescr = CommitDescr;
            }
        }

        public void FlushToDB(SQLiteConnection connect, string User, string token = null, List<string> override_repos = null)
        {
            DB.RunQuery(connect, "CREATE TABLE IF NOT EXISTS GitHubUsers (AuthorName TEXT, CommitName TEXT, CommitDate TEXT)");
           //DB.RunQuery(connect, $"DELETE FROM GitHubUsers WHERE AuthorName = '{User}'");

            var github = new GitHubClient(new ProductHeaderValue("Jmaa"));
            if (token != null)
                github.Credentials = new Credentials(token);

            List<string> repos = new List<string>();

            if (override_repos == null)
            {
                var repos_task = github.Repository.GetAllForUser(User);
                repos_task.Wait();
                List<Repository> full_repos = repos_task.Result.ToList();
                foreach (Repository rep in full_repos)
                    repos.Add(rep.Name);
            }
            else
                repos = override_repos;

            Console.WriteLine("Get all repos");

            List<Thread> threads = new List<Thread>();

            foreach (var repo in repos)
            {
                Thread thread = new Thread(() =>
                {
                    Console.WriteLine($"Start inserting commits from {repo}");
                    var commits_task = github.Repository.Commit.GetAll(User, repo);
                    try
                    {
                        commits_task.Wait();
                    }
                    catch
                    {
                        Console.WriteLine("You send requests too often");
                        return;
                    }
                    var commits = commits_task.Result;

                    for (int i = 0; i < commits.Count; i++)
                    {
                        lock (InsertLock)
                        {
                            DB.RunQuery(connect, $"INSERT INTO GitHubUsers VALUES('{commits[i].Commit.Author.Name.Replace('\'', '.')}', '{commits[i].Commit.Message.Replace('\'', '.')}', '{commits[i].Commit.Author.Date.ToUnixTimeSeconds().ToString().Replace('\'', '.')}');");
                        }
                    }

                    Console.WriteLine($"Commits from {repo} inserted!");
                });
                thread.Start();
                threads.Add(thread);
            }

            foreach (Thread t in threads)
                while (t.ThreadState != System.Threading.ThreadState.Stopped);

            FlushUiToDB(connect, User);
            Console.WriteLine("Finish Insert!");
        }

        public void FlushUiToDB(SQLiteConnection connect, string User)
        {
            User gituser = new GitFetcher().ReadDB(connect, User);

            DB.RunQuery(connect, "CREATE TABLE IF NOT EXISTS GitHubUiUsers (AuthorName TEXT, CommitsInMonth INT, CommitDate BIGINT);");

            for (int i = 0; i < gituser.WorkState.Count; i++)
            {
                Console.WriteLine("111");
                DB.RunQuery(connect, $"INSERT INTO GitHubUiUsers VALUES('{User}', {gituser.WorkState[i].WorkDays}, {(new DateTimeOffset(gituser.WorkState[i].Year, gituser.WorkState[i].Month, 1, 1, 1, 1, new TimeSpan())).ToUnixTimeSeconds()}");
            }
        }

        public User ReadDB(SQLiteConnection connection, string UserName)
        {
            User user = new User();
            user.UserName = UserName;

            List<List<string>> values = DB.ReadData(connection, "GitHubUsers", 3, $"AuthorName = '{UserName}'");
            for (int i = 0; i < values.Count; i++)
                user.Commits.Add(new Commit(values[i][1], DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(values[i][2])).DateTime));

            DateTime currentDate = DateTime.Now;
            DateTime currentMonth = new DateTime(1970, 1, 1);

            while (currentMonth <= currentDate)
            {
                List<Commit> monthDates = new List<Commit>();

                for (int i = 0; i < user.Commits.Count; i++)
                {
                    if (user.Commits[i].CommitDate.Year == currentMonth.Year && user.Commits[i].CommitDate.Month == currentMonth.Month)
                        monthDates.Add(user.Commits[i]);
                }

                if (monthDates.Count > 0)
                {
                    List<DateTime> Dates = new List<DateTime>();
                    for (int j = 0; j < monthDates.Count; j++)
                        Dates.Add(new DateTime(monthDates[j].CommitDate.Year, monthDates[j].CommitDate.Month, monthDates[j].CommitDate.Day));

                    int WorkdaysInMonth = Dates.Distinct().Where(d => d.Year == monthDates[0].CommitDate.Year && d.Month == monthDates[0].CommitDate.Month).Count();
                    user.WorkState.Add(new MonthWorkState(monthDates[0].CommitDate.Month, monthDates[0].CommitDate.Year, WorkdaysInMonth));
                }

                currentMonth = currentMonth.AddMonths(1);
            }

            return user;
        }
    }
}
