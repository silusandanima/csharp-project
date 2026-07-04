// ═══════════════════════════════════════════════════════════════
// BRANCH: feature/material-crud
// Purpose: All classes needed to perform CRUD on the Materials table
// Depends on: DatabaseHelper (shared), EntityBase + IRepository (shared)
// Note: Materials are linked to Suppliers via SupplierId (FK).
//       Supplier entity is included here for FK-validation convenience.
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
    // ───────────────────────────────────────────────────────────

    public abstract class EntityBase
    {
        public int Id { get; protected set; }
        public DateTime CreatedDate { get; protected set; }
        public DateTime? ModifiedDate { get; protected set; }

        public abstract string GetDisplayName();
        public abstract string GetEntityType();
        public abstract bool IsValid();
        public abstract List<string> GetValidationErrors();

        public virtual string GetSummary()
            => $"{GetEntityType()}: {GetDisplayName()} (ID: {Id})";

        public void MarkAsCreated() { CreatedDate = DateTime.Now; ModifiedDate = null; }
        public void MarkAsModified() { ModifiedDate = DateTime.Now; }
        protected EntityBase() { CreatedDate = DateTime.Now; }
    }

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
    /// IStockable is implemented by both Material and Product.
    /// A single utility method can manage stock for either type without
    /// knowing which concrete class it holds — classic polymorphism.
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
    // MATERIAL ENTITY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a raw material used to manufacture products.
    /// Inherits EntityBase → Id, timestamps, abstract method contract.
    /// Implements IStockable → stock can be incremented/decremented safely.
    ///
    /// Key design: Quantity is private-set; only AddStock / RemoveStock
    /// may change it, which enforces the "never go negative" rule.
    /// </summary>
    public class Material : EntityBase, IStockable
    {
        private string _materialName;
        private int    _quantity;
        private string _unit;
        private int    _supplierId;

        // ── Properties ──────────────────────────────────────────

        public new int Id
        {
            get { return base.Id; }
            private set { base.Id = value; }
        }

        public string MaterialName
        {
            get { return _materialName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Material name cannot be empty");
                _materialName = value.Trim();
                MarkAsModified();
            }
        }

        // Private setter: external code MUST use AddStock / RemoveStock.
        public int Quantity
        {
            get { return _quantity; }
            private set
            {
                if (value < 0)
                    throw new ArgumentException("Quantity cannot be negative");
                _quantity = value;
            }
        }

        public string Unit
        {
            get { return _unit; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Unit cannot be empty");
                _unit = value.Trim();
                MarkAsModified();
            }
        }

        // Foreign key to the Suppliers table.
        // Validated > 0 here so a bad FK is caught before hitting the DB.
        public int SupplierId
        {
            get { return _supplierId; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Supplier ID must be greater than 0");
                _supplierId = value;
                MarkAsModified();
            }
        }

        // IStockable requires a "StockQuantity" property;
        // we expose Quantity under that name via an expression-body property.
        public int StockQuantity => Quantity;

        // ── Constructors ─────────────────────────────────────────

        // Default constructor: required by GenericRepository's "new T()" call.
        public Material()
        {
            _materialName = string.Empty;
            _unit         = string.Empty;
            MarkAsCreated();
        }

        // Full constructor: all mandatory fields in one call.
        public Material(string materialName, int quantity, string unit, int supplierId)
        {
            MaterialName = materialName;
            Quantity     = quantity;
            Unit         = unit;
            SupplierId   = supplierId;
            MarkAsCreated();
        }

        // ── EntityBase abstract overrides ────────────────────────

        // e.g. "Cotton Thread - 500 meters"
        public override string GetDisplayName()
            => $"{_materialName} - {_quantity} {_unit}";

        public override string GetEntityType() => "Material";

        public override bool IsValid()
            => !string.IsNullOrWhiteSpace(_materialName) && _quantity >= 0 && _supplierId > 0;

        public override List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(_materialName))
                errors.Add("Material name is required");
            if (_quantity < 0)
                errors.Add("Quantity cannot be negative");
            if (_supplierId <= 0)
                errors.Add("Supplier ID is required");
            return errors;
        }

        // ── IStockable implementation ────────────────────────────

        // The only legal ways to change Quantity.
        // Domain rules (positive delta, no overdraw) are enforced here,
        // not scattered across the application.

        public void AddStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");
            Quantity += quantity;   // goes through the private setter (validates >= 0)
            MarkAsModified();
        }

        public void RemoveStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");
            if (Quantity < quantity)
                throw new InvalidOperationException(
                    $"Insufficient material. Available: {Quantity} {_unit}");
            Quantity -= quantity;
            MarkAsModified();
        }

        public bool IsInStock() => Quantity > 0;
        public bool IsInStock(int requiredQuantity) => Quantity >= requiredQuantity;

        public override string ToString() => GetDisplayName();
    }

    // ───────────────────────────────────────────────────────────
    // PRODUCT-MATERIAL LINK ENTITY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Junction / many-to-many entity: one product can require many materials;
    /// one material can be used by many products.
    /// Stores how many units of a given material are needed per product.
    /// </summary>
    public class ProductMaterial : EntityBase
    {
        private int _productId;
        private int _materialId;
        private int _quantityRequired;

        public new int Id
        {
            get { return base.Id; }
            private set { base.Id = value; }
        }

        // FK → Products table
        public int ProductId
        {
            get { return _productId; }
            set
            {
                if (value <= 0) throw new ArgumentException("Product ID must be greater than 0");
                _productId = value;
                MarkAsModified();
            }
        }

        // FK → Materials table
        public int MaterialId
        {
            get { return _materialId; }
            set
            {
                if (value <= 0) throw new ArgumentException("Material ID must be greater than 0");
                _materialId = value;
                MarkAsModified();
            }
        }

        // How many units of the material are consumed to make one unit of the product.
        public int QuantityRequired
        {
            get { return _quantityRequired; }
            set
            {
                if (value <= 0) throw new ArgumentException("Quantity required must be positive");
                _quantityRequired = value;
                MarkAsModified();
            }
        }

        public ProductMaterial() { MarkAsCreated(); }

        public ProductMaterial(int productId, int materialId, int quantityRequired)
        {
            ProductId        = productId;
            MaterialId       = materialId;
            QuantityRequired = quantityRequired;
            MarkAsCreated();
        }

        public override string GetDisplayName()
            => $"Product {_productId} requires {_quantityRequired} of Material {_materialId}";

        public override string GetEntityType() => "ProductMaterial";

        public override bool IsValid()
            => _productId > 0 && _materialId > 0 && _quantityRequired > 0;

        public override List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (_productId <= 0)        errors.Add("Product ID is required");
            if (_materialId <= 0)       errors.Add("Material ID is required");
            if (_quantityRequired <= 0) errors.Add("Quantity required must be positive");
            return errors;
        }

        public override string ToString() => GetDisplayName();
    }

    // ───────────────────────────────────────────────────────────
    // MATERIAL REPOSITORY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Handles all SQL for the Materials table.
    /// Overrides the GenericRepository methods that need custom SQL
    /// because the DB PK is "MaterialId" not "Id".
    /// </summary>
    public class MaterialRepository : GenericRepository<Material>
    {
        public MaterialRepository() : base("Materials", "MaterialId") { }

        protected override Material MapDataRowToEntity(DataRow row)
        {
            return new Material
            {
                Id           = Convert.ToInt32(row["MaterialId"]),
                MaterialName = row["MaterialName"].ToString(),
                Quantity     = Convert.ToInt32(row["Quantity"]),
                Unit         = row["Unit"].ToString(),
                SupplierId   = Convert.ToInt32(row["SupplierId"])
            };
        }

        public override Material GetById(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            DataTable dt = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM Materials WHERE MaterialId = @Id",
                new SqlParameter[] { new SqlParameter("@Id", id) });
            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
        }

        public override void Add(Material entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string query = @"
                INSERT INTO Materials (MaterialName, Quantity, Unit, SupplierId)
                VALUES (@MaterialName, @Quantity, @Unit, @SupplierId);
                SELECT SCOPE_IDENTITY();";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@MaterialName", entity.MaterialName),
                new SqlParameter("@Quantity",     entity.Quantity),
                new SqlParameter("@Unit",         entity.Unit),
                new SqlParameter("@SupplierId",   entity.SupplierId)
            };

            object result = DatabaseHelper.ExecuteScalar(query, parameters);
            entity.Id = Convert.ToInt32(result);
        }

        public override void Update(Material entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string query = @"
                UPDATE Materials
                SET    MaterialName = @MaterialName,
                       Quantity     = @Quantity,
                       Unit         = @Unit,
                       SupplierId   = @SupplierId
                WHERE  MaterialId = @MaterialId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@MaterialName", entity.MaterialName),
                new SqlParameter("@Quantity",     entity.Quantity),
                new SqlParameter("@Unit",         entity.Unit),
                new SqlParameter("@SupplierId",   entity.SupplierId),
                new SqlParameter("@MaterialId",   entity.Id)
            };

            DatabaseHelper.ExecuteNonQuery(query, parameters);
        }

        public override void Delete(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            DatabaseHelper.ExecuteNonQuery(
                "DELETE FROM Materials WHERE MaterialId = @Id",
                new SqlParameter[] { new SqlParameter("@Id", id) });
        }

        // ── Additional queries ───────────────────────────────────

        // Get all materials supplied by a specific supplier.
        // Useful when a supplier is being reviewed or replaced.
        public List<Material> GetMaterialsBySupplier(int supplierId)
        {
            if (supplierId <= 0)
                throw new ArgumentException("Supplier ID must be greater than 0");

            DataTable dt = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM Materials WHERE SupplierId = @SupplierId",
                new SqlParameter[] { new SqlParameter("@SupplierId", supplierId) });

            var materials = new List<Material>();
            foreach (DataRow row in dt.Rows)
                materials.Add(MapDataRowToEntity(row));

            return materials;
        }

        // Get materials that are below a minimum threshold (reorder alert).
        public List<Material> GetLowStockMaterials(int threshold = 10)
        {
            DataTable dt = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM Materials WHERE Quantity <= @Threshold",
                new SqlParameter[] { new SqlParameter("@Threshold", threshold) });

            var materials = new List<Material>();
            foreach (DataRow row in dt.Rows)
                materials.Add(MapDataRowToEntity(row));

            return materials;
        }
    }

    // ───────────────────────────────────────────────────────────
    // PRODUCT-MATERIAL REPOSITORY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Manages the junction table between Products and Materials.
    /// </summary>
    public class ProductMaterialRepository : GenericRepository<ProductMaterial>
    {
        public ProductMaterialRepository() : base("ProductMaterials", "Id") { }

        protected override ProductMaterial MapDataRowToEntity(DataRow row)
        {
            return new ProductMaterial
            {
                Id               = Convert.ToInt32(row["Id"]),
                ProductId        = Convert.ToInt32(row["ProductId"]),
                MaterialId       = Convert.ToInt32(row["MaterialId"]),
                QuantityRequired = Convert.ToInt32(row["QuantityRequired"])
            };
        }

        public override void Add(ProductMaterial entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string query = @"
                INSERT INTO ProductMaterials (ProductId, MaterialId, QuantityRequired)
                VALUES (@ProductId, @MaterialId, @QuantityRequired);
                SELECT SCOPE_IDENTITY();";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@ProductId",        entity.ProductId),
                new SqlParameter("@MaterialId",       entity.MaterialId),
                new SqlParameter("@QuantityRequired", entity.QuantityRequired)
            };

            object result = DatabaseHelper.ExecuteScalar(query, parameters);
            entity.Id = Convert.ToInt32(result);
        }

        // Retrieve the full bill of materials for a single product.
        public List<ProductMaterial> GetMaterialsForProduct(int productId)
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be greater than 0");

            DataTable dt = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM ProductMaterials WHERE ProductId = @ProductId",
                new SqlParameter[] { new SqlParameter("@ProductId", productId) });

            var list = new List<ProductMaterial>();
            foreach (DataRow row in dt.Rows)
                list.Add(MapDataRowToEntity(row));

            return list;
        }

        // Remove all material requirements for a product (e.g. when reformulating).
        public void DeleteAllForProduct(int productId)
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be greater than 0");

            DatabaseHelper.ExecuteNonQuery(
                "DELETE FROM ProductMaterials WHERE ProductId = @ProductId",
                new SqlParameter[] { new SqlParameter("@ProductId", productId) });
        }
    }

    // ───────────────────────────────────────────────────────────
    // GENERIC REPOSITORY (shared base)
    // ───────────────────────────────────────────────────────────

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
            foreach (DataRow row in DatabaseHelper.ExecuteQuery($"SELECT * FROM {_tableName}").Rows)
                entities.Add(MapDataRowToEntity(row));
            return entities;
        }

        public virtual T GetById(int id)
        {
            if (id <= 0) throw new ArgumentException("ID must be greater than 0");
            DataTable dt = DatabaseHelper.ExecuteQuery(
                $"SELECT * FROM {_tableName} WHERE {_idColumnName} = @Id",
                new SqlParameter[] { new SqlParameter("@Id", id) });
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
            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                $"SELECT COUNT(*) FROM {_tableName} WHERE {_idColumnName} = @Id",
                new SqlParameter[] { new SqlParameter("@Id", id) })) > 0;
        }

        public virtual int GetCount()
            => Convert.ToInt32(DatabaseHelper.ExecuteScalar($"SELECT COUNT(*) FROM {_tableName}"));

        protected virtual T MapDataRowToEntity(DataRow row)
        {
            T entity = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                if (row.Table.Columns.Contains(property.Name) && row[property.Name] != DBNull.Value)
                {
                    try { property.SetValue(entity, Convert.ChangeType(row[property.Name], property.PropertyType)); }
                    catch { }
                }
            }
            return entity;
        }
    }

    // ───────────────────────────────────────────────────────────
    // DATABASE HELPER (shared infrastructure)
    // ───────────────────────────────────────────────────────────

    public static class DatabaseHelper
    {
        private static string _connectionString;
        private static readonly object _lockObject = new object();
        private static bool _isInitialized = false;

        public static void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty");
            lock (_lockObject)
            {
                if (_isInitialized)
                    throw new InvalidOperationException("DatabaseHelper is already initialized");
                _connectionString = connectionString;
                _isInitialized = true;
            }
        }

        public static void InitializeFromConfig()
        {
            string cs = ConfigurationManager.ConnectionStrings["CraftLink"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Connection string 'CraftLink' not found in config");
            Initialize(cs);
        }

        public static string GetConnectionString() { EnsureInitialized(); return _connectionString; }
        private static void EnsureInitialized() { if (!_isInitialized) InitializeFromConfig(); }
        private static SqlConnection CreateConnection() { EnsureInitialized(); return new SqlConnection(_connectionString); }

        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = CreateConnection())
            using (var cmd  = new SqlCommand(query, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(cmd))
                { var dt = new DataTable(); adapter.Fill(dt); return dt; }
            }
        }

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
// Fresh update
