using LiteDB;

namespace ClientMessenger
{
    internal static class LocalUserDatabase
    {
        private static string _connectionString = $"Filename=C:\\Users\\Crist\\source\\repos\\ClientMessenger\\ClientMessenger\\Databases\\Userdata.db; Password=";

        public static void AddPassword(string password)
        {
            _connectionString += password;
        }

        public static void SaveorUpdateUserdata(User user)
        {
            var conn = new LiteDatabase(_connectionString);
            ILiteCollection<User> collection = conn.GetCollection<User>("User");
            collection.Upsert(new BsonValue("user"), user);
        }

        public static User GetUserdata()
        {
            var conn = new LiteDatabase(_connectionString);
            ILiteCollection<User> collection = conn.GetCollection<User>("User");
            return collection.Find(x => x.Email != "").First();
        }
    }
}
