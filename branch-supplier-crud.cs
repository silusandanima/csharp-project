// ═══════════════════════════════════════════════════════════════
// BRANCH: feature/supplier-crud
// Purpose: All classes needed to perform CRUD on the Suppliers table
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
    // SHARED INFRASTRUCTURE (EntityBase, IRepository, ISearchable,
    // GenericRepository, DatabaseHelper) — same as product branch.
    // In a real multi-branch project these live in a shared "Core"
    // assembly that every feature branch references.
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

    public interface ISearchable<T>
    {
        List<T> Search(string searchTerm);
    }

    // ───────────────────────────────────────────────────────────
    // SUPPLIER ENTITY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a company or person who supplies raw materials.
    /// Inherits EntityBase → gets Id, timestamps, and must implement
    /// GetDisplayName / GetEntityType / IsValid / GetValidationErrors.
    /// Implements ISearchable → must provide a Search method.
    /// </summary>
    public class Supplier : EntityBase, ISearchable<Supplier>
    {
        private string _supplierName;
        private string _phone;
        private string _address;

        // ── Properties ──────────────────────────────────────────

        // Hides base.Id so the setter is private to this class.
        public new int Id
        {
            get { return base.Id; }
            private set { base.Id = value; }
        }

        public string SupplierName
        {
            get { return _supplierName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Supplier name cannot be empty");
                _supplierName = value.Trim();
                MarkAsModified();      // records that something changed
            }
        }

        public string Phone
        {
            get { return _phone; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Strip non-digits so "+1 (555) 000-1234" → "15550001234" for length check.
                    string digitsOnly = new string(value.Where(char.IsDigit).ToArray());
                    if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
                        throw new ArgumentException("Phone must have 10-15 digits");
                    _phone = value.Trim();
                }
                else
                {
                    _phone = string.Empty;   // phone is optional — store "" rather than null
                }
                MarkAsModified();
            }
        }

        public string Address
        {
            get { return _address; }
            set
            {
                _address = value ?? string.Empty;  // null-safe
                MarkAsModified();
            }
        }

        // ── Constructors ─────────────────────────────────────────

        // Default constructor: required by GenericRepository's "new T()" call.
        public Supplier()
        {
            _supplierName = string.Empty;
            _phone        = string.Empty;
            _address      = string.Empty;
            MarkAsCreated();
        }

        // Convenience constructor: validates via property setters automatically.
        public Supplier(string supplierName, string phone)
        {
            SupplierName = supplierName;
            Phone        = phone;
            _address     = string.Empty;
            MarkAsCreated();
        }

        // ── EntityBase abstract overrides ────────────────────────

        // Human-readable label, e.g. "Acme Corp (0112345678)"
        public override string GetDisplayName()
            => $"{_supplierName} ({_phone})";

        public override string GetEntityType() => "Supplier";

        // Minimal validity: at least the name must be set.
        public override bool IsValid()
            => !string.IsNullOrWhiteSpace(_supplierName);

        // Full error list: collects ALL problems rather than stopping at the first.
        public override List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(_supplierName))
                errors.Add("Supplier name is required");
            return errors;
        }

        // ── ISearchable<Supplier> implementation ─────────────────

        // Instance-level search (checks whether THIS supplier matches).
        // Repository-level search (across all DB rows) lives in SupplierRepository.Search().
        public List<Supplier> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Supplier> { this };

            return _supplierName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ? new List<Supplier> { this }
                : new List<Supplier>();
        }

        public override string ToString() => GetDisplayName();
    }

    // ───────────────────────────────────────────────────────────
    // SUPPLIER REPOSITORY
    // ───────────────────────────────────────────────────────────

    /// <summary>
    /// Handles all SQL for the Suppliers table.
    /// Extends GenericRepository and overrides the methods that need
    /// custom SQL because the DB column is "SupplierId" not "Id".
    /// Also implements ISearchable for full-text supplier lookup.
    /// </summary>
    public class SupplierRepository : GenericRepository<Supplier>, ISearchable<Supplier>
    {
        // Tell GenericRepository: table = "Suppliers", PK column = "SupplierId"
        public SupplierRepository() : base("Suppliers", "SupplierId") { }

        // ── Override: manual column mapping ─────────────────────
        // GenericRepository would look for an "Id" column; we redirect to "SupplierId".
        protected override Supplier MapDataRowToEntity(DataRow row)
        {
            return new Supplier
            {
                Id           = Convert.ToInt32(row["SupplierId"]),
                SupplierName = row["SupplierName"].ToString(),
                Phone        = row["Phone"].ToString(),
                Address      = row["Address"].ToString()
            };
        }

        // Override GetById: WHERE clause must use "SupplierId".
        public override Supplier GetById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            string query = "SELECT * FROM Suppliers WHERE SupplierId = @Id";
            DataTable dt = DatabaseHelper.ExecuteQuery(query,
                new SqlParameter[] { new SqlParameter("@Id", id) });

            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
        }

        // Override Add: explicit INSERT; returns new auto-increment Id via SCOPE_IDENTITY().
        public override void Add(Supplier entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string query = @"
                INSERT INTO Suppliers (SupplierName, Phone, Address)
                VALUES (@SupplierName, @Phone, @Address);
                SELECT SCOPE_IDENTITY();";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@SupplierName", entity.SupplierName),
                new SqlParameter("@Phone",        (object)entity.Phone   ?? DBNull.Value),
                new SqlParameter("@Address",      (object)entity.Address ?? DBNull.Value)
            };

            object result = DatabaseHelper.ExecuteScalar(query, parameters);
            entity.Id = Convert.ToInt32(result);   // write the DB-generated Id back onto the object
        }

        // Override Update: targets SupplierId in the WHERE clause.
        public override void Update(Supplier entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string query = @"
                UPDATE Suppliers
                SET    SupplierName = @SupplierName,
                       Phone        = @Phone,
                       Address      = @Address
                WHERE  SupplierId = @SupplierId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@SupplierName", entity.SupplierName),
                new SqlParameter("@Phone",        (object)entity.Phone   ?? DBNull.Value),
                new SqlParameter("@Address",      (object)entity.Address ?? DBNull.Value),
                new SqlParameter("@SupplierId",   entity.Id)
            };

            DatabaseHelper.ExecuteNonQuery(query, parameters);
        }

        // Override Delete: targets SupplierId.
        public override void Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            DatabaseHelper.ExecuteNonQuery(
                "DELETE FROM Suppliers WHERE SupplierId = @Id",
                new SqlParameter[] { new SqlParameter("@Id", id) });
        }

        // ── ISearchable<Supplier> ────────────────────────────────
        // SQL LIKE search across name and phone.
        public List<Supplier> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Supplier>();

            string query = @"
                SELECT * FROM Suppliers
                WHERE  SupplierName LIKE @SearchTerm
                OR     Phone        LIKE @SearchTerm";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@SearchTerm", $"%{searchTerm}%")
            };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
            var suppliers = new List<Supplier>();
            foreach (DataRow row in dt.Rows)
                suppliers.Add(MapDataRowToEntity(row));

            return suppliers;
        }

        // ── Additional queries ───────────────────────────────────

        // Useful when you need to confirm a supplier exists by name (e.g. deduplication).
        public Supplier GetSupplierByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty");

            string query = "SELECT * FROM Suppliers WHERE SupplierName = @Name";
            DataTable dt = DatabaseHelper.ExecuteQuery(query,
                new SqlParameter[] { new SqlParameter("@Name", name.Trim()) });

            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
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
            DataTable dt = DatabaseHelper.ExecuteQuery($"SELECT * FROM {_tableName}");
            foreach (DataRow row in dt.Rows)
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
