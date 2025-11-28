using MongoDB.Bson;
using MongoDB.Driver;
using Sheessential_Sales_Finance.Models;

namespace Sheessential_Sales_Finance.helpers
{
    public class MongoHelper
    {
        private readonly ILogger<MongoHelper> _logger;
        public MongoHelper(ILogger<MongoHelper> logger)
        {
            _logger = logger;
            EnsureConnection(); // Initialize default connection immediately
        }

        // --- CONNECTION DETAILS ---
        // PRIMARY DATABASE (Cluster0, ShessentialsDB)
        private readonly string _primaryConnectionString =
            "mongodb+srv://adrial:sarmiento@cluster0.ea8gtts.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
        private readonly string _primaryDbName = "ShessentialsDB";

        // SECONDARY DATABASE (Cluster0.1uursjj, db_shessentials)
        private readonly string _secondaryConnectionString =
            "mongodb+srv://sarmiento:adrial@cluster0.1uursjj.mongodb.net/";
        private readonly string _secondaryDbName = "db_shessentials";
        private readonly string _inventoryDbName = "InventorySystemDB";


        private IMongoDatabase? _primaryDatabase;
        private IMongoDatabase? _secondaryDatabase; 
        private IMongoDatabase? _inventoryDatabase; 

        private bool _isInitialized = false;

        // --- CONNECTION INITIALIZATION ---
        private void EnsureConnection()
        {
            if (_isInitialized) return;

            // Initialize Primary Connection
            _primaryDatabase = InitializeDatabase(_primaryConnectionString, _primaryDbName);

            // Initialize Secondary Connection
            _secondaryDatabase = InitializeDatabase(_secondaryConnectionString, _secondaryDbName);
            _inventoryDatabase = InitializeDatabase(_secondaryConnectionString, _inventoryDbName);

            _isInitialized = true;
        }

        private IMongoDatabase InitializeDatabase(string connectionString, string databaseName)
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
                var client = new MongoClient(settings);

                // Ping is optional, but good for connection check
                client.GetDatabase("admin").RunCommand((Command<BsonDocument>)"{ping:1}");

                return client.GetDatabase(databaseName);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"\n\n\n⚠️ Mongo connection dropped for {databaseName}: " + ex.Message + "\n\n\n");
                throw new ApplicationException($"Connection error to {databaseName}", ex);
            }
        }

        // --- COLLECTION GETTERS ---

        // Helper to get collections from Primary DB
        private IMongoCollection<T> GetPrimaryCollection<T>(string name)
        {
            if (_primaryDatabase == null) EnsureConnection();
            return _primaryDatabase!.GetCollection<T>(name);
        }

        // Helper to get collections from Secondary DB
        private IMongoCollection<T> GetSecondaryCollection<T>(string name)
        {
            if (_secondaryDatabase == null) EnsureConnection();
            return _secondaryDatabase!.GetCollection<T>(name);
        }        
        private IMongoCollection<T> GetInventoryCollection<T>(string name)
        {
            if (_inventoryDatabase == null) EnsureConnection();
            return _inventoryDatabase!.GetCollection<T>(name);
        }


        // === PRIMARY DATABASE COLLECTIONS (ShessentialsDB) ===
        public IMongoCollection<User> Users => GetPrimaryCollection<User>("users");
        public IMongoCollection<Invoice> Invoices => GetPrimaryCollection<Invoice>("invoice");
        public IMongoCollection<Products> Inventories => GetPrimaryCollection<Products>("inventory");
        public IMongoCollection<ProductSales> ProductSales => GetPrimaryCollection<ProductSales>("ProductSales");
        public IMongoCollection<ActionLog> ActionLog => GetPrimaryCollection<ActionLog>("action_log");
        public IMongoCollection<Vendor> Vendors => GetPrimaryCollection<Vendor>("Vendors");
        public IMongoCollection<Expenses> Expenses => GetPrimaryCollection<Expenses>("Expenses");
        public IMongoCollection<Balance> Balance => GetPrimaryCollection<Balance>("Balance");


        // === SECONDARY DATABASE COLLECTIONS (db_shessentials) ===

        // New Collections using the SECONDARY Database helper
        public IMongoCollection<TbOrder> TbOrder => GetSecondaryCollection<TbOrder>("tbl_order");

        // FIX: Renamed property to avoid conflict
        public IMongoCollection<TbUser> TbUserCollection => GetSecondaryCollection<TbUser>("tbl_user");
        public IMongoCollection<Product> ProductInventory => GetInventoryCollection<Product>("Products");
        public IMongoCollection<ProductVariant> ProductVariantInventory => GetInventoryCollection<ProductVariant>("ProductVariants");
        public IMongoCollection<IngredientStockRequests> IngredientsStockRequests => GetInventoryCollection<IngredientStockRequests>("IngredientStockRequests");
        public IMongoCollection<Ingredient> Ingredients => GetInventoryCollection<Ingredient>("Ingredients");
        public IMongoCollection<InventoryUser> InventoryUsers => GetInventoryCollection<InventoryUser>("Users");
        public IMongoCollection<Supplier> Suppliers => GetInventoryCollection<Supplier>("Suppliers");




        // ✅ Safe query wrapper (updated to use EnsureConnection)
        public List<T> SafeFindAll<T>(IMongoCollection<T> collection)
        {
            try
            {
                // Ensure connections are up before querying
                EnsureConnection();
                return collection.Find(_ => true).ToList();
            }
            // ... (rest of the catch blocks are fine) ...
            catch (MongoConnectionException ex)
            {
                Console.WriteLine();
                _logger.LogInformation("\n\n\n⚠️ Mongo connection dropped: " + ex.Message + "\n\n\n");
                throw new ApplicationException("NoConnection", ex);
            }
            catch (TimeoutException ex)
            {
                _logger.LogInformation("\n\n\n⚠️ Mongo connection timeout: " + ex.Message + "\n\n\n");
                throw new ApplicationException("ConnectionTimeout", ex);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("\n\n\n⚠️ Unknown error: " + ex.Message + "\n\n\n");
                throw new ApplicationException("DatabaseError", ex);
            }
        }
    }
}