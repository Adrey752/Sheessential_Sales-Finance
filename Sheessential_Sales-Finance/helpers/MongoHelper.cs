using MongoDB.Bson;
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
        private readonly string _hrDbName = "HumanResourcesDB";
        private readonly string _payrollDbName = "sia_payroll_db";
        private readonly string _salesFinanceDbName = "SalesAndFinanceDB";

        private IMongoDatabase? _primaryDatabase;
        private IMongoDatabase? _secondaryDatabase;
        private IMongoDatabase? _inventoryDatabase;
        private IMongoDatabase? _hrDatabase;
        private IMongoDatabase? _payrollDatabase;
        private IMongoDatabase? _salesFinanceDatabase;

        private bool _isInitialized = false;

        // --- CONNECTION INITIALIZATION ---
        private void EnsureConnection()
        {
            if (_isInitialized) return;

            // Initialize Primary Connection
            _primaryDatabase = InitializeDatabase(_primaryConnectionString, _primaryDbName);

            // Initialize Secondary Connection
            _secondaryDatabase = InitializeDatabase(_secondaryConnectionString, _secondaryDbName);
            _inventoryDatabase = InitializeDatabase(_secondaryConnectionString, $"" +
                $"{_inventoryDbName}");
            _hrDatabase = InitializeDatabase(_secondaryConnectionString, _hrDbName);
            _payrollDatabase = InitializeDatabase(_secondaryConnectionString, _payrollDbName);
            _salesFinanceDatabase = InitializeDatabase(_primaryConnectionString, _salesFinanceDbName);

            MigrateFinanceCollectionsToSalesFinanceDb();

            _isInitialized = true;
        }

        private void MigrateFinanceCollectionsToSalesFinanceDb()
        {
            if (_primaryDatabase == null || _salesFinanceDatabase == null)
                return;

            var collectionNames = new[] { "Expenses", "Balance", "PaymentTransaction" };

            foreach (var collectionName in collectionNames)
            {
                try
                {
                    var source = _primaryDatabase.GetCollection<BsonDocument>(collectionName);
                    var target = _salesFinanceDatabase.GetCollection<BsonDocument>(collectionName);

                    var targetCount = target.EstimatedDocumentCount();
                    if (targetCount > 0)
                        continue;

                    var sourceDocs = source.Find(FilterDefinition<BsonDocument>.Empty).ToList();
                    if (sourceDocs.Count == 0)
                        continue;

                    target.InsertMany(sourceDocs);
                    _logger.LogInformation("Migrated {Count} docs from {SourceDb}.{Collection} to {TargetDb}.{Collection}",
                        sourceDocs.Count, _primaryDbName, collectionName, _salesFinanceDbName, collectionName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipped migration for collection {Collection}", collectionName);
                }
            }
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
        private IMongoCollection<T> GetHrCollection<T>(string name)
        {
            if (_hrDatabase == null) EnsureConnection();
            return _hrDatabase!.GetCollection<T>(name);
        }
        private IMongoCollection<T> GetPayrollCollection<T>(string name)
        {
            if (_payrollDatabase == null) EnsureConnection();
            return _payrollDatabase!.GetCollection<T>(name);
        }
        private IMongoCollection<T> GetSalesFinanceCollection<T>(string name)
        {
            if (_salesFinanceDatabase == null) EnsureConnection();
            return _salesFinanceDatabase!.GetCollection<T>(name);
        }


        // === PRIMARY DATABASE COLLECTIONS (ShessentialsDB) ===
        public IMongoCollection<User> Users => GetPrimaryCollection<User>("users");
        public IMongoCollection<Invoice> Invoices => GetPrimaryCollection<Invoice>("invoice");
        public IMongoCollection<Products> Inventories => GetPrimaryCollection<Products>("inventory");
        public IMongoCollection<ProductSales> ProductSales => GetPrimaryCollection<ProductSales>("ProductSales");
        public IMongoCollection<ActionLog> ActionLog => GetPrimaryCollection<ActionLog>("action_log");
        public IMongoCollection<Vendor> Vendors => GetPrimaryCollection<Vendor>("Vendors");
        public IMongoCollection<Expenses> LegacyExpenses => GetPrimaryCollection<Expenses>("Expenses");
        public IMongoCollection<Expenses> Expenses => GetSalesFinanceCollection<Expenses>("Expenses");
        public IMongoCollection<Balance> Balance => GetSalesFinanceCollection<Balance>("Balance");
        public IMongoCollection<PaymentTransaction> PaymentTransactions => GetSalesFinanceCollection<PaymentTransaction>("PaymentTransaction");


        // === SECONDARY DATABASE COLLECTIONS (db_shessentials) ===
        public IMongoCollection<TbOrder> TbOrder => GetSecondaryCollection<TbOrder>("tbl_order");

        // FIX: Renamed property to avoid conflict
        public IMongoCollection<TbUser> TbUserCollection => GetSecondaryCollection<TbUser>("tbl_user");
        public IMongoCollection<InventoryProducts> ProductInventory => GetInventoryCollection<InventoryProducts>("Products");

        public IMongoCollection<ProductVariant> ProductVariantInventory => GetInventoryCollection<ProductVariant>("ProductVariants");
        public IMongoCollection<IngredientStockRequests> IngredientsStockRequests => GetInventoryCollection<IngredientStockRequests>("IngredientStockRequests");
        public IMongoCollection<Ingredient> Ingredients => GetInventoryCollection<Ingredient>("Ingredients");
        public IMongoCollection<InventoryUser> InventoryUsers => GetInventoryCollection<InventoryUser>("Users");
        public IMongoCollection<Supplier> Suppliers => GetInventoryCollection<Supplier>("Suppliers");
        public IMongoCollection<PayrollRun> ParyrollRuns => GetHrCollection<PayrollRun>("PayRuns");
        public IMongoCollection<BsonDocument> HrEmployees => GetHrCollection<BsonDocument>("Employees");


        public IMongoCollection<InventoryProductSales> ProductSalesInventory => GetInventoryCollection<InventoryProductSales>("ProductSales");


        // === PAYROLL DATABASE COLLECTIONS (sia_payroll_db) ===
        public IMongoCollection<PayrollSnapshot> PayrollSnapshots =>
            GetPayrollCollection<PayrollSnapshot>("PayrollSnapshots");

        public async Task<List<string>> GetHumanResourceCollectionNamesAsync()
        {
            EnsureConnection();
            return _hrDatabase == null
                ? new List<string>()
                : await _hrDatabase.ListCollectionNames().ToListAsync();
        }

        public async Task<List<string>> GetHumanResourceCollectionAttributesAsync(string collectionName, int sampleSize = 50)
        {
            EnsureConnection();

            if (_hrDatabase == null || string.IsNullOrWhiteSpace(collectionName))
                return new List<string>();

            var collection = _hrDatabase.GetCollection<BsonDocument>(collectionName);
            var docs = await collection
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Limit(sampleSize)
                .ToListAsync();

            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var doc in docs)
            {
                CollectFieldNames(doc, fields, string.Empty);
            }

            return fields.OrderBy(x => x).ToList();
        }

        private static void CollectFieldNames(BsonDocument doc, HashSet<string> fields, string prefix)
        {
            foreach (var element in doc.Elements)
            {
                var fieldName = string.IsNullOrEmpty(prefix)
                    ? element.Name
                    : $"{prefix}.{element.Name}";

                fields.Add(fieldName);

                if (element.Value.IsBsonDocument)
                {
                    CollectFieldNames(element.Value.AsBsonDocument, fields, fieldName);
                }
            }
        }

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