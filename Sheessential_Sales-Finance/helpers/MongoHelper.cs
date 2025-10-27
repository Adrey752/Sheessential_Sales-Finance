using MongoDB.Bson;
using MongoDB.Driver;
using Sheessential_Sales_Finance.Models;

namespace Sheessential_Sales_Finance.helpers
{
    public class MongoHelper
    {
        private readonly ILogger<MongoHelper> _logger;
        public MongoHelper(ILogger<MongoHelper> logger) { 
            _logger = logger;
        }
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
                _logger.LogInformation("\n\n\n⚠️ Mongo connection dropped: " + tex.Message+"\n\n\n");
                throw new ApplicationException("ConnectionTimeout", tex);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("\n\n\n⚠️ Mongo connection dropped: " + ex.Message+"\n\n\n");
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
        public IMongoCollection<Expenses> Expenses => GetCollection<Expenses>("Expenses");


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
                Console.WriteLine();
                _logger.LogInformation("\n\n\n⚠️ Mongo connection dropped: " + ex.Message+"\n\n\n");
                throw new ApplicationException("NoConnection", ex);
            }
            catch (TimeoutException ex)
            {
                _logger.LogInformation("\n\n\n⚠️ Mongo connection timeout: " + ex.Message+"\n\n\n");
                throw new ApplicationException("ConnectionTimeout", ex);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("\n\n\n⚠️ Unknown error: " + ex.Message+"\n\n\n");
                throw new ApplicationException("DatabaseError", ex);
            }
        }
    }
}
