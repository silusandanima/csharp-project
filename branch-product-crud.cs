// ═══════════════════════════════════════════════════════════════
// BRANCH: feature/product-crud
// Purpose: All classes needed to perform CRUD on the Products table
// Depends on: DatabaseHelper (shared), EntityBase + IRepository (shared)
// ═══════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Configuration;

namespace CSharpProjectCRUD
{
    // ───────────────────────────────────────────────────────────
    // SHARED INFRASTRUCTURE
    // These classes are duplicated per branch for self-containment.
    // In production, move them to a shared "Core" project/assembly.
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Abstract base class. Every entity (Product, Supplier, etc.)
    /// inherits from this so they all share Id, timestamps, and a
    /// consistent API (GetDisplayName, IsValid, etc.).
    /// </summary>
    public abstract class EntityBase
    {
        // Id is readable by anyone but only settable by this class
        // and its children (protected set).
        public int Id { get; protected set; }
        public DateTime CreatedDate { get; protected set; }
        public DateTime? ModifiedDate { get; protected set; }   // nullable – null means never modified

        // These four are "abstract": no body here, every subclass MUST provide one.
        public abstract string GetDisplayName();
        public abstract string GetEntityType();
        public abstract bool IsValid();
        public abstract List<string> GetValidationErrors();

        // "virtual" means: has a default body, but subclasses MAY override.
        public virtual string GetSummary()
        {
            return $"{GetEntityType()}: {GetDisplayName()} (ID: {Id})";
        }

        // Stamp creation time; called from every constructor.
        public void MarkAsCreated()
        {
            CreatedDate = DateTime.Now;
            ModifiedDate = null;
        }

        // Stamp last-changed time; called from every property setter.
        public void MarkAsModified()
        {
            ModifiedDate = DateTime.Now;
        }

        protected EntityBase()
        {
            CreatedDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Generic CRUD contract. Any class that implements this promises to
    /// support GetAll, GetById, Add, Update, Delete, Exists, and GetCount.
    /// Using an interface here means the service layer never depends on a
    /// concrete repository — only on this contract.
    /// </summary>
    public interface IRepository<T> where T : EntityBase
    {
        List<T> GetAll();
        T GetById(int id);
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
        bool Exists(int id);
        int GetCount();
    }

    /// <summary>
    /// Contract for full-text search. ProductRepository implements this
    /// in addition to IRepository so callers can optionally cast and search.
    /// </summary>
    public interface ISearchable<T>
    {
        List<T> Search(string searchTerm);
    }

    /// <summary>
    /// Contract for stock management. Product (and Material) implement this,
    /// meaning one block of service code can manage stock for either type
    /// without caring which concrete class it holds.
    /// </summary>
    public interface IStockable
    {
        int StockQuantity { get; }
        void AddStock(int quantity);
        void RemoveStock(int quantity);
        bool IsInStock();
        bool IsInStock(int requiredQuantity);
    }

    // ───────────────────────────────────────────────────────────
    // PRODUCT ENTITY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a finished product that can be sold to a customer.
    /// Inherits EntityBase (gets Id + timestamps + abstract method contracts).
    /// Implements IStockable  → must provide AddStock / RemoveStock / IsInStock.
    /// Implements ISearchable → must provide a Search method.
    /// </summary>
    public class Product : EntityBase, IStockable, ISearchable<Product>
    {
        // Private backing fields – only the property setters may touch these.
        // This is encapsulation: external code cannot bypass validation.
        private string _productName;
        private string _description;
        private string _category;
        private decimal _price;
        private int _stockQuantity;

        // ── Properties ──────────────────────────────────────────
        // Each setter validates before storing, then calls MarkAsModified()
        // so EntityBase.ModifiedDate is always accurate.

        public new int Id                          // "new" hides base.Id to change the setter access level
        {
            get { return base.Id; }
            private set { base.Id = value; }       // only this class can assign Id
        }

        public string ProductName
        {
            get { return _productName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Product name cannot be empty");
                if (value.Length > 200)
                    throw new ArgumentException("Product name cannot exceed 200 characters");
                _productName = value.Trim();       // .Trim() removes accidental leading/trailing spaces
                MarkAsModified();                  // inherited from EntityBase
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value ?? string.Empty;  // null-safe: store "" rather than null
                MarkAsModified();
            }
        }

        public string Category
        {
            get { return _category; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Category cannot be empty");
                _category = value.Trim();
                MarkAsModified();
            }
        }

        public decimal Price
        {
            get { return _price; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Price cannot be negative");
                _price = Math.Round(value, 2);     // always store money as 2 d.p.
                MarkAsModified();
            }
        }

        // StockQuantity: public getter (anyone can read), private setter (only
        // AddStock / RemoveStock may change it, preserving business rules).
        public int StockQuantity
        {
            get { return _stockQuantity; }
            private set
            {
                if (value < 0)
                    throw new ArgumentException("Stock quantity cannot be negative");
                _stockQuantity = value;
            }
        }

        // ── Constructors ─────────────────────────────────────────

        // Default constructor – required by GenericRepository (uses new T()).
        public Product()
        {
            _productName = string.Empty;
            _description = string.Empty;
            _category = string.Empty;
            MarkAsCreated();
        }

        // Convenience constructor for quick creation with mandatory fields.
        public Product(string productName, decimal price, int stockQuantity)
        {
            ProductName = productName;             // goes through the validated setter
            Price = price;
            StockQuantity = stockQuantity;
            _description = string.Empty;
            _category = string.Empty;
            MarkAsCreated();
        }

        // ── EntityBase abstract overrides ────────────────────────

        // What to show when printing/logging a Product.
        public override string GetDisplayName() => $"{_productName} - {_price:C}";

        // Used by generic service methods to identify the type as a string.
        public override string GetEntityType() => "Product";

        // Quick yes/no validity check.
        public override bool IsValid()
            => !string.IsNullOrWhiteSpace(_productName) && _price >= 0 && _stockQuantity >= 0;

        // Detailed validation: returns every error found, not just the first.
        // Useful for displaying a list of problems to the user.
        public override List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(_productName))
                errors.Add("Product name is required");
            if (_price < 0)
                errors.Add("Price cannot be negative");
            if (_stockQuantity < 0)
                errors.Add("Stock quantity cannot be negative");
            return errors;
        }

        // ── IStockable implementation ────────────────────────────

        // AddStock and RemoveStock are the ONLY legal ways to change StockQuantity.
        // They contain domain rules (can't add 0 or negative, can't go below 0).
        public void AddStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");
            StockQuantity += quantity;
            MarkAsModified();
        }

        public void RemoveStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");
            if (StockQuantity < quantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {StockQuantity}");
            StockQuantity -= quantity;
            MarkAsModified();
        }

        // Two overloads: one just checks > 0, one checks a specific threshold.
        public bool IsInStock() => StockQuantity > 0;
        public bool IsInStock(int requiredQuantity) => StockQuantity >= requiredQuantity;

        // ── ISearchable<Product> implementation ──────────────────

        // Instance-level search (matches against this single product).
        // Repository-level search (across all rows) is in ProductRepository.Search().
        public List<Product> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Product> { this };

            return _productName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ? new List<Product> { this }
                : new List<Product>();
        }

        // ── Extra business logic ─────────────────────────────────

        // Calculates a sale price without modifying the stored price.
        public decimal GetDiscountedPrice(decimal discountPercentage)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentException("Discount must be between 0 and 100");
            return Math.Round(_price * (1 - discountPercentage / 100), 2);
        }

        public override string ToString() => GetDisplayName();
    }

    // ───────────────────────────────────────────────────────────
    // PRODUCT REPOSITORY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Handles all SQL for the Products table.
    /// Extends GenericRepository (inherits GetAll, Exists, GetCount, Delete, etc.)
    /// and overrides the methods that need custom SQL because the DB column
    /// is named "ProductId" not "Id".
    /// Also implements ISearchable so the service layer can do full-text search.
    /// </summary>
    public class ProductRepository : GenericRepository<Product>, ISearchable<Product>
    {
        // Pass "Products" as the table name to GenericRepository.
        public ProductRepository() : base("Products") { }

        // ── Override: column name mismatch ───────────────────────
        // The database uses "ProductId"; GenericRepository would look for "Id".
        // This override manually maps each column to the right property.
        protected override Product MapDataRowToEntity(DataRow row)
        {
            return new Product
            {
                Id          = Convert.ToInt32(row["ProductId"]),
                ProductName = row["ProductName"].ToString(),
                Description = row["Description"].ToString(),
                Category    = row["Category"].ToString(),
                Price       = Convert.ToDecimal(row["Price"]),
                StockQuantity = Convert.ToInt32(row["StockQuantity"])
            };
        }

        // Override GetById: uses "ProductId" in the WHERE clause.
        public override Product GetById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            string query = "SELECT * FROM Products WHERE ProductId = @Id";
            var parameters = new SqlParameter[] { new SqlParameter("@Id", id) };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
        }

        // Override Add: explicit INSERT with named parameters instead of reflection.
        // More reliable and easier to debug than the generic reflection approach.
        public override void Add(Product entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string query = @"
                INSERT INTO Products (ProductName, Description, Category, Price, StockQuantity)
                VALUES (@ProductName, @Description, @Category, @Price, @StockQuantity);
                SELECT SCOPE_IDENTITY();";         // returns the new auto-increment Id

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@ProductName",  entity.ProductName),
                new SqlParameter("@Description",  (object)entity.Description  ?? DBNull.Value),
                new SqlParameter("@Category",     (object)entity.Category     ?? DBNull.Value),
                new SqlParameter("@Price",        entity.Price),
                new SqlParameter("@StockQuantity", entity.StockQuantity)
            };

            object result = DatabaseHelper.ExecuteScalar(query, parameters);
            entity.Id = Convert.ToInt32(result);  // write the new DB-generated Id back onto the object
        }

        // Override Update: explicit UPDATE with ProductId in the WHERE clause.
        public override void Update(Product entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string query = @"
                UPDATE Products
                SET    ProductName   = @ProductName,
                       Description   = @Description,
                       Category      = @Category,
                       Price         = @Price,
                       StockQuantity = @StockQuantity
                WHERE  ProductId = @ProductId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@ProductName",   entity.ProductName),
                new SqlParameter("@Description",   (object)entity.Description  ?? DBNull.Value),
                new SqlParameter("@Category",      (object)entity.Category     ?? DBNull.Value),
                new SqlParameter("@Price",         entity.Price),
                new SqlParameter("@StockQuantity", entity.StockQuantity),
                new SqlParameter("@ProductId",     entity.Id)
            };

            DatabaseHelper.ExecuteNonQuery(query, parameters);
        }

        // Override Delete: uses "ProductId" column.
        public override void Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            string query = "DELETE FROM Products WHERE ProductId = @Id";
            DatabaseHelper.ExecuteNonQuery(query, new SqlParameter[] { new SqlParameter("@Id", id) });
        }

        // ── ISearchable<Product> ─────────────────────────────────
        // Full-text search across name, description, and category columns.
        // Uses SQL LIKE with % wildcards so "shirt" matches "T-Shirt", "shirt XL" etc.
        public List<Product> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Product>();

            string query = @"
                SELECT * FROM Products
                WHERE  ProductName  LIKE @SearchTerm
                OR     Description  LIKE @SearchTerm
                OR     Category     LIKE @SearchTerm";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@SearchTerm", $"%{searchTerm}%")
            };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
            var products = new List<Product>();
            foreach (DataRow row in dt.Rows)
                products.Add(MapDataRowToEntity(row));

            return products;
        }

        // ── Additional query methods ─────────────────────────────

        // Filter by category – useful for category-browsing UIs.
        public List<Product> GetProductsByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty");

            string query = "SELECT * FROM Products WHERE Category = @Category";
            var parameters = new SqlParameter[] { new SqlParameter("@Category", category) };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
            var products = new List<Product>();
            foreach (DataRow row in dt.Rows)
                products.Add(MapDataRowToEntity(row));

            return products;
        }

        // Only rows where stock > 0 – useful for the storefront.
        public List<Product> GetProductsInStock()
        {
            string query = "SELECT * FROM Products WHERE StockQuantity > 0";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);

            var products = new List<Product>();
            foreach (DataRow row in dt.Rows)
                products.Add(MapDataRowToEntity(row));

            return products;
        }
    }

    // ───────────────────────────────────────────────────────────
    // GENERIC REPOSITORY (shared base — included for self-containment)
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Provides default CRUD implementations for any EntityBase subclass.
    /// Uses reflection to build SQL dynamically, but every method is "virtual"
    /// so concrete repositories can override with hand-written SQL when needed.
    /// </summary>
    public class GenericRepository<T> : IRepository<T> where T : EntityBase, new()
    {
        protected readonly string _tableName;
        protected readonly string _idColumnName;

        public GenericRepository(string tableName, string idColumnName = "Id")
        {
            _tableName    = tableName    ?? throw new ArgumentNullException(nameof(tableName));
            _idColumnName = idColumnName;
        }

        public virtual List<T> GetAll()
        {
            var entities = new List<T>();
            DataTable dt = DatabaseHelper.ExecuteQuery($"SELECT * FROM {_tableName}");
            foreach (DataRow row in dt.Rows)
                entities.Add(MapDataRowToEntity(row));
            return entities;
        }

        public virtual T GetById(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            string query = $"SELECT * FROM {_tableName} WHERE {_idColumnName} = @Id";
            DataTable dt = DatabaseHelper.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@Id", id) });
            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
        }

        public virtual void Add(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var props = typeof(T).GetProperties().Where(p => p.Name != _idColumnName && p.Name != "Id").ToList();
            string columns = string.Join(", ", props.Select(p => p.Name));
            string values  = string.Join(", ", props.Select(p => $"@{p.Name}"));
            string query   = $"INSERT INTO {_tableName} ({columns}) VALUES ({values}); SELECT SCOPE_IDENTITY();";
            var parameters = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(entity) ?? DBNull.Value)).ToArray();
            object result  = DatabaseHelper.ExecuteScalar(query, parameters);
            (typeof(T).GetProperty(_idColumnName) ?? typeof(T).GetProperty("Id"))?.SetValue(entity, Convert.ToInt32(result));
        }

        public virtual void Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var props = typeof(T).GetProperties().Where(p => p.Name != _idColumnName && p.Name != "Id").ToList();
            string setClause = string.Join(", ", props.Select(p => $"{p.Name} = @{p.Name}"));
            string query = $"UPDATE {_tableName} SET {setClause} WHERE {_idColumnName} = @Id";
            var parameters = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(entity) ?? DBNull.Value)).ToList();
            parameters.Add(new SqlParameter("@Id", (typeof(T).GetProperty(_idColumnName) ?? typeof(T).GetProperty("Id"))?.GetValue(entity)));
            DatabaseHelper.ExecuteNonQuery(query, parameters.ToArray());
        }

        public virtual void Delete(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            DatabaseHelper.ExecuteNonQuery($"DELETE FROM {_tableName} WHERE {_idColumnName} = @Id",
                new SqlParameter[] { new SqlParameter("@Id", id) });
        }

        public virtual bool Exists(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            int count = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                $"SELECT COUNT(*) FROM {_tableName} WHERE {_idColumnName} = @Id",
                new SqlParameter[] { new SqlParameter("@Id", id) }));
            return count > 0;
        }

        public virtual int GetCount()
            => Convert.ToInt32(DatabaseHelper.ExecuteScalar($"SELECT COUNT(*) FROM {_tableName}"));

        // Reflection-based mapper: matches property names to column names automatically.
        protected virtual T MapDataRowToEntity(DataRow row)
        {
            T entity = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                if (row.Table.Columns.Contains(property.Name) && row[property.Name] != DBNull.Value)
                {
                    try { property.SetValue(entity, Convert.ChangeType(row[property.Name], property.PropertyType)); }
                    catch { /* skip unmappable columns */ }
                }
            }
            return entity;
        }
    }

    // ───────────────────────────────────────────────────────────
    // DATABASE HELPER (shared infrastructure)
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Static singleton that owns the connection string and provides
    /// three execution helpers: ExecuteQuery (SELECT), ExecuteNonQuery (INSERT/UPDATE/DELETE),
    /// and ExecuteScalar (single value, e.g. COUNT or SCOPE_IDENTITY).
    /// All methods use "using" blocks so connections are always returned to the pool.
    /// </summary>
    public static class DatabaseHelper
    {
        private static string _connectionString;
        private static readonly object _lockObject = new object();
        private static bool _isInitialized = false;

        // Call once at application startup with a known connection string.
        public static void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty");

            lock (_lockObject)                         // thread-safe: only one thread initializes
            {
                if (_isInitialized)
                    throw new InvalidOperationException("DatabaseHelper is already initialized");
                _connectionString = connectionString;
                _isInitialized = true;
            }
        }

        // Alternative: read from App.config <connectionStrings> section.
        public static void InitializeFromConfig()
        {
            string cs = ConfigurationManager.ConnectionStrings["CraftLink"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Connection string 'CraftLink' not found in config");
            Initialize(cs);
        }

        public static string GetConnectionString()
        {
            EnsureInitialized();
            return _connectionString;
        }

        // Lazy-init: if the caller hasn't called Initialize(), try config automatically.
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
                InitializeFromConfig();
        }

        private static SqlConnection CreateConnection()
        {
            EnsureInitialized();
            return new SqlConnection(_connectionString);
        }

        // Returns a DataTable (for SELECT queries).
        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = CreateConnection())
            using (var cmd  = new SqlCommand(query, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);   // opens connection, fills table, closes connection automatically
                    return dt;
                }
            }
        }

        // Returns rows-affected count (for INSERT / UPDATE / DELETE).
        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = CreateConnection())
            using (var cmd  = new SqlCommand(query, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        // Returns a single value (for COUNT(*), SCOPE_IDENTITY(), etc.).
        public static object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = CreateConnection())
            using (var cmd  = new SqlCommand(query, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }
    }
}
