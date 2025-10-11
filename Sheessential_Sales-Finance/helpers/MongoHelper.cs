using MongoDB.Driver;
using Sheessential_Sales_Finance.Models;

namespace Sheessential_Sales_Finance.helpers
{
    public class MongoHelper
    {
        private readonly IMongoDatabase _database;

        public MongoHelper()
        {
            var connectionString = "mongodb+srv://adrial:sarmiento@cluster0.ea8gtts.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("ShessentialsDB"); 
        }

        private IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }

        public IMongoCollection<User> Users => GetCollection<User>("users");
        public IMongoCollection<Inventory> Inventories => GetCollection<Inventory>("inventory");
        public IMongoCollection<Sale> Sales => GetCollection<Sale>("sales");
    }
}
