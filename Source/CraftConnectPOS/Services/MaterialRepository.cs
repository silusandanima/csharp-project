using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Data
{
    public class Material : EntityBase
    {
        public string MaterialName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public int ReorderLevel { get; set; }
        public int SupplierId { get; set; }

        public Material()
        {
            MaterialName = string.Empty;
            Unit = string.Empty;
        }
    }

    public class MaterialRepository : GenericRepository<Material>
    {
        public MaterialRepository()
            : base("Materials", "MaterialID")
        {
        }

        protected override Material MapDataRowToEntity(DataRow row)
        {
            return new Material
            {
                Id = Convert.ToInt32(row["MaterialID"]),

                MaterialName = row["MaterialName"] == DBNull.Value
                    ? string.Empty
                    : row["MaterialName"].ToString(),

                Quantity = row["Quantity"] == DBNull.Value
                    ? 0m
                    : Convert.ToDecimal(row["Quantity"]),

                Unit = row["Unit"] == DBNull.Value
                    ? string.Empty
                    : row["Unit"].ToString(),

                ReorderLevel = row["ReorderLevel"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(row["ReorderLevel"]),

                SupplierId = row["SupplierID"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(row["SupplierID"])
            };
        }

        public override void Add(Material material)
        {
            const string query = @"
                INSERT INTO Materials
                    (MaterialName, Quantity, Unit, ReorderLevel, SupplierID)
                VALUES
                    (@MaterialName, @Quantity, @Unit, @ReorderLevel, @SupplierID);

                SELECT last_insert_rowid();";

            object result = DatabaseHelper.ExecuteScalar(
                query,
                new[]
                {
                    new SqliteParameter("@MaterialName", material.MaterialName),
                    new SqliteParameter("@Quantity", material.Quantity),
                    new SqliteParameter("@Unit", material.Unit),
                    new SqliteParameter("@ReorderLevel", material.ReorderLevel),
                    new SqliteParameter("@SupplierID", material.SupplierId)
                });

            material.Id = Convert.ToInt32(result);
        }

        public override void Update(Material material)
        {
            const string query = @"
                UPDATE Materials
                SET
                    MaterialName = @MaterialName,
                    Quantity = @Quantity,
                    Unit = @Unit,
                    ReorderLevel = @ReorderLevel,
                    SupplierID = @SupplierID
                WHERE MaterialID = @MaterialID;";

            int affectedRows = DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqliteParameter("@MaterialName", material.MaterialName),
                    new SqliteParameter("@Quantity", material.Quantity),
                    new SqliteParameter("@Unit", material.Unit),
                    new SqliteParameter("@ReorderLevel", material.ReorderLevel),
                    new SqliteParameter("@SupplierID", material.SupplierId),
                    new SqliteParameter("@MaterialID", material.Id)
                });

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "The material no longer exists.");
            }
        }

        public override void Delete(int id)
        {
            const string query = @"
                DELETE FROM Materials
                WHERE MaterialID = @MaterialID;";

            int affectedRows = DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqliteParameter("@MaterialID", id)
                });

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "The material no longer exists.");
            }
        }
    }
}
