using Octokit;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Test
{
    public class YoutrackFetcher
    {
        public class UserYoutrack
        {
            public string Name;
            public int value;

            public UserYoutrack(string name, int value)
            {
                Name=name;
                this.value=value;
            }
        }

        public void FlushToDB(SQLiteConnection connect, string folder)
        {
            foreach (string file in Directory.GetFiles(folder, "*.csv"))
            {
                using (StreamReader parser = new StreamReader(file))
                {
                    if (!parser.EndOfStream)
                    {
                        string dbname = Path.GetFileNameWithoutExtension(file);
                        string[] Params = parser.ReadLine().Split(',');
                        DB.RunQuery(connect, $"DROP TABLE IF EXISTS {dbname}");

                        string create_query = $"CREATE TABLE IF NOT EXISTS {dbname} (";
                        for (int i = 0; i < Params.Length; i++) 
                            create_query += $"{Params[i].Replace('\'', '.')} TEXT" + (i != Params.Length - 1 ? ", " : ")");

                        DB.RunQuery(connect, create_query);

                        while (!parser.EndOfStream)
                        {
                            string[] Values = parser.ReadLine().Split(',');
                            if (Values.Length > 0 && Values[0] == "\"Unassigned\"") continue;

                            string insert_query = $"INSERT INTO {dbname} VALUES(";
                            for (int i = 0; i < Values.Length; i++)
                                insert_query += Values[i].Replace('\'', '.') + (i != Values.Length - 1 ? ", " : ");");

                            DB.RunQuery(connect, insert_query);
                        }
                    }
                }
            }
        }

        public List<UserYoutrack> ReadDB(SQLiteConnection connection, string dbname, int columns_count)
        {
            List<UserYoutrack> Result = new List<UserYoutrack>();
            List<List<string>> list = DB.ReadData(connection, dbname, columns_count);
            foreach (List<string> row in list)
                Result.Add(new UserYoutrack(row[0], Convert.ToInt32(row[1])));

            return Result;
        }

        internal List<Person> ReadFile(string path, List<Person> people)
        {
            List<Person> Result = new List<Person>();
            using (StreamReader parser = new StreamReader(path))
            {
                if (!parser.EndOfStream)
                {
                    parser.ReadLine();
                    while (!parser.EndOfStream)
                    {
                        string[] Values = parser.ReadLine().Split(',');
                        Values[0] = Values[0].Trim('\"');
                        Values[1] = Values[1].Trim('\"');
                        if (Values.Length > 0 && Values[0] == "Unassigned") continue;
                        int data = Convert.ToInt32(Values[1]);
                        Person person = new Person();

                        if (data <= 5)
                            person.overwork = -1;
                        else if (data >= 70)
                            person.overwork = 1;
                        person.name = Values[0];
                        Result.Add(person);
                    }
                }
            }

            for (int i = 0; i < people.Count; i++)
                for (int j = 0; j < Result.Count; j++)
                {
                    if (people[i].name == Result[j].name)
                    {
                        people[i].overwork = Result[j].overwork;
                        break;
                    }
                }

            return Result;
        }
    }
}
