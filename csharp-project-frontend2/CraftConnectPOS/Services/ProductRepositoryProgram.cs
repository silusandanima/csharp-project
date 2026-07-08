using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace CraftConnectPOS.Data
{
    // 1. Entity Base class
    public abstract class EntityBase
    {
        public int Id { get; set; }
    }

    // 2. IRepository interface
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

    // 3. ISearchable interface
    public interface ISearchable<T>
    {
        List<T> Search(string searchTerm);
    }

    // 4. GenericRepository class
    public class GenericRepository<T> : IRepository<T> where T : EntityBase, new()
    {
        protected readonly string _tableName;
        protected readonly string _idColumnName;

        public GenericRepository(string tableName, string idColumnName = "Id")
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
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
            string values = string.Join(", ", props.Select(p => $"@{p.Name}"));
            string query = $"INSERT INTO {_tableName} ({columns}) VALUES ({values}); SELECT SCOPE_IDENTITY();";
            var parameters = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(entity) ?? DBNull.Value)).ToArray();
            object result = DatabaseHelper.ExecuteScalar(query, parameters);
            (typeof(T).GetProperty(_idColumnName) ?? typeof(T).GetProperty("Id"))?.SetValue(entity, Convert.ToInt32(result));
        }

        public virtual void Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var props = typeof(T).GetProperties().Where(p => p.Name != _idColumnName && p.Name != "Id").ToList();
            string setClause = string.Join(", ", props.Select(p => $"{p.Name} = @{p.Name}"));
            string query = $"UPDATE {_tableName} SET {setClause} WHERE {_idColumnName} = @Id";
            var parameters = props.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(entity) ?? DBNull.Value)).ToList();
            var idProperty = typeof(T).GetProperty(_idColumnName) ?? typeof(T).GetProperty("Id");
            parameters.Add(new SqlParameter("@Id", idProperty?.GetValue(entity) ?? 0));
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

    // 5. DatabaseHelper class
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


        // Temporarily commented

        //public static void InitializeFromConfig()
        //{
        //    string cs = ConfigurationManager.ConnectionStrings["CraftConnect"]?.ConnectionString;
        //    if (string.IsNullOrWhiteSpace(cs))
        //        throw new InvalidOperationException("Connection string 'CraftConnect' not found in config");
        //    Initialize(cs);
        //}

        public static string GetConnectionString()
        {
            EnsureInitialized();
            return _connectionString;
        }


        // Temporarily Commented

        //private static void EnsureInitialized()
        //{
        //    if (!_isInitialized)
        //        InitializeFromConfig();
        //}

        // Temporarily Added.

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Database not initialized.");
        }


        private static SqlConnection CreateConnection()
        {
            EnsureInitialized();
            return new SqlConnection(_connectionString);
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query cannot be empty");
            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand(query, conn))
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
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }
    }

    // 6. Product class
    public class Product : EntityBase
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        // public string Category { get; set; } // Commented out - not in database
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }

    // 7. ProductRepository class
    public class ProductRepository : GenericRepository<Product>, ISearchable<Product>
    {
        public ProductRepository() : base("Products", "ProductId") // Specify the correct ID column name
        {
        }

        protected override Product MapDataRowToEntity(DataRow row)
        {
            return new Product
            {
                Id = Convert.ToInt32(row["ProductId"]),
                ProductName = row["ProductName"].ToString(),
                Description = row["Description"].ToString(),
                // Category = row["Category"].ToString(), // Commented - not in database
                Price = Convert.ToDecimal(row["Price"]),
                StockQuantity = Convert.ToInt32(row["StockQuantity"])
            };
        }

        public override Product GetById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            string query = "SELECT * FROM Products WHERE ProductId = @Id";
            var parameters = new SqlParameter[] { new SqlParameter("@Id", id) };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
        }

        public override void Add(Product entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string query = @"
                INSERT INTO Products (ProductName, Description, Price, StockQuantity)  
                VALUES (@ProductName, @Description, @Price, @StockQuantity);
                SELECT SCOPE_IDENTITY();";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@ProductName", entity.ProductName),
                new SqlParameter("@Description", (object)entity.Description ?? DBNull.Value),
                new SqlParameter("@Price", entity.Price),
                new SqlParameter("@StockQuantity", entity.StockQuantity)
            };

            object result = DatabaseHelper.ExecuteScalar(query, parameters);
            entity.Id = Convert.ToInt32(result);
        }

        public override void Update(Product entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string query = @"
                UPDATE Products
                SET    ProductName   = @ProductName,
                       Description   = @Description,
                       Price         = @Price,
                       StockQuantity = @StockQuantity
                WHERE  ProductId = @ProductId";

            var parameters = new SqlParameter[]
            {
                new SqlParameter("@ProductName", entity.ProductName),
                new SqlParameter("@Description", (object)entity.Description ?? DBNull.Value),
                new SqlParameter("@Price", entity.Price),
                new SqlParameter("@StockQuantity", entity.StockQuantity),
                new SqlParameter("@ProductId", entity.Id)
            };

            DatabaseHelper.ExecuteNonQuery(query, parameters);
        }

        public override void Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            string query = "DELETE FROM Products WHERE ProductId = @Id";
            DatabaseHelper.ExecuteNonQuery(query, new SqlParameter[] { new SqlParameter("@Id", id) });
        }

        public List<Product> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Product>();

            string query = @"
                SELECT * FROM Products
                WHERE  ProductName  LIKE @SearchTerm
                OR     Description  LIKE @SearchTerm";

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

    // 8. Program
    //internal class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        // Initialize DatabaseHelper
    //        // DatabaseHelper.Initialize("your_connection_string_here");
    //        // OR from config:
    //        // DatabaseHelper.InitializeFromConfig();

    //        // Example usage:
    //        // var productRepo = new ProductRepository();
    //        // var allProducts = productRepo.GetAll();
    //        // var product = productRepo.GetById(1);
    //        // var inStock = productRepo.GetProductsInStock();
    //        // var searchResults = productRepo.Search("laptop");
    //    }
    //}
}