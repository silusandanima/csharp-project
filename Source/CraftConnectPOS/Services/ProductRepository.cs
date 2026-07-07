using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Data
{
    public class Product : EntityBase
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }

    public class ProductRepository : GenericRepository<Product>, ISearchable<Product>
    {
        public ProductRepository()
            : base("Products", "ProductId")
        {
        }

        protected override Product MapDataRowToEntity(DataRow row)
        {
            return new Product
            {
                Id = Convert.ToInt32(row["ProductId"]),
                ProductName = row["ProductName"].ToString(),
                Description = row["Description"] == DBNull.Value
                    ? string.Empty
                    : row["Description"].ToString(),
                Category = row["Category"] == DBNull.Value
                    ? string.Empty
                    : row["Category"].ToString(),
                Price = row["Price"] == DBNull.Value
                    ? 0m
                    : Convert.ToDecimal(row["Price"]),
                StockQuantity = row["StockQuantity"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(row["StockQuantity"])
            };
        }

        public override Product GetById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            string query = "SELECT * FROM Products WHERE ProductId = @Id";
            var parameters = new SqliteParameter[] { new SqliteParameter("@Id", id) };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
            return dt.Rows.Count == 0 ? null : MapDataRowToEntity(dt.Rows[0]);
        }

        public override void Add(Product entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            string query = @"
                INSERT INTO Products
                    (ProductName, Description, Category, Price, StockQuantity)
                VALUES
                    (@ProductName, @Description, @Category, @Price, @StockQuantity);
                SELECT last_insert_rowid();";

            var parameters = new SqliteParameter[]
            {
                new SqliteParameter("@ProductName", entity.ProductName),
                new SqliteParameter("@Description", (object)entity.Description ?? DBNull.Value),
                new SqliteParameter("@Category", (object)entity.Category ?? string.Empty),
                new SqliteParameter("@Price", entity.Price),
                new SqliteParameter("@StockQuantity", entity.StockQuantity)
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
                       Category      = @Category,
                       Price         = @Price,
                       StockQuantity = @StockQuantity
                WHERE  ProductId = @ProductId";

            var parameters = new SqliteParameter[]
            {
                new SqliteParameter("@ProductName", entity.ProductName),
                new SqliteParameter("@Description", (object)entity.Description ?? DBNull.Value),
                new SqliteParameter("@Category", (object)entity.Category ?? string.Empty),
                new SqliteParameter("@Price", entity.Price),
                new SqliteParameter("@StockQuantity", entity.StockQuantity),
                new SqliteParameter("@ProductId", entity.Id)
            };

            if (DatabaseHelper.ExecuteNonQuery(query, parameters) == 0)
            {
                throw new InvalidOperationException(
                    "The product no longer exists.");
            }
        }

        public override void Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID must be greater than 0");

            string query = "DELETE FROM Products WHERE ProductId = @Id";
            if (DatabaseHelper.ExecuteNonQuery(
                    query,
                    new[] { new SqliteParameter("@Id", id) }) == 0)
            {
                throw new InvalidOperationException(
                    "The product no longer exists.");
            }
        }

        public List<Product> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Product>();

            string query = @"
                SELECT * FROM Products
                WHERE  ProductName  LIKE @SearchTerm
                OR     Description  LIKE @SearchTerm";

            var parameters = new SqliteParameter[]
            {
                new SqliteParameter("@SearchTerm", $"%{searchTerm}%")
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
}
