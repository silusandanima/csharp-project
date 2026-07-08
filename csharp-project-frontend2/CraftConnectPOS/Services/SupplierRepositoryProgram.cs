using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CraftConnectPOS.Data
{
    // This file uses EntityBase, GenericRepository and DatabaseHelper
    // already present in your working ProductRepositoryProgram.cs file.

    public class Supplier : EntityBase
    {
        public new int Id
        {
            get => base.Id;
            set => base.Id = value;
        }

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
            : base("dbo.Suppliers", "SupplierID")
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
                FROM dbo.Suppliers
                WHERE SupplierID = @SupplierID;";

            DataTable table = DatabaseHelper.ExecuteQuery(
                query,
                new[]
                {
                    new SqlParameter("@SupplierID", id)
                });

            return table.Rows.Count == 0
                ? null
                : MapDataRowToEntity(table.Rows[0]);
        }

        public override void Add(Supplier supplier)
        {
            const string query = @"
                INSERT INTO dbo.Suppliers
                    (SupplierName, Phone, Address)
                VALUES
                    (@SupplierName, @Phone, @Address);

                SELECT SCOPE_IDENTITY();";

            object result = DatabaseHelper.ExecuteScalar(
                query,
                new[]
                {
                    new SqlParameter("@SupplierName", supplier.SupplierName),
                    new SqlParameter("@Phone", supplier.Phone ?? string.Empty),
                    new SqlParameter("@Address", supplier.Address ?? string.Empty)
                });

            supplier.Id = Convert.ToInt32(result);
        }

        public override void Update(Supplier supplier)
        {
            const string query = @"
                UPDATE dbo.Suppliers
                SET
                    SupplierName = @SupplierName,
                    Phone = @Phone,
                    Address = @Address
                WHERE SupplierID = @SupplierID;";

            DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqlParameter("@SupplierName", supplier.SupplierName),
                    new SqlParameter("@Phone", supplier.Phone ?? string.Empty),
                    new SqlParameter("@Address", supplier.Address ?? string.Empty),
                    new SqlParameter("@SupplierID", supplier.Id)
                });
        }

        public override void Delete(int id)
        {
            const string query = @"
                DELETE FROM dbo.Suppliers
                WHERE SupplierID = @SupplierID;";

            DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqlParameter("@SupplierID", id)
                });
        }
    }
}