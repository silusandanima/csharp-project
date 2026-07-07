using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Data
{
    public class Supplier : EntityBase
    {
        public string SupplierName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        public Supplier()
        {
            SupplierName = string.Empty;
            Phone = string.Empty;
            Address = string.Empty;
        }
    }

    public class SupplierRepository : GenericRepository<Supplier>
    {
        public SupplierRepository()
            : base("Suppliers", "SupplierID")
        {
        }

        protected override Supplier MapDataRowToEntity(DataRow row)
        {
            return new Supplier
            {
                Id = Convert.ToInt32(row["SupplierID"]),
                SupplierName = row["SupplierName"]?.ToString() ?? string.Empty,
                Phone = row["Phone"]?.ToString() ?? string.Empty,
                Address = row["Address"]?.ToString() ?? string.Empty
            };
        }

        public override Supplier GetById(int id)
        {
            const string query = @"
                SELECT *
                FROM Suppliers
                WHERE SupplierID = @SupplierID;";

            DataTable table = DatabaseHelper.ExecuteQuery(
                query,
                new[]
                {
                    new SqliteParameter("@SupplierID", id)
                });

            return table.Rows.Count == 0
                ? null
                : MapDataRowToEntity(table.Rows[0]);
        }

        public override void Add(Supplier supplier)
        {
            const string query = @"
                INSERT INTO Suppliers
                    (SupplierName, Phone, Address)
                VALUES
                    (@SupplierName, @Phone, @Address);

                SELECT last_insert_rowid();";

            object result = DatabaseHelper.ExecuteScalar(
                query,
                new[]
                {
                    new SqliteParameter("@SupplierName", supplier.SupplierName),
                    new SqliteParameter("@Phone", supplier.Phone ?? string.Empty),
                    new SqliteParameter("@Address", supplier.Address ?? string.Empty)
                });

            supplier.Id = Convert.ToInt32(result);
        }

        public override void Update(Supplier supplier)
        {
            const string query = @"
                UPDATE Suppliers
                SET
                    SupplierName = @SupplierName,
                    Phone = @Phone,
                    Address = @Address
                WHERE SupplierID = @SupplierID;";

            int affectedRows = DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqliteParameter("@SupplierName", supplier.SupplierName),
                    new SqliteParameter("@Phone", supplier.Phone ?? string.Empty),
                    new SqliteParameter("@Address", supplier.Address ?? string.Empty),
                    new SqliteParameter("@SupplierID", supplier.Id)
                });

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "The supplier no longer exists.");
            }
        }

        public override void Delete(int id)
        {
            const string query = @"
                DELETE FROM Suppliers
                WHERE SupplierID = @SupplierID;";

            int affectedRows = DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqliteParameter("@SupplierID", id)
                });

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "The supplier no longer exists.");
            }
        }
    }
}
