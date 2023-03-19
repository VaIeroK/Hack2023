using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Test
{
    public class TelegramFetcher
    {
        public void FlushToDB(SQLiteConnection connect, string way = "file.json")
        {
            using (StreamReader r = new StreamReader(way))
            {
                string dbname = Path.GetFileNameWithoutExtension(way);
                string json = r.ReadToEnd();
                Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(json);

                DB.RunQuery(connect, $"CREATE TABLE IF NOT EXISTS Telegram_{dbname} (MessageID TEXT, MessageDate TEXT, AuthorName TEXT, MessageText TEXT)");
                foreach (Message msg in myDeserializedClass.messages)
                {
                    if (msg.from == null || (msg.text as string) == null || msg.from == "" || (msg.text as string) == "") continue;

                    DB.RunQuery(connect, $"DELETE FROM Telegram_{dbname} WHERE MessageID = '{msg.id}' AND MessageDate = '{((DateTimeOffset)msg.date).ToUnixTimeSeconds()}' AND AuthorName = '{msg.from.Replace('\'', '.')}' AND MessageText = '{(msg.text as string).Replace('\'', '.')}'");
                    DB.RunQuery(connect, $"INSERT INTO Telegram_{dbname} VALUES('{msg.id}', '{((DateTimeOffset)msg.date).ToUnixTimeSeconds()}', '{msg.from.Replace('\'', '.')}', '{(msg.text as string).Replace('\'', '.')}');");
                }
            }
        }

        public List<Message> ReadDB(SQLiteConnection connection, string dbname, string UserName = null)
        {
            List<Message> messages = new List<Message>();

            List<List<string>> values = DB.ReadData(connection, dbname, 4, UserName != null ? $"AuthorName = '{UserName}'" : null); 
            foreach (List<string> value in values)
            {
                Message msg = new Message();
                msg.id = Convert.ToInt32(value[0]);
                msg.date = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value[1])).DateTime;
                msg.from = value[2];
                msg.text = value[3];
                messages.Add(msg);
            }

            return messages;
        }

        public class Message
        {
            public int id;
            public string type { get; set; }
            public DateTime date { get; set; }
            public string date_unixtime { get; set; }
            public string actor { get; set; }
            public string actor_id { get; set; }
            public string action { get; set; }
            public string title { get; set; }
            public object text { get; set; }
            public List<TextEntity> text_entities { get; set; }
            public string inviter { get; set; }
            public DateTime? edited { get; set; }
            public string edited_unixtime { get; set; }
            public string from { get; set; }
            public string from_id { get; set; }
            public string file { get; set; }
            public string thumbnail { get; set; }
            public string media_type { get; set; }
            public string sticker_emoji { get; set; }
            public int? width { get; set; }
            public int? height { get; set; }
            public List<string> members { get; set; }
            public string photo { get; set; }
            public int? reply_to_message_id { get; set; }
        }

        public class TextEntity
        {
            public string type { get; set; }
            public string text { get; set; }
            public int? user_id { get; set; }
        }
    }

    public class Root
    {
        public string name { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public List<TelegramFetcher.Message> messages { get; set; }
    }
}
