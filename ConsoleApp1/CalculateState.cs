using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace Test
{
    internal static class CalculateState
    {
        public static List<Person> Calculate(SQLiteConnection connection, string tg_chat)
        {
            List<Person> people = PreparePeople(connection, tg_chat);

            DB.RunQuery(connection, $"SELECT * FROM {tg_chat} ORDER BY  AuthorName, MessageDate");
            SQLiteDataReader reader = DB.RunQuery(connection, $"SELECT * FROM {tg_chat} ORDER BY AuthorName, MessageDate").ExecuteReader();

            if (reader.HasRows)
            {
                long lastDate = 0;
                string lastSubject = "";
                bool startCalculate = true;
                while (reader.Read())
                {
                    double goodRep = 0;
                    double goodCommunication = 0;
                    double evilCommunication = 0;

                    double badRep = 0;
                    double maxEffective = 0;
                    double lowEffcitve = 0;

                    string message = reader.GetValue(3).ToString().ToLower();
                    string name = reader.GetValue(2).ToString();

                    if (startCalculate || reader.GetValue(2).ToString() != lastSubject)
                    {

                        lastSubject = reader.GetValue(2).ToString();
                        startCalculate = false;
                    }
                    else
                    {
                        long razniza = Math.Abs(lastDate - (long)Convert.ToDouble(reader.GetValue(1)));
                        if (razniza > 86400)
                        {
                            badRep++;
                        }
                        else if (razniza < 14400)
                            goodRep += 0.1;
                    }
                    lastDate = (long)Convert.ToDouble(reader.GetValue(1));

                    for (int i = 0; i < Filter.AgressiveFiltre.Length; i++)
                    {
                        if (message.Contains(Filter.AgressiveFiltre[i].ToLower()))
                        {
                            evilCommunication++;
                        }
                    }

                    for (int i = 0; i < Filter.PositiveFiltre.Length; i++)
                    {
                        if (message.Contains(Filter.PositiveFiltre[i].ToLower()))
                        {
                            goodCommunication++;
                        }
                    }

                    for (int i = 0; i < Filter.maxEffective.Length; i++)
                    {
                        if (message.Contains(Filter.maxEffective[i].ToLower()))
                        {
                            maxEffective++;
                        }
                    }

                    for (int i = 0; i < Filter.lowEffective.Length; i++)
                    {
                        if (message.Contains(Filter.lowEffective[i].ToLower()))
                        {
                            lowEffcitve++;
                        }
                    }


                    for (int i = 0; i < Filter.NeutralFiltre.Length; i++)
                    {
                        if (message.Contains(Filter.NeutralFiltre[i].ToLower()))
                        {
                            maxEffective+=0.01;
                            goodCommunication += 0.01;
                        }
                    }

                    AddStat(people, goodRep, badRep, goodCommunication - evilCommunication, maxEffective - lowEffcitve, name);
                }
            }

            YoutrackFetcher work = new YoutrackFetcher();
            foreach (string file in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "data"), ".csv"))
                work.ReadFile(file, people);
            return people;
        }

        static List<Person> PreparePeople(SQLiteConnection connection, string tg_chat)
        {
            List<string> result = new List<string>();

            DB.RunQuery(connection, $"SELECT * FROM {tg_chat} ORDER BY AuthorName");
            SQLiteDataReader reader = DB.RunQuery(connection, $"SELECT * FROM {tg_chat}").ExecuteReader();

            while (reader.Read())
            {
                if (!result.Contains(reader.GetValue(2).ToString()))
                {
                    result.Add(reader.GetValue(2).ToString());
                }
            }

            List<Person> readyList = new List<Person>(result.Count);

            foreach (string name in result)
            {
                readyList.Add(new Person(name));
            }

            reader.Close();

            return readyList;
        }

        static void AddStat(List<Person> list, double goodRep, double badRep, double styleCommunication, double effectivePerson, string name)
        {
            foreach (Person person in list)
            {
                if (person.name == name)
                {
                    person.plusRep += goodRep;
                    person.minusRep += badRep;
                    person.activeInchat += effectivePerson > 0 ? 1 : effectivePerson < 0 ? -1 : 0;
                    person.typeCommunecation += styleCommunication > 0 ? 1 : styleCommunication < 0 ? -1 : 0;
                    return;
                }
            }
        }

        static public void CreateBD(SQLiteConnection connection, List<Person> people, string tg_chat, string way = "database.db")
        {
            DB.RunQuery(connection, $"CREATE TABLE IF NOT EXISTS {tg_chat} (tg_user_id INT, tg_user_name TEXT, criteria TEXT, plus_stat DECIMAL(8.2), minus_stat DECIMAL(8.2), work_percent DECIMAL(4.2));");
            DB.RunQuery(connection, $"DELETE From {tg_chat}");

            for (int i = 0; i < people.Count; i++)
            {
                people[i].plusRep = Math.Round(people[i].plusRep, 2);
                people[i].minusRep = Math.Round(people[i].minusRep, 2);
                people[i].totalRep = Math.Round((people[i].minusRep + Math.Abs(people[i].overwork)) / (people[i].plusRep + people[i].minusRep + Math.Abs(people[i].overwork)), 2);
                string tag = "";

                if (Double.IsNaN(people[i].totalRep))
                    people[i].totalRep = -1;

                if (people[i].typeCommunecation > 0)
                {
                    tag += " Доброжелательность ";
                }
                else if (people[i].typeCommunecation < 0)
                {
                    tag += " Токсичность ";
                }

                if (people[i].activeInchat > 0)
                {
                    tag += " Эффективный ";
                }
                else if (people[i].activeInchat < 0)
                {
                    tag += " Малоэффективный ";
                }

                if (people[i].overwork > 0)
                {
                    tag += " Перерабатывает ";
                }
                else if (people[i].overwork < 0)
                {
                    tag += " Недорабатывает ";
                }

                if (tag.Length == 0)
                    tag = " Неактивен ";

                DB.RunQuery(connection, $"INSERT INTO {tg_chat} VALUES ({i}, '{people[i].name}', '{tag}', '{people[i].plusRep}', '{people[i].minusRep}' ,'{people[i].totalRep}' );");
            }
        }
    }
}
