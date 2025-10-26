using MongoDB.Bson;
using MongoDB.Driver;
using Sheessential_Sales_Finance.Models;

namespace Sheessential_Sales_Finance.helpers
{
    public class MongoHelper
    {
        private readonly string _connectionString =
            "mongodb+srv://adrial:sarmiento@cluster0.ea8gtts.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";

        private IMongoDatabase? _database;
        private bool _isInitialized = false;

        private void EnsureConnection()
        {
            if (_isInitialized) return;

            try
            {
                var settings = MongoClientSettings.FromConnectionString(_connectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
                var client = new MongoClient(settings);

                // Ping to confirm connection
                client.GetDatabase("admin").RunCommand((Command<BsonDocument>)"{ping:1}");

                _database = client.GetDatabase("ShessentialsDB");
                _isInitialized = true;
            }
            catch (TimeoutException tex)
            {
                throw new ApplicationException("ConnectionTimeout", tex);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("NoConnection", ex);
            }
        }

        private IMongoCollection<T> GetCollection<T>(string name)
        {
            EnsureConnection();
            return _database!.GetCollection<T>(name);
        }

        // === Collections ===
        public IMongoCollection<User> Users => GetCollection<User>("users");
        public IMongoCollection<Invoice> Invoices => GetCollection<Invoice>("invoice");
        public IMongoCollection<Product> Inventories => GetCollection<Product>("inventory");
        public IMongoCollection<ProductSale> ProductSales => GetCollection<ProductSale>("ProductSales");
        public IMongoCollection<ActionLog> ActionLog => GetCollection<ActionLog>("action_log");
        public IMongoCollection<Vendor> Vendors => GetCollection<Vendor>("Vendors");


        // ✅ Safe query wrapper (centralized error handling)
        public List<T> SafeFindAll<T>(IMongoCollection<T> collection)
        {
            try
            {
                EnsureConnection();
                return collection.Find(_ => true).ToList();
            }
            catch (MongoConnectionException ex)
            {
                Console.WriteLine("⚠️ Mongo connection dropped: " + ex.Message);
                throw new ApplicationException("NoConnection", ex);
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine("⏰ Mongo operation timed out: " + ex.Message);
                throw new ApplicationException("ConnectionTimeout", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 Unknown Mongo error: " + ex.Message);
                throw new ApplicationException("DatabaseError", ex);
            }
        }
    }
}
